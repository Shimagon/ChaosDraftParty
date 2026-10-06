# Autonomous Work Log - 2026-06-09

Started from user request: continue autonomously until 24:00 JST and improve Shrinking Island quality.

## Latest State

- Active Unity scene: `Assets/_Project/Scenes/ShrinkingIslandProto.unity`
- Latest deck: `docs/presentations/2026-06-09_progress_shimada_v8_5_safe.pptx`
- Latest presentation screenshot with temporary proxies: `Assets/Screenshots/v8_5_presentation_proxies_temporary_active.png`
- Latest default-safe screenshot: `Assets/Screenshots/v8_5_safety_default_no_proxies.png`
- Scene inventory: `docs/handoffs/scene_inventory_2026-06-09.md`

## V8.3

Goal: fix the player falling in Play mode and make the stage less rushed.

Changes:

- Added `V8_3_StableSpawnDecks`.
- Added 4 stable corner spawn decks outside the shrinking manager lifecycle.
- Retuned `ShrinkingIslandManager` pacing:
  - `_initialDelay`: `6 -> 16`
  - `_shrinkInterval`: `14 -> 20`
  - `_warningDuration`: `1.2 -> 2.2`
  - `_fadeDuration`: `0.65 -> 0.85`

Verification:

- Play 5 seconds: P1 grounded on `V8_3_SpawnDeck_Floor_-1_-1`.
- Console: 0 errors/warnings.

## V8.4

Goal: reduce flat-board feel and create M3 skill landing targets.

Changes:

- Added `V8_4_SkillHeightAndSilhouettePass`.
- Added 4 elevated skill balconies around central approaches.
- Added final-core visual top bands.
- Lowered two closest SW posts for cleaner gameplay camera framing.

Verification:

- Scene validate: 0 issues.
- Console: 0 errors/warnings.
- Play 5 seconds: P1 grounded.
- EditMode tests: 9/9 passed.

## V8.5

Goal: make screenshots read as a 2-4 player party game, not a solo stage shot.

Changes:

- Added `V8_5_PresentationPlayerProxies`.
- Cloned 3 visual-only Eggybot proxies.
- Disabled all proxy behaviours and colliders.

Verification:

- Proxy enabled behaviours: 0
- Proxy enabled colliders: 0
- P1 grounded after 5 seconds.
- Console: 0 errors/warnings.
- EditMode tests: 9/9 passed.
- Rejected rounded/chamfer mesh references: 0.

Safety review follow-up:

- `ShrinkingIslandProto.unity` is enabled in Build Settings, so presentation proxies must not be active by default.
- Fixed after review:
  - `V8_5_PresentationPlayerProxies.activeSelf=False`
  - proxy Player tags: 0
  - enabled proxy behaviours: 0/57
  - enabled proxy colliders: 0/9
  - enabled proxy animators: 0/3
  - enabled proxy network behaviours: 0/9
- For screenshots, temporarily activate the proxy root, capture, then set it inactive again.

Review status:

- `unity-reviewer` flagged active build-scene presentation proxies, Player tags, and enabled Animators.
- Fixed in scene.
- Re-verified:
  - Scene validate: 0
  - Console: 0
  - Play 5 seconds: P1 grounded, proxy root inactive
  - EditMode tests: 9/9 passed

## V8.6 Runtime Timing Check

Goal: verify that the longer shrink pacing works beyond the first few seconds of Play.

Runtime check:

- Entered Play mode with proxy root inactive.
- Waited to about 18 seconds after Play start.
  - P1 position: `(-16.00, 0.39, -18.00)`
  - P1 grounded: `True`
  - proxy root inactive: `True`
  - console errors/warnings: `0`
  - screenshot: `Assets/Screenshots/v8_6_play_18s_warning_window.png`
- Waited to about 24 seconds after Play start, after the first shrink window should have completed.
  - P1 position: `(-16.00, 0.39, -18.00)`
  - P1 grounded: `True`
  - foot ray: `V8_3_SpawnDeck_Floor_-1_-1`
  - console errors/warnings: `0`
  - screenshot: `Assets/Screenshots/v8_6_play_24s_after_first_collapse.png`
- Exited Play mode after verification.

Conclusion:

- The V8.3 stable spawn deck survives the first shrink window and keeps the test player grounded.
- The later first shrink timing did not produce console errors.

## Build Settings Cleanup

Goal: reduce confusion from old sandbox scenes without deleting scene files.

Change:

- Disabled these scenes in Build Settings:
  - `Assets/_Project/Scenes/Sandbox_v19_player_controller_test.unity`
  - `Assets/_Project/Scenes/Sandbox_v21_crouch_collider_test.unity`

Verification:

- Build Settings now show both sandbox scenes as `enabled=false`.
- Active scene remains `Assets/_Project/Scenes/ShrinkingIslandProto.unity`.
- Console errors/warnings: `0`.

No scene files were deleted.

## Not Done

- No git commit.
- No scene deletion.
- No ParrelSync / 2-client real playtest.
- No final user play-feel judgment.
- Presentation render preview could not be visually rendered with artifact-tool because `@oai/artifact-tool/presentation-jsx` was unavailable; PPTX was checked by reopening with `python-pptx`.

## Next Useful Work

- Real 2P test with ParrelSync or two editor clients.
- Hide or remove `V8_5_PresentationPlayerProxies` before recording real gameplay if they confuse the take.
- Connect M3 `dash / jump / pulse` to the V8.4 skill balconies.
- Remove sandbox scenes from Build Settings only after user confirms.

## Spec Check

Goal: verify the current stage against the active Shrinking Island design document before continuing.

Added:

- `docs/reports/2026-06-09_shrinking_island_spec_check.md`

Conclusion:

- The stage now matches the four-corner / bridge / center-pressure direction.
- The remaining major gameplay gap is not "more decoration"; it is proving that the route creates real 2P incidents and that dash / jump / pulse each have a useful situation.
- The slower timing is a deliberate prototype divergence from old `5s / 10s` timing because the user reported that rounds ended too quickly and center rush was too easy.

## V8.7 Far Route Pressure Pass

Goal: make the outer route feel less like empty distance and reduce direct center rushing.

Changes:

- Added `V8_7_FarRoutePressurePass`.
- Added outer slalom blocks on all four spawn-to-center approaches.
- Added low route markings and non-colliding landmarks to make the route bend readable.
- Added center pressure nubs for future pulse/body-bump incidents.

Visual follow-up:

- The first V8.7 screenshot had too much black mass near center.
- The center continuous lips were removed and replaced with smaller split nubs.
- Tall outer fins were lowered and retinted to mint so they read as landmarks instead of blocking pillars.

Verification:

- Console: `0` errors/warnings.
- Play check around 20s: P1 grounded on `V8_3_SpawnDeck_Floor_-1_-1`.
- Screenshot:
  - `Assets/Screenshots/v8_7_play_20s_far_route_pressure.png`
  - `Assets/Screenshots/v8_7_cleanup_play_17s_far_route_pressure.png`

## V8.8 Split Skill Balcony Sightline Pass

Goal: keep the vertical skill affordance while opening the central sightline from the default gameplay camera.

Changes:

- Added `V8_8_SplitSkillBalconySightlinePass`.
- Split the V8.4 full-width skill balconies into smaller left/right or north/south perches.
- Left an open center gap on each approach so high platforms do not become one black slab in the camera.
- Kept non-colliding cyan sightline strips across the opened gaps.

Verification:

- Console: `0` errors/warnings.
- Play check around 24s: P1 grounded on `V8_3_SpawnDeck_Floor_-1_-1`.
- Screenshot:
  - `Assets/Screenshots/v8_8_split_balcony_play_24s.png`

## V8.9 Center Shadow Lift

Goal: reduce the "black Unity block" read around the final core without deleting the center pressure geometry.

Changes:

- Added material:
  - `Assets/_Project/Materials/ShrinkingIsland/SI_V8_lifted_center_shadow.mat`
- Retinted selected V4/V5/V8 center blockers and underside parts from near-black to a lifted shadow color.
- Shrunk/lowered the heaviest final-core underside pieces.
- Lowered south-throat blocker height so the camera reads route pressure, not a wall.

Verification:

- Console: `0` errors/warnings.
- Play check around 19s: P1 grounded on `V8_3_SpawnDeck_Floor_-1_-1`.
- EditMode tests: `9 / 9` passed.
- Screenshot:
  - `Assets/Screenshots/v8_9_center_shadow_lift_play_19s.png`

## V8.10 Readable Switchback / Incident Pass

Goal: answer the external critic's top concerns:

- From gameplay camera, center rush still looked too visually tempting.
- 2P incidents might happen only at the final throat instead of mid-route.
- Cyan/detail density was starting to read as visual noise.

Changes:

- Added `V8_10_ReadableSwitchbackAndIncidentPass`.
- Added one unmistakable diagonal switchback gate per corner approach.
  - Purpose: the first gameplay-camera read should say "straight is bad; bend around."
- Added mid-route incident pockets before the final ring.
  - Each approach now has staggered cheek blockers plus a pulse/body-bump post.
  - Purpose: two players can pass, but timing, bumping, dash, or pulse should matter.
- Retinted 12 small V8.7/V8.8 cyan strips to mint.
  - Cyan is now reserved more for hinge/objective readability.
- Adjusted `CM_Player3rdPerson` `CinemachineOrbitalFollow.Radius`:
  - `11.2 -> 13.2`
  - Purpose: show safe spawn deck, risky bend, and center objective in the same gameplay frame without camera shake or FOV punch.

Metric check:

- SW / SE / NW / NE direct-to-center lanes: `5 / 5` blocked from all four starts.
- V8.10 objects: `32` renderers, `20` colliders.
- V8.10 blockers near spawn: `0`.
- Rejected rounded/chamfer gameplay mesh refs: `0`.

Verification:

- Console error/warning: `0`.
- Play check around 20s:
  - P1 grounded: `True`
  - foot ray: `V8_3_SpawnDeck_Floor_-1_-1`
  - proxy root inactive: `True`
- EditMode tests: `9 / 9` passed.

Screenshots:

- Gameplay camera:
  - `Assets/Screenshots/v8_10_switchback_play_20s.png`
  - `Assets/Screenshots/v8_10_camera_radius_13_2_play_20s.png`
- Overview:
  - `Assets/Screenshots/v8_10_overview_switchback_incidents.png`
