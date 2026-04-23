---
name: unity-urp-quality-uplift
description: Unity URP キャラクターが「Lego cube 合体感・Unity 臭」に見える時に効いた改善手順。product shot クオリティに持ち上げるレシピ集。2026-04-23 EGGYBOT セッションで実証。
---

# URP キャラクター クオリティアップ手順

「Unity 臭い」「Lego cube 合体感」「プラスチック安っぽい」キャラを product shot クオリティへ上げる実証済み手順。各項目は独立に効くが、**効果が大きい順**に並べてある。

---

## Tier S（激変）

### 1. 接合部を voxel remesh で seamless 化

**問題**: 球や楕円体を並べて body/head/arms/legs を組むと、接合線が「cube 合体」感を出す。

**解**: Blender で全 pink parts を Join (Ctrl+J) → Remesh modifier (Voxel mode, voxel_size 0.02m) → 一つの seamless mesh に welding。

**注意**: 統合 mesh は bone アニメーションが困難（1 mesh = 1 root bone 風）。**ゲーム用キャラは parts 別のまま保持**し、voxel remesh は静止ショット / MainMenu マスコットなど**動かないキャラ**限定。動くキャラでは後述の Tier A の「matching material + SSAO で接合を隠す」手を使う。

```python
bpy.ops.object.join()
remesh = pink_body.modifiers.new(name="REMESH", type='REMESH')
remesh.mode = 'VOXEL'
remesh.voxel_size = 0.02
remesh.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier="REMESH")
```

### 2. Custom SSS-fake shader（SoftPlastic）

**問題**: URP Lit は拡散反射だけで、plastic の「光が肌を透過する感じ」が出ない。

**解**: カスタム HLSL で以下を合成:

- **Wrap lighting**: `NdotL = saturate((dot(N,L) + wrap) / (1 + wrap))` — wrap 0.55 で影が柔らかく回り込む
- **Back-light translucency**: `pow(saturate(dot(-V, L + N*distortion)), power)` — 裏から光が透ける表現（distortion 0.25, power 2.5）
- **Fresnel rim**: `pow(1 - NdotV, rimPower) * rimColor` — 縁が光って立体感
- **Blinn-Phong specular**: `pow(NdotH, specExp)` — ハイライト

これで「mochi / marshmallow」感が出る。

参考実装: `Assets/_Project/Art/Shaders/EGGYBOT_SoftPlastic.shader`

研究:
- https://johnaustin.io/articles/2020/fast-subsurface-scattering-for-the-unity-urp
- https://www.alanzucconi.com/2017/08/30/fast-subsurface-scattering-1/

### 3. Fresnel glass shader（scene color refraction 付き）

**問題**: URP Lit の Transparent だと「白飽和」「発光してる」「ガラスが見えない」。

**解**: カスタム HLSL で:

- **Fresnel alpha**: center 0.06 / edge 0.90 → 端が opaque、中央が透明（実物ガラスの見え方）
- **Scene color refraction**: `SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, screenUV + N.xy * strength)` → 向こうの景色が normal ベースで歪む
- Blend One OneMinusSrcAlpha（premultiplied）
- URP Asset で `m_RequireOpaqueTexture = true` + `m_RequireDepthTexture = true` 必須

参考実装: `Assets/_Project/Art/Shaders/EGGYBOT_Glass.shader`

研究:
- https://80.lv/articles/making-stunning-glass-refraction-effects-in-unity
- https://github.com/omid3098/Unity-URP-ScreenSpaceRefraction

---

## Tier A（大きな差）

### 4. Post-processing stack（product shot 仕上げ）

Volume component に global で:

- **Bloom**: threshold 1.0, intensity 0.35, scatter 0.75 — ハイライトが柔らかく滲む
- **ColorAdjustments**: post-exposure +0.4, contrast +12, saturation +8 — pop 感
- **Tonemapping**: ACES（映画業界標準、HDR 自然）
- **Vignette**: intensity 0.28, smoothness 0.45 — 被写体に視線集中
- **DoF Gaussian**: start 3.5, end 12, maxRadius 0.9 — 背景 subtle blur

参考: `Assets/_Project/Art/CharLab_PostFx.asset`

### 5. SSAO Renderer Feature

接触部に自動影を生成。Lego cube 的な hard seam を目立たなくする。

```csharp
var ssao = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
rendererData.rendererFeatures.Add(ssao);
AssetDatabase.AddObjectToAsset(ssao, rendererData);
```

### 6. Studio lighting 5 灯体制

3 点照明は最低ライン。さらに:
- **TopAccent (Spot)**: 頭頂から crisp highlight（intensity 8, angle 80°/40°）
- **BackKick (Dir)**: warm 暖色リム光で silhouette 分離

---

## Tier B（最後の仕上げ）

### 7. Gradient skybox + reflection probe

- 3-tone gradient（top / equator / ground）でスタジオ backdrop 再現
- ReflectionProbe Realtime で glass に環境反射
- `RenderSettings.skybox = skyMat;` で設定

### 8. 頬 blush（keychain 感）

Body 色から saturated 派生色を作って `Color.Lerp(base, red, 0.55)` で各キャラ統一の頬色。楕円 mesh を head 側面にちょい埋め込み。

---

## 形状バグは shader で直さない（geometry で直す）

2026-04-23 の教訓: ユーザー「色ついてるところに空間がない」を**何度も shader 調整で返したが直らなかった**。正体は Blender の Boolean cut で pot 上面に **flat cap face が残ってた** geometry bug。bmesh で top face 削除 → 真の open bowl で即解決。

**鉄則**:
1. 「〜に見えない」系のフィードバックは **まず mesh を数値検証**（vertex 数、面数、bounding box、radius 分布）
2. Shader tweak は geometry が正しいこと確認してから
3. Blender Cycles の物理正しい render で先に geometry を validate → それから Unity URP shader 調整

## 言葉が伝わる / 伝わらない

2026-04-23 EGGYBOT session でユーザーから学んだ：

**伝わった言い方**（即ポイント特定できた）:
- **具体物の比喩**: 「**お椀**」「プリン」「金魚鉢」「皿」等、日常物の形で指定
- **「どこが見えるはず」添え書き**: 「お椀なら内側の底が見えるはず」「スプーンならへこみが〜」
- **動きの比喩**: 「水が入る形」「すくえる形」
- **リファレンス画像**（手描きスケッチ or 参照写真）

**伝わりにくかった言い方**:
- 「hollow」「空洞」だけ→俺は「内部に何も無い空間」と理解するが、ユーザーは「**視覚的に**空洞と読める見た目」を意味してた
- 「〜感がない」→主観表現、何が具体的に欠けてるか追えない

**Claude 側の対処**:
- 「どこが見えるはず？」と質問して**理想像の視点**を言語化させる
- 比喩が出たら即座に 3D mental model 構築できるか自問→できないなら「○○っていうのは、こういう形ですか？」で画像/スケッチで確認
- **多角度 screenshot 最優先** — 「見えない角度」で死角が生まれてる可能性を先に排除

## 視覚確認の鉄則

**ミス**：一つの angle だけで screenshot 撮って「見えない = 壊れてる」と判断すると実装が正しくても「ダメ」に見える。

**鉄則**：
1. **多角度スクショ必須** — front / side / top-down / **斜め 45°下俯瞰** の 4 方向最低撮る
2. **数値照合** — mesh の半径、位置、壁厚を具体値で列挙してユーザーと共有（曖昧会話回避）
3. **斜め 45° 下俯瞰が一番情報量多い** — bowl 形状の中身、rim 厚、底の陰影、すべて同時に見える
4. 「ここがおかしい」と言われたら **見えない角度** から撮り直して実証 → 数値も添えて説明。
5. ユーザー発言 (2026-04-23 EGGYBOT 15h session): 「多角的にスクショ撮っていい」「数値とかと照らし合わせたり」「斜めから撮ってないからわからなかった」

**Unity 用 helper**（execute_menu_item or `mcp__UnityMCP__manage_camera` の batch="surround"）で 6 方向一括撮影可能。

---

## 実装ゴッチャ（ハマり所）

1. **EditorTools asmdef** に `Unity.RenderPipelines.Universal.Runtime` + `Core.Runtime` 参照必須（Volume, UniversalAdditionalCameraData 使う場合）
2. **URP Lit で Transparent + Preserve Specular + 高 Smoothness = 白飽和**。pure clear には向かない、必ず custom shader 作る
3. **CBUFFER_START(UnityPerMaterial) 必須** — SRP Batcher が効かないと描画が重くなる
4. **ScreenSpaceAmbientOcclusion 型名**: `"UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime"` でリフレクション生成
5. **URP opaque texture 有効化**: `m_RequireOpaqueTexture` / `m_RequireDepthTexture` 両方 true にしないと `_CameraOpaqueTexture` sample できない
6. **Voxel remesh の落とし穴**: 手足を個別に animate したいキャラには不適。static mascot 限定
7. **SoftPlastic shader を Transparent queue に置くと refraction は不可**（`_CameraOpaqueTexture` は Opaque pass の内容しか持たない）

---

## Decision tree: どこまでやるか

- **単発静止マスコット** (MainMenu mascot 等) → Tier S 全部 OK、voxel remesh も使える
- **ゲームプレイアクター** (動く、アニメする) → Tier S の #2 #3 + Tier A 全部、voxel remesh NG（parts 分離必須）
- **モバイル mobile build** → SSAO / DoF / refraction は重い、performance budget 確認

---

## ChaosDraftParty 適用履歴

- 2026-04-23 EGGYBOT セッション: voxel remesh 統合版 (commit `08d5e68`) → 「もっちり感失う + 手足動かせない」判明
- 以後は **分離 parts + SSAO で接合隠し** 方針
- `EGGYBOT_SoftPlastic.shader`, `EGGYBOT_Glass.shader`, `CharLab_Skybox.shader` が再利用資産
