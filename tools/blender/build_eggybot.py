"""
EGGYBOT v2 Blender build — カービー型もっちり + ガチャカプセル構造（上下分離 + ヒンジ金具 + 中空）

Design per /キャラクター/詳細イメージ.JPG:
  - Lower body pink opaque (pot/bowl shape)
  - Upper clear cap (top half dome, hollow, removable — opens on hinge)
  - Head = separate small body sitting on top (retracts INSIDE cap in crouch form)
  - Metal hinge ring at parting line between cap and body
  - Arms/legs: Kirby-esque stubby ellipsoids with squishy proportions
  - Head: slightly oblate (squash Z) for もっちり feel

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python build_eggybot.py

Output:
  Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx
  Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend

Research sources:
  - Kirby shape analysis: https://articles.retroware.com/2021/09/16/kirbys-simple-character-design-defines-his-games/
  - Blender hollow shell methods: https://docs.blender.org/manual/en/latest/modeling/modifiers/generate/booleans.html
"""
import bpy
import math
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
FBX_PATH = os.path.join(OUT_DIR, "eggybot_base.fbx")
BLEND_PATH = os.path.join(OUT_DIR, "eggybot_source.blend")

os.makedirs(OUT_DIR, exist_ok=True)

# ============================================================
# 0. Clear scene
# ============================================================
bpy.ops.wm.read_factory_settings(use_empty=True)

# ============================================================
# 1. Materials
# ============================================================
def make_material(name, base_color, alpha=1.0, transparent=False, metallic=0.0, roughness=0.45):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_color, alpha)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = 0.18 if transparent else roughness
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if transparent and "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha
    mat.diffuse_color = (*base_color, alpha)
    return mat

PINK = (0.96, 0.55, 0.65)
DARK = (0.05, 0.05, 0.05)
WHITE = (1.0, 1.0, 1.0)
METAL = (0.72, 0.74, 0.78)

mat_pink = make_material("EGGYBOT_Pink", PINK, 1.0)
mat_clear = make_material("EGGYBOT_Clear", (1.0, 1.0, 1.0), 0.25, transparent=True)
mat_dark = make_material("EGGYBOT_Eye", DARK, 1.0)
mat_white = make_material("EGGYBOT_EyeShine", WHITE, 1.0)
mat_metal = make_material("EGGYBOT_Metal", METAL, 1.0, metallic=0.85, roughness=0.25)


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


def boolean_difference(target, cutter, apply_ok=True):
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new(name="CUT", type='BOOLEAN')
    mod.object = cutter
    mod.operation = 'DIFFERENCE'
    if apply_ok:
        bpy.ops.object.modifier_apply(modifier="CUT")
        bpy.data.objects.remove(cutter, do_unlink=True)


# ============================================================
# 3. Body Lower — opaque pink pot (below parting line)
#    カービー的もっちり: 軽く上下 squash + XY 広げる
# ============================================================
PARTING_Z = 0.38  # capsule 上下分割ライン

body_lower = add_sphere("BodyLower", (0, 0, 0.30), 0.32, 48, 24, mat_pink)

# Cut top above parting line
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 1.0))
boolean_difference(body_lower, bpy.context.object)

# ============================================================
# 4. Cap Lower (Clear hollow ring below cap hinge)
#    薄いリング = 上下継ぎ目を覆う透明 band
# ============================================================
cap_lower = add_sphere("BodyUpperClear", (0, 0, 0.30), 0.325, 48, 24, mat_clear)
# Keep only thin slice just above parting line (between PARTING_Z and PARTING_Z + 0.03)
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z - 1.0))
boolean_difference(cap_lower, bpy.context.object)
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 0.05 + 1.0))
boolean_difference(cap_lower, bpy.context.object)
# Solidify to give wall thickness (hollow ring)
bpy.context.view_layer.objects.active = cap_lower
sol = cap_lower.modifiers.new(name="SHELL", type='SOLIDIFY')
sol.thickness = -0.012  # negative = inner
sol.offset = 1.0
bpy.ops.object.modifier_apply(modifier="SHELL")

# ============================================================
# 5. Cap Upper — the removable dome (hinges open)
#    中空ドーム: 頭が収納できるスペース
# ============================================================
cap_upper = add_sphere("CapUpperClear", (0, 0, 0.30), 0.325, 48, 24, mat_clear)
# Cut bottom below parting line + 0.05 (sits right on top of cap_lower ring)
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 0.05 - 1.0))
boolean_difference(cap_upper, bpy.context.object)
# Solidify to hollow interior (wall thickness)
bpy.context.view_layer.objects.active = cap_upper
sol = cap_upper.modifiers.new(name="SHELL", type='SOLIDIFY')
sol.thickness = -0.015
sol.offset = 1.0
bpy.ops.object.modifier_apply(modifier="SHELL")

# ============================================================
# 6. Metal Hinge Ring — at parting line
#    ガチャカプセル金属留め金
# ============================================================
bpy.ops.mesh.primitive_torus_add(
    location=(0, 0, PARTING_Z + 0.025),
    major_radius=0.322,
    minor_radius=0.014,
    major_segments=48,
    minor_segments=10,
)
hinge = bpy.context.object
hinge.name = "HingeRing"
hinge.data.materials.append(mat_metal)
bpy.ops.object.shade_smooth()

# Small hinge pin (rear, at -Y)
bpy.ops.mesh.primitive_cylinder_add(
    radius=0.022, depth=0.07,
    location=(0, 0.32, PARTING_Z + 0.025),
    rotation=(math.pi/2, 0, 0),
)
hinge_pin = bpy.context.object
hinge_pin.name = "HingePin"
hinge_pin.data.materials.append(mat_metal)
bpy.ops.object.shade_smooth()

# Small clasp (front, at +Y for "lock")
bpy.ops.mesh.primitive_cube_add(size=0.055, location=(0, -0.32, PARTING_Z + 0.025))
clasp = bpy.context.object
clasp.name = "HingeClasp"
clasp.data.materials.append(mat_metal)
squash(clasp, 1.0, 0.6, 0.75)
bpy.ops.object.shade_smooth()

# ============================================================
# 7. Head — もっちりカービー頭（軽く squash）
# ============================================================
head = add_sphere("Head", (0, 0, 0.90), 0.26, 48, 24, mat_pink)
squash(head, 1.08, 1.08, 0.92)  # oblate、marshmallow-like

# ============================================================
# 8. Eyes — 大きい丸目 + 白ハイライト
# ============================================================
EYE_Y = -0.24
EYE_Z = 0.92
EYE_DX = 0.11
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.042, 20, 12, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.042, 20, 12, mat_dark)
hi_l = add_sphere("EyeShine_L", (-EYE_DX - 0.014, EYE_Y - 0.028, EYE_Z + 0.018), 0.012, 12, 8, mat_white)
hi_r = add_sphere("EyeShine_R", ( EYE_DX - 0.014, EYE_Y - 0.028, EYE_Z + 0.018), 0.012, 12, 8, mat_white)

# ============================================================
# 9. Mouth — 笑み smile torus
# ============================================================
MOUTH_Z = 0.83
bpy.ops.mesh.primitive_torus_add(
    location=(0, -0.248, MOUTH_Z),
    major_radius=0.055,
    minor_radius=0.006,
    rotation=(math.pi/2, 0, 0),
    major_segments=28,
    minor_segments=8,
)
mouth = bpy.context.object
mouth.name = "Mouth"
mouth.data.materials.append(mat_dark)
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.248, MOUTH_Z + 0.02))
boolean_difference(mouth, bpy.context.object)

# ============================================================
# 10. Arms — Kirby-esque stubby nubs (slightly squished)
# ============================================================
arm_l = add_sphere("Arm_L", (-0.34, 0, 0.30), 0.092, 24, 12, mat_pink)
squash(arm_l, 1.15, 0.92, 0.96)
arm_r = add_sphere("Arm_R", ( 0.34, 0, 0.30), 0.092, 24, 12, mat_pink)
squash(arm_r, 1.15, 0.92, 0.96)

# ============================================================
# 11. Legs — chunkier Kirby feet (slightly squat, bigger)
# ============================================================
leg_l = add_sphere("Leg_L", (-0.14, 0.025, 0.05), 0.115, 24, 12, mat_pink)
squash(leg_l, 1.12, 1.10, 0.78)
leg_r = add_sphere("Leg_R", ( 0.14, 0.025, 0.05), 0.115, 24, 12, mat_pink)
squash(leg_r, 1.12, 1.10, 0.78)

# ============================================================
# 12. Armature — Unity Humanoid-compatible bones
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

# CapHinge bone — allows cap_upper to rotate open
cap_hinge_bone = bone("CapHinge", (0, 0.30, PARTING_Z + 0.025), (0, 0.30, PARTING_Z + 0.10), chest)

sh_l = bone("LeftShoulder", (0.05, 0, 0.35), (0.18, 0, 0.32), chest)
up_l = bone("LeftUpperArm", (0.18, 0, 0.32), (0.28, 0, 0.30), sh_l)
lo_l = bone("LeftLowerArm", (0.28, 0, 0.30), (0.34, 0, 0.30), up_l)
hand_l = bone("LeftHand", (0.34, 0, 0.30), (0.38, 0, 0.30), lo_l)

sh_r = bone("RightShoulder", (-0.05, 0, 0.35), (-0.18, 0, 0.32), chest)
up_r = bone("RightUpperArm", (-0.18, 0, 0.32), (-0.28, 0, 0.30), sh_r)
lo_r = bone("RightLowerArm", (-0.28, 0, 0.30), (-0.34, 0, 0.30), up_r)
hand_r = bone("RightHand", (-0.34, 0, 0.30), (-0.38, 0, 0.30), lo_r)

upl_l = bone("LeftUpperLeg", (0.14, 0, 0.22), (0.14, 0, 0.10), hips)
lol_l = bone("LeftLowerLeg", (0.14, 0, 0.10), (0.14, 0, 0.05), upl_l)
foot_l = bone("LeftFoot", (0.14, 0, 0.05), (0.14, -0.02, 0.0), lol_l)

upl_r = bone("RightUpperLeg", (-0.14, 0, 0.22), (-0.14, 0, 0.10), hips)
lol_r = bone("RightLowerLeg", (-0.14, 0, 0.10), (-0.14, 0, 0.05), upl_r)
foot_r = bone("RightFoot", (-0.14, 0, 0.05), (-0.14, -0.02, 0.0), lol_r)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 13. Parent meshes to armature
# ============================================================
mesh_objects = [
    head, body_lower, cap_lower, cap_upper,
    arm_l, arm_r, leg_l, leg_r,
    eye_l, eye_r, hi_l, hi_r, mouth,
    hinge, hinge_pin, clasp,
]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# ============================================================
# 14. Save .blend + export FBX
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

print(f"=== EGGYBOT v2 build complete ===")
print(f"Blend: {BLEND_PATH}")
print(f"FBX:   {FBX_PATH}")
print(f"Bones: {len(arm.bones)}")
print(f"Meshes: {len(mesh_objects)}")
