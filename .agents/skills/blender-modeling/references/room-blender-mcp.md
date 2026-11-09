# room Blender MCP仕様

この資料は、roomプロジェクトの`.mcp.json`から起動される`room/scripts/blender-mcp.mjs`の仕様を記録する。room環境で確認した内容（2026-11-09）であり、別のプロジェクトやMCP実装にはそのまま適用しない。

出力設定の動作はBlender 5.2.3 LTSでテストした。回転・非一様スケール・位置を指定したCubeから`.blend`と`.fbx`が生成され、保存したSceneがMetric単位、回転とスケール適用済み、位置保持であることを確認した。Unityへのインポート結果はこのテストでは確認していない。

## ツールの機能

- MCPツール名: `create_fbx`
- 既存Sceneを編集せず、Blenderの空のFactory Sceneから新しいSceneを作る。
- 対象は基本形状Meshのみ: `cube`、`sphere`、`ico_sphere`、`cylinder`、`cone`、`torus`、`plane`
- 一度の呼び出しで1〜200個のObjectを作成する。
- Objectごとに名前、形状、位置、回転、スケール、任意の基本Materialを指定できる。
- 成功すると`.blend`と`.fbx`の両方を出力し、実際のパスとBlenderバージョンを返す。

任意のメッシュ形状、既存Objectの変更、UV編集、レンダリング、任意のBlender Python実行はこのツールではできない。必要な場合は、別の利用可能な手段が確認できない限り未対応として伝える。

## 入力制約

- `outputName`: 1〜64文字。英数字で始まり、英数字・`_`・`-`を使用する。
- Object名: 1〜64文字。英数字で始まり、英数字・空白・`_`・`-`を使用する。同じ呼び出し内で大文字小文字を区別しない重複名は不可。
- 位置と回転角度: 各軸`-10000`〜`10000`。回転の単位は度。
- スケール: 各軸とも`0`より大きく`10000`以下。
- Materialの色、Metallic、Roughness: それぞれ`0`〜`1`。色はRGB。
- Materialを指定しない場合、ObjectにMaterialは明示追加されない。

## 出力先と上書き

- 既定の出力先はroomプロジェクトの`Assets/Generated/Blender`。
- `BLENDER_MCP_OUTPUT_DIR`が設定されている場合はその出力先を使う。
- `.blend`と`.fbx`のいずれかが既に存在する`outputName`は失敗する。上書きせず、別の一意な名前を選ぶ。
- Unityが生成する`.meta`ファイルはMCP出力ではない。手作業で作成・変更せず、Unity側の管理に任せる。

## FBX出力

BlenderからUnity向けに出力する設定は、[Cluster公式記事「BlenderでUnity向けにモデルを出力する時のおすすめ設定」](https://creator.cluster.mu/2022/07/26/blender-unity/)を参考にする。記事は2022年公開のため、画面上の項目名はBlenderのバージョンで異なる場合がある。現在のAPI上の設定名は[Blender FBX export API](https://docs.blender.org/api/current/bpy.ops.export_scene.html#bpy.ops.export_scene.fbx)で確認する。

roomのMCPは次の設定を適用する。

- Sceneの単位系をMetric、Unit Scaleを`1.0`（1 Blender Unit = 1 meter）にする。
- Objectの回転とスケールをメッシュに適用し、位置は保持する。
- 選択Meshのみを出力し、FBX Scaleは`FBX_SCALE_UNITS`、Forwardは`-Z`、Upは`Y`にする。
- `bake_space_transform=True`（UIの「Apply Transform」）、Path Mode `COPY`、Embed Texturesを有効にする。
- アニメーションは出力しない。

現在の入力では画像Textureを作成できず、基本Materialの色などだけを指定できる。そのためCopy/Embed設定は将来のTexture対応にも備えたものだが、現状の出力に画像Textureは含まれない。

Blender Python APIは「Apply Transform」を実験的機能とし、ArmatureやAnimationで既知の問題があると警告している。このMCPはMesh専用・アニメーションなしだが、Unityに実際にインポートして結果を確認する。Object Originは各基本形状の中心のままで、記事が勧める底面Originには対応していない。底面を基準に配置する場合は、その制約を報告し、ユーザーが必要とする場合は別途対応する。

## 実行・検証

1. 依頼された内容が対応する基本形状だけで表現できるか確認する。表現できない細部を作成済みと偽らない。
2. 生成する各Objectの形状、名前、位置、回転、スケール、Materialを具体化する。
3. 同一の`outputName`が既存出力と衝突しない一意な名前を使う。
4. `create_fbx`を実行し、エラーが返った場合はその内容を報告する。
5. 戻り値の出力パスと両ファイルの存在を確認する。Unityでのインポートや見た目まで確認していない場合は未検証とする。
