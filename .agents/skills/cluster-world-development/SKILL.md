---
name: cluster-world-development
description: "Use when planning or building Cluster worlds with Blender and Unity, including FBX workflows, scene and prefab authoring, CCK integration, collision, lighting, performance, or validation."
---

# Cluster World Development

ClusterワールドをBlenderとUnityで制作・変更するときに、アセット受け渡しから実機確認までを一貫して進める。

## 使用するとき

- Cluster向けの環境、建築、家具、背景、ギミックを作成または変更するとき
- BlenderからFBXをUnityへ渡すとき
- Unityシーン、Prefab、マテリアル、ライト、Colliderを調整するとき
- Cluster Creator Kit (CCK) の構成、ビルド、公開前確認を行うとき

## 基本手順

1. 対象プロジェクト、UnityとCCKのバージョン、Render Pipeline、対象プラットフォーム、作業シーンを確認する。対応条件はプロジェクトと使用中のCCKの資料を優先し、別プロジェクトの設定をそのまま一般化しない。
2. 既存シーンとアセットの状態、バージョン管理の変更、承認済み・保護対象のルールを確認する。既存コンテンツを保持する必要があれば、適切なPreview SceneやPrefabを使い、元シーンの複製・編集は依頼範囲に限定する。
3. 要件を形状、見た目、インタラクション、移動経路、出力仕様に分け、寸法・原点・座標系・Colliderの役割を決める。不明点は仮定として明示する。
4. [制作ワークフロー](./references/authoring-workflow.md)に沿ってBlenderとUnityの作業を進める。利用可能なツールの機能・引数を確認し、出力成功と受け取り側での検証を区別する。
5. [検証チェックリスト](./references/validation-checklist.md)から該当項目を確認する。Unity Editor、Play Mode、Cluster上の実行確認はそれぞれ別の検証段階として報告する。

## 共通原則

- 1 Unity unit を1 metreと想定する場合も、FBXのUnit、軸、Transform、原点の設定を確認し、Unityへインポートした実寸と設計値を照合する。Blenderの見た目や出力成功だけで正しいスケール・向きと判断しない。
- 床・壁・天井・入口・階段・家具などの接合面を境界とBoundsで確認する。目立つ隙間、意図しない面重複、Z-fighting、浮きやめり込みを修正する。意図した接触・重なりと、偶発的な交差を区別する。
- 視覚表示と物理的な移動制約を分けて設計する。背景・遠景・視認用装飾は通常Collider不要だが、プレイヤーを止める境界、床、階段などは意図したColliderを確認する。スクリプトで強制移動する仕組みは物理衝突だけでは制御されないことがあるため、移動処理と境界制御を合わせて検証する。
- 色や発光を調整するときは、使用Shaderが対象Render PipelineとCCKに対応することを確認する。Emissionの視認性と、周囲を照らす実際のLight効果を混同しない。ライト数、影、透明描画は対象プラットフォームの性能を考慮する。
- 既存のPrefab、Component、シリアライズ参照、Spawn Point、ギミックを変更する前に依存関係を調べる。再生成ツールやEditorスクリプトを作る場合は、繰り返し実行しても重複・欠落を起こさないようにし、前提条件が満たされない場合は明確に失敗させる。
- Unityが管理する`.meta`とAsset GUIDを維持する。Unityプロジェクトの一時出力・生成物の保存先とGit管理方針は、そのプロジェクトの既存ルールに従う。
- エクスポート、静的解析、シーン確認、Play Mode、Cluster上の実動作は別々の証拠として扱う。未実施の確認を完了済みと報告しない。

## 関連Skill

- Blenderアセットの作成・編集とMCPの使い方は、[`blender-modeling`](../blender-modeling/SKILL.md)を参照する。
- ClusterScriptの作成とシーン構成チェックは、プロジェクトに導入された`cluster-script-authoring` Skillを参照する。
