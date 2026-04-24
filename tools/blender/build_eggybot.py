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

    # --- Solidify shell（dome と完全同じパラメータ）
    sol = body_mesh.modifiers.new(name="SHELL", type='SOLIDIFY')
    sol.thickness = WALL_THICKNESS
    sol.offset = 0.0
    sol.use_even_offset = True
    sol.use_quality_normals = True
    sol.use_rim = False
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

# Head — ほぼ球、底軽く flatten
head = add_sphere("Head", (0, 0, 0.82), 0.29, 64, 32, mat_pink)
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

# Hand: Kirby-style, attached at upper-body side. z=0.22 (rim 直下)
HAND_R = 0.08
HAND_Z = 0.22
hand_x = attach_xy(HAND_Z, HAND_R, overlap=0.015)

hand_l = add_sphere("Hand_L", (-hand_x, 0, HAND_Z), HAND_R, 32, 16, mat_pink)
hand_r_mesh = add_sphere("Hand_R", ( hand_x, 0, HAND_Z), HAND_R, 32, 16, mat_pink)

# Foot: Kirby-style, attached below body
FOOT_R = 0.10
FOOT_Z = -0.10
foot_x = attach_xy(FOOT_Z, FOOT_R, overlap=0.015)

foot_l = add_sphere("Foot_L", (-foot_x, 0.03, FOOT_Z), FOOT_R, 32, 16, mat_pink)
foot_r = add_sphere("Foot_R", ( foot_x, 0.03, FOOT_Z), FOOT_R, 32, 16, mat_pink)
# Y方向 stretch で Kirby 風 oval 足に（ただし tangent 計算は pre-stretch 球基準、
# 縦縮めで接触点がずれるが overlap 0.005 のマージン内）
squash(foot_l, 0.9, 1.4, 0.7)
squash(foot_r, 0.9, 1.4, 0.7)

limb_meshes = [hand_l, hand_r_mesh, foot_l, foot_r]
print(f"[limbs] attached: hand @ (±{hand_x:.3f}, 0, {HAND_Z}), foot @ (±{foot_x:.3f}, 0.03, {FOOT_Z})")

# ============================================================
# 6. Eyes + Mouth
# ============================================================
EYE_Y = -0.24
EYE_Z = 0.85
EYE_DX = 0.11
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)

MOUTH_Z = 0.78
bpy.ops.mesh.primitive_torus_add(
    location=(0, -0.305, MOUTH_Z),
    major_radius=0.075,
    minor_radius=0.012,
    rotation=(math.pi/2, 0, 0),
    major_segments=36,
    minor_segments=10,
)
mouth = bpy.context.object
mouth.name = "Mouth"
mouth.data.materials.append(mat_dark)
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.305, MOUTH_Z + 0.25))
boolean_difference(mouth, bpy.context.object)

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

hips = bone("Hips", (0, 0, 0.10), (0, 0, 0.20))
spine = bone("Spine", (0, 0, 0.20), (0, 0, 0.30), hips)
chest = bone("Chest", (0, 0, 0.30), (0, 0, 0.50), spine)
neck = bone("Neck", (0, 0, 0.50), (0, 0, 0.60), chest)
head_b = bone("Head", (0, 0, 0.60), (0, 0, 0.95), neck)
cap_hinge_bone = bone("CapHinge", (0, 0.34, PARTING_Z),
                      (0, 0.34, PARTING_Z + 0.12), chest)
sh_l = bone("LeftShoulder", (0.05, 0, 0.18), (0.15, 0, 0.12), chest)
up_l = bone("LeftUpperArm", (0.15, 0, 0.12), (0.25, 0, 0.10), sh_l)
lo_l = bone("LeftLowerArm", (0.25, 0, 0.10), (0.32, 0, 0.08), up_l)
hand_l = bone("LeftHand", (0.32, 0, 0.08), (0.36, 0, 0.07), lo_l)
sh_r = bone("RightShoulder", (-0.05, 0, 0.18), (-0.15, 0, 0.12), chest)
up_r = bone("RightUpperArm", (-0.15, 0, 0.12), (-0.25, 0, 0.10), sh_r)
lo_r = bone("RightLowerArm", (-0.25, 0, 0.10), (-0.32, 0, 0.08), up_r)
hand_r = bone("RightHand", (-0.32, 0, 0.08), (-0.36, 0, 0.07), lo_r)
upl_l = bone("LeftUpperLeg", (0.18, 0, 0.00), (0.18, 0, -0.12), hips)
lol_l = bone("LeftLowerLeg", (0.18, 0, -0.12), (0.18, 0, -0.22), upl_l)
foot_l = bone("LeftFoot", (0.18, 0, -0.22), (0.18, -0.04, -0.28), lol_l)
upl_r = bone("RightUpperLeg", (-0.18, 0, 0.00), (-0.18, 0, -0.12), hips)
lol_r = bone("RightLowerLeg", (-0.18, 0, -0.12), (-0.18, 0, -0.22), upl_r)
foot_r = bone("RightFoot", (-0.18, 0, -0.22), (-0.18, -0.04, -0.28), lol_r)

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

# body_upper (clear dome) を CapHinge 100% weight で開閉 anim 可能に
for vg_name in list(body_upper.vertex_groups.keys()):
    vg = body_upper.vertex_groups.get(vg_name)
    if vg:
        body_upper.vertex_groups.remove(vg)
vg_cap = body_upper.vertex_groups.new(name="CapHinge")
vg_cap.add(range(len(body_upper.data.vertices)), 1.0, 'REPLACE')
print(f"[armature] body_upper → CapHinge 100% weight ({len(body_upper.data.vertices)} verts)")

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
    bake_anim=False,
    path_mode='AUTO',
    embed_textures=False,
)

print(f"\n=== EGGYBOT v15 build complete (Metaball) ===")
print(f"body_lower = 1 piece metaball mass (body + arms + legs 融合)")
print(f"  resolution 0.015, threshold 0.60, Subsurf 1 level applied")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
