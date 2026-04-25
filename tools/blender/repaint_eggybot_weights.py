"""
ユーザーが骨位置を調整した後、重みの境界をなだらかにする。

問題: 手 bump 100% LeftUpperArm + 体 100% Hips → 境界で stretch
解決: 手/足 bump の周囲に gradient 帯を作る（外側ほど limb、内側ほど body）

Usage:
  /Applications/Blender.app/Contents/MacOS/Blender \
    --background \
    Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend \
    --python tools/blender/repaint_eggybot_weights.py \
    --save-as Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend
"""
import bpy
import os
import math

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
FBX_PATH = os.path.join(PROJECT_ROOT, "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx")
BLEND_PATH = os.path.join(PROJECT_ROOT, "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_source.blend")

# Limb 中心 + 楕円体半径（build_eggybot.py と同じ）
PARTING_Z = 0.30
HAND_X = 0.4314
HAND_Z = 0.22
FOOT_Z = -0.10
FOOT_X = 0.131  # attach_xy 結果

# inner ellipsoid (100% bind)
H_RX, H_RY, H_RZ = 0.120, 0.056, 0.076
F_RX, F_RY, F_RZ = 0.090, 0.140, 0.100

# outer ellipsoid (transition: ここから先は body 100%)
H_TRX, H_TRY, H_TRZ = 0.200, 0.120, 0.140
F_TRX, F_TRY, F_TRZ = 0.180, 0.220, 0.180

HAND_L_CTR = ( HAND_X, 0.0, HAND_Z)
HAND_R_CTR = (-HAND_X, 0.0, HAND_Z)
FOOT_L_CTR = ( FOOT_X, 0.03, FOOT_Z)
FOOT_R_CTR = (-FOOT_X, 0.03, FOOT_Z)

def ellip_d(p, ctr, rx, ry, rz):
    return math.sqrt(((p[0]-ctr[0])/rx)**2 + ((p[1]-ctr[1])/ry)**2 + ((p[2]-ctr[2])/rz)**2)

body = bpy.data.objects.get("BodyLower")
if not body:
    raise RuntimeError("BodyLower not found")

# 既存 limb VG を削除
for vgname in ["LeftUpperArm", "RightUpperArm", "LeftUpperLeg", "RightUpperLeg",
               "LeftHand", "RightHand", "LeftFoot", "RightFoot",
               "LeftLowerArm", "RightLowerArm", "LeftLowerLeg", "RightLowerLeg",
               "LeftShoulder", "RightShoulder"]:
    vg = body.vertex_groups.get(vgname)
    if vg:
        body.vertex_groups.remove(vg)

vg_luarm = body.vertex_groups.new(name='LeftUpperArm')
vg_ruarm = body.vertex_groups.new(name='RightUpperArm')
vg_luleg = body.vertex_groups.new(name='LeftUpperLeg')
vg_ruleg = body.vertex_groups.new(name='RightUpperLeg')
vg_hips = body.vertex_groups.get('Hips') or body.vertex_groups.new(name='Hips')

# 空 VG として残り bone を追加（Unity Humanoid Rig 要求）
for name in ['LeftShoulder', 'LeftLowerArm', 'LeftHand',
             'RightShoulder', 'RightLowerArm', 'RightHand',
             'LeftLowerLeg', 'LeftFoot', 'RightLowerLeg', 'RightFoot']:
    if not body.vertex_groups.get(name):
        body.vertex_groups.new(name=name)

# 既存 Hips weight をクリアして全頂点に再配分
hips_idx = vg_hips.index
for i in range(len(body.data.vertices)):
    vg_hips.add([i], 0.0, 'REPLACE')

count_h = count_t = count_b = 0
for i, v in enumerate(body.data.vertices):
    p = (v.co.x, v.co.y, v.co.z + PARTING_Z)

    # 各 limb との楕円体距離（< 1.0 で 100%、1.0~tr_ratio で gradient）
    dl_h_in = ellip_d(p, HAND_L_CTR, H_RX, H_RY, H_RZ)
    dr_h_in = ellip_d(p, HAND_R_CTR, H_RX, H_RY, H_RZ)
    dl_f_in = ellip_d(p, FOOT_L_CTR, F_RX, F_RY, F_RZ)
    dr_f_in = ellip_d(p, FOOT_R_CTR, F_RX, F_RY, F_RZ)

    dl_h_out = ellip_d(p, HAND_L_CTR, H_TRX, H_TRY, H_TRZ)
    dr_h_out = ellip_d(p, HAND_R_CTR, H_TRX, H_TRY, H_TRZ)
    dl_f_out = ellip_d(p, FOOT_L_CTR, F_TRX, F_TRY, F_TRZ)
    dr_f_out = ellip_d(p, FOOT_R_CTR, F_TRX, F_TRY, F_TRZ)

    # 一番近い limb を選択（inner ratio 基準）
    best = min(
        ('hl', dl_h_in, dl_h_out, vg_luarm),
        ('hr', dr_h_in, dr_h_out, vg_ruarm),
        ('fl', dl_f_in, dl_f_out, vg_luleg),
        ('fr', dr_f_in, dr_f_out, vg_ruleg),
        key=lambda c: c[1],
    )
    tag, d_in, d_out, vg = best

    if d_in < 1.0:
        # inner 100% limb
        vg.add([i], 1.0, 'REPLACE')
        vg_hips.add([i], 0.0, 'REPLACE')
        count_h += 1
    elif d_out < 1.0:
        # transition: limb 1 - normalized, body normalized
        # smoothstep で滑らか
        t = (d_out - 0.0) / 1.0  # 0=inner edge, 1=outer edge
        # d_in は inner 楕円体外で >1, d_out が 0~1 の範囲
        # transition weight: outer 楕円体内で d_in 1→tr_ratio, weight 1→0
        # 簡易: d_in=1 で w=1.0, d_out=1 で w=0.0
        # 線形補間: w = (1.0 - (d_in - 1.0) / (d_in_at_outer - 1.0))
        # d_in_at_outer は楕円体スケール比率 = tr_radius / inner_radius
        # 平均比 ~1.7 → d_in が 1〜1.7 で transition
        w_limb = max(0.0, min(1.0, (1.7 - d_in) / 0.7))
        # smoothstep
        w_limb = w_limb * w_limb * (3 - 2 * w_limb)
        vg.add([i], w_limb, 'REPLACE')
        vg_hips.add([i], 1.0 - w_limb, 'REPLACE')
        count_t += 1
    else:
        # 全部 body
        vg_hips.add([i], 1.0, 'REPLACE')
        count_b += 1

print(f"[repaint] hand/foot inner 100%: {count_h}, transition: {count_t}, body: {count_b}")

# 保存
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
print(f"[repaint] saved blend: {BLEND_PATH}")

# FBX export
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
print(f"[repaint] FBX exported: {FBX_PATH}")
