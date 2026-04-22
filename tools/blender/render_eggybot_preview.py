"""
Render EGGYBOT preview from 4 angles (front, 3/4, side, top).
Usage:
  /Applications/Blender.app/Contents/MacOS/Blender --background eggybot_source.blend --python render_eggybot_preview.py
"""
import bpy
import os
import math

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "tools", "blender", "preview")
os.makedirs(OUT_DIR, exist_ok=True)

# Setup camera + light
bpy.ops.object.camera_add(location=(0, -2.2, 0.55))
cam = bpy.context.object
cam.data.lens = 50
cam.rotation_euler = (math.radians(85), 0, 0)
bpy.context.scene.camera = cam

# 3-point lighting
bpy.ops.object.light_add(type='AREA', location=(1.5, -1.2, 1.5))
key = bpy.context.object
key.data.energy = 320
key.rotation_euler = (math.radians(50), math.radians(30), 0)

bpy.ops.object.light_add(type='AREA', location=(-1.5, -0.8, 0.8))
fill = bpy.context.object
fill.data.energy = 150

bpy.ops.object.light_add(type='AREA', location=(0, 1.5, 1.2))
back = bpy.context.object
back.data.energy = 200
back.rotation_euler = (math.radians(-50), 0, 0)

# Render settings (Eevee for speed)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in {e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items} else 'BLENDER_EEVEE'
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.render.film_transparent = True

# World background: neutral gray
if scene.world is None:
    scene.world = bpy.data.worlds.new("World")
scene.world.use_nodes = True
bg = scene.world.node_tree.nodes.get("Background")
if bg:
    bg.inputs[0].default_value = (0.5, 0.5, 0.55, 1)
    bg.inputs[1].default_value = 1.0

angles = [
    ("front", (0, -2.2, 0.55), (math.radians(85), 0, 0)),
    ("threequarter", (1.8, -1.8, 0.9), (math.radians(72), 0, math.radians(45))),
    ("side", (-2.4, 0, 0.55), (math.radians(85), 0, math.radians(-90))),
    ("top", (0, 0, 2.4), (math.radians(0), 0, 0)),
]

for name, loc, rot in angles:
    cam.location = loc
    cam.rotation_euler = rot
    scene.render.filepath = os.path.join(OUT_DIR, f"eggybot_{name}.png")
    bpy.ops.render.render(write_still=True)
    print(f"Saved {scene.render.filepath}")

print("=== preview render complete ===")
