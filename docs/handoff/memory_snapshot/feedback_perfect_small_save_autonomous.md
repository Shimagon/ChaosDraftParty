---
name: 完璧小刻み + 返信なし自律モード（デフォルト運用）
description: ChaosDraftParty 実装の基本ルール。1 タスク = 1 commit、完璧に仕上げてから次へ。ユーザー返信なしなら ScheduleWakeup で自律継続。並列は研究のみ、実装は必ず逐次。
type: feedback
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## 基本モード（ChaosDraftParty 常時適用）

ユーザー taisei の方針: **「少しずつでいいから完璧にこなして git に細かくセーブしてほしい」「返信なかったら勝手に進めて」**（2026-04-22 指示）。

**Why:** 
- 完璧小刻み = 卒業制作の品質優先、履歴が検証証跡になる、revert 容易
- 自律継続 = ユーザー離席・睡眠中の停滞を避ける、12 commit/セッション級の進捗を継続

**How to apply:**

### 1. 粒度ルール

- **1 task = 1 commit + 1 push**
- 1 task の工数目安: 30秒〜5 分
- 大きな作業は分割（例: GameManager.cs → GameManager class + SessionState enum + event hooks の 3 commit）
- 逐次的に積む（parallel 実装は禁止、研究系 Agent 呼出は並列 OK）

### 2. 検証ゲート（commit 前必須）

- C# 追加: `refresh_unity(force, wait_for_ready)` → `read_console(types=['error'])` 0 件
- SO 追加: 型 compile 済確認 → 実インスタンス作成
- テスト追加: `run_tests(EditMode)` 全 pass
- hook が自動検証（PostToolUse の error-triager）、違反あれば revert

### 3. 返信なし → 自律継続

```
Task 完了 → commit + push
         → 「〜完了、次 X に進む」を簡潔に text で出力
         → ユーザー返信を即時チェック（= 次メッセージ受信）
         ├ 30-60 秒以内に返信: 優先対応
         └ 返信なし: ScheduleWakeup(300-600 秒) + 次 task prompt をチェーン
```

### 4. 自律の停止条件

以下いずれかで即停止、ユーザー判断待ち：
- 連続自律が 10 task または 60 分超
- compile or test で fail 発生
- 判断に迷う設計選択（例: 名前空間、ファイル配置、API 選択に複数候補）
- Unity MCP bridge が down（罠 #43）
- git pending の差分が 想定外（ユーザー介入の可能性）
- Meshy credit 大量消費の前（>100 credits）
- ユーザーの Must Not に近い判断（キャラ Meshy 生成、Ragdoll 同期 等）

### 5. 並列の使いどころ

**並列 OK:**
- Agent tool の独立リサーチ（複数の general-purpose / Explore 同時起動）
- Bash の独立コマンド（git status + git log + ls 同時）
- WebFetch 複数 URL

**並列禁止:**
- Unity MCP の write ops（manage_gameobject/components/prefabs/scene/asset）
- create_script の連発（罠 #43 Unity bridge down 誘発）
- 同じファイルへの Edit 複数

### 6. commit message ルール

- プレフィックス: `feat(m1):` / `feat(visual):` / `docs(todo):` / `fix(gotcha):` / `chore(autonomy):` 等
- タイトル 50 文字以内、本文で詳細（変更箇所、DoD、verify 結果）
- 自律時は末尾に `autonomous via Claude Code session` 明記
- Co-Authored-By フッタ

### 7. 完璧の基準

以下 6 つすべて満たして「完璧」:
1. 新規型は compile pass
2. 既存 test があれば全 pass
3. console error 0 件（warning は許容、ただし新規は注意）
4. `.meta` 対称性保証（手書き .meta でも可、罠 #44）
5. TODO.md の該当項目を `[x]` or `[~]` 正しく反映
6. **Visual 変更の場合は Game View スクショで目視 OK**（罠 #48）— shader/material/UI/scene 触ったら `mcp__UnityMCP__manage_camera action=screenshot include_image=true` 必須、白飛び/不可視/位置ズレ検出

これを守れない commit は後から fix commit でフォロー（revert より forward fix 優先）。
