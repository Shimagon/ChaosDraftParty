"""
EGGYBOT v5 Blender build — pink parts voxel remesh 統合、接合部 seamless

Design:
  - PinkBody = BodyLower + Head + Arms + Legs を join → voxel remesh で 1 seamless mesh 化
  - BodyUpperClear は別 mesh (glass)
  - Eye / Mouth / EyeShine は別 mesh (material 異なる)
  - 結果: Lego cube 合体感が完全に消える、organic 1体

Research:
  https://docs.blender.org/manual/en/latest/modeling/modifiers/generate/remesh.html
  https://tryfix.it.com/how-to-join-all-meshes-together-a-guide-to-seamless-3d-models/
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
# 3. Pink Parts — modeled as separate overlapping spheres, then joined + remeshed
#    intentional overlap ensures seamless remesh welding
# ============================================================
PARTING_Z = 0.40

# Body Lower (pot)
body_lower = add_sphere("_BodyLower", (0, 0, 0.30), 0.34, 56, 28, mat_pink)
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 1.0))
boolean_difference(body_lower, bpy.context.object)

# Head — overlaps body pot rim so remesh welds them
head_part = add_sphere("_Head", (0, 0, 0.72), 0.30, 56, 28, mat_pink)
squash(head_part, 1.10, 1.10, 0.82)

# Arms — intentionally overlap body
arm_l = add_sphere("_Arm_L", (-0.29, 0, 0.26), 0.115, 32, 16, mat_pink)
squash(arm_l, 1.18, 0.92, 0.98)
arm_r = add_sphere("_Arm_R", ( 0.29, 0, 0.26), 0.115, 32, 16, mat_pink)
squash(arm_r, 1.18, 0.92, 0.98)

# Legs — overlap bottom of body pot
leg_l = add_sphere("_Leg_L", (-0.13, 0.025, 0.05), 0.135, 32, 16, mat_pink)
squash(leg_l, 1.15, 1.15, 0.72)
leg_r = add_sphere("_Leg_R", ( 0.13, 0.025, 0.05), 0.135, 32, 16, mat_pink)
squash(leg_r, 1.15, 1.15, 0.72)

# ============================================================
# 4. JOIN all pink parts → VOXEL REMESH for seamless welding
# ============================================================
bpy.ops.object.select_all(action='DESELECT')
pink_parts = [body_lower, head_part, arm_l, arm_r, leg_l, leg_r]
for p in pink_parts:
    p.select_set(True)
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.join()  # body_lower becomes the combined mesh

pink_body = bpy.context.object
pink_body.name = "PinkBody"

# Apply Remesh modifier (Voxel mode, small voxel_size = higher fidelity)
remesh = pink_body.modifiers.new(name="REMESH", type='REMESH')
remesh.mode = 'VOXEL'
remesh.voxel_size = 0.02  # 0.02 m = 2cm voxel → detail while welding
remesh.use_smooth_shade = True
bpy.ops.object.modifier_apply(modifier="REMESH")

# Shade smooth
bpy.context.view_layer.objects.active = pink_body
bpy.ops.object.shade_smooth()

# Reassign pink material (may be lost in join)
pink_body.data.materials.clear()
pink_body.data.materials.append(mat_pink)

# ============================================================
# 5. Body Upper Clear (Glass dome) — separate mesh
# ============================================================
body_upper = add_sphere("BodyUpperClear", (0, 0, 0.30), 0.345, 64, 32, mat_clear)
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z - 1.0))
boolean_difference(body_upper, bpy.context.object)
bpy.context.view_layer.objects.active = body_upper
sol = body_upper.modifiers.new(name="SHELL", type='SOLIDIFY')
sol.thickness = -0.012
sol.offset = 1.0
bpy.ops.object.modifier_apply(modifier="SHELL")

# ============================================================
# 6. Eyes + highlights
# ============================================================
EYE_Y = -0.27
EYE_Z = 0.76
EYE_DX = 0.13
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.050, 24, 14, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.050, 24, 14, mat_dark)
hi_l = add_sphere("EyeShine_L", (-EYE_DX - 0.020, EYE_Y - 0.035, EYE_Z + 0.024), 0.016, 16, 10, mat_white)
hi_r = add_sphere("EyeShine_R", ( EYE_DX - 0.020, EYE_Y - 0.035, EYE_Z + 0.024), 0.016, 16, 10, mat_white)

# ============================================================
# 7. Mouth smile
# ============================================================
MOUTH_Z = 0.67
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
# 8. Armature
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
chest = bone("Chest", (0, 0, 0.45), (0, 0, 0.58), spine)
neck = bone("Neck", (0, 0, 0.58), (0, 0, 0.64), chest)
head_b = bone("Head", (0, 0, 0.64), (0, 0, 0.90), neck)

cap_hinge_bone = bone("CapHinge", (0, 0.34, PARTING_Z), (0, 0.34, PARTING_Z + 0.12), chest)

sh_l = bone("LeftShoulder", (0.05, 0, 0.32), (0.20, 0, 0.28), chest)
up_l = bone("LeftUpperArm", (0.20, 0, 0.28), (0.28, 0, 0.26), sh_l)
lo_l = bone("LeftLowerArm", (0.28, 0, 0.26), (0.33, 0, 0.26), up_l)
hand_l = bone("LeftHand", (0.33, 0, 0.26), (0.36, 0, 0.26), lo_l)

sh_r = bone("RightShoulder", (-0.05, 0, 0.32), (-0.20, 0, 0.28), chest)
up_r = bone("RightUpperArm", (-0.20, 0, 0.28), (-0.28, 0, 0.26), sh_r)
lo_r = bone("RightLowerArm", (-0.28, 0, 0.26), (-0.33, 0, 0.26), up_r)
hand_r = bone("RightHand", (-0.33, 0, 0.26), (-0.36, 0, 0.26), lo_r)

upl_l = bone("LeftUpperLeg", (0.13, 0, 0.22), (0.13, 0, 0.10), hips)
lol_l = bone("LeftLowerLeg", (0.13, 0, 0.10), (0.13, 0, 0.05), upl_l)
foot_l = bone("LeftFoot", (0.13, 0, 0.05), (0.13, -0.02, 0.0), lol_l)

upl_r = bone("RightUpperLeg", (-0.13, 0, 0.22), (-0.13, 0, 0.10), hips)
lol_r = bone("RightLowerLeg", (-0.13, 0, 0.10), (-0.13, 0, 0.05), upl_r)
foot_r = bone("RightFoot", (-0.13, 0, 0.05), (-0.13, -0.02, 0.0), lol_r)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 9. Parent meshes to armature
# ============================================================
mesh_objects = [pink_body, body_upper, eye_l, eye_r, hi_l, hi_r, mouth]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# ============================================================
# 10. Save + Export
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

print(f"=== EGGYBOT v5 build complete ===")
print(f"PinkBody voxel remeshed, seamless")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
