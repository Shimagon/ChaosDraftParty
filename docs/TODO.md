# TODO.md — ChaosDraftParty タスク管理

> 粒度：**1タスク = 1コミット or 1プレイテスト単位**。DoD（完了条件）を必ず書く。
> 着手時 `[ ]` → 進行中 `[~]` → 完了 `[x]`、ブロック `[!]`、廃止 `[-]`。
> 仕様根拠は `Spec v9.5` / `TechSpec v1` の章番号で参照。

---

## 現在のフェーズ：M1（2026-04）

**M1 縦スライスゴール**：2人が Relay 経由で繋がり、縮む島ステージで動き回り、床消失が起き、落下判定が同期し、1ラウンド完走してスコア確定する。

**M1 末デッドライン：2026-04-30**（動かなければミニゲームを 4→2 削減、NPC 削除を即決）

---

## M1 タスク

### 0. 基盤（完了分）

- [x] Unity 6.4.3f1 プロジェクト作成
- [x] URP / Input System 1.19 / NGO 2.11 / Relay 1.2 / Lobby 1.3 / Auth 3.6 / Cinemachine 3.1 / ParrelSync 1.5 導入
- [x] フォルダ骨格（_Project/Scripts, Scenes, Prefabs, SO, Art, Audio, Settings）
- [x] Git + LFS + `.gitattributes` / `.gitignore` 設定
- [x] MCP for Unity 接続（HTTP 127.0.0.1:8080）
- [x] NotebookLM ノートブック作成（ID `528f0394`、3ソース登録）
- [x] HANDOFF.md / CLAUDE.md / README.md 整備

### 1. asmdef と Core 基盤

- [x] **asmdef 6枚作成**（Core / Network / Skills / Minigames / UI / Tests）
  - 初回 commit `c5cba93` で既存確認（2026-04-22）、UI→Core / Skills→Core+Network / Minigames→Core+Network+Skills / Tests→全依存
  - Core に UGS + NGO + Cinemachine + Addressables の refs 込み
- [x] **`GameConfig.cs`**（ScriptableObject）— 全バランス値集約
  - `Assets/_Project/Scripts/Core/Configs/GameConfig.cs` + `Assets/_Project/ScriptableObjects/Configs/GameConfig.asset` (GUID d4705ac6..)
  - 含む：移動/ジャンプ/重力、マッチ/ラウンド/ドラフト/ウォームアップ尺、凹関数スタック倍率 (1.0→1.20→1.35→1.45)、triggerGraceMs=200
  - `GetStackMultiplier(count)` / `GravityAcceleration` プロパティ済
- [ ] **`GameManager.cs`**（DontDestroyOnLoad、Singleton）
  - シーン遷移、セッション状態（Lobby / MatchInProgress / Result）保持
  - DoD：Boot→MainMenu→Lobby の遷移がキー操作で動く
- [ ] **`Bootstrap.cs`**（Bootシーンのエントリーポイント）
  - Unity Services 初期化 → Auth サインイン → MainMenu へ
  - DoD：Unity 起動 → Console に `[Boot] Authenticated as <playerId>` が出る
- [~] **Skills モジュール基盤**（M2 先取り）
  - `Skills/SkillDefinition.cs` — SO、id/displayName/category/activation/cooldownSec/magnitude/icon/vfx/sfx
  - `Skills/ISkillEffect.cs` + `SkillContext` struct — サーバー権威発動 IF
  - まだ 10 スキル個別 SO インスタンスは未作成、M2 で追加

### 2. シーン骨格

- [ ] **Boot シーン**（Bootstrap 配置のみ）
- [ ] **MainMenu シーン**（Host / Join ボタン、Quit）
- [ ] **Lobby シーン**（プレイヤーリスト表示、全員 Ready → マッチ開始）
- [ ] **Match シーン**（ミニゲームが読み込まれる親シーン、Additive ロード想定）
- [ ] **Results シーン**（最終スコア表示）
- [ ] `Build Settings → Scenes In Build` に 5シーン登録、順序固定

### 3. Network / Relay / Lobby

- [ ] **`RelayManager.cs`**（Host: Relay allocation 作成 / Join: code で接続）
  - API：`StartHostAsync()` / `StartClientAsync(string joinCode)`
  - DoD：Host が生成した JoinCode を Join 側に手渡しで入れると NGO `OnClientConnectedCallback` が発火
- [ ] **`LobbyManager.cs`**（Unity Lobby Service ラッパー）
  - DoD：Host が Lobby 作成 → Client が Lobby ID でJoin → プレイヤー一覧が同期
  - ※ M1 では Lobby UI より Relay 直結を優先。Lobby は M2 でも可。
- [ ] **`NetworkManager` prefab** 配置（NGO transport = UnityTransport、Relay 対応）
- [ ] **ParrelSync でクローン作成**、2クライアント起動確認
  - DoD：Host = オリジナル、Client = クローンで Relay 接続が通る

### 4. Player

- [ ] **`PlayerCharacter` プレハブ**（Synty のカプセル代用 or 簡易 capsule でOK）
  - 含む：`NetworkObject` / `ClientNetworkTransform` / `PlayerController.cs` / `PlayerInput`
  - DoD：クライアント側で自分のキャラが動く、相手のキャラが補間表示される
- [ ] **`PlayerController.cs`**（WASD 移動、Space ジャンプ、Q/E スキル枠）
  - Input System の `PlayerInput` 経由、Owner のみ入力を読む
  - DoD：2クライアントで互いの移動が見える、ガタつかない
- [ ] **Cinemachine 3rd Person カメラ**
  - Owner のみにカメラ追従、他プレイヤーには無効
  - DoD：Host/Client 各画面で自キャラ追従カメラ

### 5. 縮む島（最初のミニゲーム）

- [~] **縮む島プロトシーン**（視覚確認済、2026-04-21）
  - `Assets/_Project/Scenes/ShrinkingIslandProto.unity` に 8×8 チェス盤タイル + Player + カメラ/ライト配置、URP Asset 動作確認
  - `execute_code` 経由で 64 タイル + 縮小デモ実装済（1 リング削除で 64→16）
  - まだ `TileManager.cs` クラス化されていない、次のタスクで正式化
- [ ] **`TileManager.cs`**（8×8 タイル生成、外周から消失）
  - 参照：TechSpec 8.2、CLAUDE.md State Machines
  - メソッド：`Initialize()` / `ScheduleShrinkage(float interval)` / `RemoveTile(int x, int z, [ServerRpc])`
  - DoD：サーバーで15秒ごとに外周1列消失、クライアントで同期表示
- [ ] **落下判定**（`y < -5` で HP0、サーバー権威、trigger猶予200ms）
  - DoD：タイル外に落ちた瞬間、全クライアントで「脱落」表示とスコア反映
- [ ] **`ShrinkingIslandMinigame.cs`**（`MinigameBase` 継承、開始/終了/勝者判定）
  - DoD：最後の1人 or 制限時間切れで終了、スコア +1 が NetworkVariable で同期
- [ ] **ラウンド時間 90秒、カウントダウン 3秒、Ending 3秒**
  - 参照：CLAUDE.md State Machines

### 6. 最小 UI

- [ ] **HUD**：自 HP / ラウンド残り時間 / スコア一覧
- [ ] **Result 表示**：ラウンド終了時の「勝者: XXX」バナー
- [ ] DoD：UI が 2 クライアント両方に正しく出る、ずれない

### 7. M1 疎通テスト（縦スライス検証）

- [ ] **ParrelSync 2クライアント起動 → Host/Client 接続 → 縮む島 1ラウンド完走**
- [ ] **10回連続で落ちずに完走できる**
- [ ] **スクリーンキャプチャ／録画で中間発表の素材化**
- [ ] **M1 達成 or 削減判断**（4/30 判定：ミニゲーム 4→2、NPC 削除の要否）

---

## M2-M6 アウトライン（詳細は各月頭で分解）

### M2（2026-05）：スキル3種 + ドラフト + 4ラウンド構造
- Skill SO × 3（`dash` / `pulse` / `shield`）
- `SkillRegistry.cs` / `ISkillEffect` インターフェース / CD 管理
- `DraftManager.cs`（5秒ドラフト、時刻ベース被り判定）
- 4ラウンド構造（WarmUp → RuleCardReveal → Round×4 → MatchResult）

### M3（2026-06）：ミニゲーム2種追加 + スキル7種
- `TailHuntBattle`（しっぽ狩りバトル、色トレイル）
- `BombTag`（爆弾鬼、NavMesh NPC 配達員）
- Skill 追加：`jump_boost` / `grapple` / `hover` / `bomb`
- コンボペア判定（5組、重複スタック 1.0→1.20→1.35→1.45）

### M4（2026-07）：ミニゲーム4種完成 + ルールカード + スキル10種
- `TailHuntEscape`（逃走、燃える導火線）
- Skill 追加：`mirage` / `radar` / `revive`
- RuleCard 4種（`shrink_all` / `low_gravity` / `high_speed` / `short_cd`）
- プレイテスト開始（外部 2〜3 人呼ぶ）

### M5（2026-08）：シーズンメタ + MVPスロモー + バランス
- SeasonRule 3種、3試合通し
- MVP スロモー（Transform + animState 記録 → 再生）
- RuleCard 6種化、バランス調整ラウンド
- `BallThrow`（ストレッチ、可能なら）

### M6（2026-09）：QA + 最適化 + Steam ビルド + 発表
- プロファイル／最適化
- Steam ビルド（Windows / Mac）
- 外部テストラウンド
- 研究発表資料仕上げ

---

## ブロッカー / 要判断

（発生したらここに書く）

---

## 関連ドキュメント

- **[docs/HANDOFF.md](HANDOFF.md)** — 地雷地図・登録済みツール・パッケージ意図
- **[docs/RESOURCES.md](RESOURCES.md)** — MCP/Skills/OSS/API の Tier S/A/B/C 分類、6ヶ月採用タイムライン、macOS/Windows 並記セットアップ
- **[README.md](../README.md)** — 新規マシン向け両 OS セットアップ手順

## 既知の罠（毎作業前に一瞥）

参照：`docs/HANDOFF.md` の該当章

1. `.meta` / `packages-lock.json` / ProjectSettings は毎コミット対称性チェック
2. Unity はターミナルから `open -a "Unity Hub"` で起動（PATH 継承）
3. Claude Code は実体パス `~/Documents/Dev/ChaosDraftParty` で起動（symlink 旧パス非推奨）
4. MCP for Unity の UI が 🔴 Not Configured でも `claude mcp list` で ✓ なら機能的に OK
5. 新規 MCP 登録後は Claude Code 再起動必須
6. 物理は爆弾のみ、決定論リプレイには手を出さない
7. Script 移動は Unity Editor 上で（Finder/`mv` は GUID 破壊）

---

## 更新ルール

- タスク完了 → その場で `[x]` に更新してコミット（別コミット可）
- 新規タスク発見 → 関連セクションに追記
- フェーズ切替（M1→M2 等）→ 見出しを「現在のフェーズ」に昇格
- M1 末（4/30）に本ファイルを総点検、削減判断を記録

**最終更新**：2026-04-21（初版）
