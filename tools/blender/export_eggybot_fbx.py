"""
Headless FBX export from eggybot_source.blend.
ユーザーが Blender GUI で骨を微調整した後、build script を再実行せずに FBX だけ更新する用。

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender \
    --background \
    Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend \
    --python tools/blender/export_eggybot_fbx.py
"""
import bpy
import os

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FBX_PATH = os.path.join(PROJECT_ROOT, "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx")

# headless context: object_types フィルタで全部 export（select 不要）
bpy.ops.export_scene.fbx(
    filepath=FBX_PATH,
    use_selection=False,
    object_types={'ARMATURE', 'MESH'},
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_NONE',
    bake_space_transform=False,  # Apply Transform OFF
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    add_leaf_bones=False,
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    armature_nodetype='NULL',
    bake_anim=False,  # Bake Animation OFF
    path_mode='AUTO',
    embed_textures=False,
    axis_forward='-Z',
    axis_up='Y',
)
print(f"FBX exported: {FBX_PATH}")
