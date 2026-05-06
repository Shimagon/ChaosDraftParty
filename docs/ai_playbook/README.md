# AI Playbook — Claude Code 用 高クオリティ実装 knowledge base

**目的**: Claude Code (or 任意の AI agent) が新規プロジェクトでも即座に高クオリティな実装判断ができるよう、ChaosDraftParty 開発で蓄積した **普遍化されたレシピ / 設計原則 / 罠** を集約。

**使い方**: このフォルダごとコピーして新規プロジェクトの `docs/ai_playbook/` に貼れば、AI に「`docs/ai_playbook/README.md` を読んで」と頼むだけで全部読んでくれる。

---

## 📚 構造

```
docs/ai_playbook/
├── README.md                  # 本書 (index + 使い方)
├── recipes/                   # 即適用可能なレシピ (パラメータ + コード骨格)
│   └── camera_occluder_fade.md
├── patterns/                  # 普遍化された設計原則
│   ├── anticipation_tick_smooth_tail.md
│   └── parallel_agent_research.md
└── gotchas/                   # 罠と回避策 (二度踏まない)
    └── unity6_urp17_pitfalls.md
```

---

## 🎯 各カテゴリの説明

### `recipes/` — 即適用レシピ
完成形パラメータ + コード骨格 + 実装手順。Unity プロジェクトでそのままコピペで動く粒度。

| Recipe | 用途 | 適用条件 |
|---|---|---|
| [camera_occluder_fade.md](recipes/camera_occluder_fade.md) | 3D TPS の壁透過 (camera-player 間) を AAA defacto で実装 | URP + Cinemachine 3.x、cartoon〜リアル全対応 |

### `patterns/` — 設計原則
特定実装じゃなく、**普遍化された考え方**。複数の演出 / システムに応用可。

| Pattern | 一行説明 |
|---|---|
| [anticipation_tick_smooth_tail.md](patterns/anticipation_tick_smooth_tail.md) | 「即時感 + ぬるっと感」両立の core pattern。Hit Stop / Damage Flash / FOV Punch / 着地 squash 全部に応用可 |
| [parallel_agent_research.md](patterns/parallel_agent_research.md) | 思い込みで proposal せず並列 agent 3 個で多角検証する開発フロー |

### `gotchas/` — 罠
二度踏まないための失敗事例集。「これやらかすと時間溶ける」系。

| Gotcha | 対象 |
|---|---|
| [unity6_urp17_pitfalls.md](gotchas/unity6_urp17_pitfalls.md) | Unity 6 / URP 17 / Cinemachine 3.x / Input System の罠まとめ |

---

## 🚀 新規プロジェクトでの使い方

### Step 1: フォルダごとコピー
```bash
cp -r docs/ai_playbook /path/to/new_project/docs/
```

### Step 2: 新規プロジェクトの CLAUDE.md に追記
```markdown
## AI 開発フロー
- 詳細レシピ・原則・罠は `docs/ai_playbook/` に集約。
- 任意のタスクで AI に頼む前に該当 recipe/pattern/gotcha を参照させる。
```

### Step 3: AI に指示
```
docs/ai_playbook/README.md を読んで該当する recipe / pattern / gotcha があればそれに従ってください。
```

これで AI が即座に「Camera Occluder Fade なら anticipationStart 0.3 + targetUpOffset 1.0 + holdTime 0.25...」と高クオリティ実装できる。

---

## 📈 拡充ガイド

このフォルダは **生きた knowledge base**。新しいタスクで学んだら追記:

- **新 recipe**: 完成パラメータ + コード骨格 + 試行錯誤の経緯
- **新 pattern**: 「複数の case で再現した普遍原則」のみ
- **新 gotcha**: 1 度踏んだ罠でも、未来の Claude が踏まないよう即追記

各ファイルの構成テンプレートは既存ファイルを参照。

---

## 🔗 関連 (ChaosDraftParty 固有 docs)

本書は **横断 reusable** 部分のみ。以下は ChaosDraftParty 専用、新規プロジェクトには持っていかない:
- `docs/M2_quality_roadmap.md` — Day 1-5 ロードマップ (本作スケジュール)
- `docs/M2_priority_world_aligned.md` — Tier S/A/B/C/D 30 候補 (本作世界観固有)
- `docs/M2_quality_overrides_detail.md` — Color Grading 10 個詳細 (本作 Splatoon 寄り)
- `docs/reports/2026-05-*.md` — 開発日報

---

**作成日**: 2026-05-06
**作成根拠**: ChaosDraftParty M2 で 8 反復 / 並列 agent 9 個 / AI review loop 4 周を経て確定したパターンを抽出。
