---
name: ChaosDraftParty Unity MCP セットアップ
description: Claude Code から Unity Editor を操作する MCP (CoplayDev/unity-mcp) の登録状況と Unity 側プラグイン接続手順
type: reference
originSessionId: 5e61ca86-8045-42d6-9ce5-e2ecb4b5949c
---
## 採用 MCP: CoplayDev/unity-mcp（MCP for Unity）

- **GitHub**: https://github.com/CoplayDev/unity-mcp （8.7k★、MIT、`justinpbarnett/unity-mcp` の公式後継）
- **Unity package name**: `com.coplaydev.unity-mcp`
- **現行バージョン**: v9.6.7-beta.5+（main ブランチが本流、beta ブランチは開発先行）
- **Claude Code 登録スコープ**: **local**（プロジェクト限定）、HTTP transport

### ⚠️ 紛らわしい類似リポ（使わない）

- `CoplayDev/unity-plugin` は **Coplay AI Copilot**（プレミアム商用、ログイン要）— MCP Bridge ではない。誤インストール注意。
- `CoplayDev/coplay-unity-plugin` も同じく Coplay Copilot 系。

## Claude Code 側の登録

```bash
# 現行マシンでは 2026-04-21 に登録済み（~/.claude.json の local scope）
cd ~/Documents/Dev/ChaosDraftParty
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list | grep UnityMCP   # UnityMCP: http://127.0.0.1:8080/mcp (HTTP) - ✓ Connected
```

解除するとき：

```bash
claude mcp remove --scope local UnityMCP
claude mcp remove --scope user UnityMCP     # 念のため
claude mcp remove --scope project UnityMCP  # 念のため
```

## Unity 側のセットアップ（プロジェクトごと 1 回）

1. Unity プロジェクトを開く
2. `Window → Package Manager → + → Install package from git URL`
3. URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
4. インストール完了後、`Window → MCP for Unity`
5. 初回は Setup ウィザードで依存確認（Python 3.10+ / uv が要る）
6. `Start Server` ボタン → HTTP サーバー `localhost:8080` 起動
7. Client Configuration で `Claude Code` を選択
8. `Install Skills` ボタン → `~/.claude/skills/unity-mcp-skill` が展開され、Claude Code の skills リストに `unity-mcp-skill` が出る（skill 経由で Unity 操作の best practice が注入される）
9. `Configure` ボタンで自動登録（Claude CLI Path 自動検出が失敗したら Browse で `which claude` の結果を指定）
10. 🟢 **Connected ✓** を確認

## Unity はターミナルから起動する（必須）

MCP Bridge が内部で `claude` コマンドを呼ぶため、Unity プロセスが PATH を継承している必要がある。Finder からダブルクリック起動すると PATH 未継承で失敗する。

```bash
open -a "Unity Hub"  # Hub からプロジェクトを Open
```

現行マシンでの `which claude` = `/Users/taisei/.nvm/versions/node/v22.22.0/bin/claude`

## 依存関係（現行マシンで確認済み）

- `claude` 2.1.116
- `python3` 3.13.9（3.10+ 要件クリア）
- `uv` / `uvx` 0.9.26
- `node` v22.22.0
- Homebrew `/opt/homebrew/bin/brew`

## 既知の落とし穴

1. **`CoplayDev/unity-plugin#beta` は別物**（Coplay AI Copilot）。誤って入れたら Package Manager から削除して `com.coplaydev.unity-mcp` に差し替える。
2. **Finder から Unity を起動すると PATH 未継承で `claude` が見えない** → Unity MCP Bridge が繋がらない。ターミナル経由で起動する。
3. **stdio↔http 切替時は Claude Code 再起動必須** — 切替後にハングしたら Claude プロセス再起動。
4. **SO 大量生成時に `AssetDatabase.Refresh()` ラグ** — 連続 `manage_asset` 呼出を batch 内で順序保証する。
5. **Roslyn スクリプト検証が strict** — 未使用 using で reject されることあり。Runtime Code Execution (Roslyn) の Install Roslyn DLLs を使うか、`Project Settings → Script Validation → Standard` に緩める。
6. **uvx `coplay-mcp-server@latest` という PyPI パッケージは存在しない**。初期リサーチで誤認して `claude mcp add coplay-mcp ... uvx coplay-mcp-server@latest` を登録してしまったが、これは noop。正しいのは **HTTP 127.0.0.1:8080/mcp** への登録。
7. **`Configure All Detected Clients` ボタンは前の `claude mcp add --scope local` 登録を消すことがある**（2026-04-21 実測）。Configure All は初回だけ使って、以降は個別 `Configure` ボタン or `claude mcp add` コマンド。消えたら同じコマンドで再登録。
8. **新規 MCP 登録は既存 Claude Code セッションに反映されない**。`mcp__UnityMCP__*` ツールが見えない場合は Claude Code を再起動。
9. **`claude mcp list` は cwd 依存**。local scope は `~/.claude.json` の `projects['<実体絶対パス>'].mcpServers` に書かれる。symlink 経由でアクセスすると別プロジェクトとして認識される可能性。**実体パス `~/Documents/Dev/ChaosDraftParty` で Claude Code を起動する**。
10. **Install Skills ボタンは `~/.claude/skills/unity-mcp-skill` にスキルを展開する**。これが入ると Claude Code の skill リストに `unity-mcp-skill` が現れ、Unity 操作のベストプラクティスが自動で参照される。

## Deps タブ（Optional Dependencies）状態

2026-04-21 に `Install All` 実施。インストール済：

- **Roslyn (C# 12+ Compiler)** — `Assets/Plugins/Roslyn/` に 4 DLL（Microsoft.CodeAnalysis, Microsoft.CodeAnalysis.CSharp, System.Collections.Immutable, System.Reflection.Metadata）、LFS 管理、commit 済。`execute_code` が C# 12 構文（record/pattern matching/file-scoped namespace 等）対応、Unity Editor 全 API を動的実行可能
- **ProBuilder 6.0.9** — `manifest.json` に追加、`ProjectSettings/Packages/com.unity.probuilder/Settings.json` 生成、`manage_probuilder` 有効化
- **Cinemachine 3.1.6** — 既存

**Windows デスクトップ側：** `git pull && git lfs pull` → Unity 起動だけで自動認識。Deps タブで ✓ 表示。ならなければ `Install All` 冪等押し直し。

## 補助候補（今回は採用せず、必要になったら検討）

- **IvanMurzak/Unity-MCP** (2.3k★、v0.66.0+) — `[McpPluginTool]` 属性で C# 側にドメイン固有ツールを追加可能。`TileManager.ScheduleShrinkage` のような ChaosDraftParty 固有操作を専用ツール化したい M3 以降で併用検討。
- **Unity 公式 com.unity.ai.assistant 2.0.0-pre.1** — Inspector 統合 UI。Unity Muse サブスク前提で費用対効果低、卒制ではスキップ推奨。
