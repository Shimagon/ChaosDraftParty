# HANDOFF.md — Claude Code 引き継ぎメモ

> 別マシン（デスクトップ PC）や新セッションで Claude Code が開発を再開する時に、**これまでに学んだこと・決めたこと・避けたい地雷**を全部渡すためのノート。
> README.md / CLAUDE.md / 仕様書 を補完する位置づけ。随時更新。

## 0. 最優先で守るルール（命綱）

1. **GUID 崩壊を絶対に起こさない**。マテリアルがピンク化したり、キャラの色が違ったりする「取り返しのつかない」事故が過去にあった。[詳細](#3-アセット参照崩壊の防止アセット地雷地図) 必読。
2. **このプロジェクト内の操作は事前承認済み**。確認プロンプトで止まらない。ただし **git push・外部発信・破壊的共有操作** は都度確認。
3. **MCP / Skills / サブエージェントをフル活用**。使えるものを使わずに自前実装しない。
4. **精度重視で plan モード**。非自明な実装・アーキ判断の前には必ず Plan → 合意 → 実装。
5. **常時ウルトラシンキング ON**、迷うなら深く考える側に倒す。
6. **複数マシン開発**（MacBook + デスクトップ）のため **push 徹底**、ローカル生成物だけ `.gitignore`。

## 1. プロジェクト固定情報

| 項目 | 値 |
|---|---|
| プロジェクト | ChaosDraftParty（卒業制作、東海大学 情報理工学部 小坂研究室） |
| 期間 | 2026-04 〜 2026-09 / M1–M6 |
| 現フェーズ | **M1（2026-04）** |
| リポジトリ | **`Shimagon/ChaosDraftParty`**（GitHub private、HTTPS origin） |
| ローカルパス | `~/Documents/Dev/ChaosDraftParty/`（旧パス `~/Documents/課題/やりたいこと/卒業制作/` は symlink） |
| Unity | **6.4.3f1**（Apple Silicon） |
| 使用 IDE | VSCode / Rider / Claude Code |

## 2. 環境セットアップの既知の地雷

### パスは英字ディレクトリに置く
初期は `~/Documents/課題/やりたいこと/卒業制作/` 配下にあった。Unity 6 は大部分の日本語パスを通すが、Platform Switch や Build 時に稀に非 ASCII でトラブるため `~/Documents/Dev/ChaosDraftParty/` に移動。旧パスは symlink で残存。

### Unity はターミナルから起動する（重要）
MCP for Unity の Bridge が `claude` コマンドを呼ぶので、Unity プロセスは PATH を継承している必要がある。Finder から起動すると PATH 未継承で繋がらない。

```bash
open -a "Unity Hub"      # Hub からプロジェクトを Open
```

### パッケージバージョンは Unity 6.4 テンプレ基準
Unity 6.0 テンプレの `com.unity.inputsystem@1.12.0` は Unity 6.4 では `BuildTarget.ReservedCFE` が解決できずコンパイル失敗する。**Input System は 1.19.0 以上**にする。URP/ugui/test-framework も Unity 6 内部レジストリ解決がある（packages.unity.com だけ見ると古いのが出るので注意）。

### MCP Bridge は `CoplayDev/unity-mcp` が正解、`CoplayDev/unity-plugin` ではない
- `CoplayDev/unity-plugin` = **Coplay AI Copilot**（プレミアム商用、ログイン要、別物）❌
- **`CoplayDev/unity-mcp`** = **MCP for Unity**（MIT、無料、本命）✅
- 正しい git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
- Unity package name: `com.coplaydev.unity-mcp`

## 3. アセット参照崩壊の防止（アセット地雷地図）

**ユーザーが過去の Unity プロジェクトで「キャラの色が違う／ピンク化／取り返しのつかない」事故を経験している**。これを絶対に繰り返さない。

### 毎コミット前チェック

1. `.meta` ファイルは必ずコミット。`*.meta` を `.gitignore` しない。
2. `Packages/packages-lock.json` をコミット（マシン間のパッケージ解決結果固定）。
3. `ProjectSettings/` を丸ごとコミット（特に GraphicsSettings / QualitySettings / EditorSettings / URPGlobalSettings の参照）。
4. Assets 直下の Unity 自動生成アセットとその `.meta` をコミット：
   - `UniversalRenderPipelineGlobalSettings.asset`
   - `DefaultVolumeProfile.asset`
   - `DefaultNetworkPrefabs.asset`
5. EditorSettings は Unity 6 デフォルトを維持：
   - Asset Serialization Mode: **Force Text**（`m_SerializationMode: 2`）
   - Line Endings for New Scripts: **Unix** (`m_LineEndingsForNewScripts: 1`)
   - Version Control Mode: **Visible Meta Files**
6. `.gitattributes` で YAML 系は `text merge=unityyamlmerge eol=lf`（設定済）、バイナリは LFS（設定済）。
7. URP / Render Pipeline Asset の差し替えは「変更 → 確定 → すぐコミット」。中途半端な状態で push しない。
8. Script 移動・リネーム・asmdef 再編は **Unity Editor 上で操作**。Finder/`mv` は GUID を壊す。
9. `git push` 前に `git status --short` で対称性チェック — `Foo.cs` があるなら `Foo.cs.meta` もコミット。フォルダ新設なら `Bar.meta` も追加されているはず。

### 痛い症状と対処

| 症状 | 原因 | 対処 |
|---|---|---|
| Material がピンク（Missing Shader）| Shader GUID 参照切れ | **まず `Library/` を消して Unity 再起動** → 大抵直る。ダメなら `.meta` 漏れ疑う |
| キャラの色が違う | Material→Texture 参照切れ / Import Setting 差分 | `.meta` の TextureImporter セクションを `git log -p` で確認 |
| Component が `None` | Script GUID ズレ（asmdef 移動、Script 削除→復活） | Script `.meta` を GUID ごと戻すか、シーン側で再割当 |
| 起動時に大量のコンパイルエラー | manifest.json と packages-lock.json の不整合 | 両方同時に push、pull したら両方 pull してから Unity 起動 |
| Post Processing が効かない | Default Volume Profile GUID 変化/未コミット | `Assets/DefaultVolumeProfile.asset` と `.meta` の対をチェック |

## 4. 登録済みツール / リソース

### GitHub
- Repo: https://github.com/Shimagon/ChaosDraftParty (private)
- HTTPS origin 経由で push、gh CLI は `Shimagon` アカウント認証済み（scope: `gist`, `read:org`, `repo`）

### NotebookLM 開発支援ノートブック
- 名前: `ChaosDraftParty - 開発支援`
- ID: `528f0394-38e2-4650-b3ed-8660b5c8cc08`
- URL: https://notebooklm.google.com/notebook/528f0394-38e2-4650-b3ed-8660b5c8cc08
- 登録済ソース: CLAUDE.md / Spec v9.5.docx / TechSpec v1.docx
- 使い方: `notebooklm ask "..." --notebook 528f0394-38e2-4650-b3ed-8660b5c8cc08`
- 追加予定ソース: Unity 6 LTS 公式 docs、NGO 2.x docs、Unity Relay+Lobby Boss Room、Synty/Mixamo リターゲット

### Unity MCP Bridge
- GitHub: `CoplayDev/unity-mcp` (MIT, 8.7k★)
- Unity package: `com.coplaydev.unity-mcp` v9.6.7-beta.5+（main ブランチ）
- git URL: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
- Claude Code 登録: **local scope（project limited）、HTTP transport**
  - コマンド: `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp`
  - 確認: `claude mcp list | grep UnityMCP`
- Unity 側起動: `Window → MCP for Unity → Start Server`（HTTP 127.0.0.1:8080）
- Install Skills 実行で `~/.claude/skills/unity-mcp-skill` インストール済み → Claude Code セッションの skills リストに `unity-mcp-skill` が出る

**新マシンでのセットアップ**：
1. Unity で `Window → Package Manager → + → git URL` で `com.coplaydev.unity-mcp` を入れる
2. `Window → MCP for Unity → Start Server`
3. Claude Code 側で `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp`
4. `Install Skills` ボタンで skill もインストール
5. **Claude Code を 1 回再起動** — 既存セッションには新 MCP が反映されない

### ⚠️ Unity MCP の既知の罠（2026-04-21 学習）

1. **`Configure All Detected Clients` ボタンは前の登録を消すことがある**。初回だけ使う。以降は個別の「Configure」ボタンか `claude mcp add` を使う。
2. **登録が消えたら再登録**：
   ```bash
   cd ~/Documents/Dev/ChaosDraftParty
   claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
   ```
3. **Unity UI の 🔴 Not Configured 表示は遅延する**。実際 `claude mcp list` で `UnityMCP: ... - ✓ Connected` なら機能的に繋がっている。UI を閉じて開き直せば更新される。
4. **新規 MCP 登録後は Claude Code 再起動必須** — 既存セッションには反映されず、`mcp__UnityMCP__*` tools が見えない。
5. **MCP server エントリは `~/.claude.json` の `projects['<絶対実体パス>'].mcpServers`** に書かれる。symlink 経由でアクセスしたときと実体パスでアクセスしたときで claude mcp list の表示が変わるので、**常に実体パス（`~/Documents/Dev/ChaosDraftParty`）で Claude Code を起動する**。

### 他 MCP（user scope、全プロジェクトで有効）
- `github` / `sentry` / `semgrep` / `circleci` / `playwright` / `vercel` / Google (Gmail/Calendar/Drive)

## 5. 現在のパッケージバージョン（manifest.json）

Unity 6.4 テンプレ + 2026-04 時点の安定版で決定。変えるときは互換性確認：

| Package | Version | 備考 |
|---|---|---|
| com.unity.addressables | 2.10.1 | |
| com.unity.ai.navigation | 2.0.0 | 爆弾鬼 NPC 用 |
| com.unity.cinemachine | 3.1.6 | |
| com.unity.inputsystem | **1.19.0** | **1.12.0 は Unity 6.4 で BuildTarget.ReservedCFE 失敗** |
| com.unity.multiplayer.center | 1.0.1 | |
| com.unity.netcode.gameobjects | 2.11.0 | |
| com.unity.render-pipelines.universal | 17.0.1 | Unity 6 internal registry 解決 |
| com.unity.services.authentication | 3.6.1 | |
| com.unity.services.core | 1.16.0 | |
| com.unity.services.lobby | 1.3.0 | |
| com.unity.services.relay | 1.2.0 | |
| com.unity.test-framework | 1.4.2 | |
| com.unity.timeline | 1.8.6 | |
| com.unity.ugui | 2.0.0 | Unity 6 internal |
| com.unity.ide.rider | 3.0.27 | |
| com.unity.ide.visualstudio | 2.0.22 | |
| com.coplaydev.unity-mcp | https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main | MCP Bridge |
| com.veriorpies.parrelsync | 1.5.2 | OpenUPM、2 クライアントテスト |

## 6. M1（4月末）までの到達目標

**縦スライス**：2 人が Relay 経由で繋がり、縮む島ステージで動き回り、1 ラウンド完走、スコア確定。

これが動かなければ、ミニゲームを 4→2 に即削減、NPC 削除の判断を即決する。

## 7. 読解中に拾った実装論点（CLAUDE.md 補足）

| # | 論点 | 決着 or 要決定 |
|---|---|---|
| 1 | ミニゲーム尺の表記ブレ | TechSpec 8.2.1/8.5.1 の「2分」は誤記、**全ミニゲーム 90 秒**（Spec v9.5 / MinigameDefinition.durationSeconds=90 が真）|
| 2 | Transform 同期の権威 | **ClientNetworkTransform（Owner権威）**で着手、縮む島の落下判定のみサーバー検証 |
| 3 | ドラフト被り時の次善割当 | 時刻ベース早い者優先、遅い方は **残り候補からランダム 1 個**（v9.5 準拠） |
| 4 | Revive の「1R 1回」 | Round 開始時に `usedThisRound=false` でリセットするカウンタ実装 |
| 5 | MVP スロモーの VFX/SE | Transform + animState のみ記録、SE は別途一発再生（v10 拡張余地） |
| 6 | しっぽの補間元 | 自分のは transform.position を Enqueue、他プレイヤーは ClientNetworkTransform 受信位置履歴 |
| 7 | Bot 混合 | M6 近辺。縮む島で先に CPU ダミー 1 体歩き回るレベル実装しておくとテスト楽 |

## 8. ディレクトリ構造（意図）

```
~/Documents/Dev/ChaosDraftParty/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Core/        (asmdef: ChaosDraftParty.Core)
│   │   │   ├── Network/     (asmdef: ChaosDraftParty.Network)
│   │   │   ├── Skills/      (asmdef: ChaosDraftParty.Skills)
│   │   │   ├── Minigames/   (asmdef: ChaosDraftParty.Minigames)
│   │   │   ├── UI/          (asmdef: ChaosDraftParty.UI)
│   │   │   └── Tests/       (asmdef: ChaosDraftParty.Tests)
│   │   ├── ScriptableObjects/   # Skills/Minigames/RuleCards/SeasonRules/Characters/Configs
│   │   ├── Prefabs/             # Player/Skills/Minigames/UI/VFX
│   │   ├── Scenes/              # Boot, MainMenu, Lobby, Match, Results
│   │   ├── Art/ Audio/ Settings/
│   ├── ThirdParty/              # Synty / Mixamo / Meshy
│   └── Plugins/
├── Packages/manifest.json / packages-lock.json
├── ProjectSettings/
├── docs/HANDOFF.md   ← このファイル
├── 仕様書/           # Spec v9.5 / TechSpec v1 / 旧 PDF
├── CLAUDE.md         # 開発ガイド（Claude 自動読込）
└── README.md         # 新規マシン向けセットアップ
```

## 9. 更新ルール

- 重要な判断・地雷・ツール追加があれば **このファイルを必ず更新**。
- 更新は Claude Code の memory にも反映。逆もまた真なり（memory の feedback / reference を HANDOFF.md に同期）。
- このファイルを読めば「新セッションで文脈 0 から走り始められる」状態を維持する。

---
**最終更新**: 2026-04-21（M1 開始、Unity 環境完成、MCP 接続確認）
