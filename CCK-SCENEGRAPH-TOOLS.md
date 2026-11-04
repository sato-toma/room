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

PowerShellから実行します。対象シーンは既定で `Assets/room.unity` です。別のシーンを解析する場合は `-ScenePath` で指定します。

```powershell
.\scripts\analyze-scenegraph.ps1 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe"
```

シーンのComponent情報を `SceneGraph.generated.toml` に出力し、そのシーンに割り当てられたClusterScriptを解析します。オプショナルな `getUnityComponent` lookup の不足も厳密に扱う場合は、`-StrictOptionalLookups` を追加してください。

```powershell
.\scripts\analyze-scenegraph.ps1 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe" `
  -StrictOptionalLookups
```

`SceneGraph.generated.toml` は生成物としてGit管理対象外です。Unityのexportが失敗した場合は `Library/SceneGraphExporter.log` を確認してください。

解析はSceneGraphとスクリプトを使った静的な確認です。UnityやCluster上での実行時動作を保証するものではなく、`unverified` や `ambiguous` などの結果は手動確認が必要です。

## Spinサンプル

`Assets/Project/Script/src/` にSpinStart、SpinStop、PlayerScriptのTypeScriptソースがあります。Node.js 26.xを使い、リポジトリルートで依存関係を復元してClusterScript用JavaScriptを生成します。ClusterScriptの型定義はビルド時に公式ドキュメントから `node_modules/@types/cluster-script/` へ取得するため、`Assets`にもGit管理対象にも含まれず、Unityの`.meta`も生成されません。取得にはネットワーク接続が必要です。

```powershell
npm ci
npm run build:clusterscripts
```

生成先は `Assets/Project/Script/dist/` です。生成JavaScriptはGit管理対象外ですが、Unityの`.meta`ファイルは管理します。生成したJavaScriptは `Assets/room.unity` のSpinStart/SpinStopオブジェクトに割り当て済みです。ソースを変更した場合は解析やUnityでの確認の前に再ビルドしてください。シーンには移動用の床とSpawn Pointも含まれます。

SpinStartには `MovableItem` も設定しています。PlayerScriptの `setPosition`／`setRotation` を現行解析器がアイテム移動APIとして検査するためです。参照元サンプルの構成にはない、静的解析を満たすための追加コンポーネントです。
