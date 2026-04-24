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
- 2026-04-24 EGGYBOT v9: **Boolean UNION (EXACT solver)** で手足を body に merge → seam 完全消失 + ARMATURE_AUTO で rigging 可能を両立（commit `3af6666`）
- 2026-04-24 EGGYBOT v10: **Bridge Edge Loops で Boolean UNION 廃止** → rim ガビガビ (moiré) 根絶 + Kirby 風 stubby 手足の manifold 接合（commit `4658928` + `bd9794a`）

---

## Boolean UNION で手足 organic blend（v9 breakthrough）

**問題**: 手足を別 mesh のまま body に近接配置すると、視覚的に「ボールとボール貼り合わせ」感が抜けない。user 言：「ただボールとボールを靴蹴るだけだと安っぽい」。

**解**: Boolean UNION (solver='EXACT') で arm/leg を body_lower に merge。

```python
def boolean_union(target, other, remove_other=True):
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new(name="UNION", type='BOOLEAN')
    mod.object = other
    mod.operation = 'UNION'
    mod.solver = 'EXACT'  # FAST は topology 崩れる
    bpy.ops.object.modifier_apply(modifier="UNION")
    if remove_other:
        bpy.data.objects.remove(other, do_unlink=True)
```

**Rigging 保持**: UNION 後の単一 mesh を ARMATURE_AUTO で parent → bone 近接ベースで weight が自動割り当て。手足 bone は手足領域の vertex を掴んで animate 可能。

**注意点**:
- **Edge Split は UNION 前に apply**：rim の crisp さを保つため。UNION 後に全体 Edge Split すると arm/body 接合 seam まで split される。split vertex duplication が UNION 中は保持される。
- **hollow 侵入ガード**: arm ellipsoid の pot 中心からの最小距離が pot inner radius より大きいことを事前計算で検証。ellipsoid の方向 radius `r(D) = 1/sqrt((dx/a)² + (dy/b)² + (dz/c)²)` で closest point 計算。
- **voxel remesh と違い rigging 可能**: 手足 animate できる、キャラクタに使える。

## 対称 mesh で rim flush（v9 breakthrough）

**問題**: pot と dome で Solidify offset / thickness が違うと、rim の outer radius がズレて「微妙な段差」ができる。

**解**: pot と dome を完全対称パラメータで作る。
- 両方 base sphere r=0.325
- 両方 Solidify offset=0 thickness=0.05 → outer r=0.35 inner r=0.30 共通
- 両方 bmesh で境界 face を削除（pot は上半球、dome は下半球）

**UV sphere の bmesh 削除条件の罠**:
- ❌ `mean_z > 0` で削除 → 赤道 face が mean z=0 扱いで「境界側」判定 → 削除されて boundary が 0.032 ずれる
- ✅ `min_z > 0` で削除（pot）/ `max_z < 0` で削除（dome）→ 赤道 ring は確実に残る

**Rim 二重化の罠**: 両方に Solidify rim face（use_rim=True）があると、pot rim face と dome rim face が z=PARTING_Z で coplanar 重複 → Cycles で z-fighting ダークライン。解：dome 側だけ `use_rim=False`（pot の rim face のみ残す、dome は clear なので open edge でも見えない）。

## 確認 loop の高速化

**問題**: user「いちいち確認する時 Blender 開き直さなきゃいけないのだるい」。

**解**: headless build + Cycles render 6 方向 → Finder で preview フォルダ自動オープン。Blender GUI 不要。

```bash
tools/blender/eggybot_preview.sh
# 内部: blender --background --python build_eggybot.py --python render_eggybot_previews.py + open preview/
```

---

## Bridge Edge Loops で限界品質 manifold 接続（v10 breakthrough）

**問題**: Boolean UNION (EXACT solver) 後の pot rim 付近に **moiré 状のガビガビ texture** が Cycles render に出る。root cause は duplicate vertex (1e-7 オーダー) + degenerate face + normal 不連続。UNION 出力自体が非 manifold 近いため、shading が崩れる。

**解**: UNION を完全廃止し、**bmesh で body に穴を開け + pre-shaped hemisphere limb を join + `bpy.ops.mesh.bridge_edge_loops()`** で manifold 接続に切り替え。

### 核心 1: hemisphere limb の clean open edge 作成

UV sphere を斜めカットすると open edge が jagged になり bridge が spike まみれに。**pre-rotate してから axis-aligned に cut** するとクリーンな赤道 ring が得られる：

```python
def create_hemisphere_limb(name, radius, squash_xyz, outward_dir_world, attach_world, protrude_offset=0.005):
    # 1. sphere を origin に作る
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, segments=32, ring_count=16, location=(0,0,0))
    obj = bpy.context.object
    # 2. bmesh で上半球削除（local Z > 0 の face）→ 下半球 + 赤道 open edge（clean 32-edge loop）
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    faces_to_delete = [f for f in bm.faces if min(v.co.z for v in f.verts) > 0.0001]
    bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    # 3. Squash（local frame、Z は長さ方向）→ apply
    obj.scale = squash_xyz
    bpy.ops.object.transform_apply(scale=True)
    # 4. local +Z → body 方向に rotate（opening が body を向く）→ apply
    body_dir = -Vector(outward_dir_world).normalized()
    quat = Vector((0,0,1)).rotation_difference(body_dir)
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = quat
    bpy.ops.object.transform_apply(rotation=True)
    # 5. attach_world + outward*epsilon に配置（bridge 用の neck 厚）
    obj.location = Vector(attach_world) + Vector(outward_dir_world).normalized() * protrude_offset
    return obj
```

### 核心 2: bridge の edge 選択で rim を除外

join 後の boundary edges には (a) pot rim (b) body hole (c) limb opening が混在。`bridge_edge_loops` は選択された edge を全て pair するので **rim を混入させてはいけない**。world Z = PARTING_Z ± tolerance の edge を除外：

```python
for e in bm.edges:
    if not e.is_boundary:
        continue
    mp_world = (body.matrix_world @ e.verts[0].co + body.matrix_world @ e.verts[1].co) * 0.5
    if abs(mp_world.z - PARTING_Z) < 0.008:
        continue  # rim 除外
    if (mp_world - attach_world).length < hole_radius * 3.5:
        e.select_set(True)
bpy.ops.mesh.bridge_edge_loops(type='SINGLE', number_cuts=0, interpolation='LINEAR')
```

### 核心 3: Post-Bridge cleanup (moiré 根絶)

```python
def cleanup_post_bridge(obj, merge_thr=0.00005, smooth_angle_deg=60):
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=merge_thr)
    # tiny area face 削除 + dissolve_degenerate + normals_make_consistent
    # rim edge を edge.smooth=False でsharp マーク
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.shade_smooth()
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle_deg))
```

### ハマりポイント

- **Blender 5.1 で `bpy.ops.mesh.merge_by_distance` が消失** → `bmesh.ops.remove_doubles` 経由で OK
- **`bridge_edge_loops` の `twist` parameter は Blender 5.1 で除去済** → 呼び出しから削除
- **primitive_uv_sphere_add + squash の v.co 座標系**: 実測すると v.co は **local frame（origin 中心）**、object.location に translation 入る。`matrix_world @ v.co` で確実に world を取る
- **join 後 Python の limb wrapper は invalidated** → `limb.name` 等の access は join 前に save

### Kirby 制約（hollow 下の妥協点）

純 Kirby 型（球を body に半分めり込ませる）は hollow (pot inner r=0.30) 貫通で不可。制約内の最適解：
- Hemisphere r=0.15-0.17、Z 方向（outward）だけ elongated（scale 1.25-1.40）
- 他軸 (XY) は 1.0 (stubby 保持)
- attach_world は outer surface 上（z=0.22 で arm、z=-0.02 で leg）
- Bridge で body と manifold、見た目は Kirby 風の丸い出っ張り

---

## 2026-04-24 product-shot uplift の数値レシピ

**SoftPlastic shader** (v10 default):
- `_SSSPower` 2.5 → **3.8**（back-light 透過の厚み感）
- `_InteriorShadow` 0.40 → **0.22**（pot 内壁が薄く見える）
- `_RimIntensity` 0.6 → **0.80** / `_Smoothness` 0.35 → **0.68** / `_SpecularIntensity` 0.6 → **0.85**
- `_RimColor` pale (0.88,0.92) → **pure white (1,1,1)**（edge pop）

**Glass shader** (v10 default):
- `_FresnelPower` 2.8 → **2.1**（physical IOR 相当の softer fresnel）
- `_RefractionStrength` 0.025 → **0.015**（turntable 中の歪み過剰回避）

**Post-FX (CharacterLab)**:
- `TonemappingMode` ACES → **Neutral**（パーティゲームは ACES の暗部沈み不適）
- `ambientIntensity` 0.75 → **0.88** + `ambientGroundColor` warm shift (0.30,0.30,0.34) → (0.42,0.38,0.35)
- `Bloom.threshold` 1.0 → **0.85** / `intensity` 0.35 → **0.45** / `postExposure` +0.4 → **+0.5**

**Material (per variant)**:
- BodyGloss `smoothness` 0.82 → **0.68** / `spec` 1.10 → **0.85** / `rim` 0.55 → **0.80**
- HeadMatte `smoothness` 0.03 → **0.10**（micro-spec で plastic 感 add） / `rim` 0.15 → **0.30**

**Blender preview ↔ Unity parity**:
- Blender Cycles の 3-point light energy を Unity Directional intensity と 1:1 揃える
- Key 3.5 → 1.05 / Fill 1.4 → 0.45 / Rim 1.8 → 0.95 + BackKick 0.8 追加
