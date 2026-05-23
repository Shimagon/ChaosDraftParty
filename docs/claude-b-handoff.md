# Claude B (2 つ目 Claude) ハンドオフ

> このファイルは **2 つ目の Claude (Claude B)** が読み込んで作業を開始するための指示書。
> Claude A (この session、main worktree) と並列で動く想定。

---

## 🎯 役割分担 (Claude A との切り分け)

| | Claude A (main) | **Claude B (このあなた、stage worktree)** |
|---|---|---|
| **担当** | Player / Camera / VFX / Foot / 表情 ← User が頻繁試遊する部分 | ステージ装飾 / Asset 取込 / Skill 周辺 / docs / test |
| **branch** | `main` (or `develop`) | `feat/stage-*` |
| **試遊頻度** | User が直接見て調整するもの | User 待ち時間中に独立で進められるもの |
| **MCP** | Unity Editor instance 1 | Unity Editor instance 2 (別 Library) |

## 📍 起動時のセットアップ

```powershell
# あなたは既に C:\Users\taise\Documents\GitHub\ChaosDraftParty-stage で起動されている想定
# 1. branch 確認
git branch --show-current   # feat/stage-* のはず

# 2. memory 読み込み (User account 共有なので Claude A と同じ MEMORY.md)
#    Path: C:\Users\taise\.claude\projects\C--Users-taise-Documents-GitHub-ChaosDraftParty\memory\MEMORY.md
#    全 feedback_*.md を確認すること (絶対ルール)

# 3. MCP Unity instance を pin
#    set_active_instance Name@<stage-hash>
#    (Claude A の instance を間違って触ると Editor 競合する)
```

## 🔒 守るべき絶対ルール (Claude A と共通、memory 永続化済)

| ルール | 出典 |
|---|---|
| **visual / camera / VFX は AAA 動画解析 deep dive 無しに実装禁止** | `feedback_visual_aaa_deep_dive_mandatory.md` (2026-05-23、最新) |
| **新機能着手前に必ず心理学・業界 defacto・プロ視点を並列調査** | `feedback_research_psychology_industry_pro_view_mandatory.md` |
| **AI review loop は commit 前に絶対省略しない** | `feedback_ai_review_loop_mandatory.md` |
| **計画 → 計画 review → 実装 → 実装 review を OK まで反復** | `feedback_iterative_plan_review_loop_mandatory.md` |
| **AskUserQuestion / yes 確認は機械的判断ではやめる** | `feedback_autonomous_no_confirmation.md` (User 「全部許可するから俺に聞かないで」) |
| **視覚 / 体感 / ゲーム性 / 予算判断のみ User、機械は autonomous** | 同上 |
| **AAA defacto を積極的に盗む (Genshin / FF14 / モンハン / Hat in Time)** | `feedback_aaa_emulation_priority.md` |
| **思い込み禁止、まず実測** | `feedback_shader_pixel_first.md` |
| **agent の警告 (「過剰の懸念」「酔いリスク」) を無視しない** | `feedback_visual_aaa_deep_dive_mandatory.md` |

**特に重要**: User は最近 visual / camera で 2 回不満を出した (Day 4/5 Particle + Camera Impulse)。**deep dive 無しに visual 系を触らない**。Claude A が Camera 問題を解決中なので、Claude B は **Camera 触らない**。

## 📂 担当範囲 (file レベル、Claude A と非衝突)

### Claude B が触って OK
```
Assets/_Project/Prefabs/Stage/         ← ステージ装飾 prefab
Assets/_Project/Scenes/Stage_*.unity   ← ミニゲーム scene (新規作成)
Assets/_Project/ThirdParty/            ← Synty / Kenney / Meshy asset 取込
Assets/_Project/ScriptableObjects/Skills/    ← Skill 10 個の SO 定義
Assets/_Project/ScriptableObjects/Minigames/ ← Minigame 5 個の SO 定義
Assets/_Project/ScriptableObjects/RuleCards/ ← RuleCard 6 個の SO 定義
Assets/_Project/Scripts/Skills/        ← Skill 実装 (新規)
Assets/_Project/Scripts/Minigames/     ← Minigame 実装
Assets/_Project/Scripts/RuleCards/     ← RuleCard 実装
docs/                                  ← ドキュメント全般
Tests/                                 ← EditMode / PlayMode test
```

### Claude B が触らない (Claude A 専属)
```
Assets/_Project/Scripts/Core/EggybotFaceAnimator.cs
Assets/_Project/Scripts/Core/EggybotFootGroundClamp.cs
Assets/_Project/Scripts/Core/EggybotSquashStretch.cs
Assets/_Project/Scripts/Core/EggybotBodyDip.cs
Assets/_Project/Scripts/Core/EggybotPoseLock.cs
Assets/_Project/Scripts/Core/EggybotDashEffect.cs    ← Camera 系
Assets/_Project/Scripts/Core/EggybotMovementVFX.cs   ← VFX
Assets/_Project/Scripts/Core/PlayerController.cs     ← Player 中核
Assets/_Project/Prefabs/Player/EggybotPlayer.prefab  ← Player prefab
Packages/manifest.json                                ← Package は A 専属
```

### grey zone (調整は A と通信、commit 通知)
```
CLAUDE.md / AGENTS.md      ← 規約変更は両者通知
.claude/                   ← 設定変更は両者通知
docs/RESOURCES.md          ← asset 運用 (B 主、A も読む)
```

## 🎯 仕事リスト (優先順、autonomous で進める)

User の意向「ステージとか player 周りでいろんなことやらせたい」を踏まえ、以下を **上から順に** autonomous で消化。ルール厳守 (deep dive → 計画 → review → 実装 → review → commit)。

### 1. 🎮 4 ミニゲームの **基本 scene blockout** (M3 着手準備)

優先: ⭐⭐⭐⭐⭐ (M3 で必須、User は今すぐ着手して OK)

各ミニゲームの基本 scene (primitive ベースの blockout) を作る。装飾は Meshy / Kenney / 自作で後で追加。

1. **Scenes/Stage_ShrinkingIsland.unity** (縮む島)
   - 8x8 タイル (Cube grid、各 1x1m、Y=0)
   - 海面 (Plane、Y=-2、青)
   - Skybox 雲っぽく
2. **Scenes/Stage_BombTag.unity** (爆弾鬼)
   - 街角広場 (Plane 30x30m、薄灰)
   - 壁 4 面 (Cube wall、Y=2 高さ)
   - 配達員 NPC spawn point 3 個 (空 GameObject)
3. **Scenes/Stage_TailHuntBattle.unity** (しっぽ狩り バトル)
   - 草原 (Plane 40x40m、緑)
   - 看板 4 個 (Cube ベース)
4. **Scenes/Stage_TailHuntEscape.unity** (しっぽ狩り 逃走)
   - 工場 (Plane + 柱 Cube ×6)
   - 燃える導火線 spawn point

EggybotPlayer prefab を各 scene に 1 体配置 (test 用、後で SpawnPoint 制御)。

### 2. 📦 Synty Bundle 取込検討 (User が買ったら)

User が StackSocial $99 bundle を買うかもしれない。買った場合の取込手順を docs に。

- `Assets/_Project/ThirdParty/Synty/` 配置先 vs `Assets/ThirdParty/Synty/`
- prefab variant の作り方
- material URP 変換 (Synty は Standard or Built-in shader)
- LFS 設定確認

買ってないなら **Kenney + Quaternius** で代用 (前回調査済):
- Kenney https://kenney.itch.io/kenney-game-assets ($10 全部)
- Quaternius https://quaternius.com/ ($1/月 Patreon)

### 3. 🧪 ScriptableObject 定義 (M3 Skill / Draft で必須)

CLAUDE.md の表通り SO 作る:

- **SkillDefinition × 10** (`Assets/_Project/ScriptableObjects/Skills/`)
  - dash / jump_boost / grapple / hover / pulse / bomb / shield / mirage / radar / revive
  - 各 SO に id, name, category, action, CD, 効果説明
- **MinigameDefinition × 5**
- **RuleCardDefinition × 6**
- **SeasonRuleDefinition × 3**
- **CharacterDefinition × 4** (Pink / Green / Yellow / Blue、色のみ違い)

script 雛形は `Scripts/Core/` 配下の既存 ScriptableObject pattern を参考。

### 4. 📝 docs/balance/ 整理

- `docs/balance/skill_cd_table.md` (Skill 10 個の CD/効果表)
- `docs/balance/rulecard_table.md` (RuleCard 6 種)
- `docs/balance/minigame_table.md` (Minigame 5 種)

### 5. 🧪 EditMode test 雛形

- `Assets/_Project/Tests/Core/EggybotFootGroundClampTest.cs`
  - feet 配列に Mock Transform、ground Y=0、foot 球底=-0.1 → clamp 後 0 確認
- `Assets/_Project/Tests/Core/EggybotFaceAnimatorTest.cs`
  - SetMouthByMovement の hysteresis 検証 (speed 1.0 で Open、0.3 で Smile)
- `Assets/_Project/Tests/Core/PlayerControllerTest.cs`
  - IsDashing edge detection

PlayMode test は Unity Editor 必須なので EditMode 中心で。

### 6. 🎨 Meshy で stage 装飾 prompt 量産 (Codex 試運転兼用)

`docs/codex_proposals/meshy_stage_prompts.md` (前回 Codex で量産失敗してる、リトライ)。

ステージ装飾 30 prompt:
- 自然 10 (樹木 / 岩 / 草 / 花 / 雲)
- 建築・小物 10 (灯籠 / 看板 / ベンチ / 樽 / フェンス)
- 装飾・お祭り 10 (旗 / 紙テープ / 風船 / 提灯 / 星型飾り)

スタイル: low-poly stylized, cartoon, vibrant, mobile-optimized, ~500-2000 tri。
**Community 公開禁止** ($20/月 1000 credits)。

### 7. 📑 進捗 report 自動化

User の手間を減らす:
- 毎日 commit log → `docs/reports/YYYY-MM-DD.md` 自動生成
- 週次 progress summary

## 🛑 何かあったら止まる条件

- ルール違反の懸念 → 即停止 + Claude A / User に通知
- visual / camera / VFX タスク (担当外、A に振る)
- file 担当の grey zone (CLAUDE.md / AGENTS.md / Package) → A に通知
- 計画 review で BLOCK 判定 → 修正 or 中止

## ✅ commit format

```
feat(stage): ShrinkingIsland blockout 作成
fix(skill): Dash SO 値修正
docs(balance): Skill CD 表更新
test(core): EggybotFootGroundClamp EditMode test 追加

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

**Claude A も B も commit 末尾に `Co-Authored-By: Claude Opus 4.7` 付ける** (区別不要、両方 Claude)。

## 💬 Claude A との通信

- 直接通信は無し (separate session)
- 共有チャネル:
  - GitHub commit log (push 済を見る)
  - MEMORY.md (User account 共有、両者の learning が累積)
  - `docs/handoff/YYYY-MM-DD.md` (お互いの進捗を 1 日 1 回 write)

## 🎉 始め方

```
1. このファイル (docs/claude-b-handoff.md) を読了 ✅
2. memory MEMORY.md + 全 feedback_*.md を読了
3. CLAUDE.md + AGENTS.md + .claude/rules/*.md を読了
4. git log --oneline -20 で最新の commit を把握
5. Task #88 「ステージ制作 worktree 並行」を picking → 1 (ShrinkingIsland blockout) から着手
6. 並列 agent で AAA defacto 調査 → 計画 review → 実装 → AI review → commit → push
```

User が試遊する Player 周りは Claude A が握ってるので、Claude B は **裏方の積み上げ** で User 待ち時間ゼロを目指す。
