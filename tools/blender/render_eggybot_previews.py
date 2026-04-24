"""
EGGYBOT multi-angle preview renderer (Blender headless).

build_eggybot.py が作った EGGYBOT を 6 方向から render し、user にすぐ見せるための PNG を出力。
"Blender workflow" の代替として、interactive ではないが multi-angle 確認ができる。

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background \
      --python build_eggybot.py --python render_eggybot_previews.py
"""
import bpy
import os
import math

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "tools", "blender", "preview")
os.makedirs(OUT_DIR, exist_ok=True)

# Remove existing camera and lights (from build_eggybot.py prior state, if any)
for obj in list(bpy.data.objects):
    if obj.type in {'CAMERA', 'LIGHT'}:
        bpy.data.objects.remove(obj, do_unlink=True)

# Add 3-point lighting for clean preview
def add_light(name, loc, rot, energy, color, light_type='SUN'):
    light_data = bpy.data.lights.new(name=name + "_data", type=light_type)
    light_data.energy = energy
    light_data.color = color
    obj = bpy.data.objects.new(name=name, object_data=light_data)
    bpy.context.collection.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = rot

# Unity CharacterLab の 5 light setup と値を mirror（parity 確保、Unity 実機と見え方を揃える）
# key/fill/rim の比率: 1.05 : 0.45 : 0.95 → Unity と同じ illumination 分布
add_light("Key", (2, -2, 3), (math.radians(40), 0, math.radians(35)),
          energy=1.05, color=(1.0, 0.96, 0.88))
add_light("Fill", (-3, -1, 2), (math.radians(30), 0, math.radians(-135)),
          energy=0.45, color=(0.72, 0.80, 0.95))
add_light("Rim", (0, 3, 2), (math.radians(-10), 0, math.radians(185)),
          energy=0.95, color=(1.0, 0.72, 0.88))
# BackKick warm silhouette（Unity 側に合わせて追加）
add_light("BackKick", (1, 3, 1.5), (math.radians(15), 0, math.radians(165)),
          energy=0.8, color=(1.0, 0.85, 0.60))

# World background (mid-gray studio)
world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
bpy.context.scene.world = world
world.use_nodes = True
bg_node = world.node_tree.nodes.get("Background")
if bg_node:
    bg_node.inputs[0].default_value = (0.32, 0.35, 0.40, 1)
    bg_node.inputs[1].default_value = 1.3

# Render settings
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.samples = 64
scene.cycles.use_denoising = True
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.film_transparent = False

# Camera
cam_data = bpy.data.cameras.new("PreviewCam")
cam_data.lens = 50
cam_obj = bpy.data.objects.new("PreviewCam", cam_data)
bpy.context.collection.objects.link(cam_obj)
scene.camera = cam_obj

def look_at(obj, target):
    import mathutils
    direction = mathutils.Vector(target) - obj.location
    rot_quat = direction.to_track_quat('-Z', 'Y')
    obj.rotation_euler = rot_quat.to_euler()

# Target: character body center
TARGET = (0, 0, 0.40)

# 6 preview angles
angles = [
    ("01_front",       (0,    -1.5, 0.45)),
    ("02_side_left",   (-1.5,  0,   0.45)),
    ("03_tilt_45",     (-0.9, -0.9, 1.00)),   # 斜め俯瞰：pot 内部の hollow 空間が明確
    ("04_pot_inside",  (-0.2, -0.5, 0.70)),   # pot 内部 closer: rim + 内壁 + 底 見える
    ("05_top_down",    (0,    -0.3, 2.0)),    # 真上からカプセル全体
    ("06_rim_cross",   (-0.6, -1.1, 0.50)),   # pot 上縁の rim 厚み見える
]

for name, pos in angles:
    cam_obj.location = pos
    look_at(cam_obj, TARGET)
    bpy.context.view_layer.update()
    scene.render.filepath = os.path.join(OUT_DIR, f"preview_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"Rendered: {scene.render.filepath}")

print(f"\n=== Previews saved to {OUT_DIR} ===")
