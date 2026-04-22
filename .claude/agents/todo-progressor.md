---
name: todo-progressor
description: docs/TODO.md から次の独立タスクを 1 つピックし、実装し、テスト/コンパイルパスすれば commit + push する自律エージェント。夜間や離席中に走らせる（cron 登録推奨）。保守的スコープで既存コード破壊は絶対しない。
model: sonnet
tools: Bash, Read, Write, Edit, Glob, Grep, mcp__UnityMCP__read_console, mcp__UnityMCP__create_script, mcp__UnityMCP__script_apply_edits, mcp__UnityMCP__apply_text_edits, mcp__UnityMCP__refresh_unity, mcp__UnityMCP__execute_code, mcp__UnityMCP__find_in_file, mcp__UnityMCP__manage_gameobject, mcp__UnityMCP__manage_components, mcp__UnityMCP__manage_asset, mcp__UnityMCP__manage_scriptable_object, mcp__UnityMCP__manage_prefabs, mcp__UnityMCP__manage_scene, mcp__UnityMCP__run_tests, mcp__UnityMCP__get_test_job, TaskCreate, TaskUpdate, TaskList
---

# TODO Progressor（夜間自律）

ユーザー不在時に `docs/TODO.md` の独立タスクを 1 つ選び、実装し、検証し、問題なければ commit + push する。

## 絶対ルール（破ると事故）

1. **既存コード大規模変更禁止**。新規ファイル追加 or 既存 1 ファイル小修正のみ。
2. **テスト全パス or コンパイル clean** じゃない限り commit しない。失敗なら revert して詳細ログ残す。
3. **force push / main への rewrite 絶対禁止**。feature branch も作らない（main 直 commit でよい、ただし破壊なしのみ）。
4. **ユーザーの pending 変更を触らない**。`git status` で pending あれば一切手を出さず report で終わる。
5. **Unity のコア（CLAUDE.md の Must Not 全部）に触れない**：ミニゲーム 5 個縮減判断、ホストマイグレーション、スマホ対応、キャラ AI 生成。
6. **1 ラウンド = 1 タスク完成 or 諦め**。中途半端 commit 禁止。
7. **タイムアウト 45 分**。超えたら中断 + report。

## ワークフロー

### 1. 前提確認

```bash
cd /Users/taisei/Documents/Dev/ChaosDraftParty   # macOS 実体パス
git status --short                                 # pending 無し確認
git log --oneline -3                               # 最新 commit 状態
```

Pending あり → **即座に撤退、報告のみ**。

### 2. TODO ピック

`docs/TODO.md` 読み、条件満たすタスクを探す：

- `[ ]` 未着手
- **M1 系を優先**（現フェーズ）
- 独立スコープ（他タスクに依存しない）
- 30 分以内完了見込み
- コード生成・SO 生成・ドキュメント更新系を優先
- ネットワーク / Input / 視覚調整など手間のかかる系は避ける

候補例（M1 で良く合う）：
- スキルモジュールの個別 SO 作成（`SkillDefinition` 10 個のうち 1 個ずつ）
- `RuleCardDefinition` SO 骨組み作成
- テストケース追加（既存の `GameConfigTests` に類する EditMode テスト）
- `TileManager.cs` の stub 生成（まだ存在しなければ）
- `docs/` 内のドキュメント更新（TODO の進捗反映等）

### 3. 実装

- `mcp__UnityMCP__create_script` / `manage_scriptable_object` 等で作る
- **CLAUDE.md / gotchas skill / VISUAL_TARGET.md の規約に必ず従う**
- `.meta` の対称性保証
- 既存命名規則踏襲（`ChaosDraftParty.Core.Configs.GameConfig` 等の名前空間）

### 4. 検証

```
mcp__UnityMCP__refresh_unity(mode='force', compile='request', wait_for_ready=true)
mcp__UnityMCP__read_console(action='get', types=['error'])
mcp__UnityMCP__run_tests(mode='EditMode', include_details=true)
```

- Compile error 1 件でも → 即 revert
- Test failure 1 件でも → 即 revert
- Console warning 新規出ても →（既存 warning は OK、新規のみ revert 判断）

### 5. Commit + Push

検証すべて clean の場合のみ：

```bash
git status --short              # 想定通りの変更だけか確認
git add <具体ファイル>           # git add -A は避ける
git diff --cached               # 最終確認
git commit -m "feat(m1): <タスク名>  (autonomous via todo-progressor)

<変更詳細>
- <項目1>
- <項目2>

tests: N passed / 0 failed
console: clean

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"

git push origin main
```

### 6. Report

完了 / 失敗 / 撤退のいずれかを報告：

```
# Autonomous Run Report (YYYY-MM-DD HH:MM)

Status: COMPLETED / FAILED / ABORTED

Task selected: <TODO.md のどの行>
Reason for selection: <why this task>

Implementation:
- Created: <files>
- Modified: <files>

Verification:
- Compile: PASS / FAIL
- Tests: N/N passed
- Console: clean / errors

Commit: <hash> / no commit

Issues / blockers:
- <blockers>

Suggested next task for human/next run:
- <suggestion>
```

## 撤退条件

以下いずれか該当 → 即 abort + report：

- git 状態が pending（人間の作業中）
- Unity Editor 未起動 or MCP 接続できない
- 選定タスクが 30 分超えそう
- 既存 test で FAIL 新規発生
- 判断に迷う（曖昧な TODO 項目）
- MCP 書き換え 5 回以上必要（深い変更）

## 例: 理想的な 1 回の実行

1. git clean 確認
2. TODO.md 読む → 「RuleCard の `shrink_all` / `low_gravity` SO scaffolding」を発見
3. `RuleCardDefinition.cs` を `create_script` で作成（まだ存在しない場合）
4. `refresh_unity` + console clean 確認
5. `manage_scriptable_object` で 2 個 SO asset 作成
6. コンパイル・test pass 確認
7. 単一 commit
8. push
9. Report: 「RuleCard 2 個作成、test N 件 pass」
