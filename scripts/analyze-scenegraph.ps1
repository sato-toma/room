[CmdletBinding()]
param(
  [string]$ScenePath = "Assets/room.unity",
  [string]$UnityPath = $env:UNITY_PATH,
  [switch]$StrictOptionalLookups
)

$ErrorActionPreference = "Stop"

function Invoke-CheckedCommand {
  param(
    [string]$Command,
    [string[]]$Arguments,
    [string]$Purpose
  )

  & $Command @Arguments
  if ($LASTEXITCODE -ne 0) {
    throw "$Purpose failed with exit code $LASTEXITCODE."
  }
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($UnityPath)) {
  throw "Pass -UnityPath or set the UNITY_PATH environment variable."
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
  throw "Unity Editor executable not found: $UnityPath"
}

$sceneFullPath = if ([IO.Path]::IsPathRooted($ScenePath)) {
  (Resolve-Path -LiteralPath $ScenePath).Path
} else {
  (Resolve-Path -LiteralPath (Join-Path $projectRoot $ScenePath)).Path
}
$sceneArgument = if ([IO.Path]::IsPathRooted($ScenePath)) {
  $sceneFullPath
} else {
  $ScenePath.Replace("\", "/")
}
$unityExecutable = (Resolve-Path -LiteralPath $UnityPath).Path
$toolCommit = "39fb5d7f0a50ef320ea8cec52c002048a0199885"
$toolRepository = "https://github.com/sato-toma/cck-scenegraph-tools.git"
$toolRoot = Join-Path $projectRoot "Library\cck-scenegraph-tools"
$analyzerRoot = Join-Path $toolRoot "packages\ts-analyzer"
$analyzerCli = Join-Path $analyzerRoot "dist\cli.js"
$buildStamp = Join-Path $toolRoot ".room-analyzer-build"
$sceneGraphPath = Join-Path $projectRoot "SceneGraph.generated.toml"
$unityLogPath = Join-Path $projectRoot "Library\SceneGraphExporter.log"

if (-not (Test-Path -LiteralPath $sceneFullPath -PathType Leaf)) {
  throw "Unity scene not found: $sceneFullPath"
}

$nodeVersion = (& node --version).Trim()
if ($LASTEXITCODE -ne 0) {
  throw "Could not determine the Node.js version."
}
if ($nodeVersion -notmatch "^v26\.") {
  throw "Node.js 26.x is required by cck-scenegraph-tools; found $nodeVersion."
}

if (-not (Test-Path -LiteralPath (Join-Path $toolRoot ".git"))) {
  if (Test-Path -LiteralPath $toolRoot) {
    throw "Analyzer cache exists but is not a Git checkout: $toolRoot"
  }
  New-Item -ItemType Directory -Path (Split-Path $toolRoot -Parent) -Force | Out-Null
  Invoke-CheckedCommand "git" @(
    "clone",
    "--filter=blob:none",
    "--no-checkout",
    $toolRepository,
    $toolRoot
  ) "Cloning cck-scenegraph-tools"
}

$remoteUrl = (& git -C $toolRoot remote get-url origin).Trim()
if ($LASTEXITCODE -ne 0 -or $remoteUrl -ne $toolRepository) {
  throw "Analyzer cache has an unexpected origin: $toolRoot"
}

Invoke-CheckedCommand "git" @("-C", $toolRoot, "sparse-checkout", "init", "--cone") "Configuring analyzer sparse checkout"
Invoke-CheckedCommand "git" @("-C", $toolRoot, "sparse-checkout", "set", "packages/ts-analyzer") "Selecting analyzer package"

$cachedCommit = (& git -C $toolRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
  throw "Could not read the analyzer cache revision."
}
if ($cachedCommit -ne $toolCommit) {
  Invoke-CheckedCommand "git" @("-C", $toolRoot, "fetch", "--depth", "1", "origin", $toolCommit) "Fetching pinned analyzer revision"
}
Invoke-CheckedCommand "git" @("-C", $toolRoot, "checkout", "--detach", $toolCommit) "Checking out pinned analyzer revision"

if (-not (Test-Path -LiteralPath (Join-Path $analyzerRoot "package.json"))) {
  throw "Analyzer package was not found in the checkout: $analyzerRoot"
}

$builtCommit = if (Test-Path -LiteralPath $buildStamp) {
  (Get-Content -LiteralPath $buildStamp -Raw).Trim()
} else {
  ""
}
if ($builtCommit -ne $toolCommit -or
    -not (Test-Path -LiteralPath (Join-Path $analyzerRoot "node_modules")) -or
    -not (Test-Path -LiteralPath $analyzerCli)) {
  Push-Location $analyzerRoot
  try {
    Invoke-CheckedCommand "npm" @("ci") "Installing analyzer dependencies"
    Invoke-CheckedCommand "npm" @("run", "build") "Building the analyzer"
  } finally {
    Pop-Location
  }
  Set-Content -LiteralPath $buildStamp -Value $toolCommit -Encoding Ascii
}

if (Test-Path -LiteralPath $sceneGraphPath) {
  Remove-Item -LiteralPath $sceneGraphPath
}

$unityArguments = @(
  "-batchmode",
  "-quit",
  "-projectPath",
  $projectRoot,
  "-executeMethod",
  "UnitySemanticGraph.Editor.Exporter.SelectiveComponentTomlExporter.ExportGraphToToml",
  "-scenePath",
  $sceneArgument,
  "-outputPath",
  $sceneGraphPath,
  "-logFile",
  $unityLogPath
)
$quotedUnityArguments = $unityArguments | ForEach-Object {
  '"' + $_.Replace('"', '\"') + '"'
}
$unityProcess = Start-Process `
  -FilePath $unityExecutable `
  -ArgumentList ($quotedUnityArguments -join " ") `
  -PassThru `
  -Wait
if ($unityProcess.ExitCode -ne 0) {
  throw "Exporting Unity scene failed with exit code $($unityProcess.ExitCode)."
}

if (-not (Test-Path -LiteralPath $sceneGraphPath -PathType Leaf) -or
    (Get-Item -LiteralPath $sceneGraphPath).Length -eq 0) {
  throw "Unity did not create a non-empty SceneGraph file: $sceneGraphPath"
}

$analyzerArguments = @($analyzerCli, "analyze", $sceneGraphPath, "--format", "text")
if ($StrictOptionalLookups) {
  $analyzerArguments += "--strict-optional-lookups"
}
Invoke-CheckedCommand "node" $analyzerArguments "Analyzing exported scene"
