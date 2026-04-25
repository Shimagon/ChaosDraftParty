"""
Headless FBX export from eggybot_source.blend.
ユーザーが Blender GUI で骨を微調整した後、build script を再実行せずに FBX だけ更新する用。

build_eggybot.py の export 設定と完全一致させる（不一致だと骨が原点に潰れる等の事故）。

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

# 全 mesh + armature を選択（build_eggybot.py が use_selection=True を使ってる前提に合わせる）
for o in bpy.data.objects:
    o.select_set(o.type in {'ARMATURE', 'MESH'})

# build_eggybot.py と同じ export 設定（差分があると骨潰れる）
bpy.ops.export_scene.fbx(
    filepath=FBX_PATH,
    use_selection=True,
    global_scale=1.0,
    apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',  # 重要: NONE だと骨が原点に潰れる
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
print(f"FBX exported: {FBX_PATH}")
