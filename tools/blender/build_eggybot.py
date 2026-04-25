"""
EGGYBOT v15 Blender build — Metaball「一つの粘土の塊」body + dome + head

Design:
  - body_lower: 複数 metaball (body + 4 limb) を同 family に配置 → 融合 → mesh 変換
    → 手足が body から「自然にふくらんだ隆起」として seam なしで生える
  - body_upper (clear dome): 半球 shell。pot rim と flush、後で CapHinge bone で開閉 anim
  - head: 別 mesh sphere（上に乗る）
  - eyes, mouth, armature: 維持

v14 までの Boolean UNION / Bridge Edge Loops 経路を完全廃止。
user 要望「一つの粘土から手足が自然にふくらんだ風」(sketch #63) に構造的に整合。

Research sources:
  - https://docs.blender.org/api/current/bpy.types.MetaBall.html
  - https://docs.blender.org/manual/en/latest/modeling/metas.html
  - Explore agent report (2026-04-24)

Key parameters (sketch #63 integration):
  - Body metaball sphere at (0,0,0.18), r=0.37, stiffness 2.0
    → rim at world z=0.30 で outer r=√(0.37²-0.12²)=0.35、dome rim と flush
  - Arm metaball at (±0.27, 0, 0.10), r=0.14
  - Leg metaball at (±0.13, 0, -0.10), r=0.15
  - threshold 0.60, resolution 0.015
  - Subsurf 1 段 apply（rim crease preserved）
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
FBX_PATH = os.path.join(OUT_DIR, "eggybot_base.fbx")
BLEND_PATH = os.path.join(OUT_DIR, "eggybot_source.blend")

os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

PARTING_Z = 0.30

# ============================================================
# 1. Materials
# ============================================================
def make_material(name, base_color, alpha=1.0, transparent=False, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_color, alpha)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = 0.08 if transparent else roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if transparent and "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha
    mat.diffuse_color = (*base_color, alpha)
    return mat

PINK = (0.96, 0.55, 0.65)
DARK = (0.05, 0.05, 0.05)
WHITE = (1.0, 1.0, 1.0)

mat_pink = make_material("EGGYBOT_Pink", PINK, 1.0)
mat_clear = make_material("EGGYBOT_Clear", (1.0, 1.0, 1.0), 0.15, transparent=True)
mat_dark = make_material("EGGYBOT_Eye", DARK, 1.0)
mat_white = make_material("EGGYBOT_EyeShine", WHITE, 1.0)


# ============================================================
# 2. Helpers
# ============================================================
def add_sphere(name, location, radius, segments=32, rings=16, material=None):
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=radius, segments=segments, ring_count=rings, location=location
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.shade_smooth()
    if material:
        obj.data.materials.append(material)
    return obj


def apply_scale(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(scale=True)


def squash(obj, sx, sy, sz):
    obj.scale = (sx, sy, sz)
    apply_scale(obj)


def boolean_difference(target, cutter):
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new(name="CUT", type='BOOLEAN')
    mod.object = cutter
    mod.operation = 'DIFFERENCE'
    bpy.ops.object.modifier_apply(modifier="CUT")
    bpy.data.objects.remove(cutter, do_unlink=True)


# ============================================================
# 3. Metaball body_lower —「一つの粘土の塊」アプローチ
# ============================================================
def create_body_lower_metaball(material=None):
    """
    v15.3: 「中空お椀」設計。
    下 pink = 上 dome と完全同じ shell 幾何の半球お椀。玉（マスコット）が入る空洞あり。

    幾何 = dome と完全 mirror:
      - base UV sphere r=0.325（dome base と同じ）at (0, 0, PARTING_Z)
      - bisect @ z=PARTING_Z、下半分のみ残す（use_fill=False で open rim）
      - Solidify thickness=0.05 offset=0 use_rim=False → wall shell
      → outer r=0.35, inner r=0.30, 下半分の open-top bowl
    """
    BASE_SPHERE_R = 0.325   # dome と同じ base
    WALL_THICKNESS = 0.022  # dome と同じ壁厚

    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=BASE_SPHERE_R,
        segments=64,
        ring_count=32,
        location=(0, 0, PARTING_Z),
    )
    body_mesh = bpy.context.object
    body_mesh.name = "BodyLower"
    body_mesh.data.name = "BodyLowerMesh"

    # --- Bisect: 上半 (z>PARTING_Z) を切除、top は open（use_fill=False）
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.bisect(
        plane_co=(0.0, 0.0, PARTING_Z),
        plane_no=(0.0, 0.0, 1.0),
        clear_outer=True,
        clear_inner=False,
        use_fill=False,
    )
    bpy.ops.object.mode_set(mode='OBJECT')

    # --- Solidify shell（v15.9: use_rim=True で top を manifold 閉塞）
    # Boolean UNION は非 manifold で see-through を起こすため、
    # use_rim=True で outer/inner 間に ring 面を生成 → closed manifold bowl
    sol = body_mesh.modifiers.new(name="SHELL", type='SOLIDIFY')
    sol.thickness = WALL_THICKNESS
    sol.offset = 0.0
    sol.use_even_offset = True
    sol.use_quality_normals = True
    sol.use_rim = True
    sol.use_rim_only = False
    bpy.ops.object.modifier_apply(modifier="SHELL")

    # --- Shade smooth
    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(60))
    except Exception:
        pass

    if material:
        body_mesh.data.materials.append(material)

    print(f"[body_lower] v15.3 hollow shell: outer r={BASE_SPHERE_R + WALL_THICKNESS/2:.3f}, "
          f"inner r={BASE_SPHERE_R - WALL_THICKNESS/2:.3f}, "
          f"{len(body_mesh.data.vertices)} verts")
    return body_mesh


# ============================================================
# 4. Body construction
# ============================================================
body_lower = create_body_lower_metaball(material=mat_pink)

# Head mesh — bone と同名を避けて HeadMesh に rename (Unity Humanoid Rig 混乱回避)
head = add_sphere("HeadMesh", (0, 0, 0.82), 0.29, 64, 32, mat_pink)
squash(head, 1.00, 1.00, 0.98)
for v in head.data.vertices:
    wz = v.co.z + head.location.z
    if wz < 0.74:
        ratio = min(1.0, max(0.0, (0.74 - wz) / 0.08))
        v.co.z += ratio * 0.015

# ============================================================
# 5. Body Upper Clear (Glass dome) — pot rim r=0.35 と flush
# ============================================================
BASE_SPHERE_R = 0.325  # Solidify offset=0 thick=0.05 で outer=0.35 に一致
WALL_THICKNESS = 0.022

body_upper = add_sphere("BodyUpperClear", (0, 0, PARTING_Z),
                        BASE_SPHERE_R, 64, 32, mat_clear)

# world z=PARTING_Z で bisect、上半球のみ
bpy.context.view_layer.objects.active = body_upper
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.bisect(
    plane_co=(0, 0, PARTING_Z),
    plane_no=(0, 0, 1),
    clear_inner=True,
    use_fill=False,  # dome bottom は open、solidify rim=False と併せて
)
bpy.ops.object.mode_set(mode='OBJECT')

sol_dome = body_upper.modifiers.new(name="SHELL", type='SOLIDIFY')
sol_dome.thickness = WALL_THICKNESS
sol_dome.offset = 0.0
sol_dome.use_even_offset = True
sol_dome.use_quality_normals = True
sol_dome.use_rim = False
bpy.ops.object.modifier_apply(modifier="SHELL")

bpy.context.view_layer.objects.active = body_upper
bpy.ops.object.shade_smooth()
try:
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
except Exception:
    pass

# ============================================================
# 5.5 Kirby 風 手足 — 計算で attach（溶接しない、body 外球に tangent 食い込み位置）
# ============================================================
# 戦略: Boolean UNION / metaball を廃止、独立 mesh として数学的に body 外面に
# 「ほんの少しめり込む」位置を計算して配置。
#   - body outer sphere: center (0,0,PARTING_Z), radius 0.336
#   - limb sphere を body_center から (0.336 + r_limb - overlap) 離す → 外壁に数 mm 食い込む
#   - overlap = 0.005 (5mm) なら visual に gap 消えるが inner wall 0.314 には届かず hollow 保持
# 各 limb は独立 mesh で armature bone 個別 animation 可能、body shell は無傷。

BODY_OUTER_R = 0.336  # body_lower outer surface radius
HOLLOW_INNER_R = 0.314  # inner wall radius (touch しない制約)

def attach_xy(z, r_limb, overlap=0.015, body_cz=PARTING_Z, body_r=BODY_OUTER_R):
    """
    Return x (or y) displacement so a sphere of radius r_limb at (x, 0, z)
    overlaps body outer sphere by `overlap` meters (closest surfaces intersect by that much).
    Returns None if geometrically impossible (too far in z).
    """
    d_required = body_r + r_limb - overlap
    dz = z - body_cz
    x2 = d_required * d_required - dz * dz
    if x2 < 0:
        return None
    # Verify hollow stays clean: limb closest to body center must be > HOLLOW_INNER_R
    limb_closest = d_required - r_limb
    assert limb_closest > HOLLOW_INNER_R, f"limb at z={z} r={r_limb} would breach inner wall"
    return math.sqrt(x2)

# Hand: Kirby-style, v15.9 で「伸ばした手」化 — 外向きに elongate（#89 reference）
# 元 UV sphere r=0.08 を outward 1.5 倍 stretch → 実効 x 半径 0.12
# 位置計算：hand 内側 edge が body outer + 15mm overlap 位置
HAND_R = 0.08
HAND_Z = 0.22
HAND_STRETCH = (1.5, 1.0, 0.9)   # 外向き伸び + 少し上下 flatten
HAND_EFFECTIVE_XR = HAND_R * HAND_STRETCH[0]  # 0.12

# body outer surface x at HAND_Z
body_x_at_hand_z = math.sqrt(BODY_OUTER_R**2 - (HAND_Z - PARTING_Z)**2)
hand_x = body_x_at_hand_z - 0.015 + HAND_EFFECTIVE_XR  # 0.3264 - 0.015 + 0.12 = 0.4314

# Hollow 侵入 check: stretched ellipsoid の最短点距離
# vec hand→body, 正規化、effective radius in that direction
_dx, _dz = -hand_x, PARTING_Z - HAND_Z
_d = math.sqrt(_dx*_dx + _dz*_dz)
_ux, _uz = _dx/_d, _dz/_d
_a, _c = HAND_R * HAND_STRETCH[0], HAND_R * HAND_STRETCH[2]
_r_eff = 1.0 / math.sqrt((_ux*_ux)/(_a*_a) + (_uz*_uz)/(_c*_c))
_clearance = (_d - _r_eff) - HOLLOW_INNER_R
assert _clearance > 0.003, f"hand breaches inner wall: clearance={_clearance:.4f}"
print(f"[hand] stretched elbow clearance: {_clearance*1000:.1f}mm safe")

hand_l = add_sphere("Hand_L", (-hand_x, 0, HAND_Z), HAND_R, 32, 16, mat_pink)
hand_r_mesh = add_sphere("Hand_R", ( hand_x, 0, HAND_Z), HAND_R, 32, 16, mat_pink)
# Apply stretch AFTER placement, BEFORE Boolean UNION
squash(hand_l, *HAND_STRETCH)
squash(hand_r_mesh, *HAND_STRETCH)

# Foot: Kirby-style, attached below body
FOOT_R = 0.10
FOOT_Z = -0.10
foot_x = attach_xy(FOOT_Z, FOOT_R, overlap=0.015)

foot_l = add_sphere("Foot_L", (-foot_x, 0.03, FOOT_Z), FOOT_R, 32, 16, mat_pink)
foot_r = add_sphere("Foot_R", ( foot_x, 0.03, FOOT_Z), FOOT_R, 32, 16, mat_pink)
# Kirby 風 oval 足：x 少し細く、y 前後 stretch、z は 1.0 で tangent 計算通り接触
# （z を 0.7 にすると foot top が 30mm 下がって body との gap が発生するため）
squash(foot_l, 0.9, 1.4, 1.0)
squash(foot_r, 0.9, 1.4, 1.0)

limb_meshes_for_union = [hand_l, hand_r_mesh, foot_l, foot_r]
print(f"[limbs] placed for UNION: hand @ (±{hand_x:.3f}, 0, {HAND_Z}), foot @ (±{foot_x:.3f}, 0.03, {FOOT_Z})")

# ============================================================
# 5.6 Boolean UNION + Merge by Distance + Shade Smooth by Angle
# ============================================================
# Yahoo 知恵袋アプローチ (q13266906933):
#   1. Boolean モディファイア (UNION) で mesh 統合
#   2. 接してる頂点をマージ (Merge by Distance) で seam 頂点をくっつける
#   3. normals_make_consistent で内外正規化
#   4. Shade Smooth by Angle 45° で boolean seam 部分を smooth shading、
#      rim (90° edge) は sharp 保持
# → hand/foot と body の境界が mesh レベルで溶接、seam を smooth normal で消す

def boolean_union(target, cutter):
    bpy.ops.object.select_all(action='DESELECT')
    target.select_set(True)
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new(name="UNION", type='BOOLEAN')
    mod.object = cutter
    mod.operation = 'UNION'
    mod.solver = 'EXACT'
    mod.use_self = True
    try:
        bpy.ops.object.modifier_apply(modifier="UNION")
        solver_used = 'EXACT'
    except Exception as e:
        print(f"[UNION] EXACT failed on {cutter.name}: {e}, retrying with FAST")
        if "UNION" in [m.name for m in target.modifiers]:
            target.modifiers.remove(target.modifiers["UNION"])
        mod = target.modifiers.new(name="UNION", type='BOOLEAN')
        mod.object = cutter
        mod.operation = 'UNION'
        mod.solver = 'FAST'
        bpy.ops.object.modifier_apply(modifier="UNION")
        solver_used = 'FAST'
    bpy.data.objects.remove(cutter, do_unlink=True)
    return solver_used

for limb in limb_meshes_for_union:
    limb_name = limb.name
    solver = boolean_union(body_lower, limb)
    print(f"[UNION] {limb_name} → body_lower ({solver} solver)")

# Merge by Distance + recalc normals in edit mode
bpy.ops.object.select_all(action='DESELECT')
body_lower.select_set(True)
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.0015)
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')

# --- v15.9: Rim edges に crease=1.0 を付ける（Subsurf で rim sharp 保持）
# body_lower の origin は world z=PARTING_Z、LOCAL 座標で rim は z=±0.011
# （bisect は world 0.30 で切る → local z=0 で cut、その後 solidify offset=0 thick=0.022 で ±0.011）
import bmesh
RIM_OUTER_Z_LOCAL = 0.011
RIM_INNER_Z_LOCAL = -0.011
bm = bmesh.new()
bm.from_mesh(body_lower.data)
crease_layer = bm.edges.layers.float.get('crease_edge') or bm.edges.layers.float.new('crease_edge')
creased = 0
for edge in bm.edges:
    z0 = edge.verts[0].co.z
    z1 = edge.verts[1].co.z
    on_outer = abs(z0 - RIM_OUTER_Z_LOCAL) < 0.004 and abs(z1 - RIM_OUTER_Z_LOCAL) < 0.004
    on_inner = abs(z0 - RIM_INNER_Z_LOCAL) < 0.004 and abs(z1 - RIM_INNER_Z_LOCAL) < 0.004
    if on_outer or on_inner:
        edge[crease_layer] = 1.0
        creased += 1
bm.to_mesh(body_lower.data)
bm.free()
print(f"[rim crease] {creased} edges marked (local z=±0.011)")

# --- v15.9: Subdivision Surface level 1 で hand-body blend を smooth 化
# rim edges は crease=1.0 で sharp 保持、他は 1 段 subdivide で seam 滑らか
subsurf = body_lower.modifiers.new(name="SmoothBlend", type='SUBSURF')
subsurf.levels = 1
subsurf.render_levels = 1
subsurf.use_creases = True
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.modifier_apply(modifier="SmoothBlend")

# Shade smooth by angle: 45° で seam smooth、rim crease edge は sharp
bpy.ops.object.shade_smooth()
try:
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(45))
except Exception:
    pass

print(f"[body_lower] after UNION + Subsurf: {len(body_lower.data.vertices)} verts, "
      f"{len(body_lower.data.polygons)} faces")

# limb 個別 mesh は UNION で消費された
limb_meshes = []

# ============================================================
# 6. Eyes + Mouth (v15.10: #91 reference — 顔 flat embedded dots)
# ============================================================
# #90 指摘「顔パーツが浮いてる」→ head 表面に flat disc として埋め込み
# #91 sketch「簡素な :) 」→ 小さな点 eye + 短い smile arc
#
# Head 表面位置計算 (head は squashed sphere: z_scale 0.98, center (0,0,0.82), r=0.29):
#   eye 位置 (x=±0.10, z=0.87) における head 表面 world y:
#     local_z = 0.05 → 元 local_z (squash 前) = 0.05/0.98 = 0.051
#     local_y² = 0.29² - 0.10² - 0.051² = 0.0714 → |local_y| = 0.267
#     head 表面 y ≈ -0.267
EYE_DX = 0.10
EYE_Z = 0.87
EYE_R = 0.045
EYE_Y = -0.257  # head 表面より 10mm 外、squash Y 0.35 で disc 化

eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), EYE_R, 32, 16, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), EYE_R, 32, 16, mat_dark)
# Y 方向 squash で flat disc 化 — 面から「ドット」的に見える（bulge 約 6mm）
squash(eye_l, 1.0, 0.35, 1.1)
squash(eye_r, 1.0, 0.35, 1.1)

# --- Mouth: 小さな smile arc (#91 簡素な :))
# head 表面 y at (0, z=0.81): local_z=-0.01, (−0.01/0.98)²=0.000104
#   local_y² = 0.29² - 0 - 0.000104 = 0.0840 → |y| = 0.2899
#   head 表面 y ≈ -0.290、mouth を 8mm 外に配置
MOUTH_Y = -0.282
MOUTH_Z = 0.80
bpy.ops.mesh.primitive_torus_add(
    location=(0, MOUTH_Y, MOUTH_Z),
    major_radius=0.038,
    minor_radius=0.007,
    rotation=(math.pi/2, 0, 0),
    major_segments=32,
    minor_segments=8,
)
mouth = bpy.context.object
mouth.name = "Mouth"
mouth.data.materials.append(mat_dark)
# Upper half cut — smile curve だけ残す
bpy.ops.mesh.primitive_cube_add(size=0.25, location=(0, MOUTH_Y, MOUTH_Z + 0.13))
boolean_difference(mouth, bpy.context.object)

# ============================================================
# NOTE: 複数表情サポートの設計方針（M2 で実装予定）
# ============================================================
# 現状は 3D mesh eyes + mouth 固定。多表情（smile/sad/wink/laugh 等）の
# 実装方法 3 つ：
#
# A. Texture swap（推奨、Kirby / Mario / 星のカービィ 方式）:
#    - Head 前面に curved plane（decal）を貼り、UV mapping で顔 texture 割当
#    - Unity 側で Material.SetTexture("_BaseMap", expression_N) で瞬時切替
#    - 1 mesh + material swap で animation 可、最軽量、表情追加も PNG 1 枚
#    - 作業量: face plane mesh 追加、UV、PNG 5–10 種作成
#    - 難易度: 中（texture 作成が手間、Meshy AI or 2D illustrator ツール活用）
#
# B. Shape keys（Blender native morph target）:
#    - smile/sad/angry 各表情を shape key として作成
#    - FBX export 時に bake、Unity で Animator/BlendTree 制御
#    - 作業量: shape key × N 表情 × eye/mouth ...
#    - 難易度: 高（Blender sculpting 必要、export の互換性注意）
#
# C. Swappable mesh parts（素朴）:
#    - Eye_L_Smile / Eye_L_Wide / ... 各 variant mesh を armature 配下に
#    - Script で SetActive 切替
#    - 作業量: mesh × N × parts、管理煩雑、非推奨
#
# 推奨: A → B → C の順。当面は fixed mesh で OK、M2 フェーズで A に拡張。
# 顔 plane を追加する準備として、現 eye/mouth は将来 plane に置換可能な
# shallow embed 形状で設計（Y squash 0.35 でほぼ surface decal 同等）。
#
# 「顔が胴体に収納される」ギミック（user 言及）:
#   - "Head" bone で head を translate down（カプセル閉じ時に body 内部へ）
#   - CapHinge bone で dome 開閉 → 組み合わせで gashapon opening anim 実現
#   - 現 armature に Head bone 既存、animation 側で実装（M3-M4）

# ============================================================
# 7. Armature
# ============================================================
bpy.ops.object.armature_add(location=(0, 0, 0))
armature_obj = bpy.context.object
armature_obj.name = "EggybotArmature"
arm = armature_obj.data
arm.name = "EggybotRig"

bpy.ops.object.mode_set(mode='EDIT')
for b in list(arm.edit_bones):
    arm.edit_bones.remove(b)

def bone(name, head_, tail_, parent=None):
    eb = arm.edit_bones.new(name)
    eb.head = head_
    eb.tail = tail_
    if parent is not None:
        eb.parent = parent
        eb.use_connect = False
    return eb

# v15.11: 手足 mesh 位置に bone を合わせる
# 手: (±0.431, 0, 0.22), stretched (1.5, 1.0, 0.9) → 範囲 x∈[±0.311, ±0.551]
# 足: (±0.131, 0.03, -0.10), stretched (0.9, 1.4, 1.0) → 範囲 y∈[-0.11, 0.17]
# Mixamo / Unity Humanoid Rig 互換 naming を維持

hips = bone("Hips", (0, 0, 0.05), (0, 0, 0.18))
spine = bone("Spine", (0, 0, 0.18), (0, 0, 0.28), hips)
chest = bone("Chest", (0, 0, 0.28), (0, 0, 0.55), spine)
neck = bone("Neck", (0, 0, 0.55), (0, 0, 0.65), chest)
head_b = bone("Head", (0, 0, 0.65), (0, 0, 1.10), neck)

# CapHinge: dome bottom back rim (gashapon clam-shell 開閉軸)
cap_hinge_bone = bone("CapHinge", (0, 0.34, PARTING_Z),
                      (0, 0.34, PARTING_Z + 0.12), chest)

# Left arm chain — hand mesh center (0.431, 0, 0.22) に向かって伸びる
sh_l = bone("LeftShoulder", (0.15, 0, 0.28), (0.28, 0, 0.23), chest)
up_l = bone("LeftUpperArm", (0.28, 0, 0.23), (0.40, 0, 0.22), sh_l)
lo_l = bone("LeftLowerArm", (0.40, 0, 0.22), (0.48, 0, 0.22), up_l)
hand_l = bone("LeftHand", (0.48, 0, 0.22), (0.54, 0, 0.22), lo_l)

# Right arm chain — mirror
sh_r = bone("RightShoulder", (-0.15, 0, 0.28), (-0.28, 0, 0.23), chest)
up_r = bone("RightUpperArm", (-0.28, 0, 0.23), (-0.40, 0, 0.22), sh_r)
lo_r = bone("RightLowerArm", (-0.40, 0, 0.22), (-0.48, 0, 0.22), up_r)
hand_r = bone("RightHand", (-0.48, 0, 0.22), (-0.54, 0, 0.22), lo_r)

# Left leg chain — foot mesh center (0.131, 0.03, -0.10)
upl_l = bone("LeftUpperLeg", (0.10, 0, 0.05), (0.131, 0, -0.02), hips)
lol_l = bone("LeftLowerLeg", (0.131, 0, -0.02), (0.131, 0, -0.09), upl_l)
foot_l = bone("LeftFoot", (0.131, 0.03, -0.09), (0.131, -0.08, -0.15), lol_l)

# Right leg chain
upl_r = bone("RightUpperLeg", (-0.10, 0, 0.05), (-0.131, 0, -0.02), hips)
lol_r = bone("RightLowerLeg", (-0.131, 0, -0.02), (-0.131, 0, -0.09), upl_r)
foot_r = bone("RightFoot", (-0.131, 0.03, -0.09), (-0.131, -0.08, -0.15), lol_r)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 8. Parent mesh to armature (ARMATURE_AUTO + CapHinge 明示 weight)
# ============================================================
mesh_objects = [body_lower, head, body_upper, eye_l, eye_r, mouth] + limb_meshes
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# v15.11: body_lower は merged mesh（body + hands + feet 統合）→ ARMATURE_AUTO の
# 熱拡散 weight だと腕 bone が body 半分に影響（31% 汚染検証済）。
# 空間座標で clean に limb 領域を assign し直す。
#   Hand 領域: |x| > 0.30 AND 0.10 < z < 0.32 → LeftUpperArm / RightUpperArm 100%
#   Foot 領域: z < -0.04                       → LeftUpperLeg / RightUpperLeg 100%
#   その他   : ARMATURE_AUTO の既存 weight（Hips/Spine/Chest/Neck/Head/CapHinge）維持

# 既存 limb bone weight を body_lower から剥がす
LIMB_VG_NAMES = ['LeftShoulder', 'LeftUpperArm', 'LeftLowerArm', 'LeftHand',
                 'RightShoulder', 'RightUpperArm', 'RightLowerArm', 'RightHand',
                 'LeftUpperLeg', 'LeftLowerLeg', 'LeftFoot',
                 'RightUpperLeg', 'RightLowerLeg', 'RightFoot']
for vg_name in LIMB_VG_NAMES:
    vg = body_lower.vertex_groups.get(vg_name)
    if vg:
        body_lower.vertex_groups.remove(vg)

# 空間座標 assign：limb bones 復活
vg_luarm = body_lower.vertex_groups.new(name='LeftUpperArm')
vg_ruarm = body_lower.vertex_groups.new(name='RightUpperArm')
vg_luleg = body_lower.vertex_groups.new(name='LeftUpperLeg')
vg_ruleg = body_lower.vertex_groups.new(name='RightUpperLeg')

# 楕円体距離で limb bump にだけ bind（squashed shape に追従、体本体には伸びない）
# Hand 形状: (x stretch 1.5, y 0.7, z 0.95) × HAND_R(0.08) → 0.12 × 0.056 × 0.076
# Foot 形状: (x 0.9, y 1.4, z 1.0) × FOOT_R(0.10) → 0.09 × 0.14 × 0.10
# ellipsoid 半径 = bump 実寸 × 1.15（境界 transition だけ少し緩める）
HAND_RX, HAND_RY, HAND_RZ = 0.120, 0.056, 0.076  # 1.0× bump 実寸（bump 表面のみ）
FOOT_RX, FOOT_RY, FOOT_RZ = 0.090, 0.140, 0.100

HAND_L_CTR = ( hand_x, 0.0, HAND_Z)
HAND_R_CTR = (-hand_x, 0.0, HAND_Z)
FOOT_L_CTR = ( foot_x, 0.03, FOOT_Z)
FOOT_R_CTR = (-foot_x, 0.03, FOOT_Z)

def ellip_d_sq(p, ctr, rx, ry, rz):
    return ((p[0]-ctr[0])/rx)**2 + ((p[1]-ctr[1])/ry)**2 + ((p[2]-ctr[2])/rz)**2

hand_count_l = hand_count_r = foot_count_l = foot_count_r = 0
for i, v in enumerate(body_lower.data.vertices):
    p = (v.co.x, v.co.y, v.co.z + PARTING_Z)

    dl_h = ellip_d_sq(p, HAND_L_CTR, HAND_RX, HAND_RY, HAND_RZ)
    dr_h = ellip_d_sq(p, HAND_R_CTR, HAND_RX, HAND_RY, HAND_RZ)
    dl_f = ellip_d_sq(p, FOOT_L_CTR, FOOT_RX, FOOT_RY, FOOT_RZ)
    dr_f = ellip_d_sq(p, FOOT_R_CTR, FOOT_RX, FOOT_RY, FOOT_RZ)

    candidates = [
        (dl_h, vg_luarm, 'hl'),
        (dr_h, vg_ruarm, 'hr'),
        (dl_f, vg_luleg, 'fl'),
        (dr_f, vg_ruleg, 'fr'),
    ]
    candidates.sort(key=lambda c: c[0])
    best_d, best_vg, best_tag = candidates[0]
    if best_d < 1.0:  # 楕円体内
        best_vg.add([i], 1.0, 'REPLACE')
        if best_tag == 'hl': hand_count_l += 1
        elif best_tag == 'hr': hand_count_r += 1
        elif best_tag == 'fl': foot_count_l += 1
        elif best_tag == 'fr': foot_count_r += 1

# 残りの bone VG は空 group として追加（Unity Humanoid Rig が要求）
for vg_name in ['LeftShoulder', 'LeftLowerArm', 'LeftHand',
                'RightShoulder', 'RightLowerArm', 'RightHand',
                'LeftLowerLeg', 'LeftFoot',
                'RightLowerLeg', 'RightFoot']:
    if not body_lower.vertex_groups.get(vg_name):
        body_lower.vertex_groups.new(name=vg_name)

print(f"[armature] body_lower spatial weights: "
      f"hand L={hand_count_l} R={hand_count_r}, foot L={foot_count_l} R={foot_count_r}")

# body_upper (clear dome) を CapHinge 100% weight で開閉 anim 可能に
for vg_name in list(body_upper.vertex_groups.keys()):
    vg = body_upper.vertex_groups.get(vg_name)
    if vg:
        body_upper.vertex_groups.remove(vg)
vg_cap = body_upper.vertex_groups.new(name="CapHinge")
vg_cap.add(range(len(body_upper.data.vertices)), 1.0, 'REPLACE')
print(f"[armature] body_upper → CapHinge 100% weight ({len(body_upper.data.vertices)} verts)")

# v15.11: head / eye_l / eye_r / mouth を Head bone 100% weight に明示設定
# ARMATURE_AUTO は小さい object で weight 不確定になりがち → 明示化で顔 animation の
# 予測可能性を保証、また gashapon 開閉時の head retract も Head bone 制御で完結
for obj, obj_name in [(head, "Head"), (eye_l, "Eye_L"),
                       (eye_r, "Eye_R"), (mouth, "Mouth")]:
    for vg_name in list(obj.vertex_groups.keys()):
        obj.vertex_groups.remove(obj.vertex_groups[vg_name])
    vg = obj.vertex_groups.new(name="Head")
    vg.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')
print(f"[armature] head + eyes + mouth → Head bone 100% weight (4 objects)")

# ============================================================
# 9. Save + Export
# ============================================================
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(
    filepath=FBX_PATH,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    object_types={'ARMATURE', 'MESH'},
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    use_armature_deform_only=False,
    add_leaf_bones=False,
    bake_anim=False,  # v15.13: ユーザー判断で anim なし clean FBX。Mixamo retarget を Unity 側で実施
    path_mode='AUTO',
    embed_textures=False,
)

print(f"\n=== EGGYBOT v15 build complete (Metaball) ===")
print(f"body_lower = 1 piece metaball mass (body + arms + legs 融合)")
print(f"  resolution 0.015, threshold 0.60, Subsurf 1 level applied")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
