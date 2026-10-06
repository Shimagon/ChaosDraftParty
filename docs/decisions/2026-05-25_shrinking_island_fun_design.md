# Shrinking Island Fun Design Reset

Date: 2026-05-25
Status: Active design direction

## Problem

`Sandbox_ShrinkingIsland_v1` reached "can run" quality, but the stage was still essentially a flat 8x8 test board. That is not enough for a party TPS minigame. A flat shrinking board tends to produce safe, defensive play:

- move toward center
- avoid other players
- wait for the board to collapse

That creates survival, but not enough player-generated incidents. ChaosDraftParty needs the opposite: players should want safety, but the route to safety should force contact, pressure, readable risk, and funny failure.

## Design Target

Core sentence:

> Run to safety, but the safe route makes you collide with other players.

The target aesthetics are:

- Challenge: player feels "I could have survived if I moved better."
- Fellowship / rivalry: players shout, blame, laugh, and remember who pushed whom.
- Sensation: the board visibly collapses and the final arena feels tense.
- Comedy: failures should chain in ways that are surprising but understandable.

## Research Grounding

Reference direction:

- Fall Guys works because its rules are simple, visually readable, watchable, and built for unexpected player collisions.
- Fall Guys designers describe a useful survival-course ratio as roughly 50% chaos / 50% skill.
- Overcooked improved when level disruptions forced players to rethink roles mid-round instead of repeating a stable plan.
- Self-Determination Theory maps sustained game appeal to competence, autonomy, and relatedness.
- MDA says "fun" must be decomposed into target player experiences, then implemented through mechanics and dynamics.

## New Blockout Rules

### 1. No Flat Full Board

The board must not read as a continuous square. It should read as:

- four corner islands
- narrow bridges
- a small final center arena

### 2. Center Is Not Free Camping

The final center can remain after shrink, but it must be small enough that two or more players cannot ignore each other.

### 3. Chokes Create Incidents

Bridge entries should be narrow. Two players crossing at the same time should create:

- body blocking
- dash timing choices
- pulse/push opportunities
- panic jump decisions

### 4. Meaningful Choices Beat Many Choices

Each route should communicate a simple tradeoff:

- wide island = safer movement, but disappears earlier
- narrow bridge = fastest route, but collision risk
- final center = survival, but high player pressure

### 5. Failure Must Be Readable

Warning, edge readability, and tile color zones matter. The player should know whether they fell because:

- they were late
- they chose the risky route
- another player pushed them
- they mistimed jump/dash

## Implemented Blockout Direction

Use the existing 8x8 tiles, but reposition/scale them into:

- `ShrinkingIsland_Stage`: shrinking tiles only
- `FinalArena_Static`: 2x2 center tiles, not managed by shrink
- `InteractionBlockers`: low half-wall bumpers around center/bridge entries

Expected dynamics:

| Layout Element | Intended Interaction |
|---|---|
| Four corner islands | early exploration, safe movement, player separation |
| Narrow bridges | forced crossings, dash timing, body blocking |
| Static final 2x2 | terminal confrontation, no "all tiles gone" randomness |
| Low half walls | push angles, route obstruction, jump/dash expression |
| Zone colors | immediate readability from camera distance |
| Cooldown recovery launchers | late recovery back toward center, with enough reuse to feel intentional |

## 2026-05-25 Implementation Update

The bounce pads should not be generic trampolines. In this stage they are recovery launchers:

- they add upward speed so the player feels rescued
- they add planar velocity toward the center arena so the next objective is obvious
- they use a short cooldown in the current prototype so they read as an intentional gimmick, not as a broken one-shot object
- they reset on `ResetIsland()` so repeated rounds remain valid

This keeps the pad aligned with the core sentence: run to safety, but the route back to safety creates contact and panic.

A temporary prototype HUD was added for Shrinking Island scenes so first-time playtesters immediately see the objective:

- get to the center
- corners fall first
- bridges are risky
- launch pads are recovery tools

## Meshy Pass Decision

The first Meshy pass is rejected for in-scene use. The generated pad/sign/bumper assets were too noisy and did not communicate gameplay function at camera distance.

Current rule:

- use clean primitives for gameplay readability first
- only replace primitives with Meshy/Blender assets after the silhouette, scale, and player-read are proven
- generated assets must be accepted only if they are clearer than the blockout from the default gameplay camera

## Visual Implementation Notes

2026-05-25 current accepted direction:

- closed box mesh for floors and bumpers, after a rushed beveled mesh made side faces look transparent from gameplay camera distance
- top plates and trim as separate safe detail pieces instead of destructive tile bevels
- custom gradient skybox/post/fog replacing the default Unity sky feel
- authored launcher language: cyan recovery disks, white chevrons, dark bases, and center-facing direction
- center marker is a small low signal, not a large object that blocks the camera
- center marker and ready launchers use subtle pulse motion so they read as active stage rules instead of static decoration
- warning/fade colors now apply to all tile child renderers, including top plates, not only the parent cube

Technical lesson:

- `Tile` must recreate `MaterialPropertyBlock` when needed. Unity play/reload paths can leave runtime-only renderer state invalid, and warning/reset visuals must not depend on a stale block.
- Sandbox single-player ShrinkingIsland play needs a fall respawn fallback. NGO KO does not run when `IsSpawned == false`, and letting the camera follow a falling player into empty sky destroys playtest readability.

2026-05-25 material pass:

- Replaced flat URP/Lit ShrinkingIsland materials with `ChaosDraftParty/ShrinkingIslandSurface`.
- Added subtle world-space surface variation, face-aware side shading, edge darkening, and soft rim response.
- Added `SI_TopChamferBox` and assigned it to tile bodies, top plates, and wall/blocker pieces to reduce raw cube silhouettes.
- Retuned bright top plates away from pure white to reduce "default Lit plane" feel.
- Added tile fade motion: falling tiles sink and tilt during fade instead of only changing color and disabling.

2026-05-25 authored-surface pass:

- Rejected oversized center-route cyan bars after gameplay-camera screenshot review.
- Replaced them with shorter, muted embedded dash groups so the marks read as floor language instead of UI.
- Added small inset/groove pieces to managed tiles under `Visual_SurfaceInsets`; these are children of each tile so they participate in warning/fade/gone state.
- Retuned skybox, ambient trilight, fog, directional light, and post-process values to lower white clipping and move the scene away from a default Unity render.
- Changed tile warning/fade color from pure red to warm hazard orange.

2026-05-25 warning/readability pass:

- Added `SI_TileWarningVfx.prefab` and assigned it to all 64 managed tiles through the existing `Tile` warning prefab hook.
- Added `SI_TileFadeBurstVfx.prefab` for collapse feedback and removed the particle velocity setting that caused Unity warnings.
- Fixed repeated sandbox Play/reset flows by resetting `ShrinkingIslandManager` runtime state and invalidating stale shrink coroutines with a round version.
- Reduced structural material side noise after low-angle screenshots showed the side faces becoming too tooth-like.

2026-05-25 objective/camera/Meshy pass:

- Retuned the default Cinemachine orbital yaw so sandbox Play frames the center objective in front of the player instead of showing only nearby tiles.
- Generated a Meshy preview for a rounded goal-beacon prop and imported the OBJ under `Assets/_Project/ThirdParty/Meshy/Generated/CenterBeacon/`.
- Placed `Meshy_CenterGoalBeacon` in the final arena with the existing mint stage material, then removed the temporary procedural center-ring stack.
- Kept the Meshy asset in preview/untextured form for now because local stage materials give better art-direction control than an unreviewed refine texture.
- Added bounce-pad launch feedback: visual punch scale, softened launch sparks, and a Cinemachine impulse hook.
- Retuned `ShrinkingIslandConfig`: initial delay `5 -> 3`, shrink interval `10 -> 8`, warning `1 -> 1.15`, fade `0.5 -> 0.55`.
- Verified a Play-mode screenshot around 4 seconds in: first outer-ring warning is visible, so the stage no longer feels static at first contact.
- Tried `SI_RoundedTilePrism.asset`, but rejected it after user inspection found side-face problems again.
- Reverted all scene uses of `SI_RoundedTilePrism` to `SI_ClosedBox`; current accepted count is `SI_RoundedTilePrism=0`, `SI_TopChamferBox=0`, `SI_ClosedBox=192`.
- Changed tile warning color application from full orange replacement to restrained base-color tinting (`_warningTintStrength = 0.48`).
- Removed the dark backdrop shards because they reused the rejected rounded mesh and could create affordance confusion.
- Fixed falling limb stretch: foot ground clamp now skips while the player is airborne/falling below the clamp band, and sandbox respawn restores squash/dip visual offsets.

Technical lesson:

- Setting the Main Camera transform in edit mode is not enough. Cinemachine can overwrite it; the saved orbital axis values must be checked in Play with a camera screenshot.
- Meshy can help erase primitive Unity silhouettes, but generated assets still need the same camera-read test as handmade meshes.
- First visible stage motion should happen quickly enough for a cold playtest. A 5-second silent start plus 10-second interval made the prototype feel inert.
- Closed render meshes remain the only accepted gameplay surface mesh until a Blender/ProBuilder mesh survives user inspection.
- Danger feedback should preserve authored material identity. Full-tile orange is readable but drifts back toward prototype/debug visualization.
- Character part world clamps must be disabled while the root is falling. Otherwise feet/hands can stay near the old ground plane while the body drops.

## Playtest Questions

Use these instead of asking only "is it fun?":

1. Did you understand the objective within 10 seconds?
2. Did at least two "incident moments" happen in one round?
3. Did KO feel like your mistake or another player's good play?
4. Was center camping obviously dominant?
5. Did dash/jump/pulse each have a different useful situation?
6. Was it fun to watch even when not controlling the player?

## Next Implementation Priority

After this blockout, do not add decoration first. Add mechanics only if playtest shows the blockout supports contact:

1. real single-use rescue/bouncy tiles
2. pulse hit feedback and push tuning
3. tile warning SFX/VFX
4. score/KO ceremony
5. art pass
