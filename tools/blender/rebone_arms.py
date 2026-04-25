"""
腕の骨を再配置: UpperArm を「肩 pivot で長く」、LowerArm/Hand を「先端で短く」。

問題: 現状 UpperArm pivot が腕中央 → Walking 振りで bump が歪む
解決: UpperArm を肩関節から伸ばす長い骨にして、回転 pivot を肩へ
"""
import bpy
import os
import math

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FBX_PATH = os.path.join(PROJECT_ROOT, "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx")
BLEND_PATH = os.path.join(PROJECT_ROOT, "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend")

# Body 中心 + サイズ
PARTING_Z = 0.30
BODY_CTR_Z = 0.18
BODY_R = 0.37
HAND_X = 0.4314
HAND_Z = 0.22

# 肩関節 pivot: body 表面 at hand 高さ
import math as m
shoulder_x = m.sqrt(BODY_R**2 - (HAND_Z - BODY_CTR_Z)**2)  # ≈ 0.367
print(f"[rebone] shoulder pivot x = {shoulder_x:.3f}")

# 新しい腕骨設計（character left = +x）
# Shoulder: 体内浅い位置 → 体表面（pivot=肩）。短い stub
# UpperArm: 肩 pivot → hand 先端の手前（長い、bump 全体を包む）
# LowerArm: hand 先端の手前 → hand 先端（短い）
# Hand: tip 周辺
NEW_BONES = {
    'LeftShoulder':  ((0.10,  0, HAND_Z),       (shoulder_x,  0, HAND_Z)),
    'LeftUpperArm':  ((shoulder_x,  0, HAND_Z), (HAND_X + 0.08,  0, HAND_Z)),
    'LeftLowerArm':  ((HAND_X + 0.08,  0, HAND_Z), (HAND_X + 0.10,  0, HAND_Z)),
    'LeftHand':      ((HAND_X + 0.10,  0, HAND_Z), (HAND_X + 0.12,  0, HAND_Z)),
    'RightShoulder': ((-0.10, 0, HAND_Z),       (-shoulder_x, 0, HAND_Z)),
    'RightUpperArm': ((-shoulder_x, 0, HAND_Z), (-HAND_X - 0.08, 0, HAND_Z)),
    'RightLowerArm': ((-HAND_X - 0.08, 0, HAND_Z), (-HAND_X - 0.10, 0, HAND_Z)),
    'RightHand':     ((-HAND_X - 0.10, 0, HAND_Z), (-HAND_X - 0.12, 0, HAND_Z)),
}

# Edit mode で骨頭尾を更新
arm = bpy.data.objects.get("EggybotArmature")
if not arm:
    raise RuntimeError("EggybotArmature not found")

bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')

ebones = arm.data.edit_bones
for bname, (head, tail) in NEW_BONES.items():
    eb = ebones.get(bname)
    if eb is None:
        print(f"[rebone] WARN: {bname} not found")
        continue
    eb.head = head
    eb.tail = tail
    print(f"[rebone] {bname}: head={head} tail={tail}")

bpy.ops.object.mode_set(mode='OBJECT')

# ========= 重み再ペイント =========
# 新しい UpperArm 骨は「肩から hand 先端まで」の長い線分
# bump 周辺の頂点は UpperArm に bind、shoulder 寄りも UpperArm
# transition で body と滑らか接続

H_RX, H_RY, H_RZ = 0.130, 0.060, 0.080  # inner ellipsoid (100% UpperArm)
F_RX, F_RY, F_RZ = 0.090, 0.140, 0.100
H_TRX, H_TRY, H_TRZ = 0.220, 0.110, 0.130  # outer (transition)
F_TRX, F_TRY, F_TRZ = 0.180, 0.220, 0.180

# bone center for weight = bump center（mesh 位置）
HAND_L_CTR = ( HAND_X, 0.0, HAND_Z)
HAND_R_CTR = (-HAND_X, 0.0, HAND_Z)
FOOT_X = 0.131
FOOT_Z = -0.10
FOOT_L_CTR = ( FOOT_X, 0.03, FOOT_Z)
FOOT_R_CTR = (-FOOT_X, 0.03, FOOT_Z)

def ellip_d(p, ctr, rx, ry, rz):
    return math.sqrt(((p[0]-ctr[0])/rx)**2 + ((p[1]-ctr[1])/ry)**2 + ((p[2]-ctr[2])/rz)**2)

body = bpy.data.objects.get("BodyLower")

# 関連 VG クリア + 再生成
for vgname in ["LeftUpperArm", "RightUpperArm", "LeftUpperLeg", "RightUpperLeg",
               "LeftHand", "RightHand", "LeftFoot", "RightFoot",
               "LeftLowerArm", "RightLowerArm", "LeftLowerLeg", "RightLowerLeg",
               "LeftShoulder", "RightShoulder", "Hips"]:
    vg = body.vertex_groups.get(vgname)
    if vg:
        body.vertex_groups.remove(vg)

vg_luarm = body.vertex_groups.new(name='LeftUpperArm')
vg_ruarm = body.vertex_groups.new(name='RightUpperArm')
vg_luleg = body.vertex_groups.new(name='LeftUpperLeg')
vg_ruleg = body.vertex_groups.new(name='RightUpperLeg')
vg_hips = body.vertex_groups.new(name='Hips')

for name in ['LeftShoulder', 'LeftLowerArm', 'LeftHand',
             'RightShoulder', 'RightLowerArm', 'RightHand',
             'LeftLowerLeg', 'LeftFoot', 'RightLowerLeg', 'RightFoot']:
    if not body.vertex_groups.get(name):
        body.vertex_groups.new(name=name)

count_inner = count_trans = count_body = 0
for i, v in enumerate(body.data.vertices):
    p = (v.co.x, v.co.y, v.co.z + PARTING_Z)

    dl_h_in = ellip_d(p, HAND_L_CTR, H_RX, H_RY, H_RZ)
    dr_h_in = ellip_d(p, HAND_R_CTR, H_RX, H_RY, H_RZ)
    dl_f_in = ellip_d(p, FOOT_L_CTR, F_RX, F_RY, F_RZ)
    dr_f_in = ellip_d(p, FOOT_R_CTR, F_RX, F_RY, F_RZ)

    best = min(
        ('hl', dl_h_in, vg_luarm, H_TRX/H_RX),
        ('hr', dr_h_in, vg_ruarm, H_TRX/H_RX),
        ('fl', dl_f_in, vg_luleg, F_TRX/F_RX),
        ('fr', dr_f_in, vg_ruleg, F_TRX/F_RX),
        key=lambda c: c[1],
    )
    tag, d_in, vg, tr_ratio = best

    if d_in < 1.0:
        vg.add([i], 1.0, 'REPLACE')
        count_inner += 1
    elif d_in < tr_ratio:
        # smooth transition
        t = (tr_ratio - d_in) / (tr_ratio - 1.0)
        w = t * t * (3 - 2 * t)
        vg.add([i], w, 'REPLACE')
        vg_hips.add([i], 1.0 - w, 'REPLACE')
        count_trans += 1
    else:
        vg_hips.add([i], 1.0, 'REPLACE')
        count_body += 1

print(f"[rebone] weights: inner={count_inner}, transition={count_trans}, body={count_body}")

# 保存 + FBX export
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
print(f"[rebone] saved blend")

for o in bpy.data.objects:
    o.select_set(o.type in {'ARMATURE', 'MESH'})

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
print(f"[rebone] FBX exported")
