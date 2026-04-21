# ChaosDraftParty

2〜4 人オンライン **3D TPS パーティゲーム**。ミニゲームラッシュ × スキルドラフト × カオスルール。
「見た目カオスでも勝つのは上手いやつ」— Smash Bros 路線。

> 卒業制作（東海大学 情報理工学部 情報メディア学科 小坂研究室）
> Unity 6 LTS + Netcode for GameObjects、2026 年 4 月〜9 月（M1–M6）、1 人制作 + Claude Code + Unity MCP 併用。
> **macOS（MacBook）と Windows（デスクトップ PC）の両対応**。

## 仕様書・ガイド

- [ゲーム設計書 v9.5](仕様書/ChaosDraftParty_Spec_v9.5.docx) — ゲームデザイン全体
- [技術仕様書 v1.0](仕様書/ChaosDraftParty_TechSpec_v1.docx) — API / データ / アルゴリズム
- [CLAUDE.md](CLAUDE.md) — Claude Code 向け開発ガイド（実装判断の原則）
- [docs/HANDOFF.md](docs/HANDOFF.md) — **別マシン/新セッション引き継ぎノート**（地雷地図・登録済みツール・パッケージバージョン意図）
- [docs/TODO.md](docs/TODO.md) — タスク管理（M1〜M6 の進行状況）
- [docs/RESOURCES.md](docs/RESOURCES.md) — MCP / Skills / OSS / API のマスターリスト（Tier 評価付き）

## 技術スタック

| 領域 | バージョン |
|---|---|
| Unity | **6.4.3f1 LTS**（Apple Silicon / Windows x64 両対応） |
| レンダリング | URP 17.0.1 |
| ネットワーク | Netcode for GameObjects 2.11 + Relay 1.2 + Lobby 1.3 + Authentication 3.6 |
| 入力 | Input System 1.19 |
| カメラ | Cinemachine 3.1 |
| アセット | Addressables 2.10、Timeline 1.8、AI Navigation 2.0 |
| テスト | Test Framework 1.4.2 + **ParrelSync 1.5**（OpenUPM）|
| IDE | ide.rider 3.0.27 / ide.visualstudio 2.0.22 |

## 開発環境セットアップ（新規マシン向け）

### 必要ソフト

| ツール | macOS | Windows |
|---|---|---|
| Unity Hub | `brew install --cask unity-hub` | `winget install Unity.UnityHub` |
| Unity | Hub から **6.4.3f1** + **Mac Silicon + Windows Build Support (Mono)** | Hub から **6.4.3f1** + **Windows Build Support + Mac Build Support** |
| Git + LFS | `brew install git git-lfs` | `winget install Git.Git GitHub.GitLFS` |
| Node.js 22+ | `brew install nvm && nvm install 22` | [nvm-windows](https://github.com/coreybutler/nvm-windows) で `nvm install 22` |
| Python 3.11+ | `brew install python@3.12` | `winget install Python.Python.3.12` |
| uv / uvx | `curl -LsSf https://astral.sh/uv/install.sh \| sh` | `powershell -c "irm https://astral.sh/uv/install.ps1 \| iex"` |
| Claude Code | `npm i -g @anthropic-ai/claude-code` | 同左 |
| gh CLI | `brew install gh` | `winget install GitHub.cli` |

### 手順

#### macOS

```bash
# 1. Clone + LFS
gh repo clone Shimagon/ChaosDraftParty ~/Documents/Dev/ChaosDraftParty
cd ~/Documents/Dev/ChaosDraftParty
git lfs install --local && git lfs pull

# 2. Unity Hub でプロジェクトを Open（初回 5〜10 分、Library/ が生成される）
open -a "Unity Hub"

# 3. Unity 側に MCP Plugin を入れる（プロジェクトごとに 1 回）
#    Window → Package Manager → + → "Install package from git URL"
#    URL:  https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
#    → Window → MCP for Unity → Start Server（HTTP 127.0.0.1:8080）
#    → Client: Claude Code → Configure（CLI Path 自動検出、失敗したら `which claude` を Browse で指定）
#    → Install Skills（`~/.claude/skills/unity-mcp-skill` 展開）

# 4. Claude Code 側に MCP を local scope で登録（プロジェクトごとに 1 回）
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list | grep UnityMCP   # → UnityMCP: ... - ✓ Connected

# 5. Claude Code を再起動（既存セッションに新 MCP は反映されない）
```

#### Windows

```powershell
# 1. Clone + LFS（ASCII 英字パス推奨、日本語・空白は避ける）
cd C:\Dev
gh repo clone Shimagon/ChaosDraftParty
cd ChaosDraftParty
git lfs install --local ; git lfs pull

# 2. Unity Hub を起動 → Open project → C:\Dev\ChaosDraftParty
#    （スタートメニューから起動、エクスプローラーのダブルクリックは PATH 継承されず MCP 失敗）

# 3. Unity 側に MCP Plugin を入れる（手順は macOS と同じ）
#    Window → Package Manager → + → "Install package from git URL"
#    URL:  https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main
#    → Window → MCP for Unity → Start Server
#    → Client: Claude Code → Configure（`where.exe claude` の結果を Browse で指定）
#    → Install Skills

# 4. Claude Code 側に MCP を local scope で登録
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list | Select-String UnityMCP

# 5. Claude Code を再起動
```

### Input System 有効化（両 OS 共通）

```
Edit → Project Settings → Player → Active Input Handling → "Input System Package (New)"
→ 再起動プロンプトで Apply
```

### 重要：Unity はターミナル経由で起動する

MCP Bridge が `claude` コマンドを呼ぶため、Unity プロセスが PATH を継承している必要があります。**Finder / Explorer からダブルクリック起動は失敗します。**

```bash
# macOS
open -a "Unity Hub"
```

```powershell
# Windows（スタートメニューから起動、または PATH が通った状態で）
start "" "C:\Program Files\Unity Hub\Unity Hub.exe"
```

## リポジトリ構成

```
.
├── Assets/_Project/          # 自作アセット・スクリプト（CLAUDE.md 準拠）
│   ├── Scripts/
│   │   ├── Core/             # GameManager, SessionManager, Configs, RuleCards, Season
│   │   ├── Network/          # NGO, Player, Draft
│   │   ├── Skills/           # 10スキル実装
│   │   ├── Minigames/        # 縮む島 / 爆弾鬼 / しっぽ狩り×2 / 玉入れ
│   │   ├── UI/               # HUD, DraftUI, MainMenu
│   │   └── Tests/            # ユニットテスト
│   ├── ScriptableObjects/    # SkillDefinition, MinigameDefinition 等
│   └── Prefabs/              # Player, Skills, Minigames, UI, VFX
├── Packages/manifest.json    # パッケージ定義
├── ProjectSettings/
├── 仕様書/                     # docx, pdf（v9.5 + v1.0）
├── docs/                     # HANDOFF.md / TODO.md / RESOURCES.md
├── CLAUDE.md                  # Claude 向け開発ガイド
└── README.md                  # このファイル
```

## 開発運用

- ブランチ: `main`（リリース）/ `develop` / `feature-*`
- コミット規約: `feat:` `fix:` `refactor:` `docs:` `test:` `chore:`
- LFS 対象: fbx / png / wav / anim / controller / 動画 / 圧縮系（`.gitattributes` 参照）
- 改行コード: `.gitattributes` の `eol=lf` 準拠。Windows 側は `git config --global core.autocrlf false` 推奨
- **2 クライアントテスト**: ParrelSync で Clone → Host、Main → Client の疎通を毎日実施
- 月 2 回プレイテスト（M2 以降）

## OS 別の落とし穴

### macOS
- Unity は必ずターミナル経由で起動（PATH 継承）
- Claude Code は実体パス `~/Documents/Dev/ChaosDraftParty` で起動（symlink 旧パスは避ける）
- SteamCMD 等の Valve バイナリ初回は Gatekeeper で弾かれる → `xattr -d com.apple.quarantine <path>`

### Windows
- **日本語・空白パス厳禁**（`C:\Dev\ChaosDraftParty` 推奨）
- Windows Defender が `Library/` 書込みを遅らせる → Unity プロジェクト配下を除外設定
- PowerShell 実行ポリシー初回のみ `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`
- `steamcmd` は [Valve 公式 zip](https://developer.valvesoftware.com/wiki/SteamCMD) を `C:\steamcmd\` に展開

詳細は [docs/HANDOFF.md](docs/HANDOFF.md) および [docs/RESOURCES.md](docs/RESOURCES.md) を参照。

## ライセンス

非公開・卒業制作用途。外部アセット（Synty Studios / Mixamo / Meshy AI）はそれぞれのライセンスに従う。
