# CCK SceneGraph Tools

このUnityプロジェクトでは、[cck-scenegraph-tools](https://github.com/sato-toma/cck-scenegraph-tools)を使って、Unityシーンと割り当て済みClusterScriptの構成を確認できます。

## Copilot Agent skill

`cluster-script-authoring` skill を [`.agents/skills/cluster-script-authoring/`](.agents/skills/cluster-script-authoring/) に導入しています。ClusterScriptの作成、APIに必要なUnity Componentの確認、シーン構成チェックのガイドとチェックリストを提供します。

このskillは解析器本体を含みません。サンプルコードやツールの詳細は[ツールリポジトリ](https://github.com/sato-toma/cck-scenegraph-tools)を参照してください。

## SceneGraph解析

Unity Exporterは `Packages/manifest.json` から導入されます。解析器は初回実行時に `Library/cck-scenegraph-tools` へ取得・ビルドされます。サンプルUnityプロジェクトなど解析に不要な内容まで複製しないよう、リポジトリ全体をサブモジュールにはせず、解析器パッケージだけを疎なcheckoutで取得します。Unity packageとNode.js解析器は同じコミットに固定しています。

### 必要環境

- Node.js 26.x
- Unity Editor 6000.2.6f2

### 実行

PowerShellから対象シーンとUnity Editorの実行ファイルを指定します。

```powershell
.\scripts\analyze-scenegraph.ps1 `
  -ScenePath "Assets\room.unity" `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe"
```

シーンのComponent情報を `SceneGraph.generated.toml` に出力し、そのシーンに割り当てられたClusterScriptを解析します。オプショナルな `getUnityComponent` lookup の不足も厳密に扱う場合は、`-StrictOptionalLookups` を追加してください。

```powershell
.\scripts\analyze-scenegraph.ps1 `
  -ScenePath "Assets\room.unity" `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe" `
  -StrictOptionalLookups
```

`SceneGraph.generated.toml` は生成物としてGit管理対象外です。Unityのexportが失敗した場合は `Library/SceneGraphExporter.log` を確認してください。

解析はSceneGraphとスクリプトを使った静的な確認です。UnityやCluster上での実行時動作を保証するものではなく、`unverified` や `ambiguous` などの結果は手動確認が必要です。
