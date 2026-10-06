---
name: ai-review-loop
description: コード変更（git pending changes or 直近 commit）を unity-reviewer → code-reviser → unity-reviewer の loop で品質向上させてから commit する。1 人開発で「もう一個の目」が欲しい時、commit 前の最終ゲート、push 前の自己レビューに使う。最大 3 周で打ち切り。Mac/Windows 共通。
---

# AI Review Loop

ChaosDraftParty 専用の **2 役 AI レビューループ**。`unity-reviewer`（指摘役）と `code-reviser`（修正役）を交互に走らせて、人間が見る前に品質を 1 段階上げる。

## いつ使うか

- commit 前 / push 前の最終ゲート
- 大きめの実装をした後（M1 タスク 1 件完了時）
- 自分で「これ怪しいけど判断つかん」と思った時
- 夜間の `todo-progressor` 完了後の自動チェック（option）

## いつ使わないか

- 1〜2 行の typo 修正、`.meta` 追加だけ
- WIP commit（後で squash する前提のもの）
- 視覚調整（shader / VFX / scene）→ 人間判断必須（AGENTS.md 原則 5）

## アーキテクチャ

```
[ git diff / 最新 commit ]
        │
        ▼
   unity-reviewer  ──── PASS  ──→ [ commit OK ]
        │
        │ ISSUES (HIGH / MEDIUM)
        ▼
   code-reviser   ──── DEFERRED only ──→ [ 人間判断へ ]
        │
        │ FIXED
        ▼
   unity-reviewer (2 周目)
        │
        │ まだ ISSUES
        ▼
   code-reviser (2 周目)
        │
        ▼
   unity-reviewer (3 周目、判定のみ)
        │
        ▼
   ┌─ option: codex 3rd reviewer ─┐
   │ (ChatGPT Plus 契約時、別 LLM    │
   │  視点で盲点探索、read-only)     │
   └────────────────────────────────┘
        │
        ▼
   [ レポート + 人間判断 ]
```

## 起動方法

### A. Codex 内から（推奨、ゼロ設定）

ユーザーが Codex にこう頼む：

> 「今の pending 変更に AI レビューループ回して」
> 「直近 commit を 2 周レビューして」
> 「`scripts/ai-review-loop` 走らせて」

Codex（メインスレッド）が orchestrator となり、本 SKILL.md の手順に従って `unity-reviewer` と `code-reviser` を交互に Task tool で呼ぶ。

### B. 補助スクリプト経由（CI 想定、option）

`tools/ai_review_loop.sh` を叩くと、Codex CLI 経由で同じループを起動。
（pre-commit hook / GitHub Action から呼ぶ用、現時点では用意せず必要になった時に追加）

## オーケストレーション手順（Codex メインがこの通り動く）

### Step 0: 入力確定

```bash
git status --short
git diff --name-only HEAD          # pending
# または
git diff --name-only HEAD~1 HEAD   # 直近 commit
```

レビュー対象ファイル一覧を確定（.cs / .asmdef / .asset / .prefab / .unity）。
**0 件なら即終了**「変更なし」。

### Step 1: 1 周目レビュー

`Task` tool で `unity-reviewer` 起動：
- 入力: 対象ファイル一覧 + `git diff` 全文
- 出力: HIGH / MEDIUM / LOW + 判定（PASS / PASS w/ nits / BLOCK）

**判定が `PASS` または `PASS w/ nits` で HIGH 0 件**なら → ループ終了、人間に commit 提案。

### Step 2: 1 周目修正

`Task` tool で `code-reviser` 起動：
- 入力: 1 周目 reviewer の出力 + 対象ファイル + `Iteration: 1`
- 出力: FIXED / DEFERRED / REVERTED + compile 結果

**DEFERRED が HIGH を含む**なら → 人間判断へエスカレ、ループ中断。
**全部 FIXED**なら Step 3 へ。

### Step 3: 2 周目レビュー

`unity-reviewer` 再起動：
- 入力: 修正後の `git diff`（1 周目修正分も含めた最新差分）

**HIGH 0 件かつ MEDIUM ≤ 1**なら → ループ終了、commit 提案。

### Step 4: 2 周目修正

`code-reviser` 再起動（`Iteration: 2`）。

### Step 5: 3 周目レビュー（最終判定）

`unity-reviewer` 再起動。3 周目は **判定のみ**、修正は走らせない。

### Step 5.5: Codex 3rd reviewer (option、ChatGPT Plus 契約時)

「もう一個別の視点が欲しい」時、または PASS w/ nits で commit 前に不安が残る時、
Codex CLI を 3rd reviewer として呼ぶ:

```powershell
codex exec --sandbox read-only "@<file_paths> を review。観点: null check、boundary case、edge case、AGENTS.md / .Codex/rules/ 違反。HIGH/MEDIUM/LOW + PASS/BLOCK 判定で。"
```

**役割分担:**
- Codex `unity-reviewer`: Unity 6 / URP / NGO の罠、本作固有規約 (AGENTS.md / rules)
- Codex `code-reviser`: 修正実行
- **Codex (3rd)**: 別 LLM 視点での盲点探索、business logic / edge case / OSS パターン違反

**注意:**
- Codex は read-only 起動必須 (`--sandbox read-only`)
- ChatGPT Plus の 5h 窓 (45-225 msg) を考慮、**毎 commit 走らせない** (大きい実装 / 不安残る時のみ)
- Codex の出力は Codex (orchestrator) が整理して User に提示
- 詳細: `AGENTS.md` の「Codex の役割」section

### Step 6: 最終レポート

人間に最終レポートを返す：

```
# AI Review Loop Result

Iterations: N
Final verdict: PASS / PASS w/ nits / BLOCK

## Summary
- 初回 issue: HIGH x, MEDIUM y, LOW z
- 修正済み: HIGH a, MEDIUM b
- 残存: HIGH c, MEDIUM d
- DEFERRED（人間判断）: ...

## Recommendation
- commit OK / commit 前に <これだけ> 確認 / 修正再考
```

## 終了条件

| 条件 | 動作 |
|---|---|
| reviewer が PASS（HIGH 0） | 即終了 |
| reviser が全 issue FIXED + 次 review PASS | 終了 |
| 3 周到達 | 強制終了、最終レポート |
| reviser が DEFERRED [HIGH] を返す | 中断、人間判断 |
| compile error が増えた | reviser が revert 後、終了 |
| 同じ issue が 2 周連続で残る | 終了（無限ループ防止） |

## 既存 agent との統合

| 既存 | 役割 | 本 loop での扱い |
|---|---|---|
| `unity-reviewer` | レビュアー (Codex) | **そのまま使う**、改造しない |
| `code-reviser` | 修正役 (Codex) | **そのまま使う** |
| `codex` CLI | 3rd reviewer (OpenAI、option) | Step 5.5 で `codex exec --sandbox read-only` で呼ぶ。詳細は `AGENTS.md` |
| `unity-error-triager` | console error 修正 | loop 外で別タイミングで動く |
| `todo-progressor` | 夜間自律実装 | 完了時に本 loop を呼んでから push（option） |
| `Codex-guide` | ガイド | 関与せず |

## 失敗パターンと回避策

- **レビュアーが厳しすぎてループ終わらない** → `code-reviser` 側で「3 周目は新規修正禁止」ルールで断ち切る
- **修正で新規バグ** → reviser の検証 step で compile error 検出 → 即 revert
- **視覚調整に踏み込む** → reviser 側ルールで shader/VFX を ask before touch、勝手に変えない
- **API 互換破壊** → reviser ルール「既存 API 署名を変えない」、ぶつかったら DEFERRED

## prototype 制限（M1 中の運用）

- Anthropic Python SDK は **入れない**（subagent で済む、依存最小）
- pre-commit hook 化は **保留**（手動起動で十分、誤発火コスト > 利益）
- GitHub Action 化は **M5 以降**（プレイテスト前の品質ゲートとして検討）
- 実 SaaS 化、Web UI、別プロセス常駐 など重実装は **やらない**

## 起動例

```
# Codex 上で
> 直近の pending 変更を AI review loop で 2 周回して、その後 commit して

→ Codex:
1. git diff で対象確定
2. Task("unity-reviewer", ...) 1 周目
3. Task("code-reviser", ...) 1 周目修正
4. Task("unity-reviewer", ...) 2 周目
5. レポート出して、人間に commit 確認
```

## 参考

- `.Codex/agents/unity-reviewer.md` — レビュー観点 A〜G の詳細
- `.Codex/agents/code-reviser.md` — 修正役の制約
- `AGENTS.md` — ChaosDraftParty 全体規約
- `.Codex/skills/chaosdraftparty-gotchas/SKILL.md` — 既知罠 42 件
