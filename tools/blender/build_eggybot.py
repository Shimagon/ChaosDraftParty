"""
EGGYBOT v3 Blender build — 純クリアガラス球 cap + ぷにぷに強化 + 金具削除

Design per user's reference image (gacha capsule with cap swung open):
  - Body: pink opaque pot with wide open rim on top
  - Cap: pure clear glass sphere top half (fully hollow)
  - Head: pink mochi sphere (heavy squash for ぷにぷに feel)
  - NO metal hinges/clasps (user says not that)
  - Cap can rotate open via CapHinge bone at rear

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python build_eggybot.py

Research sources:
  - URP glass Fresnel: https://danielilett.com/shader-toolbox/glass/
  - Kirby squash: https://articles.retroware.com/2021/09/16/kirbys-simple-character-design-defines-his-games/
"""
import bpy
import math
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
FBX_PATH = os.path.join(OUT_DIR, "eggybot_base.fbx")
BLEND_PATH = os.path.join(OUT_DIR, "eggybot_source.blend")

os.makedirs(OUT_DIR, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

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
# 3. Body Lower — pink pot with open top rim
# ============================================================
PARTING_Z = 0.40

body_lower = add_sphere("BodyLower", (0, 0, 0.30), 0.34, 48, 24, mat_pink)
# Cut top above parting line
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 1.0))
boolean_difference(body_lower, bpy.context.object)

# ============================================================
# 4. Cap Upper Clear — complete hollow glass hemisphere
#    (keeping mesh name "BodyUpperClear" for Unity CharacterLabBuilder compat)
# ============================================================
body_upper = add_sphere("BodyUpperClear", (0, 0, 0.30), 0.345, 56, 28, mat_clear)
# Cut bottom below parting line
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z - 1.0))
boolean_difference(body_upper, bpy.context.object)
# Hollow via Solidify (wall thickness)
bpy.context.view_layer.objects.active = body_upper
sol = body_upper.modifiers.new(name="SHELL", type='SOLIDIFY')
sol.thickness = -0.012
sol.offset = 1.0
bpy.ops.object.modifier_apply(modifier="SHELL")

# ============================================================
# 5. Head — pink mochi (heavy squash for ぷにぷに)
#    Head の底を body rim に少し沈み込ませる + 底面を slight flat で受け皿感
#    Blender Stylized Character Workflow 参考:
#    https://studio.blender.org/training/stylized-character-workflow/
# ============================================================
head = add_sphere("Head", (0, 0, 0.78), 0.30, 56, 28, mat_pink)
squash(head, 1.10, 1.10, 0.82)  # oblate = marshmallow 感

# 頭の下半分を少し flat に（body rim に乗る「接地面」をつくる）
# 下半分の vertex を Z+ 方向に 0.03 押し上げる → 底面が flatten
bpy.context.view_layer.objects.active = head
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='DESELECT')
bpy.ops.object.mode_set(mode='OBJECT')
# 底面付近の vertex を flatten
for v in head.data.vertices:
    world_z = v.co.z + head.location.z
    if world_z < 0.70:  # head 底部の vertex
        flatten_ratio = (0.70 - world_z) / 0.15
        flatten_ratio = min(1.0, max(0.0, flatten_ratio))
        v.co.z += flatten_ratio * 0.05  # 底を押し上げて flat に

# ============================================================
# 6. Eyes + highlights
# ============================================================
EYE_Y = -0.27
EYE_Z = 0.82
EYE_DX = 0.13
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.048, 20, 12, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.048, 20, 12, mat_dark)
hi_l = add_sphere("EyeShine_L", (-EYE_DX - 0.018, EYE_Y - 0.033, EYE_Z + 0.022), 0.015, 14, 10, mat_white)
hi_r = add_sphere("EyeShine_R", ( EYE_DX - 0.018, EYE_Y - 0.033, EYE_Z + 0.022), 0.015, 14, 10, mat_white)

# ============================================================
# 7. Mouth — smile
# ============================================================
MOUTH_Z = 0.73
bpy.ops.mesh.primitive_torus_add(
    location=(0, -0.28, MOUTH_Z),
    major_radius=0.060,
    minor_radius=0.007,
    rotation=(math.pi/2, 0, 0),
    major_segments=32,
    minor_segments=8,
)
mouth = bpy.context.object
mouth.name = "Mouth"
mouth.data.materials.append(mat_dark)
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.28, MOUTH_Z + 0.02))
boolean_difference(mouth, bpy.context.object)

# ============================================================
# 8. Arms — ぷにぷに stubby、body 側面に 0.04 食い込ませて seamless に
# ============================================================
arm_l = add_sphere("Arm_L", (-0.31, 0, 0.26), 0.110, 28, 14, mat_pink)
squash(arm_l, 1.20, 0.88, 0.95)
arm_r = add_sphere("Arm_R", ( 0.31, 0, 0.26), 0.110, 28, 14, mat_pink)
squash(arm_r, 1.20, 0.88, 0.95)

# ============================================================
# 9. Legs — カービー型 big round feet、body 下部に食い込み
# ============================================================
leg_l = add_sphere("Leg_L", (-0.13, 0.025, 0.04), 0.135, 28, 14, mat_pink)
squash(leg_l, 1.15, 1.15, 0.72)
leg_r = add_sphere("Leg_R", ( 0.13, 0.025, 0.04), 0.135, 28, 14, mat_pink)
squash(leg_r, 1.15, 1.15, 0.72)

# ============================================================
# 10. Armature
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

hips = bone("Hips", (0, 0, 0.25), (0, 0, 0.35))
spine = bone("Spine", (0, 0, 0.35), (0, 0, 0.45), hips)
chest = bone("Chest", (0, 0, 0.45), (0, 0, 0.60), spine)
neck = bone("Neck", (0, 0, 0.60), (0, 0, 0.70), chest)
head_b = bone("Head", (0, 0, 0.70), (0, 0, 1.10), neck)

# Cap hinge at rear (so cap swings back when rotated)
cap_hinge_bone = bone("CapHinge", (0, 0.34, PARTING_Z), (0, 0.34, PARTING_Z + 0.12), chest)

sh_l = bone("LeftShoulder", (0.05, 0, 0.35), (0.20, 0, 0.30), chest)
up_l = bone("LeftUpperArm", (0.20, 0, 0.30), (0.30, 0, 0.28), sh_l)
lo_l = bone("LeftLowerArm", (0.30, 0, 0.28), (0.35, 0, 0.28), up_l)
hand_l = bone("LeftHand", (0.35, 0, 0.28), (0.40, 0, 0.28), lo_l)

sh_r = bone("RightShoulder", (-0.05, 0, 0.35), (-0.20, 0, 0.30), chest)
up_r = bone("RightUpperArm", (-0.20, 0, 0.30), (-0.30, 0, 0.28), sh_r)
lo_r = bone("RightLowerArm", (-0.30, 0, 0.28), (-0.35, 0, 0.28), up_r)
hand_r = bone("RightHand", (-0.35, 0, 0.28), (-0.40, 0, 0.28), lo_r)

upl_l = bone("LeftUpperLeg", (0.14, 0, 0.22), (0.14, 0, 0.10), hips)
lol_l = bone("LeftLowerLeg", (0.14, 0, 0.10), (0.14, 0, 0.05), upl_l)
foot_l = bone("LeftFoot", (0.14, 0, 0.05), (0.14, -0.02, 0.0), lol_l)

upl_r = bone("RightUpperLeg", (-0.14, 0, 0.22), (-0.14, 0, 0.10), hips)
lol_r = bone("RightLowerLeg", (-0.14, 0, 0.10), (-0.14, 0, 0.05), upl_r)
foot_r = bone("RightFoot", (-0.14, 0, 0.05), (-0.14, -0.02, 0.0), lol_r)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 11. Parent meshes to armature
# ============================================================
mesh_objects = [
    head, body_lower, body_upper,
    arm_l, arm_r, leg_l, leg_r,
    eye_l, eye_r, hi_l, hi_r, mouth,
]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# ============================================================
# 12. Save + Export
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

print(f"=== EGGYBOT v3 build complete ===")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
