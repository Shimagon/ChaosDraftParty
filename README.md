# ChaosDraftParty

2〜4 人オンライン **3D TPS パーティゲーム**。ミニゲームラッシュ × スキルドラフト × カオスルール。
「見た目カオスでも勝つのは上手いやつ」— Smash Bros 路線。

> 卒業制作（東海大学 情報理工学部 情報メディア学科 小坂研究室）
> Unity 6 LTS + Netcode for GameObjects、2026 年 4 月〜9 月（M1–M6）、1 人制作 + Claude Code + Unity MCP 併用。

## 仕様書・ガイド

- [ゲーム設計書 v9.5](仕様書/ChaosDraftParty_Spec_v9.5.docx) — ゲームデザイン全体
- [技術仕様書 v1.0](仕様書/ChaosDraftParty_TechSpec_v1.docx) — API / データ / アルゴリズム
- [CLAUDE.md](CLAUDE.md) — Claude Code 向け開発ガイド（実装判断の原則）
- [docs/HANDOFF.md](docs/HANDOFF.md) — **別マシン/新セッション引き継ぎ用のノート**（地雷地図・登録済みツール・パッケージバージョン意図）

## 技術スタック

| 領域 | バージョン |
|---|---|
| Unity | **6.4.3f1 LTS** (Apple Silicon) |
| レンダリング | URP 17.0.1 |
| ネットワーク | Netcode for GameObjects 2.11 + Relay 1.2 + Lobby 1.3 + Authentication 3.6 |
| 入力 | Input System 1.19 |
| カメラ | Cinemachine 3.1 |
| アセット | Addressables 2.10、Timeline 1.8、AI Navigation 2.0 |
| テスト | Test Framework 1.4.2 + **ParrelSync 1.5**（OpenUPM）|
| IDE | ide.rider 3.0.27 / ide.visualstudio 2.0.22 |

## 開発環境セットアップ（新規マシン向け）

### 必要ソフト

- [Unity Hub](https://unity.com/download) → **Unity 6.4.3f1** をインストール
  - **必須モジュール**: Windows Build Support (Mono) — Steam 配布ビルド用
- `git` + `git-lfs` （`brew install git-lfs` or [公式](https://git-lfs.com)）
- [`gh` CLI](https://cli.github.com/) — GitHub 認証
- **Unity MCP 用**: Python 3.11+、`uvx`（`brew install uv`）、Node.js 22+
- [Claude Code CLI](https://docs.claude.com/claude-code)

### 手順

```bash
# 1. Clone + LFS
gh repo clone Shimagon/ChaosDraftParty ~/Dev/ChaosDraftParty
cd ~/Dev/ChaosDraftParty
git lfs install --local && git lfs pull

# 2. Unity で開く
#    Unity Hub → "Add project from disk" でこのフォルダを選択
#    → Unity 6.4.3f1 で Open（初回 5〜10 分、Library/ が生成される）

# 3. Claude Code 側に Unity MCP を user scope で登録（一度だけ）
claude mcp add coplay-mcp -s user -t stdio \
  -e MCP_TOOL_TIMEOUT=720000 \
  -- uvx --python ">=3.11" coplay-mcp-server@latest
claude mcp list   # coplay-mcp が ✓ Connected

# 4. Unity 側の MCP Plugin を入れる（プロジェクトごとに 1 回）
#    Unity を起動し、Window → Package Manager → + → "Install package from git URL"
#    URL:  https://github.com/CoplayDev/unity-plugin.git#beta
#
#    完了後、Window → MCP for Unity を開く
#    Transport: stdio → "Auto-Configure Claude Code"
#    Claude バイナリのフルパスを聞かれる → ターミナルで `which claude` の結果を指定

# 5. Input System 有効化
#    Edit → Project Settings → Player → Active Input Handling → "Input System Package (New)"
#    再起動プロンプト → Apply
```

### 重要：Unity はターミナルから起動する

CoplayDev MCP Bridge が `claude` コマンドを呼ぶため、Unity プロセスが PATH を継承している必要があります。Finder からダブルクリック起動すると PATH 未継承で Bridge が繋がりません。

```bash
open -a "Unity Hub"   # Hub から Open project
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
├── CLAUDE.md                  # Claude 向け開発ガイド
└── README.md                  # このファイル
```

## 開発運用

- ブランチ: `main` (リリース) / `develop` / `feature-*`
- コミット規約: `feat:` `fix:` `refactor:` `docs:` `test:` `chore:`
- LFS 対象: fbx / png / wav / anim / controller / 動画 / 圧縮系（`.gitattributes` 参照）
- **2 クライアントテスト**: ParrelSync で Clone → Host、Main → Client の疎通を毎日実施
- 月 2 回プレイテスト（M2 以降）

## ライセンス

非公開・卒業制作用途。外部アセット（Synty Studios / Mixamo / Meshy AI）はそれぞれのライセンスに従う。
