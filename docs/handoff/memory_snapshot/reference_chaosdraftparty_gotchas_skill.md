---
name: ChaosDraftParty 罠回避スキル
description: プロジェクト固有の Unity/Claude ハマりどころは .claude/skills/chaosdraftparty-gotchas/SKILL.md に成長型で蓄積。実装前にそのファイルを参照
type: reference
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## 本体

`~/Documents/Dev/ChaosDraftParty/.claude/skills/chaosdraftparty-gotchas/SKILL.md`

git commit 済み、project scope、**macOS/Windows 両マシンで共有**。

## いつ参照するか

- `execute_code` で Unity 操作する前
- 新規シーン作成時（URP 未 assign 問題の確認）
- 環境構築時（Unity 起動方法・MCP 登録方法）
- マテリアルがピンク化・Missing Shader 発生時
- MCP 接続トラブル時
- 新規セッション開始時の orientation として

## 成長ルール

新しい罠を踏んだら Claude が即追記し commit。コミットメッセージ規約 `docs(gotchas): add <症状名> 回避策`。
各項目は **症状 → 原因 → 回避策 → 初遭遇日** の 4 要素。

## 2026-04-22 時点の収録罠

1. execute_code 本文に using 書けない
2. Unity 6 新規シーンで URP 未 assign → ピンク化
3. Unity GUI 起動で PATH 継承されない → MCP 失敗
4. MCP CLI Path の `.exe` は macOS でも正常
5. symlink 経由起動で memory 分離
6. `npx skills add` は `-y` 必須
7. `Configure All Clients` で既存 MCP 登録消える
8. 新規 MCP は既存セッションに反映されない
