# Unity Feel Erasure

Date: 2026-05-25
Status: Always-on visual direction

## Prime Directive

ChaosDraftParty must not look like a default Unity prototype.

Every visual, camera, VFX, asset, skybox, material, and UI decision should be checked against this question:

> Does this still smell like Unity defaults, or does it look like a deliberate game world?

This is not a polish-only rule. It affects gameplay readability, player trust, and whether the game feels authored.

## Common Unity Smell To Remove

- Default procedural skybox with flat horizon.
- Plain URP Lit materials with no authored palette or surface language.
- Randomly colored primitives with no role grammar.
- Unstyled white/gray UI or default fonts.
- Generic particle presets.
- Asset-store or AI-generated objects dropped in without matching silhouette, color, scale, and material language.
- Camera defaults that feel like a sample scene instead of a designed party game camera.
- Lighting with no mood, no gradient, no fog/atmosphere, and no grade.

## Reference Translation

Use professional references, but translate them into this game instead of copying.

| Reference | What To Learn | ChaosDraftParty Translation |
|---|---|---|
| Fall Guys | Simple shapes and color systems stay readable under chaos. | Hazards, launchers, bumpers, safe zones, and player colors need distinct silhouettes and palettes. |
| Splatoon | Color is identity, function, and world fiction at the same time. | Minigame rules should have bold color language that feels authored, not debug-colored. |
| Overcooked | Space changes create comedy when they force players into new decisions. | Stage events should visibly reshape routes and create player contact, not only remove floor. |
| Unity visual docs | Skybox and post-processing are art direction, not afterthoughts. | Replace default sky/lighting first; then tune materials and VFX under that look. |

## Shrinking Island Visual Direction

Current gameplay blockout is acceptable as a design test, but the next visual pass must:

- Keep the readable primitive language until a better asset is proven from gameplay camera distance.
- Replace the default skybox with a stylized authored sky/void.
- Add a global color grade that pushes the scene away from Unity's default blue-gray.
- Make center goal, recovery launcher, bumper, warning tile, and falling tile visually unmistakable.
- Reject Meshy/AI props unless they are clearer than the blockout.
- Prefer one strong authored shape over noisy decorative detail.

## Acceptance Test

Before accepting any visual change:

1. Screenshot from the default gameplay camera.
2. Screenshot from top-down/debug view.
3. Ask what each visible gameplay object does without reading labels.
4. If a prop looks like "random generated object" or "Unity sample scene", remove or redesign it.

## Lessons Learned

### 2026-05-25: Bevel Mesh Failure

Do not accept custom mesh edits until the side faces are inspected from the gameplay camera.

What happened:

- A quick beveled tile mesh made the floor and walls look less cubic from above.
- From the gameplay camera, the side faces appeared cut out / transparent.
- This made the scene look broken and more prototype-like than the original cube.

Decision:

- Reverted floor and bumper visuals to a closed box mesh (`SI_ClosedBox`).
- Future roundness should be added with safe extra pieces such as top plates, trim, caps, and shadow accents, not by rushed destructive bevel geometry.
- Mesh changes must pass both default camera and top-down screenshots before staying.

### 2026-05-25: Tile Child Renderer Warning

Visual detail pieces must stay connected to gameplay state.

What happened:

- Top plates made the board feel more authored, but they also created child renderers on each tile.
- The original tile warning/fade code only targeted the parent renderer.
- That risked leaving the new plate detail visually healthy while the underlying tile was warning or disappearing.

Decision:

- `Tile` now caches all child `MeshRenderer` components and applies healthy/warning/fade colors to each one.
- Runtime renderer state must recreate `MaterialPropertyBlock` if it is missing, because Play/domain reload paths can invalidate runtime-only fields.
- Any future tile decoration must be tested through warning and gone states, not only in the healthy screenshot.

### 2026-05-25: Camera Must Never Chase Empty Sky

Single-player sandbox tests need their own recovery guard.

What happened:

- In direct Sandbox Play, NGO KO does not run because the player is not spawned as a network object.
- After falling, the gameplay camera kept following the player downward into empty sky.
- That makes a working stage look broken and destroys the user's ability to judge the layout.

Decision:

- `PlayerController` now respawns only non-networked ShrinkingIsland sandbox players after falling below the KO threshold.
- Networked KO behavior remains separate.
- Any future camera/game-feel check must include "what does the camera show after failure?"

### 2026-05-25: Cube Color-Swap Smell

The user correctly called out that the stage still looked like colored Unity cubes.

What caused it:

- URP/Lit single-color materials made every face read as a flat debug surface.
- Hard 90-degree cube silhouettes made the board look like primitives even after palette and skybox work.
- Bright top panels clipped toward white, hiding material information.
- Static disappearing tiles felt like editor toggles, not stage behavior.

Decision:

- Added `ChaosDraftParty/ShrinkingIslandSurface`, a stage-only URP shader with subtle world-space variation, face-aware top/side shading, rim, and edge darkening.
- Added `SI_TopChamferBox`, a closed top-chamfer mesh. It is assigned to tile bodies, top plates, and blocker/wall pieces after gameplay and top-down screenshot checks.
- Retuned top plate colors away from pure white.
- Added tile fade motion: fading tiles now sink and tilt before becoming inactive.

Guardrail:

- Material noise must stay subtle. If side surfaces look dirty or striped, reduce side/noise before adding more decoration.
- Mesh edits are accepted only after both gameplay camera and top-down screenshots show no missing faces.

### 2026-05-25: Surface Craft And Route Marks

Gameplay readability marks must look like authored world surfaces, not debug UI.

What happened:

- A first route-language pass used long cyan bars toward the center.
- From the gameplay camera, one mark looked like a large UI strip pasted onto the floor.
- The stage also still had broad unbroken top faces, which kept the "colored cube board" smell even after shader and bevel work.

Decision:

- Shortened and muted the route marks into small embedded dash groups.
- Added `Visual_SurfaceInsets` children to each managed tile: tiny grooves/catches that move, warn, fade, and disappear with the tile.
- Added three subtle inset materials: soft shadow, cream light, and cool shadow.
- Retuned sky, ambient, fog, sun, and post-processing to reduce white clipping and default-scene flatness.
- Changed tile warning/fade color from full debug red to a warm hazard orange.

Guardrail:

- Route marks are allowed only if they read as floor craft at gameplay distance.
- If inset pieces start looking like random confetti, reduce count or saturation before adding any new decoration.

### 2026-05-25: Side Faces Regressed Again

Do not use `SI_TopChamferBox` in the scene until it is rebuilt from a safer mesh source.

What happened:

- The top-chamfer mesh again produced missing/thin side-face reads from player-visible angles.
- Even if some screenshots looked acceptable, the user correctly caught the regression in-editor.

Decision:

- Replaced every scene use of `SI_TopChamferBox` with `SI_ClosedBox`.
- Current accepted scene count: `SI_TopChamferBox=0`, `SI_ClosedBox=192`.
- Future bevel/rounding should be rebuilt in Blender/ProBuilder or generated with explicit face checks, then verified from low gameplay angles before use.

### 2026-05-25: Warning Must Read As Stage Language

Default red flashing is a prototype smell.

What happened:

- Pure red tile flashing did communicate danger, but it looked like a debug state.
- Fade particles initially produced repeated Unity warnings because velocity curves used incompatible modes.
- Editor Play tests also revealed that stale shrink coroutines can survive repeated sandbox play/reset flows when Unity reload settings are optimized.

Decision:

- Added `SI_TileWarningVfx.prefab`: a warm hazard-orange edge frame that appears through the existing `Tile._warningParticlePrefab` hook.
- Added `SI_TileFadeBurstVfx.prefab` and disabled the problematic velocity module.
- Changed tile warning/fade color to warm hazard orange instead of `Color.red`.
- `ShrinkingIslandManager` now resets round state on start/spawn/reset, stops all owned coroutines, and guards shrink loops with a round version.

Guardrail:

- Warning VFX should tell the player "this platform is about to go" without making the floor look like editor gizmos.
- Particle prefabs must be verified with the Unity Console open; no repeated particle warnings are acceptable.

### 2026-05-25: Objective Camera And Meshy Beacon

The stage must explain itself from the default gameplay camera, not only from top-down editor inspection.

What happened:

- The camera was showing the player and nearby tiles while the center objective sat off-screen.
- This made the stage read like disconnected platforms instead of "reach the safe center before the island disappears."
- A first procedural center beacon helped, but stacked rings became visual noise.
- A Meshy preview asset produced a readable rounded goal-ring silhouette, so it was imported as a controlled art prop instead of running a full texture refine pass.

Decision:

- Retuned the Cinemachine orbital yaw so direct sandbox Play starts with the center objective visible ahead of the player.
- Added `Assets/_Project/ThirdParty/Meshy/Generated/CenterBeacon/preview.obj` as `Meshy_CenterGoalBeacon` in the center arena.
- Assigned the existing stage material `SI_CenterBeacon_Glow` to keep it in the Shrinking Island palette.
- Removed the temporary procedural center-ring stack to avoid overdecorating the objective.
- Added launch-pad punch scale, soft launch sparks, and a Cinemachine impulse hook so bounce feedback comes from motion plus camera response, not only particles.
- Shortened the first shrink beat from 5 seconds to 3 seconds, with an 8-second layer interval, so the stage visibly starts acting during the cold-start window.

Guardrail:

- Meshy assets are acceptable only when they improve silhouette/readability from the gameplay camera. Do not keep generated detail just because it is generated.
- The initial camera must answer "where do I go?" within the first seconds of Play.
- The first hazard beat should be visible fast enough that players do not think the stage is static.

### 2026-05-25: Rounded Tile Prism Rejected

Raw square tile silhouettes still read as placed Unity cubes even after palette, shader, and camera work.

What happened:

- The first bevel attempt (`SI_TopChamferBox`) was rejected because side faces looked missing/thin from gameplay angles.
- A rounded-prism mesh was generated as a closed convex mesh instead of a destructive chamfer.
- It looked acceptable in a limited screenshot pass, but the user again caught missing/incorrect side-face reads in-editor.

Decision:

- Added `Assets/_Project/Art/Meshes/SI_RoundedTilePrism.asset`.
- Reverted every scene MeshFilter using `SI_RoundedTilePrism` back to `SI_ClosedBox`.
- Removed `Backdrop_FloatingShards`; it also depended on the rejected rounded mesh and risked confusing playable space.
- Current accepted scene count after revert: `SI_RoundedTilePrism=0`, `SI_TopChamferBox=0`, `SI_ClosedBox=192`.
- Reduced warning color application from full orange replacement to a restrained tint (`_warningTintStrength = 0.48`) so the tile keeps its authored base material while VFX/edge language carries the danger read.

Guardrail:

- Do not use `SI_RoundedTilePrism` in scenes. It joins `SI_TopChamferBox` as rejected for gameplay stage surfaces.
- Future roundness must come from vetted Blender/ProBuilder geometry or separate decorative trim that does not replace platform side faces.
- Warning may be obvious, but it should not look like the material was replaced by a debug color.
- Background depth props must not reuse rejected platform meshes.

### 2026-05-25: Falling Limb Stretch Fix

The user caught a severe fall bug: after falling, a hand/foot-like object could appear far from the body.

Cause:

- `EggybotFootGroundClamp` clamped foot world Y to the stage ground even while the player root was falling below the arena.
- This could leave foot visuals near world Y=0 while the body continued downward, creating huge apparent limb separation.

Decision:

- `EggybotFootGroundClamp` now auto-finds `PlayerController` and skips clamping while the player is not grounded or has dropped below the clamp band.
- Sandbox respawn now restores `footSquash`, `squash`, and `bodyDip` runtime visual offsets.
- Forced-fall test after the fix kept feet/hands within normal distance of the player root.

Guardrail:

- World-space clamps for character parts must be gated by grounded/near-ground state. Never clamp limbs to the old stage plane while the root is falling.
