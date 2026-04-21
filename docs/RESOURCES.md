# RESOURCES.md — 外部資源カタログ

> ChaosDraftParty で採用検討する **MCP / Skills / OSS / API** のマスターリスト。
> **Tier S**（即採用）／**A**（1〜2ヶ月以内）／**B**（場面限定）／**C**（検討余地）／**🔴 赤札**（採用禁止）で管理。
> **MacBook（macOS）と デスクトップ PC（Windows）の両方で動くこと** を最低要件に、CLI 例は両 OS 並記。
>
> 更新履歴は末尾。新ツール追加時は Tier 判定と根拠を添えること。

---

## 🟢 Tier S — 即採用

### MCP サーバー

| 名称 | 用途 | 導入時期 |
|---|---|---|
| `CoplayDev/unity-mcp`（導入済） | Unity Editor 操作全般 | 全期間 |
| `anthropic/github-mcp`（導入済） | PR/Issue 管理 | 全期間 |
| `sentry-mcp`（導入済） | エラー監視 | M1〜 |

### Skills

| 名称 | install | 用途 |
|---|---|---|
| `nice-wolf-studio/unity-claude-skills` | `npx skills add https://github.com/nice-wolf-studio/unity-claude-skills -g` | Unity 28skill（`unity-multiplayer` `unity-input` `unity-cinemachine` `unity-data-driven` `unity-testing`） |
| `donchitos/claude-code-game-studios` | `npx skills add https://github.com/donchitos/claude-code-game-studios -g` | 49agent+72skill（`balance-check` `playtest-report` `design-review`） |
| `anthropics/skills@pptx` | `npx skills add https://github.com/anthropics/skills --skill pptx` | 卒論スライド / 中間発表資料 |

### OSS リポジトリ（参考・移植）

| 名称 | URL | 用途 |
|---|---|---|
| KitchenChaosNetcode | https://github.com/OzgenKoklu/KitchenChaosNetcode | NGO 化済み CodeMonkey Kitchen Chaos、**縮む島にゲームループを流用**（M1） |
| Unity Multiplayer Samples Bitesize | https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize | 公式 NGO 2.x サンプル、SO + NetworkVariable 教科書（M1-M2） |
| EGamePlay ★2346 | https://github.com/m969/EGamePlay | SO ベースのスキル・バフシステム、**M2 で fork vendoring** |
| Steamworks.NET | https://github.com/rlabrecque/Steamworks.NET | Steam 実績/クラウドセーブ C# バインディング（M6） |

### API / 外部サービス

| 名称 | 無料枠 | 用途 | 導入時期 |
|---|---|---|---|
| Sentry（`taisei-ai` org） | 5,000 event/月 | クラッシュ+breadcrumb | M1 |
| GameAnalytics | **完全無料・無制限** | DAU、スキル採用率、勝率、バランス根拠 | M2 |
| Discord Webhook | 完全無料 | 試合結果通知 bot | M5 |
| Claude Haiku 4.5 | $1/$5 per 1M tok | RuleCard文言を事前生成（SO焼込） | M2 |
| Groq Llama 3.3 70B | Free 30 req/min | 試合中 MVP 講評（200ms応答） | M5 |
| Poly Haven | CC0 全商用可 | HDRI、PBRテクスチャ | M2 |
| Freesound（CC0 フィルタ） | 無料 | 足音、UIクリック | M3 |
| UGS Relay/Lobby/Auth（採用済） | 50 CCU、50K MAU | 接続基盤 | M1 |

---

## 🟡 Tier A — 時期来たら

### MCP

| 名称 | 導入月 | 用途 |
|---|---|---|
| `IvanMurzak/Unity-MCP` | M2 | `[McpPluginTool]` でC#独自ツール化。ISkillEffect を MCP tool 化 |
| `ahujasid/blender-mcp` ★20.2k | M3 | Synty リグ調整、プロップ量産 |
| `elevenlabs/elevenlabs-mcp` | M3 | SFX + TTS |

### OSS

| 名称 | URL | 導入月 |
|---|---|---|
| TCleard/PredictionReconciliationNetwork | https://github.com/TCleard/PredictionReconciliationNetwork | M3-M4（ラグが出たら） |
| Unity Multiplayer Samples Co-op (Boss Room) | https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop | Lobby→Match 遷移だけ参考（M2） |
| Sentry Unity SDK | https://github.com/getsentry/sentry-unity | M5 |
| Heathen Steamworks Foundation | — | Steamworks.NET ラッパー（M6） |

### API

| 名称 | 料金 | 用途 | 導入月 |
|---|---|---|---|
| Flux Dev (FAL.ai) | $0.025/画像 | スキルアイコン10種、ルールカード6種 | M2 |
| Recraft V3 | $0.04ラスタ / $0.08ベクタ | UIアイコン SVG | M2 |
| Meshy Pro | $20/月 | ステージ小物のみ（**キャラ不可**） | M3 |
| ElevenLabs Starter | $6/月、商用可 | パルス弾/ボム/しっぽ切断 SFX | M3 |
| Stable Audio 2.5 | 25cr 無料、$1M未満は商用可 | ミニゲームBGM ループ | M4 |
| VOICEVOX | 無料 OSS（**キャラ別クレジット必須**） | 日本語アナウンサー | M4 |
| Cartesia Sonic 3 | Free 10K文字/月 | TTS 低レイテンシ | M5 予備 |
| UGS Leaderboards | 50K MAU 無料 | シーズンランキング | M5 |
| Steamworks Direct | $100 買切 | Steam 配信権 | M6 |
| SteamCMD | 無料 | CLI ビルドアップロード | M6 |

### Skills

| 名称 | install | 用途 |
|---|---|---|
| `rmyndharis/antigravity-skills --skill unity-developer` | 上記 | Unity 総合メタ戦略 |
| `obra/superpowers --skill test-driven-development` | `npx skills add ...` | スキル効果式の TDD |
| `jwynia/agent-skills --skill presentation-design` | 同上 | 発表構成レビュー |
| `wshobson/agents --skill unity-ecs-patterns` | 同上 | しっぽセグメント最適化（M4） |

---

## 🔵 Tier B — 条件付き

| 名称 | 条件 |
|---|---|
| `gregkop/sketchfab-mcp-server` | Synty で不足した時のみ CC モデル探索 |
| `meshy-dev/meshy-mcp-server` | Meshy Pro 契約時のみ |
| `MarkusPfundstein/mcp-obsidian` | 個人メモ派なら |
| PostHog | UI ヒートマップ / セッション再生が必要な M5 以降 |
| PlayFab | UGS 超過時の移行候補 |
| Nakama (heroic-labs) | UGS 卒業後の自前ホスト |
| Sketchfab API | 補助アセット検索 |
| Twitch / YouTube Data API | M6 以降の配信連動（時間があれば） |

---

## 🟤 Tier C — 見送り

| 名称 | 理由 |
|---|---|
| CoderGamester/mcp-unity | CoplayDev と機能重複 |
| AnkleBreaker/unity-mcp-server | License `Other` で不明瞭、卒業制作では避ける |
| Figma 公式 MCP | UI 5画面に対して Pro $15/月 割高、手書き＋Claude 口頭で足りる |
| Notion MCP | Issues + CLAUDE.md + TODO.md で代替 |
| Steam Web API（既存 MCP） | 所持ゲーム取得用途のみ、Partner API 非対応 |
| Mixpanel / Amplitude / Firebase GA4 | GameAnalytics で十分 |
| Godot / Unreal MCP | エンジン違い |
| rivet-dev/skills@multiplayer-game | JS/TS+Rapier、NGO 非対応 |
| Vercel 系 skill 全般 | Web 特化、Unity に無関係（v10 でランキング Web 作るなら復活） |

---

## 🔴 赤札 — 採用禁止

| 名称 | 理由 |
|---|---|
| **Suno / Udio** | RIAA / UMG / Sony 訴訟継続、卒業審査で指摘リスク |
| **CSM Cube** | 2026-01-05 シャットダウン済 |
| **X API v2 新規契約** | 2026-02 有料化で費用対効果崩壊、発表は手動投稿で十分 |
| **Meshy をキャラに使用** | CLAUDE.md Must Not、Synty + Mixamo 固定 |
| **Figma / Meshy の Free プラン商用利用** | 帰属表示必須で卒業審査不向き |

---

## 📅 6ヶ月 採用タイムライン（Tier S のみ、月額）

| 月 | 追加 | 月額 |
|---|---|---|
| M1 (2026-04) | UGS、Sentry、GitHub MCP、Unity MCP | ¥0 |
| M2 (2026-05) | +Flux(FAL)、Poly Haven、Claude Haiku事前生成、GameAnalytics | ~$5 |
| M3 (2026-06) | +Meshy Pro、ElevenLabs Starter、Blender MCP | ~$31 |
| M4 (2026-07) | +Stable Audio、VOICEVOX、Recraft V3 | ~$36 |
| M5 (2026-08) | +Groq、UGS Leaderboards、Discord Webhook | ~$36 |
| M6 (2026-09) | +Steam Direct $100 買切、Steamworks.NET、SteamCMD | ~$136 |

**6ヶ月合計 ≈ $180（約27,000円）**

---

## 🖥 セットアップ（macOS / Windows 両対応）

### 前提ツール（両 OS）

| ツール | macOS | Windows |
|---|---|---|
| Unity Hub | `brew install --cask unity-hub` | `winget install Unity.UnityHub` |
| Node.js (nvm) | `brew install nvm` | [nvm-windows](https://github.com/coreybutler/nvm-windows) |
| Python 3.12+ | `brew install python@3.12` | `winget install Python.Python.3.12` |
| uv | `curl -LsSf https://astral.sh/uv/install.sh \| sh` | `powershell -c "irm https://astral.sh/uv/install.ps1 \| iex"` |
| Git + LFS | `brew install git git-lfs` | `winget install Git.Git GitHub.GitLFS` |
| Claude Code | `npm i -g @anthropic-ai/claude-code` | 同左（nvm-windows 経由） |
| gh CLI | `brew install gh` | `winget install GitHub.cli` |

### プロジェクト clone（両 OS）

```bash
# macOS
cd ~/Documents/Dev
git clone https://github.com/Shimagon/ChaosDraftParty
cd ChaosDraftParty
git lfs install --local && git lfs pull
```

```powershell
# Windows（PowerShell、ASCII 英字パス推奨）
cd C:\Dev
git clone https://github.com/Shimagon/ChaosDraftParty
cd ChaosDraftParty
git lfs install --local ; git lfs pull
```

### Unity Hub からプロジェクトを開く

- **macOS**: `open -a "Unity Hub"` →「Open」→ `~/Documents/Dev/ChaosDraftParty`
- **Windows**: スタートメニューから Unity Hub →「Open」→ `C:\Dev\ChaosDraftParty`
- **どちらも Finder / Explorer のダブルクリックで起動しない**（PATH 継承されず MCP が繋がらない）

### MCP for Unity の Claude Code 登録

```bash
# macOS（実体パスで）
cd ~/Documents/Dev/ChaosDraftParty
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list | grep UnityMCP
```

```powershell
# Windows
cd C:\Dev\ChaosDraftParty
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
claude mcp list | Select-String UnityMCP
```

### `which claude` 相当

```bash
# macOS
which claude
# → /Users/taisei/.nvm/versions/node/v22.22.0/bin/claude
```

```powershell
# Windows
where.exe claude
# → C:\Users\taisei\AppData\Roaming\nvm\v22.22.0\claude.cmd など
```

### Skills 一括インストール

両 OS 共通（`npx` があれば動く）：

```bash
npx skills add https://github.com/nice-wolf-studio/unity-claude-skills -g
npx skills add https://github.com/donchitos/claude-code-game-studios -g
npx skills add https://github.com/anthropics/skills --skill pptx
```

---

## ⚠️ Windows 特有の注意

1. **パスに日本語・空白を入れない**。`C:\Dev\ChaosDraftParty\` 推奨。macOS より事故りやすい
2. **改行コード**：`.gitattributes` の `eol=lf` に従う。`git config --global core.autocrlf false` 推奨
3. **Windows Defender が `Library/` 書込みを遅らせる** → Unity プロジェクト配下を除外設定に追加
4. **LFS の既存ファイル**：clone 後に必ず `git lfs pull`
5. **Unity Editor の初回起動は 10 分以上かかることあり**。`Library/` が生成されるまで待つ
6. **`steamcmd` は Valve 公式 zip** を `C:\steamcmd\` に展開して PATH 通す
7. **PowerShell 実行ポリシー**：初回のみ `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

---

## ⚠️ macOS 特有の注意

1. **Unity は必ずターミナル経由で起動**（Finder ダブルクリックは PATH 継承されず MCP 失敗）
2. **Rosetta 要否**：Unity 6.4.3f1 は Apple Silicon ネイティブ、Rosetta 不要
3. **Gatekeeper**：SteamCMD 等の Valve バイナリは初回 `xattr -d com.apple.quarantine` が必要な場合あり
4. **symlink 経由パス注意**：Claude Code は **実体パス `~/Documents/Dev/ChaosDraftParty` で起動**（旧パス `~/Documents/課題/やりたいこと/卒業制作` は symlink、メモリの分離が起きる）

---

## 🔄 更新ルール

- 新ツール検討 → Tier 判定して本ファイルに追加
- Tier 変更（例：B→A 昇格） → 日付 + 根拠を追記
- **MCP は必ず `~/.claude.json` / `%USERPROFILE%\.claude.json` を両マシンで同期**（手動コピー or 設定ファイルの git 管理）
- 赤札の追加は慎重に（理由が変わることがある、例：訴訟終結、無料枠復活等）

---

**最終更新**：2026-04-21（初版、M1 時点）
