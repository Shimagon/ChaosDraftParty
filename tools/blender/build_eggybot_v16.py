"""
EGGYBOT v16 Blender build — 胴体のみ (手足完全削除)。

v15 から手足 (Hand_L/R + Foot_L/R) の sphere mesh、Boolean UNION、対応 bone、
vertex group を全部撤去。出力は body shell のみで closed manifold な「卵カプセル」。
手足は Unity 側で primitive Sphere を Transform parent で後付けする。

Bones (v15 の 20本 → v16 で 6本):
  Hips → Spine → Chest → Neck → Head
                 └─ CapHinge (dome 開閉)

Mesh:
  - BodyLower: 半球 bowl shell (Solidify thickness 0.022, closed manifold)
  - BodyUpperClear: 半球 dome shell (CapHinge bone weight 100%)
  - HeadMesh: 上に乗る頭 sphere
  - Eye_L / Eye_R / Mouth: 顔パーツ
  - 手足 mesh は一切なし

Output: eggybot_v16.fbx (eggybot_base.fbx と並行管理、v15 retarget 設定を壊さない)
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
FBX_PATH = os.path.join(OUT_DIR, "eggybot_v16.fbx")
BLEND_PATH = os.path.join(OUT_DIR, "eggybot_v16_source.blend")

os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

PARTING_Z = 0.30

# ============================================================
# 1. Materials
# ============================================================
def make_material(name, base_color, alpha=1.0, transparent=False):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_color, alpha)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = 0.08 if transparent else 0.45
        if transparent and "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha
    mat.diffuse_color = (*base_color, alpha)
    return mat

PINK = (0.96, 0.55, 0.65)
DARK = (0.05, 0.05, 0.05)

mat_pink = make_material("EGGYBOT_Pink", PINK, 1.0)
mat_clear = make_material("EGGYBOT_Clear", (1.0, 1.0, 1.0), 0.15, transparent=True)
mat_dark = make_material("EGGYBOT_Eye", DARK, 1.0)

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
# 3. BodyLower (bowl shell, no limbs)
# ============================================================
def create_body_lower(material=None):
    BASE_SPHERE_R = 0.325
    WALL_THICKNESS = 0.022

    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=BASE_SPHERE_R,
        segments=64,
        ring_count=32,
        location=(0, 0, PARTING_Z),
    )
    body_mesh = bpy.context.object
    body_mesh.name = "BodyLower"
    body_mesh.data.name = "BodyLowerMesh"

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

    sol = body_mesh.modifiers.new(name="SHELL", type='SOLIDIFY')
    sol.thickness = WALL_THICKNESS
    sol.offset = 0.0
    sol.use_even_offset = True
    sol.use_quality_normals = True
    sol.use_rim = True
    sol.use_rim_only = False
    bpy.ops.object.modifier_apply(modifier="SHELL")

    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(60))
    except Exception:
        pass

    if material:
        body_mesh.data.materials.append(material)

    print(f"[body_lower v16] hollow shell: outer r={BASE_SPHERE_R + WALL_THICKNESS/2:.3f}, "
          f"{len(body_mesh.data.vertices)} verts (NO LIMBS)")
    return body_mesh

# ============================================================
# 4. Build: body, head, dome, eyes, mouth
# ============================================================
body_lower = create_body_lower(material=mat_pink)

# Head
head = add_sphere("HeadMesh", (0, 0, 0.82), 0.29, 64, 32, mat_pink)
squash(head, 1.00, 1.00, 0.98)
for v in head.data.vertices:
    wz = v.co.z + head.location.z
    if wz < 0.74:
        ratio = min(1.0, max(0.0, (0.74 - wz) / 0.08))
        v.co.z += ratio * 0.015

# BodyUpperClear (dome)
BASE_SPHERE_R = 0.325
WALL_THICKNESS = 0.022
body_upper = add_sphere("BodyUpperClear", (0, 0, PARTING_Z), BASE_SPHERE_R, 64, 32, mat_clear)
bpy.context.view_layer.objects.active = body_upper
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.bisect(
    plane_co=(0, 0, PARTING_Z),
    plane_no=(0, 0, 1),
    clear_inner=True,
    use_fill=False,
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

# Eyes
EYE_DX = 0.10
EYE_Z = 0.87
EYE_R = 0.045
EYE_Y = -0.257

eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), EYE_R, 32, 16, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), EYE_R, 32, 16, mat_dark)
squash(eye_l, 1.0, 0.35, 1.1)
squash(eye_r, 1.0, 0.35, 1.1)

# Mouth (smile arc)
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
bpy.ops.mesh.primitive_cube_add(size=0.25, location=(0, MOUTH_Y, MOUTH_Z + 0.13))
boolean_difference(mouth, bpy.context.object)

# Rim crease for BodyLower (preserve sharp rim during Subsurf)
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
print(f"[rim crease] {creased} edges marked")

# ============================================================
# 5. Armature (v16: 6 bones only, no arms/legs)
# ============================================================
bpy.ops.object.armature_add(location=(0, 0, 0))
armature_obj = bpy.context.object
armature_obj.name = "EggybotArmature"
arm = armature_obj.data
arm.name = "EggybotV16Rig"

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

hips = bone("Hips", (0, 0, 0.05), (0, 0, 0.18))
spine = bone("Spine", (0, 0, 0.18), (0, 0, 0.28), hips)
chest = bone("Chest", (0, 0, 0.28), (0, 0, 0.55), spine)
neck = bone("Neck", (0, 0, 0.55), (0, 0, 0.65), chest)
head_b = bone("Head", (0, 0, 0.65), (0, 0, 1.10), neck)
cap_hinge_bone = bone("CapHinge", (0, 0.34, PARTING_Z),
                      (0, 0.34, PARTING_Z + 0.12), chest)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 6. Parent meshes to armature with explicit weights
# ============================================================
mesh_objects = [body_lower, head, body_upper, eye_l, eye_r, mouth]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# Override: explicit single-bone weights (clean, no auto bleed)
def assign_single_bone(obj, bone_name):
    for vg_name in list(obj.vertex_groups.keys()):
        obj.vertex_groups.remove(obj.vertex_groups[vg_name])
    vg = obj.vertex_groups.new(name=bone_name)
    vg.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')

# BodyLower → Hips 100%
assign_single_bone(body_lower, "Hips")
# BodyUpperClear → CapHinge 100% (dome 開閉用)
assign_single_bone(body_upper, "CapHinge")
# Head + Eye + Mouth → Head 100%
assign_single_bone(head, "Head")
assign_single_bone(eye_l, "Head")
assign_single_bone(eye_r, "Head")
assign_single_bone(mouth, "Head")

print(f"[armature v16] {len(arm.bones)} bones, {len(mesh_objects)} meshes assigned")

# ============================================================
# 7. Save + Export
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

print(f"\n=== EGGYBOT v16 build complete (NO LIMBS, body-only) ===")
print(f"Bones: {len(arm.bones)} (vs v15 = 20)")
print(f"Meshes: {len(mesh_objects)} (vs v15 = 8)")
print(f"FBX: {FBX_PATH}")
print(f"BLEND: {BLEND_PATH}")
