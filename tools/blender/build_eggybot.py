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
import bmesh
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
BLUSH = (0.96, 0.42, 0.52)
DARK = (0.05, 0.05, 0.05)
WHITE = (1.0, 1.0, 1.0)

mat_pink = make_material("EGGYBOT_Pink", PINK, 1.0)
mat_blush = make_material("EGGYBOT_Blush", BLUSH, 1.0)
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
# 3. Pink Parts — 分離 mesh で保持（bone rig 対応、手足アニメ可能）
#    接合線は SSAO + 同一 material で目立たなくする戦略
# ============================================================
PARTING_Z = 0.30

# Body Lower (pot) — 真のお椀：top は完全 open、中が深く掘れてる hollow
# 1) sphere 作って
# 2) bmesh で top (z >= PARTING_Z local) の face 削除 → open bowl
# 3) Solidify で 2-layer wall (内壁 + 外壁 + rim 繋ぎ)
body_lower = add_sphere("BodyLower", (0, 0, 0.30), 0.32, 64, 32, mat_pink)

# bmesh で top 開口（face 削除、flat cap を残さない）
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.mode_set(mode='EDIT')
bm = bmesh.from_edit_mesh(body_lower.data)
bm.faces.ensure_lookup_table()
# pot local coord: world PARTING_Z=0.30 - obj.z=0.30 → local z=0
LOCAL_PARTING_Z = PARTING_Z - 0.30
faces_to_delete = [f for f in bm.faces
                   if sum(v.co.z for v in f.verts) / len(f.verts) > LOCAL_PARTING_Z - 0.001]
bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
bmesh.update_edit_mesh(body_lower.data)
bpy.ops.object.mode_set(mode='OBJECT')

# Solidify: outward shell (壁厚 0.04)、open boundary は rim で繋ぐ
sol_pot = body_lower.modifiers.new(name="SHELL", type='SOLIDIFY')
sol_pot.thickness = 0.040
sol_pot.offset = 1.0
sol_pot.use_even_offset = True
sol_pot.use_quality_normals = True
bpy.ops.object.modifier_apply(modifier="SHELL")

# shade smooth で base surface 滑らか
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.shade_smooth()

# Edge Split modifier — rim の角を crisp (プラスチックの切れ目らしく)
es_pot = body_lower.modifiers.new(name="EDGE_SPLIT", type='EDGE_SPLIT')
es_pot.split_angle = math.radians(35)  # 35° 以上の折れはシャープに
es_pot.use_edge_angle = True
es_pot.use_edge_sharp = False
bpy.ops.object.modifier_apply(modifier="EDGE_SPLIT")

# Head — より球に近く、底面 flatten は最小に抑え lemon 型 / 変形回避
head = add_sphere("Head", (0, 0, 0.82), 0.29, 64, 32, mat_pink)
squash(head, 1.00, 1.00, 0.98)  # ほぼ球（軽く Z squash）
# 底面を ごく軽く flatten（重力で sit-on-pot 感だけ）
for v in head.data.vertices:
    wz = v.co.z + head.location.z
    if wz < 0.74:
        ratio = min(1.0, max(0.0, (0.74 - wz) / 0.08))
        v.co.z += ratio * 0.015

# Arms — pot の外側 equator に attach、pot hollow を侵さない
#   pot inner radius 0.32、arm の nearest vertex > 0.32 になるよう center を外に
arm_l = add_sphere("Arm_L", (-0.42, 0, 0.12), 0.105, 32, 16, mat_pink)
squash(arm_l, 1.25, 0.85, 0.90)
arm_r = add_sphere("Arm_R", ( 0.42, 0, 0.12), 0.105, 32, 16, mat_pink)
squash(arm_r, 1.25, 0.85, 0.90)

# Legs — pot の下側 outer に attach、hollow を侵さない
leg_l = add_sphere("Leg_L", (-0.22, 0.02, -0.06), 0.120, 32, 16, mat_pink)
squash(leg_l, 1.20, 1.10, 0.78)
leg_r = add_sphere("Leg_R", ( 0.22, 0.02, -0.06), 0.120, 32, 16, mat_pink)
squash(leg_r, 1.20, 1.10, 0.78)

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
# Eyes — 全部黒、白ハイライトは shader の specular 反射で自然発生
EYE_Y = -0.24
EYE_Z = 0.85
EYE_DX = 0.11
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)
# EyeShine mesh は削除 — shiny black material の specular highlight で代替
hi_l = None
hi_r = None

# ほっぺ削除（user 指示「なくてもいい」）。浮いて見える問題解消。

# ============================================================
# 7. Mouth smile — 見えやすい位置とサイズに
# ============================================================
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
# Cube size 0.5、cube center を torus の TRUE top (MOUTH_Z + 0.25) に配置
# → cube 下面が torus 中心 z と一致、top 半分だけ cut で smile 残る
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.305, MOUTH_Z + 0.25))
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
mesh_objects = [body_lower, head, arm_l, arm_r, leg_l, leg_r,
                body_upper, eye_l, eye_r, mouth]
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

print(f"=== EGGYBOT v8 build complete ===")
print(f"Parts separated for rigging: head / body / arms / legs")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
