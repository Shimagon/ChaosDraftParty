"""
EGGYBOT v16 limb-strip — eggybot_source.blend (v15、ユーザー手調整済) を copy 起点に
Hand_L/R + Foot_L/R mesh + 対応 bone + vertex group を削除して FBX 再 export。

build_eggybot.py で再生成すると user の手動 mesh 編集が消える可能性があるので、
.blend を直接編集する方が安全 (ユーザー指示)。
"""
import bpy
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
SRC_BLEND = os.path.join(OUT_DIR, "eggybot_source_v16.blend")  # 既に shell でコピー済み
FBX_PATH = os.path.join(OUT_DIR, "eggybot_v16.fbx")

print(f"[open] {SRC_BLEND}")
bpy.ops.wm.open_mainfile(filepath=SRC_BLEND)

# --- 1. List objects before
print("\n[before]")
for obj in bpy.data.objects:
    print(f"  {obj.type}: {obj.name}")

# --- 2. Delete limb meshes
LIMB_MESH_NAMES = ["Hand_L", "Hand_R", "Foot_L", "Foot_R"]
deleted_meshes = []
for name in LIMB_MESH_NAMES:
    obj = bpy.data.objects.get(name)
    if obj is not None:
        # Also remove the mesh data
        mesh_data = obj.data if obj.type == 'MESH' else None
        bpy.data.objects.remove(obj, do_unlink=True)
        if mesh_data and mesh_data.users == 0:
            bpy.data.meshes.remove(mesh_data)
        deleted_meshes.append(name)
print(f"\n[deleted meshes] {deleted_meshes}")

# --- 3. Delete limb bones from armature
LIMB_BONE_NAMES = [
    "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
    "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
    "LeftUpperLeg", "LeftLowerLeg", "LeftFoot",
    "RightUpperLeg", "RightLowerLeg", "RightFoot",
]
armature_obj = None
for obj in bpy.data.objects:
    if obj.type == 'ARMATURE':
        armature_obj = obj
        break

deleted_bones = []
if armature_obj:
    bpy.ops.object.select_all(action='DESELECT')
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    bpy.ops.object.mode_set(mode='EDIT')
    for bn in LIMB_BONE_NAMES:
        eb = armature_obj.data.edit_bones.get(bn)
        if eb is not None:
            armature_obj.data.edit_bones.remove(eb)
            deleted_bones.append(bn)
    bpy.ops.object.mode_set(mode='OBJECT')
print(f"[deleted bones] {deleted_bones}")

# --- 4. Clean up vertex groups on remaining meshes
remaining_meshes = ["BodyLower", "BodyUpperClear", "HeadMesh", "Eye_L", "Eye_R", "Mouth"]
for mesh_name in remaining_meshes:
    obj = bpy.data.objects.get(mesh_name)
    if obj is None:
        continue
    removed_vg = []
    for vg_name in LIMB_BONE_NAMES:
        vg = obj.vertex_groups.get(vg_name)
        if vg:
            obj.vertex_groups.remove(vg)
            removed_vg.append(vg_name)
    if removed_vg:
        print(f"  [{mesh_name}] removed VG: {len(removed_vg)} groups")

# --- 5. List after
print("\n[after]")
for obj in bpy.data.objects:
    print(f"  {obj.type}: {obj.name}")
if armature_obj:
    print(f"\n[bones remaining] {len(armature_obj.data.bones)}")
    for b in armature_obj.data.bones:
        print(f"  {b.name}")

# --- 6. Save modified blend (overwrite v16 copy)
bpy.ops.wm.save_as_mainfile(filepath=SRC_BLEND)
print(f"\n[saved] {SRC_BLEND}")

# --- 7. Export FBX (overwrite eggybot_v16.fbx)
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

print(f"\n=== EGGYBOT v16 limb-strip complete ===")
print(f"FBX: {FBX_PATH}")
print(f"BLEND: {SRC_BLEND}")
