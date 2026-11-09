# Cluster Creator Kit テンプレートプロジェクト

[cluster](https://cluster.mu/) にゲームワールドを制作する際のテンプレートとなる Unity プロジェクトです。

## 要件

### Unity 6000.2.6f2

環境に応じて以下の追加モジュールが必要です。

#### Windows の場合
- Android Build Support
- iOS Build Support
- Mac Build Support (Mono)

#### macOS の場合
- Android Build Support
- iOS Build Support
- Windows Build Support (Mono)

### Cluster Creator Kit v3.0.0 以上
プロジェクトに自動で導入されます。
起動時にエラーが出る場合や Unity のメニューに「Cluster」の項目が追加されていない場合は [Creator Kitの導入](https://clustervr.gitbook.io/creatorkit/installation/install-creatorkit) を参考に Creator Kit の手動での導入をお試しください。

## Blender MCP

ワークスペースのMCP設定から、Node.js製MCPサーバー経由でBlenderの基本形状を作成し、`.blend`と`.fbx`を書き出せます。Node.jsとBlenderが必要です。BlenderのPython APIはBlender同梱のPythonで実行するため、別途Pythonを導入する必要はありません。

依存関係を復元する場合は、このフォルダーで `npm ci` を実行してください。Blenderが標準のインストール場所にない場合は、`BLENDER_EXECUTABLE` に実行ファイルのパスを設定してください。出力先は `Assets/Generated/Blender` で、変更する場合は `BLENDER_MCP_OUTPUT_DIR` を設定します。

`create_fbx` ツールは、立方体・球・正二十面体球・円柱・円錐・トーラス・平面を作成できます。各オブジェクトに位置、回転、スケール、基本色、メタリック、粗さを指定できます。同名の出力ファイルは上書きしません。

FBX出力は[Cluster公式のBlender→Unity設定](https://creator.cluster.mu/2022/07/26/blender-unity/)を参考にし、Metric単位、回転・スケールの適用、FBX Unit Scale、Forward `-Z` / Up `Y`、Apply Transform、テクスチャのCopy/Embedを設定しています。現在のツールは画像Textureを入力できないため、Textureの埋め込みは未検証です。設定と制約の詳細は[Blender Skill](.agents/skills/blender-modeling/SKILL.md)を参照してください。

## テンプレートの一覧

### ミュージック
`Assets/ClusterCreatorKitTemplate/Music/Scenes/Music.unity`

音楽に合わせて動く様々なエフェクトが詰まったテンプレートです。

### アスレチック
`Assets/ClusterCreatorKitTemplate/Playground/Scenes/Playground.unity`

さまざまなアトラクションを作るのに便利な仕掛けがたくさん詰め込まれたテンプレートです。

### プログレッション
`Assets/ClusterCreatorKitTemplate/Progression/Scenes/Progression.unity`

「鉱石を集めてレベルを上げて、貯まったお金で強いアビリティを買って、もっと鉱石を集める」ゲームのテンプレートです。
プレイヤーの経験値とレベル、鉱石の所持個数、プレイヤーアビリティの効果と購入・付け外しなどを実装しています。

### シューター
`Assets/ClusterCreatorKitTemplate/Shooter/Scenes/Shooter.unity`

銃で的を撃って破壊できるテンプレートです。
銃にはハンドガン、マシンガン、チャージガンの3種類があります。

### 乗り物
`Assets/ClusterCreatorKitTemplate/Vehicle/Scenes/Vehicle.unity`

ベーシックな乗り物のテンプレートです。
ゴーカート、ヘリコプター、ポニーの3種類があります。観戦席にも座れます。

### フィッシング
`Assets/ClusterCreatorKitTemplate/Fishing/Scenes/Fishing.unity`

ランダムに釣れる魚を集め、図鑑を埋めていく釣りゲームです。
完成品のPrefabが付属しているので、既存のワールドにも簡単に組み込むことができます。

### Minimal Sample
`Assets/ClusterCreatorKitTemplate/Minimal/Scenes/MinimalSample.unity`

ワールドの必須コンポーネントだけを設置したシンプルなシーンです。
Cluster Creator KitサンプルプロジェクトのMinimal Sampleと同じものです。

## ミニテンプレート
既存のワールドに組み込むことを目的とした、パーツ単位のテンプレートです。

### カプセルトイ
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_CapsuleBasket.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_Machine.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_Machine_Lite.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_MachineRetro.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_MachineRetro_Lite.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_Machines_2x4.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_Machines_Stand.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_ResetButton.prefab`
`Assets/ClusterCreatorKitTemplate/GameCenter/Prefabs/CT_Stand.prefab`

使用すると景品の入ったカプセルを排出するギミックです。
そのまま置くだけでなく、手軽にカプセルの中身を入れ替えることもできます。

## ライセンス

本プロジェクトは [Creative Commons Attribution 4.0 International](https://creativecommons.org/licenses/by/4.0/) で提供されています。
本プロジェクトの全部もしくは一部を使用して作成されたコンテンツを [cluster](https://cluster.mu/) で公開する場合は Cluster, Inc. のクレジット表示は不要です。

-------------------------

### 個別アセット: BGM

`https://bgmer.net/wp-content/uploads/2023/06/373_BPM125.mp3` のBGMは、本プロジェクトのライセンス対象外です。`npm run fetch:bgm` で `Assets/Project/BGM/373_BPM125.mp3` に取得できます。この音源およびUnityが生成するメタデータはGit管理されず、再配布しないでください。利用にあたっては配布元の利用条件も確認してください。
