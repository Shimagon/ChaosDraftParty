# Codex -> Claude Handoff: Controller / Camera Feel

Date: 2026-05-24
Repo: `C:\Users\taise\Documents\GitHub\ChaosDraftParty`
Scene used: `Assets/_Project/Scenes/Sandbox_v21_crouch_collider_test.unity`

## Read First

Claude should still follow the normal boot order:

1. `RULES.md`
2. `CLAUDE.md`
3. `AGENTS.md`
4. `.claude/rules/*.md`
5. `docs/KNOWLEDGE-INDEX.md`

This handoff only covers the Codex work from the controller/camera feel session.

## Current User Feedback

- Dust VFX after reduction: user said it was good: `おけ結構いいね`.
- Camera shake/FOV/Impulse fixes: user said motion sickness was gone: `酔わなくなった`.
- Fall gravity change: user said it felt very good: `めちゃええやん`.
- First gamepad auto-camera assist was rejected hard: `これはひどい。やりずらすぎる`.

Important: do **not** re-enable the first auto-follow behavior by default.

## Commits Made By Codex Today

Recent relevant commits, newest first:

```text
4db3c47 fix(camera): 自動追従を切りカメラリセットを追加
2e10892 feat(camera): プロコン向け半自動カメラを追加
bf5530b fix(player): 落下重力を締める
d35096c fix(camera): ジャンプ時の縦揺れを抑制
1e2d2fa fix(camera): ダッシュとジャンプの揺れを停止
b6e2ace fix(player): 移動慣性とジャンプ補助を調整
a9a84b7 fix(vfx): 砂埃を控えめでかわいい表現に調整
```

The bad auto-camera behavior from `2e10892` was corrected by `4db3c47`.

## Current Intended State

### Camera comfort

The user was getting motion sick from camera motion. The stable direction that worked:

- No dash FOV punch.
- No Cinemachine Impulse / listener shake.
- No dash-time camera radius shift.
- No Cinemachine noise.
- Vertical jump follow softened in the active Sandbox scene:
  - `CinemachineOrbitalFollow.TrackerSettings.PositionDamping.y = 0.85`
  - `CinemachineRotationComposer.Damping.y = 0.85`
  - vertical dead zone enabled with size `(0.08, 0.30)`

### Movement / jump

`Assets/_Project/Scripts/Core/PlayerController.cs`

- Ground movement was split into accel/decel/turn accel to reduce drift.
- Coyote time / jump buffer / jump cut are in.
- Fall gravity is asymmetric:
  - normal gravity while rising
  - `fallGravityMultiplier = 1.55` while falling
  - `maxFallSpeed = 18`

This fall-gravity change was positively received.

### Gamepad

Switch Pro Controller works through Unity Input System.

Current controls:

- Left stick: move
- Right stick: camera
- B / buttonSouth: jump
- A / buttonEast: crouch
- ZL / leftTrigger: dash
- R / rightShoulder: camera reset
- Keyboard `R`: camera reset

Gamepad right-stick sensitivity is now separate from mouse:

- `gamepadYawSpeed = 180 deg/sec`
- `gamepadPitchSpeed = 120 deg/sec`
- `gamepadLookDeadzone = 0.12`

Auto camera assist is currently **OFF**:

- `gamepadCameraAssist = false`

Camera reset is explicit/manual:

- `cameraResetYawSpeed = 420 deg/sec`
- `cameraResetYawEpsilon = 1.5`

## Critical Lesson From Failed Auto Camera

The first auto-camera assist was bad because it rotated the camera toward `horizontalVel`.

That conflicts with camera-relative movement:

1. Movement direction is computed from `cameraTransform.forward/right`.
2. Assist rotated the camera toward the current movement velocity.
3. The camera basis changed while the same stick input was held.
4. Strafing/backpedaling/air-control felt like the camera was stealing control.

This is a design conflict, not just overtuned values.

Do not implement any auto-follow that:

- Uses `horizontalVel` as the camera yaw target during camera-relative movement.
- Runs during strafe, backpedal, or air movement.
- Runs immediately after right-stick input.
- Is enabled by default without a user setting.

If auto assist is revisited later, use these constraints:

- Default OFF, or settings: `OFF / Low / Default`.
- Only grounded.
- Only forward input, e.g. `moveInput.y > 0.5f && abs(moveInput.x) < 0.35f`.
- Wait after manual look: `0.8s+`.
- Weak yaw speed: start around `20-45 deg/sec`.
- Do not adjust pitch automatically during normal play.
- Manual right-stick input cancels immediately.

## Sub-Agent Research Summary

Three Codex sub-agents were used:

1. AAA / large-game camera patterns.
2. Unity Cinemachine 3 implementation patterns.
3. Code review of why commit `2e10892` felt bad.

Shared conclusion:

- For this game, use **manual-first camera**.
- Add explicit camera reset/recenter button.
- Strong automatic movement-direction yaw is risky.
- Lock-on / Z-target style is not default-suitable for a 2-4 player party TPS; if ever added, prefer tap-to-face-target, not permanent lock.
- Avoid adding `CinemachineInputAxisController` while `PlayerController` directly writes `OrbitalFollow.HorizontalAxis/VerticalAxis`. One owner only.

Useful references from the research:

- Unity Cinemachine Orbital Follow: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html
- Unity Cinemachine Input Axis Controller: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/manual/CinemachineInputAxisController.html
- Unity Cinemachine Rotation Composer: https://docs.unity.cn/Packages/com.unity.cinemachine@3.1/manual/CinemachineRotationComposer.html
- Unity Input System Gamepad: https://docs.unity.cn/Packages/com.unity.inputsystem@1.9/manual/Gamepad.html
- Unreal third-person camera design article: https://www.unrealengine.com/tech-blog/six-ingredients-for-a-dynamic-third-person-camera
- Xbox accessibility guideline for camera/motion comfort: https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117

## Verification Already Done

After the final commit:

- Unity console: 0 errors / 0 warnings.
- Unity serialized values checked:
  - `gamepadCameraAssist=False`
  - `cameraResetYawSpeed=420`
  - `cameraResetYawEpsilon=1.5`
  - `gamepadYawSpeed=180`
  - `gamepadPitchSpeed=120`

User still needs to playtest `R` / Pro Controller `R` camera reset feel.

## Current Worktree Warning

There are many unrelated dirty/untracked files in the worktree, mostly character lab / art / material / `.claude/state` files.

Do not revert or stage them unless the user explicitly asks.

At the time of this handoff, Codex intended only the handoff file itself to be a new change.

## Recommended Next Steps For Claude

1. Ask user to test Pro Controller again with the latest state:
   - Right stick camera sensitivity.
   - `R` shoulder camera reset while jumping/dashing.
   - Whether explicit reset solves the right-thumb burden without stealing control.
2. If reset feels good, keep auto assist OFF.
3. If right stick still feels slow, tune only:
   - `gamepadYawSpeed` in `150-220`
   - `gamepadPitchSpeed` in `90-140`
4. If camera reset is too fast/slow:
   - tune `cameraResetYawSpeed` around `300-540`.
5. Only after user asks for settings UI, expose:
   - gamepad look sensitivity
   - invert Y
   - camera reset speed
   - camera assist OFF/Low/Default

