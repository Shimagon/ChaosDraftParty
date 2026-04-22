"""
EGGYBOT Blender build script — Headless, 球 primitive × N + boolean で幾何組む
Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background --python build_eggybot.py

Reference: ../../../キャラクター/3D生成AI用キャラクタープロンプト.md
Output:
  Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx
  Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend
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
def make_material(name, base_color, alpha=1.0, transparent=False):
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*base_color, alpha)
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = 0.18 if transparent else 0.45
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = 0.0
        if transparent:
            if "Alpha" in bsdf.inputs:
                bsdf.inputs["Alpha"].default_value = alpha
            for tname in ("Transmission Weight", "Transmission"):
                if tname in bsdf.inputs:
                    bsdf.inputs[tname].default_value = 0.85
                    break
            if "IOR" in bsdf.inputs:
                bsdf.inputs["IOR"].default_value = 1.45
            if hasattr(mat, 'blend_method'):
                try:
                    mat.blend_method = 'BLEND'
                except Exception:
                    pass
    mat.diffuse_color = (*base_color, alpha)
    return mat

PINK = (0.96, 0.55, 0.65)
DARK = (0.05, 0.05, 0.05)

mat_pink = make_material("EGGYBOT_Pink", PINK, 1.0)
mat_clear = make_material("EGGYBOT_Clear", (1.0, 1.0, 1.0), 0.25, transparent=True)
mat_dark = make_material("EGGYBOT_Eye", DARK, 1.0)


# ============================================================
# 2. Helper: create UV sphere
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


def boolean_difference(target, cutter):
    bpy.context.view_layer.objects.active = target
    mod = target.modifiers.new(name="CUT", type='BOOLEAN')
    mod.object = cutter
    mod.operation = 'DIFFERENCE'
    bpy.ops.object.modifier_apply(modifier="CUT")
    bpy.data.objects.remove(cutter, do_unlink=True)


# ============================================================
# 3. Body lower (pink opaque) — cut top above parting line
# ============================================================
PARTING_Z = 0.38  # 胴体の赤道より少し上

body_lower = add_sphere("BodyLower", (0, 0, 0.30), 0.30, 48, 24, mat_pink)

# Cut top: cube positioned above PARTING_Z
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z + 1.0))
cutter_top = bpy.context.object
boolean_difference(body_lower, cutter_top)

# ============================================================
# 4. Body upper clear — cut bottom below parting line
# ============================================================
body_upper = add_sphere("BodyUpperClear", (0, 0, 0.30), 0.30, 48, 24, mat_clear)

# Cut bottom: cube positioned below PARTING_Z
bpy.ops.mesh.primitive_cube_add(size=2, location=(0, 0, PARTING_Z - 1.0))
cutter_bot = bpy.context.object
boolean_difference(body_upper, cutter_bot)

# Slight scale up on clear cap to sit just outside the lower (gives visible groove)
body_upper.scale = (1.02, 1.02, 1.02)
bpy.ops.object.transform_apply(scale=True)

# ============================================================
# 5. Head (pink opaque), sitting on top of body
# ============================================================
head = add_sphere("Head", (0, 0, 0.82), 0.22, 48, 24, mat_pink)

# ============================================================
# 6. Arms — short nub spheres on lower body sides
# ============================================================
arm_l = add_sphere("Arm_L", (-0.30, 0, 0.26), 0.075, 24, 12, mat_pink)
arm_r = add_sphere("Arm_R", ( 0.30, 0, 0.26), 0.075, 24, 12, mat_pink)

# ============================================================
# 7. Legs — short nub spheres at bottom, slight ellipse (flatter bottom)
# ============================================================
leg_l = add_sphere("Leg_L", (-0.12, 0.02, 0.04), 0.09, 24, 12, mat_pink)
leg_l.scale = (1.0, 1.0, 0.8)
leg_r = add_sphere("Leg_R", ( 0.12, 0.02, 0.04), 0.09, 24, 12, mat_pink)
leg_r.scale = (1.0, 1.0, 0.8)
bpy.ops.object.select_all(action='DESELECT')
leg_l.select_set(True); bpy.context.view_layer.objects.active = leg_l
bpy.ops.object.transform_apply(scale=True)
bpy.ops.object.select_all(action='DESELECT')
leg_r.select_set(True); bpy.context.view_layer.objects.active = leg_r
bpy.ops.object.transform_apply(scale=True)

# ============================================================
# 8. Eyes — small dark spheres on head front (-Y)
# ============================================================
eye_l = add_sphere("Eye_L", (-0.07, -0.20, 0.84), 0.018, 16, 8, mat_dark)
eye_r = add_sphere("Eye_R", ( 0.07, -0.20, 0.84), 0.018, 16, 8, mat_dark)

# ============================================================
# 9. Mouth — small U-shaped curve via torus, rotated + placed on head front
# ============================================================
bpy.ops.mesh.primitive_torus_add(
    location=(0, -0.205, 0.80),
    major_radius=0.035,
    minor_radius=0.004,
    rotation=(math.pi/2, 0, 0),
    major_segments=24,
    minor_segments=8,
)
mouth = bpy.context.object
mouth.name = "Mouth"
mouth.data.materials.append(mat_dark)

# Keep only bottom half of torus to make smile (cut away top half)
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.205, 0.82))
cut_smile = bpy.context.object
bpy.context.view_layer.objects.active = mouth
mod = mouth.modifiers.new(name="CUT", type='BOOLEAN')
mod.object = cut_smile
mod.operation = 'DIFFERENCE'
bpy.ops.object.modifier_apply(modifier="CUT")
bpy.data.objects.remove(cut_smile, do_unlink=True)

# ============================================================
# 10. Armature (Humanoid-compatible 15 bones for Mixamo retarget)
# ============================================================
bpy.ops.object.armature_add(location=(0, 0, 0))
armature_obj = bpy.context.object
armature_obj.name = "EggybotArmature"
arm = armature_obj.data
arm.name = "EggybotRig"

bpy.ops.object.mode_set(mode='EDIT')
# Remove default bone
for b in list(arm.edit_bones):
    arm.edit_bones.remove(b)

def bone(name, head, tail, parent=None):
    eb = arm.edit_bones.new(name)
    eb.head = head
    eb.tail = tail
    if parent is not None:
        eb.parent = parent
        eb.use_connect = False
    return eb

# Root → Hips (inside lower body center)
hips = bone("Hips", (0, 0, 0.25), (0, 0, 0.35))
# Spine chain (short, all inside body)
spine = bone("Spine", (0, 0, 0.35), (0, 0, 0.45), hips)
chest = bone("Chest", (0, 0, 0.45), (0, 0, 0.60), spine)
# Neck + Head
neck = bone("Neck", (0, 0, 0.60), (0, 0, 0.68), chest)
head_b = bone("Head", (0, 0, 0.68), (0, 0, 0.95), neck)

# Shoulders + Arms (Unity Humanoid naming)
sh_l = bone("LeftShoulder", (0.05, 0, 0.35), (0.15, 0, 0.32), chest)
up_l = bone("LeftUpperArm", (0.15, 0, 0.32), (0.24, 0, 0.28), sh_l)
lo_l = bone("LeftLowerArm", (0.24, 0, 0.28), (0.30, 0, 0.26), up_l)
hand_l = bone("LeftHand", (0.30, 0, 0.26), (0.34, 0, 0.26), lo_l)

sh_r = bone("RightShoulder", (-0.05, 0, 0.35), (-0.15, 0, 0.32), chest)
up_r = bone("RightUpperArm", (-0.15, 0, 0.32), (-0.24, 0, 0.28), sh_r)
lo_r = bone("RightLowerArm", (-0.24, 0, 0.28), (-0.30, 0, 0.26), up_r)
hand_r = bone("RightHand", (-0.30, 0, 0.26), (-0.34, 0, 0.26), lo_r)

# Legs (Unity Humanoid naming, hidden knee inside leg nub)
upl_l = bone("LeftUpperLeg", (0.12, 0, 0.22), (0.12, 0, 0.10), hips)
lol_l = bone("LeftLowerLeg", (0.12, 0, 0.10), (0.12, 0, 0.04), upl_l)
foot_l = bone("LeftFoot", (0.12, 0, 0.04), (0.12, -0.02, 0.0), lol_l)

upl_r = bone("RightUpperLeg", (-0.12, 0, 0.22), (-0.12, 0, 0.10), hips)
lol_r = bone("RightLowerLeg", (-0.12, 0, 0.10), (-0.12, 0, 0.04), upl_r)
foot_r = bone("RightFoot", (-0.12, 0, 0.04), (-0.12, -0.02, 0.0), lol_r)

bpy.ops.object.mode_set(mode='OBJECT')

# ============================================================
# 11. Parent meshes to armature with Automatic Weights
# ============================================================
mesh_objects = [head, body_lower, body_upper, arm_l, arm_r, leg_l, leg_r, eye_l, eye_r, mouth]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# ============================================================
# 12. Save .blend
# ============================================================
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

# ============================================================
# 13. Export FBX (Unity-friendly axes)
# ============================================================
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

print(f"=== EGGYBOT build complete ===")
print(f"Blend: {BLEND_PATH}")
print(f"FBX:   {FBX_PATH}")
print(f"Bones: {len(arm.bones)}")
print(f"Meshes: {len(mesh_objects)}")
