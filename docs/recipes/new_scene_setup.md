# New Scene Setup Checklist

Use this before accepting any new minigame scene or major stage art pass.

## First 10 Seconds

- The default Play camera must answer: where am I, where do I go, what is dangerous?
- Verify with a Play-mode game camera screenshot, not only Scene view.
- If the objective is off-screen at spawn, fix camera/orbital values before adding decoration.
- Prototype HUD is allowed only as backup; the world itself should explain the rule.

## Unity Feel Erasure

- Replace default sky, flat URP/Lit colors, ungraded lighting, default particles, and raw primitive silhouettes.
- Use stage-specific materials or shaders for floors, walls, goals, and interactables.
- Do not solve a primitive-looking scene by adding more saturated colors. First fix value, lighting, material response, silhouette, and camera.
- Generated assets are accepted only if they improve gameplay-camera readability.

## Mesh And Side-Face Safety

- Gameplay floors, walls, and blockers must use closed meshes.
- Do not place `SI_TopChamferBox` in scenes. It produced missing/thin side-face reads twice.
- Bevels/rounding require low-angle and gameplay-camera screenshots before acceptance.
- If side faces look transparent, thin, striped, or tooth-like, revert to closed structural meshes and add safe trim/top pieces.

## Gameplay State Binding

- Visual children on tiles or interactables must follow gameplay state: healthy, warning, fading, gone, cooldown.
- Warning should read as authored hazard language, not debug red.
- VFX changes require a Unity Console check. Repeated particle warnings are not acceptable.
- Repeated Play/reset must not leave stale coroutines, stale material blocks, or half-reset objects.

## Camera

- Cinemachine can overwrite Main Camera transforms. Save and verify orbital axis values in Play.
- Capture both start-of-round and failure/KO camera states.
- Avoid follow-camera states that show empty sky after a fall.

## Acceptance Evidence

- Required screenshots:
  - Play-mode game camera at spawn.
  - Low-angle side-face check.
  - Warning/fade VFX preview.
  - Top-down or high oblique layout read.
- Required checks:
  - Unity Console has zero new errors/warnings.
  - Relevant EditMode tests pass.
  - `dotnet build` passes for touched assemblies.
