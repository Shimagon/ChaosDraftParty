"""eggybot_source.blend から全骨の head/tail/parent/roll を JSON で吐く。

build_eggybot.py が再ビルドする時にこの JSON を読み込んで骨を配置する。
これにより、ユーザーが Blender GUI で手動調整した骨位置が永久保存される。

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background \
    Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend \
    --python tools/blender/extract_bone_layout.py
"""
import bpy
import json
import os

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
JSON_PATH = os.path.join(PROJECT_ROOT, "tools/blender/eggybot_bone_layout.json")

arm = bpy.data.armatures.get("EggybotRig")
if arm is None:
    raise RuntimeError("EggybotRig armature not found")

layout = {}
for b in arm.bones:
    layout[b.name] = {
        "head": list(b.head_local),
        "tail": list(b.tail_local),
        "parent": b.parent.name if b.parent else None,
    }

# Edit mode で roll 取得（pose mode の matrix では取れない）
arm_obj = bpy.data.objects.get("EggybotArmature")
bpy.context.view_layer.objects.active = arm_obj
bpy.ops.object.mode_set(mode='EDIT')
for eb in arm_obj.data.edit_bones:
    if eb.name in layout:
        layout[eb.name]["roll"] = eb.roll
bpy.ops.object.mode_set(mode='OBJECT')

with open(JSON_PATH, "w") as f:
    json.dump(layout, f, indent=2)

print(f"[extract] {len(layout)} bones saved to {JSON_PATH}")
for name in sorted(layout.keys()):
    info = layout[name]
    print(f"  {name}: head={tuple(round(x,3) for x in info['head'])} "
          f"tail={tuple(round(x,3) for x in info['tail'])} "
          f"parent={info['parent']} roll={round(info.get('roll', 0),3)}")
