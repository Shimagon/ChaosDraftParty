---
title: Claude → Codex VFX 実装 handoff (Phase 4-6 完了 / Editor 手動 pending)
date: 2026-06-02
status: script + shader 実装完了。Editor 手動作業 (prefab / material / scene wiring) Codex 担当。commit は User play test 後に判断。
---

# 概要

縮む島 (ShrinkingIsland) の VFX 刷新を Phase 4-6 で完了。Claude 側で script (BouncePad.cs / Tile.cs / ShrinkingIslandManager.cs) + shader 3 個 (SI_BouncePadRing / SI_TileWarning / SI_TileDissolve) を実装し、AI review loop で品質を担保した。残作業は Codex に引継ぐ Editor 手動作業のみ。

**AI review 戦績**

- Phase 5: 15 findings 抽出 → adversarial verify 3 voters → 5 confirmed / 10 dismissed
- Phase 6: confirmed 5 件全 fix + reverify PASS
- 残存 LOW: **F9 UniTask 未採用** (M2 まで暫定許容、TODO コメント明示済)

**Codex タスク全体像**

- Section A: `BouncePadAaa.prefab` 作成
- Section B: `SI_TileDustVfx.prefab` 作成
- Section C: Material 6 個作成
- Section D: Sandbox_ShrinkingIsland_v1 配置 + wiring + Play test
- Section E: 注意事項 / License / commit ルール / Codex prompt 例

各 Section は数値全部明記済。Codex は上から順に 1 つずつ実行可能。

---

# Section A: BouncePadAaa.prefab 作成手順

### A1. PATH

`Assets/_Project/Prefabs/Minigames/ShrinkingIsland/BouncePadAaa.prefab`

### A2. Hierarchy 作成順

1. 空 GameObject `BouncePadAaa` 作成 → Project window へ drag で prefab 化 → prefab 開いて以下を構築
2. Root に Component 追加: `CapsuleCollider` (isTrigger=true, Radius=0.6, Height=1.4)、`Rigidbody` (isKinematic=true, useGravity=false)、`CinemachineImpulseSource`、`BouncePad.cs`
3. 子作成: `Pillar` / `TopDisc` / `RingMesh` / `PS_Streak` / `PS_Spark` / `PS_IdleGlow`

### A3. 子 Transform / Mesh / Material

- [ ] `Pillar`: Cylinder mesh, pos=(0,0.4,0), scale=(0.8,0.4,0.8), Material `M_BouncePad_Pillar` (Section C.6)
- [ ] `TopDisc`: Cylinder mesh, pos=(0,0.85,0), scale=(1.2,0.05,1.2), Material `M_BouncePad_Top` (Section C.6)
- [ ] `RingMesh`: Quad mesh, pos=(0,0.9,0), rotation=(90,0,0), scale=(1,1,1), Material `M_BouncePad_Ring` (Section C.1)、**MeshRenderer.enabled=false** (初期非表示、Script が駆動)

### A4. ParticleSystem 数値

**PS_Streak (pos=(0,0.9,0))**

- [ ] Main: Duration 0.5、Start Lifetime 0.12、Start Speed 8.0、Start Size 0.25、Looping OFF、Play On Awake OFF、Simulation Space World
- [ ] Emission: Burst Count 3 @ time 0
- [ ] Shape: Cone, Angle 12°, Radius 0.1, 上向き
- [ ] Renderer: Stretched Billboard, Length Scale 4.0, Material URP/Particles/Unlit Premultiplied

**PS_Spark (sub-emitter)**

- [ ] Main: Lifetime 0.3、Speed 3.0、Size 0.08、Looping OFF、Play On Awake OFF
- [ ] Emission: Burst 6
- [ ] Renderer: Billboard, Material URP/Particles/Unlit Premultiplied

**PS_Streak Sub-Emitter 設定**

- [ ] PS_Streak の Sub Emitters module を有効化 → Type=Death、Particle System に PS_Spark を assign

**PS_IdleGlow (pos=(0,0.9,0))**

- [ ] Main: Duration 2.0、Lifetime 1.5、Speed 0.3、Size 0.4、Looping ON、Play On Awake ON
- [ ] Emission: Rate over Time 8
- [ ] Color over Lifetime: alpha 0→0.4→0 (soft halo)
- [ ] Renderer: Billboard, Material URP/Particles/Unlit Premultiplied (additive blend)

### A5. CinemachineImpulseSource

- [ ] Impulse Channel: 1 (Bump)
- [ ] Impulse Shape: Bump
- [ ] Impulse Duration: 0.18
- [ ] Default Velocity: (0, 0.35, 0)

### A6. BouncePad.cs Inspector 値

- [ ] Launch: HeightWorld=6.0、Duration=0.55、TargetXZMagnetism=0.8
- [ ] Squash: Anticipation=0.07/0.65/1.30、Punch=0.12/1.18/0.90、Settle=0.38
- [ ] Idle: Frequency=0.8、EmissiveAmplitude=0.25
- [ ] Ring: Duration=0.15、ScaleStart=0.3、ScaleEnd=2.5、RingRenderer に RingMesh をドラッグ assign
- [ ] Impulse: Amplitude=0.18、Duration=0.35、ImpulseSource に self をドラッグ assign

### A7. Verification チェックリスト

- [ ] Prefab root に CapsuleCollider/Rigidbody/CinemachineImpulseSource/BouncePad 全 attach 済
- [ ] 子 6 個 (Pillar/TopDisc/RingMesh/PS_Streak/PS_Spark/PS_IdleGlow) 揃っている
- [ ] RingMesh.MeshRenderer.enabled = false (初期非表示)
- [ ] PS_Streak と PS_Spark の Play On Awake = OFF、PS_IdleGlow のみ ON
- [ ] PS_Streak Sub Emitters Death → PS_Spark 紐付け確認
- [ ] 全 PS Renderer Material が **URP/Particles/Unlit Premultiplied** (Phase 2 A6 magenta 罠回避)
- [ ] Inspector 全 default 値が Synthesizer Phase 3 確定数値と一致
- [ ] Prefab 保存後 Sandbox_v21 既存 4 箇所と差替え可能 (Section D.D1 参照)

参考 path: `Assets/_Project/Scripts/Minigames/ShrinkingIsland/BouncePad.cs`、`Assets/_Project/Art/Shaders/SI_BouncePadRing.shader`

---

# Section B: SI_TileDustVfx.prefab 作成手順

### B1. Prefab 作成と配置

- 保存先: `Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileDustVfx.prefab`
- 空 GameObject `SI_TileDustVfx` (root, Transform reset) を作成し、子に `PS_Dust` と `PS_Chip` (Particle System) を追加して prefab 化

### B2. PS_Dust — Main Module

Duration `0.6` / Looping `false` / Prewarm `false` / Start Delay `0` / Start Lifetime `0.55` / Start Speed `1.2` / Start Size `0.3` / Start Rotation random `-30..30` / 3D Start Size OFF / Gravity Modifier `0` / Simulation Space `World` / **Play On Awake `true`** / **Stop Action `Destroy`** / Max Particles `32`

### B3. PS_Dust — Emission / Shape

- Emission: Rate over Time `0`, **Bursts: Time=0, Count=8, Cycles=1**
- Shape: `Cone`, Angle `45`, Radius `0.05`, Rotation X=`-90` (上向き噴射), Position Y=`0.02`

### B4. PS_Dust — ColorOverLifetime (MANDATORY, A6 真因)

ON にして Gradient Mode=`Gradient`:

- Color keys: `0.0=#FFFFFF`, `0.3=#D2B48C` (tan), `1.0=#D2B48C`
- **Alpha keys: `0.0=255`, `0.6=180`, `1.0=0`** ← 末端 0 が必須 (これが OFF だと A6 で Codex NG 真因)

### B5. PS_Dust — Texture Sheet Animation (UVModule)

ON / Mode=`Grid` / **Tiles X=4 Y=4** / Animation=`WholeSheet` / Time Mode=`Lifetime` / Frame Over Time=`Curve linear 0→1` / **Start Frame=Random Between 0..3** / Cycles=`1` / Affected UV Channels=`Everything` / **Flip-U/V OFF / Enable Frame Blending=ON**

### B6. PS_Dust — SizeOverLifetime

ON / Curve: `(0, 0.0)` → `(1, 1.5)` (linear easing-out)

### B7. PS_Dust — Renderer

Render Mode=`Billboard` / Material=`M_TileDust_Particle` (Section C.4) / **Sort Mode=By Distance / Sorting Fudge=-0.5 / Render Alignment=View** / Min Particle Size=`0` / Max Particle Size=`0.5` / Render Queue=`Transparent` / Cast/Receive Shadows OFF

### B8. PS_Chip — debris (sibling)

- Main: Duration `0.6` / Lifetime `0.7` / Start Speed `3.5` / Start Size `0.08` / **Gravity Modifier `4.0`** / 3D Start Rotation ON, Random `0..360` 全軸 / Simulation Space `World` / Play On Awake `true` / Stop Action `Destroy`
- Emission Bursts: Time=0, **Count=6**
- Shape: `Hemisphere`, Radius `0.1`, Y=`0.05`
- Renderer: **Render Mode=`Mesh`**, Mesh=Unity 組込 `Cube`, Material=`M_TileChip` (Section C.5), Cast Shadows ON

### B9. ライフサイクル安全策

両 PS とも **Stop Action=Destroy** + Looping=false。Tile.cs Phase 4 F2 fix の defensive `Destroy(instance, lifetime+0.5f)` と二重保険 (どちらが先でも leak 無し)。Sub-Emitter は **使用禁止** (Devil 指摘 GC 回避)。chip は独立 PS で並列再生。

### B10. 検証 (Codex Editor で実行)

1. prefab を開き Scene View で Particle Effect ウィンドウ `Simulate` 実行
2. 0.0s 時点で smoke 8 個 + chip 6 個が同時 burst することを確認
3. 0.55s で smoke alpha が完全に 0 に達し、UV flipbook が 0→15 frame まで遷移していることを確認
4. 0.7s で chip が重力で着地・回転していることを確認
5. 1.1s までに両 PS が Destroy され Hierarchy から消えることを確認 (Stop Action 動作確認)

---

# Section C: Material 6 個作成手順

### C0. 共通前提

- 配置先: `Assets/_Project/Art/Materials/ShrinkingIsland/`
- 命名: `M_<用途>.mat`
- Project ペインで右クリック → Create → Material → 命名 → Inspector で Shader 選択 → property 編集
- 編集後 `Ctrl+S` で .meta も含めて保存。Inspector 最下部に **「SRP Batcher: compatible」** が緑表示されることを毎回確認 (出なければ shader 側の CBUFFER 不整合)

### C1. M_BouncePad_Ring.mat

Shader: `Custom/SI_BouncePadRing`

- _BaseColor: (1, 1, 1, 1)
- _RimColor: **HDR** (1.0, 0.85, 0.35, 1) gold、Intensity 0 で開始
- _InnerRadius: 0.35 / _OuterRadius: 0.50 / _RingWidth: 0.08
- _Progress: 0 (Pad.cs から駆動)
- _EmissionMul: 3.5
- HDR 確認: Color picker に Intensity スライダーが出ること

### C2. M_TileWarning.mat

Shader: `Custom/SI_TileWarning`

- _BaseColor: (1, 1, 1, 1)、_BaseMap: 既存 Tile texture (Kenney prototype grid 等) を assign
- _WarningProgress: 0、_WarningColor: (1.0, 0.4, 0.3, 1) warm red
- _WarningTintLerp: **0.4** (1.0 禁止、WCAG 配慮)
- _EdgeEmissionMul: 2.5、_PulseFrequency: **2.5Hz** (epilepsy 閾値 3Hz 未満)、_EdgeWidth: 0.1

### C3. M_TileDissolve.mat

Shader: `Custom/SI_TileDissolve`

- _BaseColor + _BaseMap: M_TileWarning と同一 (継続感)
- _DissolveProgress: 0、_DissolveScale: 8.0、_DissolveEdgeWidth: 0.08
- _DissolveEdgeColor: **HDR** (1.0, 0.6, 0.2, 1) warm orange
- _EdgeEmissionMul: 4.0

### C4. M_TileDust_Particle.mat

Shader: `Universal Render Pipeline/Particles/Unlit`

- Surface Type: **Transparent** / Blending Mode: **Premultiply**
- _BaseColor: (1, 1, 1, 1) (PS の Vertex Color 乗算前提)
- _BaseMap: `Assets/_Project/ThirdParty/Kenney/.../smoke_04.png`
- Soft Particles: ON 推奨

### C5. M_TileChip.mat

Shader: `Universal Render Pipeline/Lit`

- Surface: **Opaque**、_BaseColor: Tile base 色と同系
- **Enable GPU Instancing: ON** (64 chip 同時生成 perf)
- Smoothness: 0.2、Metallic: 0

### C6. M_BouncePad_Pillar.mat / M_BouncePad_Top.mat

Shader: `Universal Render Pipeline/Lit`、Opaque

- Pillar: gray (0.6, 0.6, 0.6)、Smoothness 0.3
- Top: gold-tinted (0.9, 0.75, 0.4)、Smoothness 0.5、GPU Instancing ON

### C7. Verification

Scene view で各 mat を Quad に仮 assign → 色味 / emission bloom / dissolve noise が期待通りか目視。全 6 mat で SRP Batcher compatible が緑であることを確認後、Section D へ進む。

---

# Section D: Sandbox_ShrinkingIsland_v1 配置 + wiring + Play test

絶対 path: `C:\Users\taise\Documents\GitHub\ChaosDraftParty\Assets\_Project\Scenes\Sandbox_ShrinkingIsland_v1.unity`

### D1. BouncePad 置換 (4 箇所)

1. Hierarchy で既存 4 BouncePad の position/rotation/Transform を Notepad にコピー (Inspector 右上 三点 → Copy Component)
2. 既存 4 BouncePad を削除
3. `Assets/_Project/Prefabs/Minigames/ShrinkingIsland/BouncePadAaa.prefab` (Section A 産物) を Hierarchy にドラッグ × 4、記録した position/rotation を Paste Values
4. 各 instance を選択し Inspector で以下を wiring:
   - `_impulseSource` → 自身の CinemachineImpulseSource (同 GameObject、Add Component 済前提)
   - `_ringMesh` → child `RingMesh` の MeshRenderer
   - `_streakParticle` → child `PS_Streak`
   - `_sparkParticle` → child `PS_Spark`
   - `_idleGlowParticle` → child `PS_IdleGlow`
   - `_visualRenderer` → child `Pillar` の MeshRenderer
   - `_visualRoot` → child `Pillar` の Transform
   - `_launchTarget` → 中央 island 中心の空 GameObject (無ければ `(0, 1.5, 0)` に `LaunchTarget_Center` 作成)
5. Inspector default 値が Synthesizer 仕様 (cooldown 0.6 / anticipation 0.12 / punch 0.18 / settle 0.35) か確認

### D2. Tile.prefab 更新

1. `Tile.prefab` を Project 窓で Open Prefab
2. MeshRenderer の Materials 配列を **size 1 のまま `M_TileWarning` (Section C.2) に置換** (既存 SI_ClosedBox material は退避)
3. Tile.cs 側は `_warningMaterial` / `_dissolveMaterial` の 2 field 参照 → Inspector で `M_TileWarning` / `M_TileDissolve` (Section C.3) を両方 drag (state 切替は sharedMaterial swap、MaterialPropertyBlock で `_DissolveAmount` を駆動)
4. `_fadeParticlePrefab` → `SI_TileDustVfx.prefab` (Section B 産物)
5. `_warningParticlePrefab` → **null** (shader pulse でカバー、PS 追加禁止)
6. Apply All

### D3. ShrinkingIslandManager wiring

- Sandbox scene の Manager GameObject を選択
- `_staggerPattern = RowByRow`、`_rowStaggerDelay = 0.08`
- `ShrinkingIslandConfig.asset` を開き `_warningPhaseDuration=0.4` / `_sinkPhaseDuration=0.45` / `_dustPhaseDuration=0.6` 確認 (差分あれば Synthesizer 値に揃える)

### D4. 削除 (不使用 asset)

1. `SI_TileFadeBurstVfx.prefab` / `SI_TileWarningVfx.prefab` を選択
2. **必ず先に** 右クリック → Find References In Project で他 scene 未参照を確認
3. Tile.prefab 側の参照が null 化されているのを確認してから Delete

### D5. Play 前 Verification

- Scene view: 4 BouncePad が新 prefab で表示、`PS_IdleGlow` の breathing が見える
- Console: **0 errors / 0 warnings** (赤黄ゼロが play 条件)
- Tile prefab Inspector: Materials[0]=M_TileWarning、_dissolveMaterial=M_TileDissolve、_fadeParticlePrefab=SI_TileDustVfx
- Manager Inspector: stagger 値が default

### D6. Play test 順序

1. Editor Play、gameplay camera 視点固定
2. **BouncePad 接触**: 3 段 squash (anticipation 縮 → punch 伸 → settle) + Ring flash + Streak 上昇 + Spark 放射 + Camera impulse の縦揺れ確認
3. **Tile shrink (5s 後)**: warm red 点滅 (pulse 2.5Hz、lerp 強度 0.4) → tilt → sink + dissolve + 下方 dust burst
4. gameplay camera 主観評価: 「Unity 臭」消滅 / side-face 抜け無し / 砂埃が地面方向に流れる
5. **4 screenshot 取得**: spawn 直後 / 低角度 (side-face 抜け確認) / warning VFX 中 / 俯瞰
6. NG なら commit せず Section B/C へ feedback

---

# Section E: 注意事項 + License + commit ルール + Codex prompt 例

### E1. Codex 既存ファイル保護方針

User 明示ルール「dirty worktree の無関係ファイルは触らない」を厳守。

**Phase 4-6 で Claude が触ったファイル (commit OK 範囲):**

- `Shaders/SI_BouncePadRing.shader` / `SI_TileWarning.shader` / `SI_TileDissolve.shader` (新規 3 個)
- `Scripts/.../BouncePad.cs` / `Tile.cs` / `ShrinkingIslandManager.cs` (拡張)
- `ChaosDraftParty.Minigames.asmdef`
- `Prefabs/Minigames/ShrinkingIsland/SI_TileWarningVfx.prefab` / `SI_TileFadeBurstVfx.prefab`
- `Materials/ShrinkingIsland/*.mat` (6 個) / `Sandbox_ShrinkingIsland_v1.unity`

**触ってはいけない (Codex 成果物、保護):**

- `StageSignalPulse.cs` / `ShrinkingIslandPrototypeHud.cs` (完全に手を出さない)
- `SI_StylizedSurface.shader` / `SI_GradientSkybox.shader` / `StylizedOutline.shader` (別系統、削除厳禁)
- `SI_TileFadeBurstVfx.prefab` を削除する場合は事前に User 確認

### E2. License

- Kenney CC0 (smoke_04.png 等): Public Domain、商用可、attribution 不要
- `.codex/refs/vfx/` の Unity 公式 VFX サンプル: Unity Companion License、graph 構造の参考のみ可、直 import 禁止 (本セッションでは touch しない)
- Meshy: キャラ生成禁止 / 装飾 only / Pro 契約済
- Phase 4 新規 shader 3 個: 自作、project 内 license、外部公開なし

### E3. Commit 戦略 (NEVER commit without User OK)

- 本 handoff 完了時点で **commit しない**
- User Play test 視覚 OK 取得後、User 指示でのみ commit
- commit scope は Phase 4-6 で Claude が触った範囲のみに限定 (E1 参照)
- Codex 既存ファイル (StageSignalPulse 等) は別 commit / 別 branch で User 判断
- branch: 現状 main 直、Phase 4-6 は WIP commit でも OK (User 判断)

### E4. AI Review Loop 戦績の引継ぎ

- Phase 5: 15 findings 抽出 → adversarial verify 3 voters → 5 confirmed / 10 dismissed
- Phase 6: confirmed 5 件全 fix + reverify PASS
- 残存 LOW: **F9 UniTask 未採用** (M2 まで暫定許容、TODO コメント明示済)

### E5. Codex への引継ぎ prompt 例

```
docs/handoffs/codex_to_claude_2026-06-02_vfx_next.md (前回 Codex handoff) と
docs/handoffs/claude_to_codex_2026-06-02_vfx_impl.md (本 handoff、Phase 4-6 結果) を読んで、
Section A-D の Editor 手動作業を実行。commit はしない、User Play test 後に判断。
```

### E6. Cleanup Checklist (Phase 4-6 完了確認)

- [ ] Unity Console: 0 errors / 0 warnings
- [ ] 新規 shader 3 個に `.meta` 自動生成済
- [ ] prefab 2 個に `.meta` 自動生成済
- [ ] Material 6 個に `.meta` 自動生成済
- [ ] Tile prefab の Material slot に `SI_TileDissolve` 系 mat 設定
- [ ] BouncePadAaa prefab Inspector wiring 全項目設定済 (ring renderer / particle / audio)
- [ ] Sandbox scene で再生 → エラー無し → User 視覚確認待ち

---

**最終ルール**: commit はしない。User Play test 後の判断を待つこと。
