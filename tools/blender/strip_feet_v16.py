"""
EGGYBOT v16 foot-strip — eggybot_source_v16.blend (手 mesh 削除済) を起点に
body_lower 内の foot UNION 凸を vertex 削除して fill、clean な bowl shell に戻す。

足は Unity 側で primitive Sphere x2 を LeftHip/RightHip Transform anchor の child として
追加するので、Blender 側は body_lower から foot 凸を完全除去。

戦略:
  1. eggybot_source_v16.blend を開く
  2. body_lower の bmesh で foot 楕円体内 vertex を select
  3. delete vertices
  4. fill resulting holes
  5. save + FBX export (eggybot_v16.fbx 上書き)

Foot 楕円体定義 (build_eggybot.py より):
  center: (±0.131, 0.03, PARTING_Z + (-0.10)) = (±0.131, 0.03, 0.20) world
  radii: (0.090, 0.140, 0.100) (squashed sphere shape)
  → ellipsoid distance d² = ((x-cx)/rx)² + ((y-cy)/ry)² + ((z-cz)/rz)² < 1 の vertex を select
"""
import bpy
import bmesh
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy", "Characters", "EGGYBOT")
SRC_BLEND = os.path.join(OUT_DIR, "eggybot_source_v16.blend")
FBX_PATH = os.path.join(OUT_DIR, "eggybot_v16.fbx")

print(f"[open] {SRC_BLEND}")
bpy.ops.wm.open_mainfile(filepath=SRC_BLEND)

# Foot ellipsoid (Blender world coordinates)
# build_eggybot.py: add_sphere("Foot_L", (-foot_x, 0.03, FOOT_Z=-0.10), ...) は world location
# squash (0.9, 1.4, 1.0) で Y stretch → 楕円体半径 (0.090, 0.140, 0.100)
FOOT_L_CTR = ( 0.131, 0.03, -0.10)
FOOT_R_CTR = (-0.131, 0.03, -0.10)
FOOT_RX, FOOT_RY, FOOT_RZ = 0.090, 0.140, 0.100

# Add small margin so we cut a bit larger area to ensure clean removal
MARGIN = 1.15  # 15% larger ellipsoid to delete more aggressively

def in_foot_ellipsoid(p_world):
    for ctr in [FOOT_L_CTR, FOOT_R_CTR]:
        rx = FOOT_RX * MARGIN
        ry = FOOT_RY * MARGIN
        rz = FOOT_RZ * MARGIN
        dx = (p_world[0] - ctr[0]) / rx
        dy = (p_world[1] - ctr[1]) / ry
        dz = (p_world[2] - ctr[2]) / rz
        if dx*dx + dy*dy + dz*dz < 1.0:
            return True
    return False

body_lower = bpy.data.objects.get("BodyLower")
if body_lower is None:
    print("[err] BodyLower not found")
    raise SystemExit(1)

print(f"[before] BodyLower verts={len(body_lower.data.vertices)}")

# Switch to edit mode + bmesh for vertex select
bpy.ops.object.select_all(action='DESELECT')
body_lower.select_set(True)
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.mode_set(mode='EDIT')

bm = bmesh.from_edit_mesh(body_lower.data)
selected = 0
mat_world = body_lower.matrix_world
print(f"[matrix_world] {body_lower.matrix_world}")
print(f"[obj loc] {body_lower.location}")

# Sample some vertex coords for debugging
sample_count = 0
for v in bm.verts:
    if sample_count < 5:
        p_world = mat_world @ v.co
        print(f"  vert sample local={tuple(v.co)} world={tuple(p_world)}")
        sample_count += 1

# Find min/max in world coords
min_x = min_y = min_z = 999
max_x = max_y = max_z = -999
for v in bm.verts:
    p = mat_world @ v.co
    min_x = min(min_x, p.x); max_x = max(max_x, p.x)
    min_y = min(min_y, p.y); max_y = max(max_y, p.y)
    min_z = min(min_z, p.z); max_z = max(max_z, p.z)
print(f"[world bounds] x=[{min_x:.3f},{max_x:.3f}] y=[{min_y:.3f},{max_y:.3f}] z=[{min_z:.3f},{max_z:.3f}]")

verts_to_delete = []
for v in bm.verts:
    p_world = mat_world @ v.co
    if in_foot_ellipsoid(p_world):
        verts_to_delete.append(v)
        selected += 1

print(f"[select] {selected} foot verts")

# Use bmesh.ops.delete for reliable removal (context=1 = VERTS)
bmesh.ops.delete(bm, geom=verts_to_delete, context='VERTS')

# Find boundary edges (open loops left after deletion) and fill them
boundary_edges = [e for e in bm.edges if e.is_boundary]
print(f"[boundary] {len(boundary_edges)} edges to fill")

bmesh.update_edit_mesh(body_lower.data)

# Fill holes via operator (works on selection-marked geometry)
bpy.ops.mesh.select_all(action='DESELECT')
# select only boundary
for e in bm.edges:
    e.select = e.is_boundary
bmesh.update_edit_mesh(body_lower.data)
try:
    bpy.ops.mesh.fill_holes(sides=64)
    print("[fill] fill_holes ok")
except Exception as e:
    print(f"[fill] failed: {e}")

bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.normals_make_consistent(inside=False)

bpy.ops.object.mode_set(mode='OBJECT')
print(f"[after] BodyLower verts={len(body_lower.data.vertices)}")

# Save + Export
bpy.ops.wm.save_as_mainfile(filepath=SRC_BLEND)
print(f"[saved] {SRC_BLEND}")

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
print(f"[FBX] {FBX_PATH}")
print(f"\n=== EGGYBOT v16 foot-strip complete (clean shell + Unity primitive feet) ===")
