---
name: ChaosDraftParty NotebookLM リソース
description: 卒業制作プロジェクト専用の NotebookLM ノートブック ID・ソース ID 一覧。仕様質問や deep research の投入先
type: reference
originSessionId: 5e61ca86-8045-42d6-9ce5-e2ecb4b5949c
---
## ノートブック

- **名前**: `ChaosDraftParty - 開発支援`
- **ID**: `528f0394-38e2-4650-b3ed-8660b5c8cc08`
- **URL**: https://notebooklm.google.com/notebook/528f0394-38e2-4650-b3ed-8660b5c8cc08
- **作成日**: 2026-04-21

## 登録済みソース

| index | title | source_id | type |
|---|---|---|---|
| 1 | CLAUDE.md | `6a07a487-a2c3-4355-9305-ecea5575b1a9` | MARKDOWN |
| 2 | ChaosDraftParty_Spec_v9.5.docx | `5e57c56b-998c-482e-b713-97d8bade55ac` | DOCX |
| 3 | ChaosDraftParty_TechSpec_v1.docx | `fd64d069-4a89-4c3e-b33e-043d854753bb` | DOCX |

## 使い方（パラレル安全のため `--notebook` で明示）

```bash
# 仕様質問
notebooklm ask "しっぽ狩りバトルの勝利条件と 2人モード調整を教えて" --notebook 528f0394-38e2-4650-b3ed-8660b5c8cc08

# 特定ソースだけに絞って質問
notebooklm ask "ホスト権威の同期対象は？" --notebook 528f0394-38e2-4650-b3ed-8660b5c8cc08 -s fd64d069-4a89-4c3e-b33e-043d854753bb

# Deep research で Unity NGO のベストプラクティス追加
notebooklm source add-research "Unity Netcode for GameObjects 2.x ClientNetworkTransform vs server-authoritative best practices 2025" --mode deep --notebook 528f0394-38e2-4650-b3ed-8660b5c8cc08 --no-wait
```

## 追加予定ソース（時が来たら）

- Unity 6 LTS 公式 docs（URP, Input System, Cinemachine 3）
- Netcode for GameObjects 2.x 公式 docs
- Unity Relay + Lobby サンプル (Boss Room) GitHub README
- Synty / Mixamo 使い方（アニメリターゲット、ヒューマノイド設定）

## 他 NotebookLM リソース（Crypto 側、別プロジェクト）

- `0d899a58` Brain-日次市場分析
- `2e51a14b` Brain-銘柄リサーチ
- `0e8df22f` Brain-戦略改善ラボ
