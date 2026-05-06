# Gotchas: Unity 6 / URP 17 / Cinemachine 3.x / Input System の罠

**集約期間**: 2026-04-29 〜 2026-05-06 (継続更新)
**用途**: 同じ罠を二度踏まない。**新規プロジェクトでも該当 Unity バージョンなら同じ罠が出る**ので即参照。

---

## 🟥 Shader / URP

### URP 17 で `LitPassFragment` は **void + out param** 形式
旧 (URP 12 以前): `half4 LitPassFragment(Varyings input) : SV_Target`
新 (URP 17 / Unity 6): `void LitPassFragment(Varyings input, out half4 outColor [, out uint outRenderingLayers])`

旧形式で wrapper 書くと **magenta error** ("'LitPassFragment': no matching 1 parameter function")。

**確認方法**: `Library/PackageCache/com.unity.render-pipelines.universal@*/Shaders/LitForwardPass.hlsl` を grep。

**正しい wrapper**:
```hlsl
void OcclusionDitherFragment(
    Varyings input,
    out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
) {
    // 前処理 (clip 等)
#ifdef _WRITE_RENDERING_LAYERS
    LitPassFragment(input, outColor, outRenderingLayers);
#else
    LitPassFragment(input, outColor);
#endif
}
```

加えて URP 17 で必須の multi_compile pragma:
- `_FORWARD_PLUS` (Forward+ レンダラー対応)
- `_WRITE_RENDERING_LAYERS`
- `DEBUG_DISPLAY` (Frame Debugger)
- `DOTS_INSTANCING_ON` (DOTS 統合)

### URP Volume の SetGlobalFloat は SRP Batcher 脱落
`_DitherAmount` のような custom property を Material CBUFFER 外に置くと SRP Batcher 脱落 (per-renderer 1 instance なら影響軽微)。

CBUFFER UnityPerMaterial に追加する場合は **shader 内 `CBUFFER_START(UnityPerMaterial)` ブロック内に宣言**。

---

## 🎥 Cinemachine 3.x

### TPS で Camera Roll on Strafe (Lens.Dutch) は禁忌
Genshin / Splatoon / Mario Odyssey / Boss Room / Unity TPS Sample **全部 0°**。
- FPS の Quake 流 2° は TPS だと**確実に酔う**
- 本作 cartoon パーティでも避ける
- 入れるなら 1° 以下、SmoothDamp 0.4s+ で

### `OrbitalFollow.HorizontalAxis.Value` 直書きは Damping bypass
`InputAxisController` の `AccelTime` / `DecelTime` を経由しないので、damping が効かない。
- code 直書きするなら **入力側で SmoothDamp 後に書く**
- ただし SmoothDamp は副作用 (ゆっくり時高感度・早く時遅延) があり、**素直な linear が AAA defacto**

### `OrbitalFollow.TargetOffset` 直書きは Look-Ahead Composer より粗い
Cinemachine 3.x には `CinemachinePositionComposer.Lookahead` (Time / Smoothing / IgnoreY) という公式機能あり。これは Aim 段の Composer 内蔵。

自前 TargetOffset 直書きより堅牢で damping 内蔵。**LookAt あり構成にして RotationComposer.Lookahead を使う**のが defacto。

### Cinemachine Deoccluder Damping は **入り:出 = 1:2 非対称**
`DampingWhenOccluded` (default 0.2s) / `Damping` (default 0.4s)。`Damper.Damp` 関数は指数減衰 `1 - e^(-4.605 * dt / dampTime)`。

[Anticipation Tick + Smooth Tail pattern](../patterns/anticipation_tick_smooth_tail.md) 参照。

### CinemachineExtension.PostPipelineStageCallback の stage 選択
- `Stage.Body`: 位置決定後
- `Stage.Aim`: 向き決定後
- `Stage.Finalize`: 最終
- ロール変更や TargetOffset 操作は Body / Aim / Finalize のどれが正しいかで挙動が違う

---

## 🖱️ Input System

### `Application.runInBackground = true` は **Editor で完全に効果なし**
Editor では always-true 扱い。Bootstrap.Awake / PlayerController.Awake で設定しても無意味。

**Build 時のみ必要**: `PlayerSettings.runInBackground = true` で別途設定。

### Game view focus 失った時に Mouse / Keyboard 切れる真因
真犯人は **InputSystem の Background Behavior**:
- `backgroundBehavior = ResetAndDisableNonBackgroundDevices` (default) → focus 失うと device disable
- `editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus` (default) → Game view 専用

**修正**:
```csharp
UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior =
    UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
    UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDevicesRespectGameViewFocus;
```

### Mouse delta SmoothDamp / Yaw rate cap は逆効果
- SmoothDamp → ゆっくり時高感度・早く時遅延
- Rate cap → 早く動かすと clamp で押さえつけ (「早く遅い」現象)

**defacto**: Mouse は **raw を直接 sensitivity 倍してそのまま反映**、何もしない。

---

## 🛠️ Editor 自動化 / MCP

### Play 中に呼べない API
- `EditorSceneManager.MarkSceneDirty()` → `This cannot be used during play mode`
- `EditorSceneManager.SaveOpenScenes()` → 同上

**回避**: Play 停止後に asset 永続化、Play 中は runtime instance に直書きで対応。

### Game view "Always Refresh" の C# API は Unity 6 で見つからない
旧 `alwaysRefresh` プロパティは存在せず、`autoRepaintOnSceneChange` は別物。
**user 手動**: Game view 右上 3 点メニュー → Always Refresh

### script Edit 後の Play 中 reflection は domain reload で参照無効化
`var ego = GameObject.Find(...)` した後 script を Edit すると、domain reload で `ego` 参照が無効になる。**再 Find が必要**。

### `execute_code` は method body 形式 (using 不可)
```csharp
// NG
using UnityEditor;
var path = ...

// OK
var path = ...
UnityEditor.AssetDatabase.LoadAssetAtPath(...)
```
完全修飾名で書く。

---

## 🎨 URP Volume / Color Grading

### Splatoon / Fall Guys 等の clean cartoon は lens artifact を意図的に避けてる
**Tier C (削減推奨)** = 「映画/Cyberpunk/リアル系」defacto:
- Chromatic Aberration (Splatoon/Fall Guys/Mario 全部不採用、PC Gamer "actual worst" 批判)
- Film Grain (clean cartoon は使わない、candy 色を汚す)
- Lens Distortion (Mario Odyssey 不採用、8 分高テンポで疲れる)
- Panini Projection (広角 FPS 用、close cam では不要)
- Split Toning (Hollywood blockbuster cliché、candy と逆方向)
- 常時 Vignette (4 人画面で隅暗いと不利平等性)

### clean cartoon の Tier S (4 個) で Unity 感 70% 消える
- Tonemapping = **Neutral** (単独で 60%)
- Color Adjustments (saturation +20-30、contrast +5-10、Color Filter 微暖)
- Bloom (intensity 0.4-0.5、threshold 0.95、scatter 0.7)
- White Balance (Temp +5-10、Tint -2)

### URP Lit shader を runtime で Surface Type 切替は罠多い
Opaque ↔ Transparent 切替には keyword 5 個 + property 6 個 + render queue を全部正しく設定が必要。URP バージョン差で挙動違う。**避けるべき**。

代わりに **screen-door dither (Bayer 4x4 + Alpha Clip + Opaque)** が安定。Z 書込・影が壊れない。

---

## 🦴 Animation / Avatar

### SkinnedMR の `bounds.center` は罠
bone weight 単位で計算されるので、**mesh の見た目の中心ではない**。判定球 / SDF center を決める時は vertex の min/max から幾何中心を計算する。

### bone と mesh の親子関係は別 hierarchy
Transform.parent と SkinnedMR.bones は別物。Transform 上は同じ親でも、SkinnedMR.bones が違う rig を指してたら mesh は別の動きをする。

### Mecanim Hips Y 補正は LateUpdate で Hips 直書きが正解
`Animator.bodyPosition` shift は Mixamo baked クリップで効かない。
**正解**: LateUpdate で `Hips.position.y` を直接書換える。

### humanoid Avatar 再構築は user 手動 bone 編集を破壊する
muscle 正規化で user 編集が消える + Animator が Edit でも bone を改竄する。**再構築せず PoseLock で補正**が安全。

### Mixamo fbx の import 設定は Walking 等 working clip と完全一致させる
`sourceAvatar` / `keepOriginalPositionY` / `lockRootHeightY` / `heightFromFeet` を統一しないと Hips Y 浮きが出る。

---

## 🌐 NGO (Netcode for GameObjects)

### Boss Room / Hello World tutorial の流れは **localhost UTP → Relay の 2 段階**
NGO 層 (NetworkObject / NetworkTransform / IsOwner / NetworkAnimator) を localhost で確定させてから Relay 設定。**Relay 固有問題と切り分けるのが定石**。

### NGO Player Prefab は scene 内 instance を delete して prefab 化
NetworkManager.PlayerPrefab に登録、scene 内 instance は削除。

### UDP socket は Editor Play 停止後も release 待ちが残る
port 7777 が in use で host できない時は port retry (7777 → 7781 等) ロジック必須。

### NetworkAnimator AuthorityMode は NetworkTransform と統一
NetworkTransform=Owner なら NetworkAnimator も Owner、不一致だと挙動破綻。

### しゃがみ / coroutine ベースの local Transform は NetworkAnimator では同期されない
NetworkAnimator は parameter のみ同期。**Crouch Sequence の頭/蓋アニメ完全同期は別途 NetworkVariable + RPC が必要**。

---

## 📚 関連

- [recipes/camera_occluder_fade.md](../recipes/camera_occluder_fade.md) — 本 gotcha が反映された実装
- [patterns/anticipation_tick_smooth_tail.md](../patterns/anticipation_tick_smooth_tail.md) — 「即時 + ぬるっと」両立 pattern
- [patterns/parallel_agent_research.md](../patterns/parallel_agent_research.md) — 罠を見つける開発フロー
