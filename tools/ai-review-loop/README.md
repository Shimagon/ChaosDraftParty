# ai-review-loop

ChaosDraftParty 専用の AI レビューループ機構。

## 何をするか

コード変更を `unity-reviewer`（指摘役） → `code-reviser`（修正役） → `unity-reviewer`（再判定）の loop で 2〜3 周回し、commit 前に品質を 1 段階上げる。1 人開発で「もう 1 個の目」が欲しいユースケース向け。

## 構成

```
.claude/
├── agents/
│   ├── unity-reviewer.md   # 既存 (指摘役)
│   └── code-reviser.md     # 新規 (修正役)
└── skills/
    └── ai-review-loop/
        └── SKILL.md        # 新規 (orchestrator)

tools/
└── ai-review-loop/
    ├── run.sh              # option, CLI / hook 用ラッパー
    └── README.md           # 本ファイル
```

## 起動方法

### A. 通常運用（推奨）

Claude Code セッション内で頼むだけ：

```
今の pending 変更に AI review loop 回して
```

→ Claude が `.claude/skills/ai-review-loop/SKILL.md` の手順に従って `unity-reviewer` と `code-reviser` を Task tool で交互に呼ぶ。

### B. CLI / hook 経由（option）

```bash
./tools/ai-review-loop/run.sh           # pending 変更
./tools/ai-review-loop/run.sh --staged  # staged のみ
./tools/ai-review-loop/run.sh --last    # 直近 commit
```

`claude` CLI が PATH に入っていれば動く。pre-commit hook 化する時は `.git/hooks/pre-commit` から本スクリプトを呼ぶ。

## 終了条件

- reviewer が PASS（HIGH 0）→ 終了
- 3 周到達 → 強制終了
- DEFERRED [HIGH] 残存 → 人間判断にエスカレ
- compile error 増 → reviser が revert 後に終了

## 使いどころ

- commit 前 / push 前の最終ゲート
- M1 タスク 1 件完了時
- `todo-progressor` が夜間 commit する前のゲート（option）

## 使わないところ

- typo / .meta 追加だけ
- WIP commit
- 視覚調整（shader / VFX / scene）→ 人間判断必須

## 既存 agent との関係

| 既存 | 改造 | loop での使い方 |
|---|---|---|
| `unity-reviewer` | 無し（そのまま） | 指摘役として呼ぶ |
| `unity-error-triager` | 無し | loop 外、別タイミング |
| `todo-progressor` | 無し | 完了時に loop を呼ぶ option あり |

## なぜ Python SDK を使わなかったか

- subagent + skill で同じことができ、依存ゼロ
- Claude Code の認証 / 課金 / context 管理がそのまま使える
- Mac/Win で別ランタイム不要
- M1 期に「動くものを最短で」原則に沿う

将来 GitHub Action / CI で headless 実行したくなったら `run.sh` を経由するか、Anthropic Python SDK で書き直すのは可能（その時に `claude-api` skill を使う）。
