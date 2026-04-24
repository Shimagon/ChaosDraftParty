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
    v15.7: Voxel Remesh「ペタって覆う」方式で Kirby 風 seamless blend。

    パイプライン:
      1. Outer 半球（z<=PARTING_Z のみ）+ Hand x2 + Foot x2 を join（同一 object）
      2. Voxel Remesh で全てを包む 1 枚の連続皮に再生成 → 手足 body 接合 seamless
      3. Inner 半球（smooth, flipped normals）を別途作成 → hollow 保持
      4. Outer remeshed + Inner を最終 join

      結果: outer に手足 bulge が滑らかに融合、inner 側は smooth なまま中空保持。
    """
    OUTER_R = 0.336
    INNER_R = 0.314

    # ===== 1. Outer hemisphere =====
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=OUTER_R, segments=64, ring_count=32,
        location=(0, 0, PARTING_Z),
    )
    outer = bpy.context.object
    outer.name = "BodyLowerOuter"

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.bisect(
        plane_co=(0, 0, PARTING_Z), plane_no=(0, 0, 1),
        clear_outer=True, use_fill=False,
    )
    bpy.ops.object.mode_set(mode='OBJECT')

    if material:
        outer.data.materials.append(material)

    # ===== 2. Hand/foot spheres at calculated attach positions =====
    def attach_xy(z, r_limb, overlap=0.015, body_r=OUTER_R):
        d_req = body_r + r_limb - overlap
        dz = z - PARTING_Z
        return math.sqrt(d_req * d_req - dz * dz)

    HAND_R, HAND_Z = 0.08, 0.19  # rim から少し離して voxel remesh artifact 防ぐ
    FOOT_R, FOOT_Z = 0.10, -0.10
    hand_x = attach_xy(HAND_Z, HAND_R)
    foot_x = attach_xy(FOOT_Z, FOOT_R)

    limb_specs = [
        ("HandL", (-hand_x, 0, HAND_Z), HAND_R, (1, 1, 1)),
        ("HandR", ( hand_x, 0, HAND_Z), HAND_R, (1, 1, 1)),
        ("FootL", (-foot_x, 0.03, FOOT_Z), FOOT_R, (0.9, 1.4, 1.0)),
        ("FootR", ( foot_x, 0.03, FOOT_Z), FOOT_R, (0.9, 1.4, 1.0)),
    ]
    limbs = []
    for name, loc, r, stretch in limb_specs:
        bpy.ops.mesh.primitive_uv_sphere_add(radius=r, segments=32, ring_count=16, location=loc)
        l = bpy.context.object
        l.name = name + "_tmp"
        if stretch != (1, 1, 1):
            l.scale = stretch
            bpy.ops.object.select_all(action='DESELECT')
            l.select_set(True)
            bpy.context.view_layer.objects.active = l
            bpy.ops.object.transform_apply(scale=True)
        if material:
            l.data.materials.append(material)
        limbs.append(l)

    # ===== 3. Join outer + limbs into one =====
    bpy.ops.object.select_all(action='DESELECT')
    for l in limbs:
        l.select_set(True)
    outer.select_set(True)
    bpy.context.view_layer.objects.active = outer
    bpy.ops.object.join()
    body_outer = outer  # joined result

    # ===== 4. Voxel Remesh — user's 「ペタって覆う」 idea =====
    # voxel 小さいほど形保持、大きいほど滑らかブレンド。0.012 (12mm) で balance
    remesh = body_outer.modifiers.new(name="VoxelWrap", type='REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = 0.006
    remesh.use_smooth_shade = True
    bpy.ops.object.modifier_apply(modifier="VoxelWrap")
    print(f"[body_lower] v15.7 voxel remesh: {len(body_outer.data.vertices)} verts (outer)")

    # --- Smooth modifier で voxel artifacts を後処理
    smooth = body_outer.modifiers.new(name="SmoothVoxelArtifacts", type='SMOOTH')
    smooth.factor = 1.0
    smooth.iterations = 15
    bpy.ops.object.modifier_apply(modifier="SmoothVoxelArtifacts")

    # Subsurf 1 level で最終 polish
    subsurf = body_outer.modifiers.new(name="FinalSmooth", type='SUBSURF')
    subsurf.levels = 1
    subsurf.render_levels = 1
    bpy.ops.object.modifier_apply(modifier="FinalSmooth")

    # ===== 5. Inner smooth hemisphere (flipped normals, 手足 bump なし) =====
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=INNER_R, segments=64, ring_count=32,
        location=(0, 0, PARTING_Z),
    )
    inner = bpy.context.object
    inner.name = "BodyLowerInner"

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.bisect(
        plane_co=(0, 0, PARTING_Z), plane_no=(0, 0, 1),
        clear_outer=True, use_fill=False,
    )
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.flip_normals()
    bpy.ops.object.mode_set(mode='OBJECT')

    if material:
        inner.data.materials.append(material)

    # ===== 6. Join inner into body_outer for final shell =====
    bpy.ops.object.select_all(action='DESELECT')
    inner.select_set(True)
    body_outer.select_set(True)
    bpy.context.view_layer.objects.active = body_outer
    bpy.ops.object.join()

    body_mesh = body_outer
    body_mesh.name = "BodyLower"
    body_mesh.data.name = "BodyLowerMesh"

    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(60))
    except Exception:
        pass

    print(f"[body_lower] v15.7 final: {len(body_mesh.data.vertices)} verts (outer+inner joined)")
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
# v15.7: 手足は create_body_lower_metaball() 内で Voxel Remesh により body と融合済み

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
mesh_objects = [body_lower, head, body_upper, eye_l, eye_r, mouth]
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
