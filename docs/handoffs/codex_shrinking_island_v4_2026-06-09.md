# Codex Handoff: Shrinking Island V8 Ring Gauntlet

Date: 2026-06-09
Scene: `Assets/_Project/Scenes/ShrinkingIslandProto.unity`

## V8 Update

- Moved the work from sandbox-only into the build-listed proto scene: `Assets/_Project/Scenes/ShrinkingIslandProto.unity`.
- Reworked the stage from a broad square/S-route read into a ring gauntlet:
  - four outer corner islands for player starts
  - visible bridge entries into the central ring
  - middle and inner blockers that stop direct center rushing
  - raised final 2x2 core with tight throat blockers
- Kept the managed shrink contract unchanged:
  - `Tile` count: `64`
  - layer counts: `28 / 20 / 12 / 4`
  - all checked shrink phases stay connected as one component after `Physics.SyncTransforms()`
- Added `V8_RingGauntlet` scene root:
  - `V8_RingGauntlet_Blockers`: active gameplay blockers/posts
  - `V8_RingGauntlet_Readability`: cyan gate beacons/fins
  - `V8_CornerIsland_Rails`: outer island guard rails
  - `V8_CornerIsland_BridgeReadability`: bridge direction strips
- Disabled old V6/V7 readability roots in the proto scene so their old route signs do not conflict with V8.
- Added `Playtest_EggybotPlayer` from `Assets/_Project/Prefabs/Player/EggybotPlayer.prefab` at the SW corner island for local video capture.
- Retuned `CM_Player3rdPerson` initial orbit:
  - radius: `9.5`
  - horizontal axis: `0`
  - vertical axis: `24`
  - Play screenshot now frames the player and the route toward the center.
- Recovery pads now target side/middle approach points, not the final center.

## V8 Verification

- Loaded scenes: only `ShrinkingIslandProto`.
- Active scene: `ShrinkingIslandProto`, dirty: `false`.
- Scene validate: issue `0`.
- Console Error/Warning after Play check: `0`.
- EditMode tests: `9 / 9` passed.
- Rejected mesh guard: rounded/chamfer/top-chamfer refs `0`.
- Active old V6/V7 collider count: `0`.
- V8 active blocker colliders: `34`.
- Connectivity guard:
  - before shrink: `64 floors / 1 component`
  - after layer 0 removed: `36 floors / 1 component`
  - after layer 1 removed: `16 floors / 1 component`
  - after layer 2 removed: `4 floors / 1 component`
- Play camera screenshot verified:
  - `Assets/Screenshots/screenshot-20260609-193108.png`

## V8 Screenshot Candidates

- `Assets/Screenshots/screenshot-20260609-192441.png` - overview from gameplay camera after corner-island reshape
- `Assets/Screenshots/screenshot-20260609-192441_1.png` - topdown V8 layout
- `Assets/Screenshots/screenshot-20260609-193108.png` - Play camera with `Playtest_EggybotPlayer`

## V7 History

## What Changed

- Rebuilt the Shrinking Island stage into V7 around four spawn islands, S-shaped routes, gate pressure, and a raised final dais.
- Expanded the readable footprint with distant non-walkable terraces so the stage feels larger without adding free safe ground.
- Moved the player/camera start to the SW inner spawn pad: spawn pads sit on layer 1 safe pads, not layer 0, so the first collapse does not immediately delete the spawn floor.
- Authored the 64 managed `Tile_X*_Z*` objects as route phases:
  - layer 0: outer visual / early-loss rim
  - layer 1: spawn islands and first S-route safety
  - layer 2: narrow high approaches / gate pressure
  - layer 3: final entry bridge tiles
- Wrote the authored shrink order into each `Tile._gridX/_gridZ`, so shrink no longer depends on the visual `Tile_X*_Z*` name layout.
- Kept the final dais as the only static survival floor. The final four managed tiles were moved to entry bridge positions so their fade does not overlap the static core.
- Wired all six recovery pads with real `BouncePad` components, trigger colliders, kinematic rigidbodies, and launch targets at gate/choke positions.
- Retuned sandbox spawn/respawn so direct Play starts on the SW layer 1 spawn island, not center.
- Retuned camera radius for this scene: `cameraBaseRadius=7.5`, `cameraDashRadius=8.5`, FOV about `60`.
- Set shrink timing to `initialDelay=6`, `shrinkInterval=14`, `warning=1.2`, `fade=0.65`.
- Added V6/V7 visual pass:
  - layer-colored tile materials: outer warm, route mint, approach ash, final orange
  - darker underside/shadow plinths under all 64 tiles
  - low route blockers and side rails to force weaving and 2P contact
  - center gate beams, cyan signs, and final-core ring/readability markers
  - darker lighting/fog to reduce the washed-out Unity primitive look
- Added V7 readability pass:
  - non-collider embedded cyan dash groups on the S-routes
  - extra underside supports so bridges read as elevated, not paper-thin
  - gate glow strips and side fins at the final throat
  - deeper non-collider plinths below the four spawn islands
- Added V7 recovery readability pass:
  - non-collider cyan receiver marks at launch targets
  - short non-collider direction dash groups near outer recovery pads
  - recovery pad targets stay near gate/approach positions, not free center
- Moved V7 stage materials from plain `Universal Render Pipeline/Lit` to the existing `ChaosDraftParty/ShrinkingIslandSurface` shader.
  - This adds authored surface variation and edge/side darkening instead of flat single-color plates.
  - Exception: `SI_V6_signal_cyan` stays on URP/Lit because `ShrinkingIslandSurface` has no emission property and cyan signals need glow/readability.

## Verification

- Scene saved and not dirty after save.
- Console after Play checks: 0 error/warning.
- EditMode tests: `9 / 9` passed.
- PlayMode test runner: passed with `0` real tests discovered.
- Authored shrink grid:
  - tile count: `64`
  - unique authored grid coords: `64`
  - layer counts: `28 / 20 / 12 / 4`
- Recovery launchers:
  - `BouncePad` count: `6`
  - launch target assigned: `6`
  - trigger collider: `6`
  - Rigidbody: `6`
  - launch targets are about `7.0m` to `8.5m` from center, so recovery returns players into routes/gates rather than free center camping
- Player movement VFX:
  - `EggybotMovementVFX` on scene `EggybotPlayer`
  - `LandingDust`, `Footstep_L`, `Footstep_R`, and `DashTrail` assigned
- Mesh guard:
  - rejected rounded/chamfer/top-chamfer refs: `0`
- Static art root collider guard:
  - `ShrinkingIsland_AuthoredStageV4_20260609` enabled colliders: `0`
- Connectivity guard after V7 S-route rewrite:
  - before shrink: `65 floors / 1 component`
  - after removing layer 0: `37 floors / 1 component`
  - after removing layer 1: `17 floors / 1 component`
  - after removing layer 2: `5 floors / 1 component`
  - all four spawn pads still reach the final core in each checked phase
- Collider audit:
  - after moving route floors, `Physics.SyncTransforms()` is required before reading `Collider.bounds`
  - synced Bounds guard passes with `1 component` in all checked shrink phases
- Spawn safety:
  - `SpawnPad_P1/P2/P3/P4` nearest floor after layer 0 removal is layer 1, distance about `0.6m`
- Current path length:
  - V5 shortest path was about `34.9m / 9 floor steps`
  - V7 final S-route shortest path is about `42.2m / 9 floor steps`
- 2P contact pressure:
  - south final entry pair (`Tile_X3_Z3` / `Tile_X4_Z3`) center distance: `1.10m`, Bounds touch
  - north final entry pair (`Tile_X3_Z4` / `Tile_X4_Z4`) center distance: `1.10m`, Bounds touch
  - approach lanes stay separated, then converge at the final throat
- Play checks:
  - Play screenshots show player/camera start on the SW route with no camera blanking.
  - MCP `execute_code` time sampling advances too much to trust exact second labels; use screenshots + Console first for visual QA.
  - Console stayed clean during Play checks.
- Sub-agent critique:
  - `unity-critic` asked for direct-center weakening, collision gates, clearer height tiers, non-white floor roles, and gameplay-camera landmarks.
  - V7 S-route + readability pass was made against those five points.

## Screenshots

- `Assets/Screenshots/codex_si_v7_readability_topdown_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_readability_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_final_reconnected_topdown_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_final_reconnected_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_surface_shader_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_embedded_marks_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_recovery_marks_topdown_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_recovery_marks_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_sroute_topdown_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_darkpalette_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_play_immediate_2026-06-09.png`
- `Assets/Screenshots/codex_si_v7_sroute_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v5_final_gamecam_2026-06-09.png`
- `Assets/Screenshots/codex_si_v5_final_topdown_2026-06-09.png`
- `Assets/Screenshots/codex_si_v5_play_6p8s_warning_2026-06-09.png`
- `Assets/Screenshots/codex_si_v5_play_9s_respawnfixed_2026-06-09.png`

## Report / PPT Notes

- `docs/reports/2026-06-09.md`
- `docs/reports/2026-06-09_slide_update_notes.md`

## Next Playtest Questions

- Does the first route read immediately from the gameplay camera?
- Does the 6 second first collapse feel urgent without being unfair?
- In 2P, do players meet at gates or still pass through separate lanes without contact?
- Are the high shortcut branches now narrow enough to create push/attack risk?
- Do recovery pads throw players into conflict instead of granting free center safety?
- Is the final dais small enough for 2-4 players to collide instead of orbiting safely?
- Does the stronger mint/orange palette help or is it too saturated in motion?

## Not Done

- No git commit was made.
- No ParrelSync 2-client test was run.
- No user play-feel judgment yet.
- No code files were intentionally edited in this V7 pass; this was scene/material/config/screenshot/handoff work.
## V8.5 Update - presentation player proxies

Time: 2026-06-09 20:22 JST

What changed:

- Added `V8_5_PresentationPlayerProxies`.
  - Three static visual-only Eggybot proxies were cloned from `Playtest_EggybotPlayer`.
  - They are placed on another spawn deck, an approach pocket, and a far route.
  - Their MonoBehaviours and Colliders are disabled, so they do not affect Play mode, networking, collision, or tests.
  - Purpose: screenshots now read as a 2-4 player party game instead of a single-character stage screenshot.

V8.5 verification:

- Scene validate: `0` issues.
- Console error/warning: `0`.
- Play check after 5 seconds:
  - P1 `CharacterController.isGrounded=True`.
  - proxy enabled behaviours: `0`
  - proxy enabled colliders: `0`
- Screenshot:
  - `Assets/Screenshots/v8_5_gameplay_player_proxies_visible.png`
  - `Assets/Screenshots/v8_5_playmode_proxy_check.png`

Presentation note:

- Use the V8.5 gameplay screenshot when explaining "2P Playtest" or "other players collide at the bridge / L-gate."
- `V8_5_PresentationPlayerProxies` is now inactive by default because `ShrinkingIslandProto.unity` is enabled in Build Settings.
- If a screenshot with proxies is needed, temporarily activate the root, capture, then set it inactive again.
- Safety contract after review:
  - proxy root inactive by default
  - proxy Player tags: `0`
  - enabled proxy behaviours: `0 / 57`
  - enabled proxy colliders: `0 / 9`
  - enabled proxy animators: `0 / 3`
  - enabled proxy network behaviours: `0 / 9`
- Screenshot with temporary activation:
  - `Assets/Screenshots/v8_5_presentation_proxies_temporary_active.png`
- Default safe screenshot:
  - `Assets/Screenshots/v8_5_safety_default_no_proxies.png`

## V8.4 Update - skill-height silhouettes

Time: 2026-06-09 20:10 JST

What changed:

- Added `V8_4_SkillHeightAndSilhouettePass`.
  - Four elevated skill balconies were added around the central approaches.
  - These read as future dash / jump / pulse landing targets for M3.
  - They add vertical silhouette and route landmarks without opening a free center rush.
  - Each balcony has underside mass, graphite lips, support posts, and a small orange inset.
- Added non-collider top bands around the final core to reduce the single-block look.
- Lowered the two closest SW corner posts slightly so the gameplay camera keeps a cleaner view of the route.

V8.4 verification:

- Scene validate: `0` issues.
- Console error/warning: `0`.
- Play check after 5 seconds:
  - `CharacterController.isGrounded=True`.
  - Foot ray still hits `V8_3_SpawnDeck_Floor_-1_-1`.
- New screenshots:
  - `Assets/Screenshots/v8_4_gameplay_skill_height.png`
  - `Assets/Screenshots/v8_4_overview_skill_height.png`
  - `Assets/Screenshots/v8_4_playmode_grounded.png`

Design rationale:

- The stage should not solve "wide" only by adding flat floor.
- V8.4 uses vertical landmarks and future skill landing pads to make the arena feel larger while preserving central contact pressure.
- This supports the next M3 story: selected skills create alternate timing and landing options inside the same shrinking-island route.

## V8.3 Update - stable outer spawn decks and longer round pacing

Time: 2026-06-09 20:03 JST

What changed after the V8 pass:

- Added `V8_2_LaneFunnelAndDepthPass`.
  - Four diagonal L-gates were added near the central ring entrances.
  - Bridge contact pockets now have one low cover and one vertical post, so two players are pushed into a readable conflict line instead of running straight to center.
  - Central low pocket blockers break trivial circular orbiting around the final core.
  - Large underside shadow decks and far non-collider plates were added to make the stage read as layered and broad, not as a flat Unity board.
- Tuned V8.1 visual markers.
  - Oversized cyan caps and chevrons were reduced and moved toward material-like inset markers.
  - Far ghost islands now use `SI_V6_backdrop_distant_plate` instead of bright white.
- Added `V8_3_StableSpawnDecks`.
  - Four stable outer spawn decks were added at the corners.
  - These are under `V8_RingGauntlet`, outside the shrinking manager's managed tile lifecycle, so the playtest player no longer falls when runtime tile state changes.
  - Each deck has a warm floor, graphite underside/rails, and a small route inset.
- Retuned round pacing on `ShrinkingIslandManager`.
  - `_initialDelay`: `6.0 -> 16.0`
  - `_shrinkInterval`: `14.0 -> 20.0`
  - `_warningDuration`: `1.2 -> 2.2`
  - `_fadeDuration`: `0.65 -> 0.85`
  - `_rowStaggerDelay`: `0.08 -> 0.10`
  - Reason: user feedback was that rounds ended too quickly and players reached center too easily. The new pacing gives time for outer-island contact before the first collapse.

Verification added in V8.3:

- Scene validate: `0` issues.
- Console error/warning: `0`.
- EditMode tests: `9 / 9` passed.
- Play check after 5 seconds:
  - `Playtest_EggybotPlayer` stayed grounded.
  - `CharacterController.isGrounded=True`.
  - Foot ray hit `V8_3_SpawnDeck_Floor_-1_-1`.
- Mesh guard:
  - rejected `SI_TopChamferBox` / rounded / chamfer mesh refs: `0`.

New screenshot candidates:

- `Assets/Screenshots/v8_3_playmode_grounded_spawn_deck.png`
- `Assets/Screenshots/v8_3_overview_stable_spawn_decks.png`
- `Assets/Screenshots/v8_2_gameplay_lgate.png`
- `Assets/Screenshots/v8_2_overview_lgate.png`

Recommended presentation wording:

> V8.3 changed Shrinking Island from a board where players can rush center into a four-corner route gauntlet. Players now start on stable outer decks, pass through bridge pockets and L-gates, then collide near the central ring before the final 2x2 core.

Open risks:

- No ParrelSync / two-client playtest was run in this loop.
- Gameplay feel still needs user playtest judgment.
- Runtime shrinking now starts later for playability; this should be checked against the intended match length once M3 skills are connected.

## V8.7-V8.9 Update - route pressure and visual cleanup

Time: 2026-06-09 21:05 JST

Why:

- User feedback: the stage still felt narrow, players could get to center too quickly, and the scene still had too much "Unity block" feel.
- Spec check confirmed that the biggest remaining design gap is proving route pressure, not adding decoration first.

Added:

- `docs/reports/2026-06-09_shrinking_island_spec_check.md`
- `V8_7_FarRoutePressurePass`
  - outer slalom blocks on all four approaches
  - route bend markers
  - center pressure nubs
- `V8_8_SplitSkillBalconySightlinePass`
  - split V8.4 high skill balconies into smaller perches
  - kept height/skill affordance while opening the center sightline
- `Assets/_Project/Materials/ShrinkingIsland/SI_V8_lifted_center_shadow.mat`
  - lifted center shadow color for selected old V4/V5 dark parts

Verification:

- Console error/warning: `0`
- Play check around 19s:
  - P1 grounded: `True`
  - foot ray: `V8_3_SpawnDeck_Floor_-1_-1`
- EditMode tests: `9 / 9` passed
- Rejected rounded/chamfer gameplay mesh refs: `0`
- `V8_5_PresentationPlayerProxies` remained inactive by default.

Latest screenshots:

- `Assets/Screenshots/v8_7_cleanup_play_17s_far_route_pressure.png`
- `Assets/Screenshots/v8_8_split_balcony_play_24s.png`
- `Assets/Screenshots/v8_9_center_shadow_lift_play_19s.png`

Remaining risks:

- Visual density is now higher. Real user camera/video review is still required.
- No ParrelSync / two-client real collision test yet.
- The stage prepares dash / jump / pulse spaces, but those skill interactions are not connected to a full M3 loop yet.

## V8.10 Update - readable switchbacks and incident pockets

Time: 2026-06-09 21:25 JST

Why:

- A pro/critic review of V8.7-V8.9 said the stage was physically wider, but the default gameplay camera still made the center line look too tempting.
- The main design risk had shifted from "center is reachable" to "the route pressure may read as decoration instead of a rule."

Changed:

- Added `V8_10_ReadableSwitchbackAndIncidentPass`.
  - One diagonal switchback gate was added on each corner approach to visibly break the straight center line.
  - Mid-route incident pockets were added before the final ring: staggered cheek blockers plus a bump/pulse post per approach.
  - Small cyan detail strips from V8.7/V8.8 were retinted to mint so cyan is reserved for high-value route/objective signals.
- Adjusted `CM_Player3rdPerson` `CinemachineOrbitalFollow.Radius` from `11.2` to `13.2`.
  - This is a framing change, not a speed-feel change.
  - The first gameplay frame now shows the spawn deck, risky bend, and center objective together.

Verification:

- Console error/warning: `0`
- EditMode tests: `9 / 9` passed
- Play check around 20s:
  - P1 grounded: `True`
  - foot ray: `V8_3_SpawnDeck_Floor_-1_-1`
  - `V8_5_PresentationPlayerProxies` inactive
- Direct center-rush ray sampling:
  - SW / SE / NW / NE starts: `5 / 5` sampled lanes blocked on all four starts
- Rejected rounded/chamfer gameplay mesh refs: `0`

Latest screenshots:

- `Assets/Screenshots/v8_10_switchback_play_20s.png`
- `Assets/Screenshots/v8_10_camera_radius_13_2_play_20s.png`
- `Assets/Screenshots/v8_10_overview_switchback_incidents.png`

Latest deck:

- `docs/presentations/2026-06-09_progress_shimada_v8_10_autonomous.pptx`

Remaining risks:

- Visual density is still the biggest open art-direction risk. Do not add more decorative blockers unless a gameplay metric proves the need.
- The next meaningful proof is 2P / ParrelSync: confirm that dash, jump, and pulse create intentional accidents at the bridge pockets and V8.10 incident posts.
