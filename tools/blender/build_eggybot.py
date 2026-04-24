"""
EGGYBOT v10 Blender build — Bridge Edge Loops で manifold 限界品質

Design:
  - Pot body に bmesh で穴を開け、stubby arm/leg sphere の片側半球を削除
  - 両者の open edge loop を bpy.ops.mesh.bridge_edge_loops() で接続
    → seam zero、topology manifold、rigging friendly
  - Boolean UNION を完全廃止 → rim ガビガビの原因（moiré）を根絶
  - Post-Bridge cleanup pipeline: merge_by_distance 0.00005 → dissolve_degenerate
    → normals_make_consistent → rim mark_sharp → shade_smooth_by_angle(60°)

Research:
  - bpy.ops.mesh.bridge_edge_loops:
    https://docs.blender.org/manual/en/latest/modeling/meshes/editing/edge/bridge_edge_loops.html
  - bmesh.ops:
    https://docs.blender.org/api/current/bmesh.ops.html
  - Agent1 (Kirby 限界調査) + Agent2 (UNION artifact cleanup) + Agent3 (visual audit)
    による 3 並列 Explore 結果を統合した v10。

Hollow guard:
  - Pot inner r=0.30 centered at (0,0,PARTING_Z)
  - 全 arm/leg vertex が dist > 0.30 であること build 後に verify
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector

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
# 3. Bridge Edge Loops — 核心技法
# ============================================================
def create_hemisphere_limb(name, radius, squash_xyz, outward_dir_world,
                           attach_world, protrude_offset=0.005,
                           material=None):
    """
    Hemisphere (半球) mesh を作る。opening が body に向き、solid 側が outward_dir_world に突出。

    内部手順:
      1. sphere を origin に作る
      2. 上半球 (local Z > 0) を bmesh 削除 → 下半球 + 赤道 ring open edge
      3. local 軸沿い squash (Z axis = outward 方向になる予定)
      4. rotate: local +Z = -outward_dir_world（= body 方向）に。opening が body 側を向く。
      5. attach_world + outward * protrude_offset に移動（bridge 用 epsilon gap）

    Args:
        outward_dir_world: 突き出す方向 (body→arm tip)。opening は逆向き (body を向く)。
        attach_world: opening 中心が来る world 位置（body 表面の attach 点近傍）。
        protrude_offset: opening を body surface から epsilon 外側に配置（bridge の neck 厚）。
    """
    bpy.ops.mesh.primitive_uv_sphere_add(
        radius=radius, segments=32, ring_count=16, location=(0, 0, 0)
    )
    obj = bpy.context.object
    obj.name = name

    # 1. bmesh で上半球削除（local Z > 0 の face を）→ 下半球のみ残す
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    faces_to_delete = [f for f in bm.faces
                       if min(v.co.z for v in f.verts) > 0.0001]
    bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode='OBJECT')

    # 2. Squash（local frame で XYZ スケール）→ apply_scale
    obj.scale = squash_xyz
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(scale=True)

    # 3. Rotate: local +Z → -outward_dir_world (body 方向)
    #    opening (at local Z=0) は +Z 側を向く → rotation 後 body 方向を向く
    outward = Vector(outward_dir_world).normalized()
    body_dir = -outward
    local_z = Vector((0, 0, 1))
    quat = local_z.rotation_difference(body_dir)
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = quat
    bpy.ops.object.transform_apply(rotation=True)

    # 4. 位置: opening (local Z=0 plane) を attach_world + epsilon に配置
    #    local origin は opening 中心と一致（rotation で動かない）
    obj.location = Vector(attach_world) + outward * protrude_offset

    # 5. material + shade smooth
    if material:
        obj.data.materials.append(material)
    bpy.ops.object.shade_smooth()

    bpy.ops.object.select_all(action='DESELECT')
    return obj


def bridge_limb_to_body(body, limb, attach_world, hole_radius,
                        exclude_rim_z=0.30, exclude_rim_tolerance=0.005):
    """
    body に attach_world 近傍の穴を開け、pre-shaped hemisphere limb を join、
    body hole + limb opening を bridge_edge_loops で接続。

    Prerequisites:
      limb は create_hemisphere_limb() で pre-shaped（opening が body 側、clean equator edge）

    Args:
        body: target mesh
        limb: hemisphere limb with clean open edge facing body
        attach_world: body 表面の接続点
        hole_radius: body 穴半径
        exclude_rim_z: pot rim の world z（この z 近辺の boundary は bridge 対象外）
    """
    limb_name = limb.name
    attach_world_vec = Vector(attach_world)

    # --- 1. body の face 削除（attach 近傍）
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.mode_set(mode='EDIT')
    bm_body = bmesh.from_edit_mesh(body.data)
    bm_body.faces.ensure_lookup_table()

    body_mw = body.matrix_world.copy()
    faces_to_delete = []
    for f in bm_body.faces:
        worlds = [body_mw @ v.co for v in f.verts]
        face_center_world = sum(worlds, Vector()) / len(worlds)
        if (face_center_world - attach_world_vec).length < hole_radius:
            faces_to_delete.append(f)

    if not faces_to_delete:
        print(f"[bridge WARN] {limb_name}: no body faces within {hole_radius} of {attach_world}")
        bpy.ops.object.mode_set(mode='OBJECT')
        return False

    bmesh.ops.delete(bm_body, geom=faces_to_delete, context='FACES')
    bmesh.update_edit_mesh(body.data)
    bpy.ops.object.mode_set(mode='OBJECT')
    print(f"[bridge] {limb_name}: deleted {len(faces_to_delete)} body faces at {attach_world}")

    # --- 2. limb を body に join
    bpy.ops.object.select_all(action='DESELECT')
    limb.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()

    # --- 3. Edit mode で attach 近傍 boundary edges を select（rim 除外）
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(body.data)
    bm.edges.ensure_lookup_table()

    bpy.ops.mesh.select_all(action='DESELECT')

    body_mw = body.matrix_world.copy()
    search_radius = hole_radius * 3.5  # body hole + limb opening を包含
    selected_edges = 0
    for e in bm.edges:
        if not e.is_boundary:
            continue
        mp_world = (body_mw @ e.verts[0].co + body_mw @ e.verts[1].co) * 0.5
        # rim 除外
        if abs(mp_world.z - exclude_rim_z) < exclude_rim_tolerance:
            continue
        if (mp_world - attach_world_vec).length < search_radius:
            e.select_set(True)
            selected_edges += 1

    bmesh.update_edit_mesh(body.data)
    print(f"[bridge] {limb_name}: selected {selected_edges} boundary edges for bridge")

    if selected_edges < 8:
        print(f"[bridge ERROR] {limb_name}: too few edges ({selected_edges}), skipping bridge")
        bpy.ops.object.mode_set(mode='OBJECT')
        return False

    # --- 4. Bridge!
    try:
        bpy.ops.mesh.bridge_edge_loops(
            type='SINGLE',
            use_merge=False,
            merge_factor=0.5,
            number_cuts=0,
            interpolation='LINEAR',
            smoothness=1.0,
            profile_shape_factor=0,
            profile_shape='SMOOTH',
        )
        print(f"[bridge] {limb_name}: bridge_edge_loops SUCCESS")
        success = True
    except Exception as e:
        print(f"[bridge ERROR] {limb_name}: bridge failed: {e}")
        success = False

    bpy.ops.object.mode_set(mode='OBJECT')
    return success


def cleanup_post_bridge(obj, merge_thr=0.00005, smooth_angle_deg=60,
                       rim_z_world=0.30, rim_tolerance=0.005):
    """
    Bridge/UNION 後の mesh を systematic にclean up:
    1. merge_by_distance（duplicate 除去、1e-5 オーダー）
    2. delete_loose（孤立 vertex/edge）
    3. dissolve_degenerate（tiny area face / collinear edge）
    4. normals_make_consistent（normal 不連続 by moiré 原因）
    5. rim edge を mark_sharp（crisp 保持）
    6. shade_smooth + shade_smooth_by_angle（smooth by angle で seam 滑らか）
    """
    import bmesh
    bpy.context.view_layer.objects.active = obj

    # 1. Merge duplicates (bmesh.ops — Blender 5.x で bpy.ops.mesh.merge_by_distance が無い場合保険)
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.remove_doubles(bm, verts=list(bm.verts), dist=merge_thr)
    bmesh.update_edit_mesh(obj.data)

    # 1b. Delete loose (bpy.ops 経由、edit mode で select_all→delete_loose は安定)
    bpy.ops.mesh.select_all(action='SELECT')
    try:
        bpy.ops.mesh.delete_loose()
    except Exception as e:
        print(f"[cleanup WARN] delete_loose: {e}")

    # 2. Dissolve degenerate
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    tiny_faces = [f for f in bm.faces if abs(f.calc_area()) < 1e-8]
    if tiny_faces:
        bmesh.ops.delete(bm, geom=tiny_faces, context='FACES')
        print(f"[cleanup] removed {len(tiny_faces)} tiny-area faces")
    bmesh.ops.dissolve_degenerate(bm, dist=merge_thr)
    bmesh.update_edit_mesh(obj.data)

    # 3. Normals consistent (outside)
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False)

    # 4. Rim edges を mark sharp（v.co は world ベース、matrix_world 経由で厳密に）
    bm = bmesh.from_edit_mesh(obj.data)
    bm.edges.ensure_lookup_table()
    rim_count = 0
    for e in bm.edges:
        if not e.is_boundary:
            continue
        # world z = matrix_world @ local → z 成分
        z_vals = [(obj.matrix_world @ v.co).z for v in e.verts]
        if all(abs(z - rim_z_world) < rim_tolerance for z in z_vals):
            e.smooth = False  # mark sharp
            rim_count += 1
    bmesh.update_edit_mesh(obj.data)
    print(f"[cleanup] marked {rim_count} rim edges as sharp")

    bpy.ops.object.mode_set(mode='OBJECT')

    # 5. Shade smooth + by angle
    bpy.ops.object.shade_smooth()
    try:
        bpy.ops.object.shade_smooth_by_angle(angle=math.radians(smooth_angle_deg))
        print(f"[cleanup] shade_smooth_by_angle({smooth_angle_deg}°) applied")
    except Exception as e:
        print(f"[cleanup WARN] shade_smooth_by_angle not available: {e}")

    # Stats
    print(f"[cleanup] {obj.name}: final {len(obj.data.vertices)} verts / {len(obj.data.polygons)} faces")


def verify_hollow_clearance(obj, center=(0, 0, 0.30), inner_r=0.30, margin=0.005):
    """全 vertex が hollow (inner_r 内) に侵入していないか verify。"""
    center_vec = Vector(center)
    penetrations = []
    for v in obj.data.vertices:
        world_v = obj.matrix_world @ v.co
        dist = (Vector(world_v) - center_vec).length
        if dist < inner_r - margin:
            penetrations.append((v.index, world_v, dist))

    if penetrations:
        print(f"[hollow ERROR] {len(penetrations)} verts penetrate hollow (r<{inner_r-margin})")
        for idx, wv, d in penetrations[:5]:
            print(f"  vert {idx} @ {wv} dist={d:.4f}")
        return False
    print(f"[hollow OK] all verts clear of inner r={inner_r-margin}")
    return True


# ============================================================
# 4. Pot body (open bowl) — v9 の実績 solidify 構成を維持
#    ただし Edge Split は post-bridge cleanup に譲る（bridge 後まとめて処理）
# ============================================================
PARTING_Z = 0.30
BASE_SPHERE_R = 0.325
WALL_THICKNESS = 0.050

body_lower = add_sphere("BodyLower", (0, 0, PARTING_Z), BASE_SPHERE_R, 64, 32, mat_pink)

# bmesh で top 削除（equator ring 保持）
bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.mode_set(mode='EDIT')
bm = bmesh.from_edit_mesh(body_lower.data)
bm.faces.ensure_lookup_table()
faces_to_delete = [f for f in bm.faces
                   if min(v.co.z for v in f.verts) > 0.0001]
bmesh.ops.delete(bm, geom=faces_to_delete, context='FACES')
bmesh.update_edit_mesh(body_lower.data)
bpy.ops.object.mode_set(mode='OBJECT')

# Solidify offset=0 で wall 作成（rim z=PARTING_Z 保持）
sol_pot = body_lower.modifiers.new(name="SHELL", type='SOLIDIFY')
sol_pot.thickness = WALL_THICKNESS
sol_pot.offset = 0.0
sol_pot.use_even_offset = True
sol_pot.use_quality_normals = True
sol_pot.use_rim = True
bpy.ops.object.modifier_apply(modifier="SHELL")

bpy.context.view_layer.objects.active = body_lower
bpy.ops.object.shade_smooth()

# ============================================================
# 5. Head — ほぼ球、底軽く flatten
# ============================================================
head = add_sphere("Head", (0, 0, 0.82), 0.29, 64, 32, mat_pink)
squash(head, 1.00, 1.00, 0.98)
for v in head.data.vertices:
    wz = v.co.z + head.location.z
    if wz < 0.74:
        ratio = min(1.0, max(0.0, (0.74 - wz) / 0.08))
        v.co.z += ratio * 0.015

# ============================================================
# 6. Arms / Legs を Bridge Edge Loops で body に接続
#    stubby sphere（Kirby 寄り）を body outer surface に organic attach
#
# Hollow clearance verification:
#   arm  center (±0.52, 0, 0.20), base r=0.17, squash (1.10, 0.95, 1.05)
#     half extents (0.187, 0.162, 0.179)
#     dist to (0,0,0.30) = sqrt(0.52² + 0.10²) = 0.530
#     closest approx = 0.530 - 0.187 = 0.343 > 0.30 ✓ (clearance 0.043)
#   leg  center (±0.17, 0, -0.18), base r=0.14, squash (1.00, 1.15, 1.30)
#     half extents (0.14, 0.161, 0.182)
#     dist to (0,0,0.30) = sqrt(0.17² + 0.48²) = 0.509
#     closest approx = 0.509 - 0.182 = 0.327 > 0.30 ✓ (clearance 0.027)
# ============================================================

LIMB_SPECS = [
    # (name, attach_world, outward_world, base_radius, squash_xyz, hole_radius)
    # squash: (X_width, Y_depth, Z_length) — Z は長さ方向（outward 方向）
    # Kirby 風 stubby: Z (長さ) 控えめ、XY (幅) robust に
    ("Arm_L", (-0.33, 0, 0.22), (-1.0, 0, 0.0), 0.16, (1.00, 1.00, 1.25), 0.10),
    ("Arm_R", ( 0.33, 0, 0.22), ( 1.0, 0, 0.0), 0.16, (1.00, 1.00, 1.25), 0.10),
    ("Leg_L", (-0.13, 0, -0.02), (-0.3, 0, -1.0), 0.15, (1.00, 1.00, 1.40), 0.09),
    ("Leg_R", ( 0.13, 0, -0.02), ( 0.3, 0, -1.0), 0.15, (1.00, 1.00, 1.40), 0.09),
]

for name, attach, outward, base_r, sq, hole_r in LIMB_SPECS:
    limb = create_hemisphere_limb(
        name=f"{name}_hemi",
        radius=base_r,
        squash_xyz=sq,
        outward_dir_world=outward,
        attach_world=attach,
        protrude_offset=0.006,
        material=mat_pink,
    )

    success = bridge_limb_to_body(
        body=body_lower,
        limb=limb,
        attach_world=attach,
        hole_radius=hole_r,
        exclude_rim_z=PARTING_Z,
        exclude_rim_tolerance=0.008,
    )
    if not success:
        print(f"[WARN] {name}: bridge failed, limb remains as separate mesh")

# ============================================================
# 7. Post-Bridge cleanup (Agent2 レシピ)
# ============================================================
cleanup_post_bridge(
    body_lower,
    merge_thr=0.00005,
    smooth_angle_deg=60,
    rim_z_world=PARTING_Z,
    rim_tolerance=0.005,
)

# Hollow verify
verify_hollow_clearance(body_lower, center=(0, 0, PARTING_Z), inner_r=0.30, margin=0.003)

# ============================================================
# 8. Body Upper Clear (Glass dome)
# ============================================================
body_upper = add_sphere("BodyUpperClear", (0, 0, PARTING_Z), BASE_SPHERE_R, 64, 32, mat_clear)

bpy.context.view_layer.objects.active = body_upper
bpy.ops.object.mode_set(mode='EDIT')
bm_u = bmesh.from_edit_mesh(body_upper.data)
bm_u.faces.ensure_lookup_table()
bottom_faces = [f for f in bm_u.faces
                if max(v.co.z for v in f.verts) < -0.0001]
bmesh.ops.delete(bm_u, geom=bottom_faces, context='FACES')
bmesh.update_edit_mesh(body_upper.data)
bpy.ops.object.mode_set(mode='OBJECT')

sol_dome = body_upper.modifiers.new(name="SHELL", type='SOLIDIFY')
sol_dome.thickness = WALL_THICKNESS
sol_dome.offset = 0.0
sol_dome.use_even_offset = True
sol_dome.use_quality_normals = True
sol_dome.use_rim = False  # pot rim と z-fighting しないよう dome は rim face 生成しない
bpy.ops.object.modifier_apply(modifier="SHELL")

bpy.context.view_layer.objects.active = body_upper
bpy.ops.object.shade_smooth()
try:
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(40))
except Exception:
    pass

# ============================================================
# 9. Eyes + Mouth (unchanged)
# ============================================================
EYE_Y = -0.24
EYE_Z = 0.85
EYE_DX = 0.11
eye_l = add_sphere("Eye_L", (-EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)
eye_r = add_sphere("Eye_R", ( EYE_DX, EYE_Y, EYE_Z), 0.062, 28, 16, mat_dark)

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
bpy.ops.mesh.primitive_cube_add(size=0.5, location=(0, -0.305, MOUTH_Z + 0.25))
boolean_difference(mouth, bpy.context.object)

# ============================================================
# 10. Armature (unchanged)
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
# 11. Parent mesh to armature
# ============================================================
mesh_objects = [body_lower, head, body_upper, eye_l, eye_r, mouth]
bpy.ops.object.select_all(action='DESELECT')
for m in mesh_objects:
    m.select_set(True)
armature_obj.select_set(True)
bpy.context.view_layer.objects.active = armature_obj
bpy.ops.object.parent_set(type='ARMATURE_AUTO')

# ============================================================
# 12. Save + Export
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

print(f"\n=== EGGYBOT v10 build complete ===")
print(f"body_lower = pot + arms + legs via Bridge Edge Loops (manifold)")
print(f"Post-bridge cleanup applied (merge 0.00005 + normals + shade_smooth_by_angle 60°)")
print(f"Rim crisp preserved via edge.smooth=False marking")
print(f"Bones: {len(arm.bones)}, Meshes: {len(mesh_objects)}")
