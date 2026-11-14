# Clusterワールド検証チェックリスト

該当しない項目は「該当なし」、確認していない項目は「未検証」とする。確認した事実と、合格と判断した根拠を区別する。

## Blender / FBX

- [ ] Scene、Object、出力対象を特定した
- [ ] 寸法、原点、Transform、単位、軸を確認した
- [ ] 接合面、法線、意図しない重複面を用途に応じて確認した
- [ ] Exporterの設定と実際に対応する機能を確認した
- [ ] 出力ファイルが存在し、Unityにインポートできた

## Unityアセットとシーン

- [ ] インポート後のBounds、実寸、位置、向きが想定どおり
- [ ] Material、Shader、Render Pipelineが対象環境に対応している
- [ ] 床・壁・天井・家具・装飾の隙間、浮き、めり込み、Z-fightingを確認した
- [ ] 表示用と衝突用の役割が分かれ、ColliderとTriggerが意図どおり
- [ ] Spawn Point、移動経路、境界、既存PrefabやScriptの参照を確認した
- [ ] 背景・装飾に不要なColliderや影がなく、Light・透明描画の負荷を考慮した
- [ ] Scene / Prefab が参照する依存Assetと`.meta`が揃っている
- [ ] Unity Consoleのエラーを調査し、スクリプトコンパイルが成功した

## 実行確認

- [ ] Game ViewまたはScene Viewで、関連する視点から見た目を確認した
- [ ] Play ModeでSpawn、移動、ギミック、衝突を確認した
- [ ] CCKの静的検査やBuildを、プロジェクトで利用可能な手段で実行した
- [ ] Cluster上の動作を確認した（実施した場合のみ）

Consoleを単にクリアして既存診断を隠してはならない。ログを消去する場合は、先に原因を調査し、必要な検証を再実行する。Unity Editor、Game View、Build、Cluster実行のどれかが利用できない場合は、その段階を未検証として明示する。
