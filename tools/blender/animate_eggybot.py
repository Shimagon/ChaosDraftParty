"""
EGGYBOT animation builder — Blender Python で 5 基本 anim を programmatic 作成。

作成する Action:
  1. Idle          — 立ち状態の息バウンス (60 frame, 2.5s loop)
  2. Walk          — 歩行ループ (48 frame, 2s loop)
  3. Jump          — クラウチ→跳躍→着地 (36 frame, 1.5s one-shot)
  4. Wave          — 右手を振る (60 frame, 2.5s loop)
  5. CapsuleOpen   — dome を CapHinge で開く (30 frame, 1.25s one-shot)

全 Action を armature に attach → FBX export で bake_anim_use_all_actions=True にして
Unity 側で clip 分割 import。

Mixamo で retarget できない chibi 比率向け bespoke anim。
"""
import bpy
import math
import os

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_PATH = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy",
                          "Characters", "EGGYBOT", "eggybot_source.blend")
FBX_PATH = os.path.join(PROJECT_ROOT, "Assets", "ThirdParty", "Meshy",
                        "Characters", "EGGYBOT", "eggybot_base.fbx")

# Load existing blend
bpy.ops.wm.open_mainfile(filepath=BLEND_PATH)

armature = bpy.data.objects['EggybotArmature']
bpy.ops.object.select_all(action='DESELECT')
armature.select_set(True)
bpy.context.view_layer.objects.active = armature

# Enter pose mode
bpy.ops.object.mode_set(mode='POSE')

# All bones use XYZ euler for predictable keyframes
for pb in armature.pose.bones:
    pb.rotation_mode = 'XYZ'
    pb.rotation_euler = (0, 0, 0)
    pb.location = (0, 0, 0)


def set_scene_fps(fps=24):
    bpy.context.scene.render.fps = fps


def new_action(name):
    """新 Action 作成、armature に attach してアクティブに。"""
    if name in bpy.data.actions:
        bpy.data.actions.remove(bpy.data.actions[name], do_unlink=True)
    action = bpy.data.actions.new(name=name)
    # マーカー対策: use_fake_user で export 時も Action が残る
    action.use_fake_user = True
    if armature.animation_data is None:
        armature.animation_data_create()
    armature.animation_data.action = action
    return action


def key_bone(bone_name, frame, rot=None, loc=None):
    """bone_name の pose bone を frame で keyframe。"""
    pb = armature.pose.bones.get(bone_name)
    if not pb:
        print(f"  [warn] bone {bone_name} not found")
        return
    pb.rotation_mode = 'XYZ'
    if rot is not None:
        pb.rotation_euler = tuple(math.radians(d) for d in rot)
        pb.keyframe_insert(data_path='rotation_euler', frame=frame, group=bone_name)
    if loc is not None:
        pb.location = loc
        pb.keyframe_insert(data_path='location', frame=frame, group=bone_name)


def reset_pose():
    """全 bone を rest pose に戻す。"""
    for pb in armature.pose.bones:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


# ============================================================
# 1. Idle — 立ち状態の息バウンス
# ============================================================
set_scene_fps(24)
reset_pose()
action = new_action("Idle")
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 60

# Hips を Z 方向に微振動 (息)
# 60 frame で 2 呼吸 (sin 2π)
for f in [1, 15, 30, 45, 60]:
    # 24fps で 60 frame = 2.5s、1 呼吸 1.25s
    phase = (f - 1) / 59 * 2 * math.pi * 2
    z_off = -0.015 * (math.sin(phase) ** 2)  # 0 〜 -15mm
    key_bone("Hips", f, loc=(0, 0, z_off))

# Head bone も軽く傾き (首を微妙に傾げる)
key_bone("Head", 1, rot=(0, 0, 0))
key_bone("Head", 30, rot=(-2, 0, 0))
key_bone("Head", 60, rot=(0, 0, 0))

print(f"[Idle] 60 frame created")


# ============================================================
# 2. Walk — 歩行ループ
# ============================================================
reset_pose()
action = new_action("Walk")
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 48

# 足の前後 swing、腕は counter-swing、Hips も上下
# Left leg: forward at f=1, neutral f=12, back at f=24, neutral f=36, forward f=48
# Right leg: opposite phase
for f, phase_deg in [(1, -25), (12, 0), (24, 25), (36, 0), (48, -25)]:
    key_bone("LeftUpperLeg",  f, rot=(phase_deg,  0, 0))
    key_bone("RightUpperLeg", f, rot=(-phase_deg, 0, 0))

# Arm counter-swing (Y axis: 腕の前後振り)
for f, phase_deg in [(1, 20), (12, 0), (24, -20), (36, 0), (48, 20)]:
    key_bone("LeftUpperArm",  f, rot=(0, phase_deg, 0))
    key_bone("RightUpperArm", f, rot=(0, -phase_deg, 0))

# Hips 上下 (歩行の bounce) — 足接地時下がる、踏み出し時上がる
for f, z in [(1, 0), (6, -0.02), (12, 0), (18, -0.02), (24, 0),
             (30, -0.02), (36, 0), (42, -0.02), (48, 0)]:
    key_bone("Hips", f, loc=(0, 0, z))

print(f"[Walk] 48 frame created")


# ============================================================
# 3. Jump — crouch → leap → land (one-shot)
# ============================================================
reset_pose()
action = new_action("Jump")
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 36

# f=1: 通常立ち
# f=6: クラウチ (膝曲げ、Hips 下がる)
# f=12: 伸び上がり (腕上げ、Hips 上)
# f=20: 空中 (ピーク)
# f=28: 着地 (crouch 再)
# f=36: 通常立ち

key_bone("Hips", 1, loc=(0, 0, 0))
key_bone("Hips", 6, loc=(0, 0, -0.06))  # crouch
key_bone("Hips", 12, loc=(0, 0, 0.04))  # launch
key_bone("Hips", 20, loc=(0, 0, 0.15))  # air peak
key_bone("Hips", 28, loc=(0, 0, -0.05)) # land impact
key_bone("Hips", 36, loc=(0, 0, 0))     # return

# 膝 bend (UpperLeg rotation)
key_bone("LeftUpperLeg", 1, rot=(0, 0, 0))
key_bone("LeftUpperLeg", 6, rot=(-30, 0, 0))   # bend
key_bone("LeftUpperLeg", 12, rot=(10, 0, 0))   # extend
key_bone("LeftUpperLeg", 20, rot=(-5, 0, 0))
key_bone("LeftUpperLeg", 28, rot=(-25, 0, 0))
key_bone("LeftUpperLeg", 36, rot=(0, 0, 0))

key_bone("RightUpperLeg", 1, rot=(0, 0, 0))
key_bone("RightUpperLeg", 6, rot=(-30, 0, 0))
key_bone("RightUpperLeg", 12, rot=(10, 0, 0))
key_bone("RightUpperLeg", 20, rot=(-5, 0, 0))
key_bone("RightUpperLeg", 28, rot=(-25, 0, 0))
key_bone("RightUpperLeg", 36, rot=(0, 0, 0))

# 腕上げ (jump 中)
key_bone("LeftUpperArm", 1, rot=(0, 0, 0))
key_bone("LeftUpperArm", 12, rot=(0, 0, 30))  # up-outward
key_bone("LeftUpperArm", 20, rot=(0, 0, 40))
key_bone("LeftUpperArm", 28, rot=(0, 0, 10))
key_bone("LeftUpperArm", 36, rot=(0, 0, 0))

key_bone("RightUpperArm", 1, rot=(0, 0, 0))
key_bone("RightUpperArm", 12, rot=(0, 0, -30))
key_bone("RightUpperArm", 20, rot=(0, 0, -40))
key_bone("RightUpperArm", 28, rot=(0, 0, -10))
key_bone("RightUpperArm", 36, rot=(0, 0, 0))

print(f"[Jump] 36 frame created")


# ============================================================
# 4. Wave — 右手を振る
# ============================================================
reset_pose()
action = new_action("Wave")
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 60

# 右腕を上に (frame 1→15)
key_bone("RightUpperArm", 1, rot=(0, 0, 0))
key_bone("RightUpperArm", 15, rot=(0, 0, -80))   # arm up

# 上で左右に 3 回振る (frame 15→45)
for f, z_deg in [(15, -80), (25, -70), (35, -80), (45, -70)]:
    key_bone("RightUpperArm", f, rot=(0, 0, z_deg))

# 手を振る前後 (LeftHand を上下)
for f, z in [(15, 0), (22, 15), (29, -5), (36, 15), (43, -5), (50, 15), (55, 0)]:
    key_bone("RightHand", f, rot=(z, 0, 0))

# 戻す (frame 45→60)
key_bone("RightUpperArm", 60, rot=(0, 0, 0))

# Head 少し右向き (wave 中は右手見る)
key_bone("Head", 1, rot=(0, 0, 0))
key_bone("Head", 15, rot=(0, 0, -8))
key_bone("Head", 45, rot=(0, 0, -8))
key_bone("Head", 60, rot=(0, 0, 0))

print(f"[Wave] 60 frame created")


# ============================================================
# 5. CapsuleOpen — dome 開閉
# ============================================================
reset_pose()
action = new_action("CapsuleOpen")
bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 30

# CapHinge を X axis で 0 → 55° 開く
key_bone("CapHinge", 1, rot=(0, 0, 0))
key_bone("CapHinge", 20, rot=(55, 0, 0))  # open
key_bone("CapHinge", 30, rot=(55, 0, 0))  # hold

print(f"[CapsuleOpen] 30 frame created")


# ============================================================
# Save blend
# ============================================================
reset_pose()
# 最終的な action state は Idle に
if "Idle" in bpy.data.actions:
    armature.animation_data.action = bpy.data.actions["Idle"]

bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

print(f"\n=== Animation build complete ===")
print(f"Actions: {[a.name for a in bpy.data.actions]}")
print(f"Saved: {BLEND_PATH}")


# ============================================================
# Re-export FBX with all actions baked
# ============================================================
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
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=1.0,
    path_mode='AUTO',
    embed_textures=False,
)
print(f"FBX re-exported with {len(bpy.data.actions)} animation clips")
