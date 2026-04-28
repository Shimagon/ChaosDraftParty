---
name: ChaosDraftParty 作業方針（全権委任）
description: 卒業制作プロジェクト（/Users/taisei/Documents/Dev/ChaosDraftParty）での作業ルール。MCP/Skills/サブエージェント全活用、確認不要、planモード活用、ウルトラシンキング常時ON
type: feedback
originSessionId: 5e61ca86-8045-42d6-9ce5-e2ecb4b5949c
---
## ルール

卒業制作プロジェクト（`/Users/taisei/Documents/Dev/ChaosDraftParty`、ChaosDraftParty）での作業時は以下を常時適用：

1. **MCP とスキルをフル活用** — 使える MCP があれば必ず優先、skill が使える場面では必ず先に検索・適用
2. **サブエージェントを遠慮なく使う** — 並列調査・並列実装を積極的に行う。大きめのタスクは Explore / general-purpose / Plan エージェントに投げる
3. **新 MCP・新 skill・新 UPM パッケージ の導入は勝手に実施 OK、聞かずに進める** — 「これがあれば良くなる」と判断したら即 `npx skills add <owner/repo> -g` / `claude mcp add ...` / `manage_packages` 等で導入して使う。**2026-04-21 ユーザー「勝手にダウンロードして欲しい」再強調**。導入前に確認プロンプトで止まらない（確認された行為をもう一度聞くな）。ただし **有料サブスク契約・API key 発行で課金発生するもの** は事前確認
4. **このプロジェクト内の操作は全て事前承認済み** — ファイル編集、実行、インストール、外部呼び出し含めて確認不要。**docs・設定・memory・non-destructive な commit/push も確認不要**（2026-04-21 ユーザー「いいよ別に聞かなくてもいいよ」で恒久許可）。ただし **force push / main への破壊的 rewrite / PR 作成 / 秘密情報コミット / 外部公開**（Issue/SNS/発表）は従来通りユーザー確認

5. **細かく commit + push（2026-04-22 ユーザー再指示）** — 実験・検証・実装の各 phase ごとに git commit + push を挟む。大きな batch 完了まで commit を溜め込まない。理由：複数マシン（Mac + Windows）で開発する際の同期粒度、実装ロスト防止、git log が検証の軌跡になる。**ユーザー確認不要、Claude が自律的に適切な粒度で切る**。コミットタイトルは `feat(m1):` `docs(gotchas):` `chore:` 等のプレフィックス付き。

6. **`.claude/settings.json` に全 MCP 許可リスト展開済み（2026-04-21）** — `mcp__UnityMCP__*`（42 tool 全）, `mcp__github__*`, `mcp__sentry__*`, `mcp__playwright__*`, `mcp__semgrep__*`, `mcp__circleci-mcp-server__*`, `mcp__plugin_vercel_vercel__*`, ListMcpResourcesTool, ReadMcpResourceTool, WebFetch, WebSearch, 主要 Skill, 全 git/gh/npx/uv/node 系 Bash を `allow` に列挙。`deny` は破壊的 git/rm/sudo のみ。**ユーザーが「こういうの聞いてくるのうざい。全部許可するから聞かないで」と明示要求**。Claude Code の permission prompt が出るはずのツールで毎回止まっていたため恒久解消。**新 MCP 追加時は `.claude/settings.json` の allow に追記してコミット**（Windows デスクトップ側も同じ設定が効くように project scope）
5. **精度重視で plan モードを使う** — 非自明な実装・アーキ判断の前には必ず Plan エージェントか ExitPlanMode で方針合意してから着手
6. **ウルトラシンキング常時 ON** — 迷うなら深く考える側に倒す

**Why:** ユーザーが 2026-04-21 の会話開始時に明示的に宣言。成功のために手段を制限しない方針。ChaosDraftParty は 2026年4月〜9月の卒業制作で、M1〜M6 の月次マイルストーン進行。1人＋Claude Code 開発のため Claude の自律度を最大化したい。

**How to apply:** ChaosDraftParty プロジェクト（`~/Documents/Dev/ChaosDraftParty/`、旧パス `~/Documents/課題/やりたいこと/卒業制作` は symlink として残存）配下で動いている時は、このモードをデフォルトにする。タスクを受けたら先に skills / MCP を検討 → サブエージェント並列化を検討 → plan モードで方針固め → 実装。確認プロンプトで止まらず進む。ただし git push・外部発信・破壊的共有操作は引き続きユーザー確認を取る。

**パスに関する注意:** 2026-04-21 に日本語パス回避のため `~/Documents/課題/やりたいこと/卒業制作/` → `~/Documents/Dev/ChaosDraftParty/` に移動済み。旧パスは symlink。Unity 6 は概ね日本語パスも動くが、Platform Switch や Build 時に稀に非 ASCII パスでトラブるため予防的に移動した。

**複数マシン運用（重要）:** ユーザーは MacBook（現行）に加えて **デスクトップ PC** でも開発する予定。引き継ぎのため GitHub private repo に「コミット・プッシュ徹底」方針。ローカル生成ファイル（`Library/` `Temp/` `Obj/` `Build/` `.vs/` `.idea/` `UserSettings/`）のみ `.gitignore`。secrets は現状無しだが、万一入る場合は明示確認してから扱う。GitHub: `Shimagon/ChaosDraftParty` (private 予定)。
