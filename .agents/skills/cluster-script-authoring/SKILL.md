---
description: Use when writing ClusterScript TypeScript or building Unity scenes for Cluster Creator Kit items in any project, including choosing the required Unity components for a ClusterScript API, wiring ScriptableItem references, and checking the result with the cck-scenegraph-tools analyzer.
metadata:
    github-path: .github/skills/cluster-script-authoring
    github-ref: refs/heads/main
    github-repo: https://github.com/sato-toma/cck-scenegraph-tools
    github-tree-sha: 7ea51c7c53c00c13a569286d33fff1b45666b3c8
name: cluster-script-authoring
---
# ClusterScript Authoring

Cluster Creator Kit で ClusterScript を書き、Unity シーンに配置する作業を支援する。リポジトリ外のプロジェクトでも使えるよう、参照資料はこの skill フォルダ内にまとめている。

## 参照資料

- サンプル実装: `packages/unity-semantic-graph/Samples~/Assets/Script/src/` の ClusterScript（コールバック、State、Component 参照などの書き方）。リポジトリ外から使う場合は `gh repo clone sato-toma/cck-scenegraph-tools` で取得する。
- [Unity シーン構成チェックリスト](references/unity-scene-checklist.md): Item に付ける Component、ScriptableItem 参照、Export と解析の手順。

## 基本ルール

1. ClusterScript API を使う前に、公式の API 定義と Component 仕様（下記）で必要 Component と呼び出し条件を確認する。API の仕様はこの skill では管理せず、公式ドキュメントを正とする。
2. 必要 Component は、ClusterScript を配置する Item GameObject に付ける。
3. `$.on...` のコールバック登録の位置や、各 API を呼べる場所は、公式 API 定義の記載に従う。
4. 状態は `$.state` の直接変更を避け、`$.setStateCompat` を使う。配列の `push` や入れ子代入は解析ルールが検出する。
5. Unity 側の変更（Component 追加、ScriptableItem 参照）は、ユーザーの承認を得てから行う。

## 公式情報

- ClusterScript API 定義: https://docs.cluster.mu/script/interfaces/ClusterScript.html
- 型定義ファイル: https://docs.cluster.mu/script/index.d.ts（Unity 管理外の場所に保存し、`.meta`を生成しない）
- Item Component の仕様: https://docs.cluster.mu/creatorkit/item-components/

TypeScript で型チェックする場合、公式型定義は `node_modules/@types/cluster-script/index.d.ts` など `Assets` の外に取得し、`typeRoots` を設定する。プロジェクトに取得・更新スクリプトがある場合はそれを使い、型定義を Unity の `Assets` に置かない。

## 検証

ClusterScript の解析は、Unity の Export 結果（`SceneGraph.toml`）と ClusterScript ソースを組み合わせて行う。手順は [Unity シーン構成チェックリスト](references/unity-scene-checklist.md) の「解析」を参照する。

## 注意

- 公式ドキュメントと解析ツールの検査範囲は一致しない。解析ツールが通ることは、公式の要件を満たしたことを保証しない。
- 解析ツールが通ることと、Cluster 上で動くことは同じではない。実機確認を行った場合のみ動作確認済みと報告する。
