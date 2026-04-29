# CLAUDE.md — カオス・ドラフト・パーティー開発ガイド

このファイルは Claude Code による自動読込を前提とした、本プロジェクトのハンドオフガイドです。実装判断に迷ったら本書または `ChaosDraftParty_TechSpec_v1.docx`（技術仕様書）を参照してください。

**新規マシンでの開発環境セットアップは [README.md](README.md) を参照**（Unity / MCP / NotebookLM / ターミナル起動ルール）。

---

## プロジェクト概要

**ChaosDraftParty** — 2〜4人オンライン 3D TPS パーティゲーム。

- 1試合8分（4ラウンド × 90秒 + ドラフト5秒 × 4 + リザルト）
- 3試合で1シーズン
- Unity 6 LTS + C# + Netcode for GameObjects + Unity Relay
- 開発：1人 + Claude Code + Unity MCP、2026年4月〜9月（M1-M6）

**コンセプト：**「見た目カオスでも勝つのは上手いやつ」
ミニゲームラッシュ × 軽量スキル蓄積 × カオスルール

---

## 現在の状態

- **フェーズ：M1（4月）**
- 参照仕様：ゲーム設計書 v9.5（`ChaosDraftParty_Spec_v9.5.docx`）+ 技術仕様書 v1.0（`ChaosDraftParty_TechSpec_v1.docx`）
- 評価スコア：面白さ 9.2/10 / 実装現実性 ★3.5/5（サブエージェント9周ループで確定）

---

## 最重要：実装判断の原則

### Must

1. **ホスト権威型ネットワーク**。クライアントは入力送信と補間表示のみ。
2. **決定論物理には手を出さない**。Rigidbody は爆弾などごく最小限。
3. **同期対象は最小化**。見た目はクライアント側で再現。
4. **全バランス値は ScriptableObject / GameConfig に集約**。
5. **ParrelSync で2クライアント動作確認**してからコミット。

### Must Not

1. **スキル無限ネスト、物理連鎖、決定論リプレイ** — 実装コストが高すぎる。
2. **スマホセンサ / VR / モバイル対応** — PC (Steam) 専用。
3. **ホストマイグレーション / 切断再参加** — v10送り。
4. **大規模キャラ自作** — Synty + Mixamo既製品を活用。

---

## 技術スタック

### エンジン・パッケージ

```yaml
Unity: 6 LTS
Render: URP (Universal Render Pipeline)
Network: com.unity.netcode.gameobjects (2.x)
Relay: com.unity.services.relay
Lobby: com.unity.services.lobby
Auth: com.unity.services.authentication
Input: com.unity.inputsystem
Camera: com.unity.cinemachine 3.x
Testing: ParrelSync (GitHub)
```

### 外部アセット

- **Synty Studios** — ローポリ3Dキャラ・ステージ
- **Mixamo** — キャラクターアニメーション（Adobe、無料）
- **Meshy AI Pro**（🟢 2026-04-22 契約済、$20/月 1000 credits） — ステージ小物・装飾 **限定**（キャラには使わない、Community 公開禁止）。MCP `@meshy-ai/meshy-mcp-server` + skill `meshy-dev/meshy-3d-agent` + プロジェクト skill `.claude/skills/meshy-unity-workflow/` で運用。詳細は `docs/RESOURCES.md` と当該 skill 参照

---

## フォルダ構造

```
Assets/
├─ _Project/
│  ├─ Scenes/       # Boot, MainMenu, Lobby, Match, Results
│  ├─ Prefabs/
│  │  ├─ Player/
│  │  ├─ Skills/
│  │  ├─ Minigames/
│  │  ├─ UI/
│  │  └─ VFX/
│  ├─ ScriptableObjects/
│  │  ├─ Skills/           # 10個
│  │  ├─ Minigames/        # 5個
│  │  ├─ RuleCards/        # 6個
│  │  ├─ SeasonRules/      # 3個
│  │  ├─ Characters/       # 4色
│  │  └─ Configs/          # GameConfig
│  ├─ Scripts/
│  │  ├─ Core/             # GameManager, SessionManager, Bootstrap
│  │  ├─ Network/
│  │  ├─ Draft/
│  │  ├─ Skills/
│  │  ├─ Player/
│  │  ├─ Minigames/
│  │  │  ├─ MinigameBase.cs
│  │  │  ├─ ShrinkingIsland/
│  │  │  ├─ BombTag/
│  │  │  ├─ TailHuntBattle/
│  │  │  ├─ TailHuntEscape/
│  │  │  └─ BallThrow/       # ストレッチ
│  │  ├─ RuleCards/
│  │  ├─ Season/
│  │  ├─ UI/
│  │  └─ Utils/
│  ├─ Art/
│  ├─ Audio/
│  └─ Settings/
├─ ThirdParty/     # Synty, Mixamo, Meshy
└─ Plugins/
```

### Assembly Definitions

- `ChaosDraftParty.Core` — 基盤
- `ChaosDraftParty.Network` — NGO関連
- `ChaosDraftParty.Skills` — スキル実行
- `ChaosDraftParty.Minigames` — ミニゲーム実装
- `ChaosDraftParty.UI` — UI
- `ChaosDraftParty.Tests` — テスト

---

## 主要データモデル

### SkillDefinition（10個）

| id | 名前 | cat | act | CD | 効果 |
|---|---|---|---|---|---|
| `dash` | ダッシュ | Move | Active | 8s | 2秒間速度1.8倍 |
| `jump_boost` | 大ジャンプ | Move | Active | 6s | 2.5倍高ジャンプ1回 |
| `grapple` | ロープ引寄 | Move | Active | 10s | 15m先を引寄 |
| `hover` | ホバー | Move | Active | 12s | 空中1.5秒滞空 |
| `pulse` | パルス弾 | Hit | Active | 5s | 前方吹き飛ばし弾 |
| `bomb` | ボム | Hit | Active | 7s | 投擲爆弾1.5秒後爆発 |
| `shield` | 盾 | Hit | Active | 15s | 3秒無敵（速度-30%） |
| `mirage` | ミラクル | Hit | Active | 20s | 敵の移動入力2秒反転 |
| `radar` | レーダー | Sense | Passive | - | 15m内の敵マーカー |
| `revive` | 復活 | Sense | Trigger | 1R | HP0時HP1で踏ん張り |

### 重複強化（凹関数）

```csharp
public static float GetStackMultiplier(int count)
{
    if (count <= 1) return 1.0f;
    if (count == 2) return 1.20f;
    if (count == 3) return 1.35f;
    return 1.45f; // 4+で頭打ち
}
```

### コンボペア（5組）

- `grapple + bomb` → **引き寄せ爆破**：確定命中
- `mirage + shield` → **囮戦術**：ミラクル中被ダメ無効
- `dash + pulse` → **突進弾**：弾速2倍・範囲拡大
- `mirage + grapple` → **閉じ込め**：引き寄せ後操作反転
- `jump_boost + bomb` → **空中爆撃**：頂点でボム投下

### MinigameDefinition（4コア + 1ストレッチ）

1. **縮む島** (`shrinking_island`) — 8×8チェス盤、外周消失、押し出し相撲
2. **爆弾鬼** (`bomb_tag`) — ホットポテト、NPC配達員あり
3. **しっぽ狩り・バトル** (`tail_hunt_battle`) — 色トレイル切断、看板
4. **しっぽ狩り・逃走** (`tail_hunt_escape`) — 燃える導火線、長いと鈍足、看板
5. **玉入れ合戦** (`ball_throw`) — ストレッチ、M4着手

### RuleCardDefinition（6種）

- `shrink_all` — 全員小型化（スケール60%）
- `low_gravity` — 重力70%
- `high_speed` — 速度1.5倍
- `short_cd` — 全スキルCD50%
- `fog_of_war` — 視界15m制限
- `bounce_walls` — 境界反射壁

### SeasonRuleDefinition（3種、シーズン3試合目に発動）

- `winner_takes_all` — 3試合目の勝者がシーズン勝利確定
- `reverse_multiplier` — 下位1.5倍、上位0.8倍
- `double_skill` — 毎ラウンド2スキル取得

---

## State Machines

```
Session
├─ Lobby
├─ SessionStart
├─ MatchInProgress (3回)
│   ├─ WarmUp (10s)
│   ├─ RuleCardReveal (5s)
│   ├─ RoundPlaying (×4)
│   │   ├─ MinigameLoading
│   │   ├─ Countdown (3s)
│   │   ├─ Playing (90s)
│   │   ├─ Ending (3s)
│   │   ├─ Draft (5s)
│   │   └─ Completed
│   └─ MatchResult (15s)
├─ InterMatch
├─ SeasonEnding
└─ SessionEnd
```

---

## ネットワーク同期ポリシー

| 対象 | 方法 | 方向 | 備考 |
|---|---|---|---|
| プレイヤー位置 | ClientNetworkTransform | Owner→All | 補間あり |
| プレイヤーHP | NetworkVariable<int> | S→C | サーバー書込 |
| スキル発動 | ServerRpc → ClientRpc | C→S→C | CD判定はS |
| ドラフト選択 | ServerRpc | C→S | 被り判定 |
| スコア | NetworkVariable<int> | S→C | プレイヤー毎 |
| ラウンド状態 | NetworkVariable<enum> | S→C | StateMachine連動 |
| 爆弾 | NetworkTransform（サーバー権威） | S→C | 物理はホストのみ |
| しっぽセグメント | NetworkVariable<int> + RPC | S→C | 数値のみ同期、見た目はローカル |
| パーティクル | 同期しない | ローカル | RPCでトリガーのみ |

---

## 月次マイルストーン

- **M1（4月）**：NGO + Relay 疎通、プレイヤー移動、縮む島プロト → **2人が同一シーンで動き、床消失が起き、落下判定が同期**
- **M2（5月）**：スキル3種、ドラフトUI、4ラウンド構造 → **2人が4ラウンド完走してスコア確定**
- **M3（6月）**：しっぽ狩りバトル、爆弾鬼、スキル7種 → **ミニゲーム3種ランダムで1試合回る**
- **M4（7月）**：しっぽ狩り逃走、スキル10種、ルールカード4種 → **ほぼ全要素統合、プレイテスト開始**
- **M5（8月）**：シーズンメタ、MVPスロモー、バランス調整 → **3試合通し、MVP表彰まで動く**
- **M6（9月）**：QA、最適化、発表準備、ビルド → **Steamビルド + 外部テスト完了**

**M1末に疎通が動かなければ、ミニゲームを4→2に即削減、NPC削除の判断を即決する。**

---

## 入力マップ

| アクション | Keyboard | Gamepad | |
|---|---|---|---|
| Move | WASD | 左スティック | 移動 |
| Look | Mouse | 右スティック | 視点 |
| Jump | Space | A / ✕ | ジャンプ |
| Skill1 | Q | RB / R1 | スキル1 |
| Skill2 | E | LB / L1 | スキル2 |
| Action | LMB | RT / R2 | 汎用 |
| Draft1/2/3 | 1/2/3 | X/Y/B | ドラフト |

---

## Git運用

- リポジトリ：GitHub プライベート
- ブランチ：`main`（リリース）/ `develop` / `feature-*`
- コミット規約：`feat:` `fix:` `refactor:` `docs:` `test:` `chore:`
- LFS：`.fbx .png .wav .anim .controller .asset` 他バイナリ
- `.gitignore`：`Library/` `Temp/` `Obj/` `Build/` `Logs/`

---

## Claude Code への依頼粒度

**良い例（小さく切る）：**
- 「技術仕様書 7.3 の DashEffect を実装してください。ScriptableObject + ISkillEffect 実装のみ。」
- 「技術仕様書 8.2 の TileManager.ScheduleShrinkage メソッドを書いてください。外周から15秒ごとに消えるロジック。」

**悪い例（大きすぎる）：**
- 「M1の全タスクを実装してください。」
- 「しっぽ狩りを完成させてください。」

### セッション開始時の推奨プロンプト

```
プロジェクトルートの CLAUDE.md を読んで、現在の状況を把握してください。
加えて `docs/ChaosDraftParty_TechSpec_v1.docx` の該当章を参照しながら進めます。
今日のタスクは [具体的タスク] です。
```

---

## Shader / SDF / Visual デバッグの原則（2026-04-29 追加）

### 原則 1: 思い込みで proposal するな、まず実測

shader / SDF / mesh / bone の挙動が想定と違う時:

1. **数値を取る前に修正案を出さない**
2. Unity MCP `execute_code` で:
   - mesh の全 vertex world position
   - vertex の target との距離
   - shader uniform の global 値（`Shader.GetGlobalVector`）
   - SkinnedMR の `bones`, `rootBone`, `bounds.center`, weights
   - Animator の `humanScale`, `bodyPosition`, 各 bone localPos
3. 並列 Agent 3 個で多角検証:
   - **Agent A**: 既存仮説の検証 or 反証（数値ベース）
   - **Agent B**: pixel-level / vertex-level の実態解析
   - **Agent C**: 代替アプローチ網羅（Web 検索、Unity Doc 含む）

### 原則 2: SkinnedMR の bounds.center ≠ mesh の幾何中心

SkinnedMR の bounds は bone weight 単位で計算される。**mesh の見た目の中心ではない**。

判定球 / SDF center を決める時は、`bounds.center` を信じず、vertex の min/max から幾何中心を計算する:

```csharp
var verts = smr.sharedMesh.vertices;
var min = verts[0]; var max = verts[0];
foreach (var v in verts) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
var center = smr.transform.TransformPoint((min + max) / 2f);
```

### 原則 3: bone と mesh の親子関係は別 hierarchy で繋がる

Transform.parent と SkinnedMR.bones は別物。**Transform 上は同じ親でも、SkinnedMR.bones が違う rig を指してたら mesh は別の動きをする**。

例: EGGYBOT v16 では bowl (BodyLower / BodyUpperClear) が `EggybotArmature` の bones に bind されてて、Hand_L/R は `mixamorig:` 系の Transform child。両者は **独立して動く**ので判定球の追従設計を間違えると壊れる。

### 原則 4: 失敗を残せ

うまく行かなかった試行は **memory に記録**。同じ罠を二度踏まない:
- `memory/feedback_*.md`: 失敗パターンと回避策
- `memory/session_*.md`: 試行の流れと教訓
- `.claude/skills/chaosdraftparty-gotchas/SKILL.md`: 罠の追加

詳細: `feedback_shader_pixel_first.md`, `feedback_bone_mesh_hierarchy.md`, `project_eggybot_v16_clip_v16final.md`

### 原則 5: 視覚 / 微調整は user、機械的構築は Claude

bone 位置の最終微調整、色の最終決定、形状の判断は **user**。
rough rig 構築、script 実装、初期値設定、scene 操作は **Claude**。

「不安なところは user、機械的なところは Claude」を堅持。Claude が勝手に視覚デザインを変えない（feedback_ask_before_visual_change.md）。

---

## 既知の技術リスク（対策付き）

| リスク | 影響 | 対策 |
|---|---|---|
| 物理マルチ同期の崩れ | 高 | ホスト権威、物理は爆弾のみ、trigger猶予200ms |
| Synty/Mixamoリギングずれ | 中 | Mixamo標準リグ厳守、独自アニメ作らない |
| NPC配達員バランス | 中 | ランダム歩行+定間隔のみ、AI高度化しない |
| しっぽセグメント同期負荷 | 中 | int+イベントのみ、見た目はローカル補間 |
| ドラフト被りの不公平感 | 中 | 時刻ベース判定、遅い方は次善自動 |
| 3人集まらない | 中 | Bot混合、2人モードを第一級市民扱い |
| 毎日開発が4ヶ月目で崩壊 | 中 | 週末集中型に移行、中間発表で動作物死守 |

---

## 参照ドキュメント

- **ゲーム設計書 v9.5**：`ChaosDraftParty_Spec_v9.5.docx` — ゲームデザイン全体像
- **技術仕様書 v1.0**：`ChaosDraftParty_TechSpec_v1.docx` — 本書の詳細版（API、データ、アルゴリズム）
- **発表用パワポ**：`ChaosDraftParty_Pitch_5min.pptx` — 企画発表資料
- **研究計画版**：`ChaosDraftParty_Research_Full.pptx` — 研究フレーム版

---

## 変更履歴

- **v1.0 (2026-04-21)**：初版。M1開始時点のベースライン。
- **v1.1 (2026-04-29)**：「Shader / SDF / Visual デバッグの原則」セクション追加。EGGYBOT v16 SDF clip 修正セッションの教訓を反映（思い込み禁止、並列 agent + 実測、SkinnedMR bounds.center の罠）。
- 以降、実装進行と共に本書を更新すること。

---

**作成者：ごん（東海大学 情報理工学部 情報メディア学科 小坂研究室）**
