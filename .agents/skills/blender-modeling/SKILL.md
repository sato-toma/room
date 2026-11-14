---
name: blender-modeling
description: "Use when creating, editing, inspecting, or exporting 3D assets in Blender, especially with the room Blender MCP create_fbx tool and Unity asset output."
---

# Blender Modeling

Blenderで3Dアセットを作成・編集・確認する作業を支援する。Blenderの技術資料はこのリポジトリに集約し、詳細な手順は参照資料に追加する。

## 使用するとき

- Blender内のObject、Mesh、Material、UV、Sceneなどを作成・編集・確認するとき
- Blender MCPを使った操作や、他ツール向けのアセット出力を行うとき

## 手順

1. 利用中のBlenderバージョン、現在のScene、選択Object、既存ファイルの状態を確認する。
2. 要件から形状・寸法・原点・座標系・見た目・出力形式を整理する。不明な仕様は推測で確定しない。Cluster向けアセットでは、隣接する[`cluster-world-development`](../cluster-world-development/SKILL.md)のワークフローも適用する。Unity向けでは[Cluster公式のBlender→Unity設定](./references/room-blender-mcp.md)を基準にする。
3. [MCP操作ガイド](./references/mcp-operations.md)に従い、現在の環境で利用可能なツールと引数を確認する。roomの`create_fbx`を使う場合は[room Blender MCP仕様](./references/room-blender-mcp.md)を適用する。
4. 変更対象を限定して作業する。既存アセットの上書き・削除や大きな変更は、必要ならユーザーの承認を得る。
5. [作成ワークフロー](./references/authoring-workflow.md)を参考に、必要な範囲でメッシュ、変換、Material、UVなどを整える。
6. [検証チェックリスト](./references/validation-checklist.md)から対象に合う項目を確認する。未実施の項目は未検証として残す。
7. roomの`create_fbx`を使う場合は`outputName`が未使用であることを確認し、出力先を変える要求がなければ`Assets/Generated/Blender`への出力を使う。戻り値のパスと生成ファイルを確認する。
8. 実施内容、変更・出力ファイル、検証結果、未解決事項を報告する。

## 注意

- MCPのツール名、引数、機能を推測で作らない。利用できない操作は、できないことを明示する。
- roomの`create_fbx`は基本形状から新しいSceneを作るツールであり、既存Sceneや既存Objectの編集には使えない。
- roomのFBX出力はメートル単位、回転・スケール適用、FBX Unit Scale、`-Z` Forward / `Y` Up、Apply Transformを使用する。原点は各基本形状の中心で、底面原点は指定できない。
- Blenderのバージョンや出力先ツールによって挙動が異なる可能性があるため、適用条件を確認する。
- エクスポート成功だけで、UnityやCluster上での互換性・見た目・動作が確認できたとは報告しない。
- 作業時に必要な技術資料は、このSkillに同梱する`references/`に追加する。リポジトリ全体の知識整理と出典管理は、配布元リポジトリの`docs/`で行う。
