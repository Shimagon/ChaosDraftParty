# Shrinking Island Spec Check - 2026-06-09

Purpose: compare the current V8.x Shrinking Island stage against the active design direction before continuing autonomous work.

Source of truth:

- `docs/decisions/2026-05-25_shrinking_island_fun_design.md`
- `docs/TODO.md`
- `docs/reports/2026-06-09_autonomous_log.md`

## Design Target

Core sentence:

> Run to safety, but the safe route makes you collide with other players.

Current read:

- The stage is no longer a flat 8x8 board.
- V8.1-V8.4 now read as corner starts, route gauntlets, bridge pressure, and a small center.
- V8.3 longer pacing is a deliberate prototype divergence from the old fast shrink values because the user reported that the stage ended too quickly and players could reach center too immediately.

## Spec Match

| Requirement | Current Status | Evidence / Gap |
|---|---|---|
| Four corner islands | Met | V8 route gauntlet and V8.3 spawn decks create clear separated starts. |
| Narrow bridges / chokes | Mostly met | L-gates, bridge pockets, posts, and center blockers force line choice. Needs real 2P body-block verification. |
| Center is not free camping | Partial | The final core is smaller and blocker-heavy, but real 2P pressure is unverified. |
| Meaningful route tradeoff | Partial | Outer route is safer and longer; bridge is faster and riskier. Need stronger far-route draw so players do not sprint straight inward every time. |
| Failure readability | Partial | Warning/fade/launcher language exists and V8.6 runtime checks passed. User video still needed for whether KO feels fair. |
| Skill use: dash / jump / pulse | Planned, not proven | V8.4 balconies prepare skill landing targets, but skill-specific route affordances are not yet tested. |
| Watchable multiplayer chaos | Partial | V8.5 screenshot proxies help reporting only. Real 2P/ParrelSync test is still missing. |
| Build Settings clarity | Met | Boot/MainMenu/Lobby/Match/Results/ShrinkingIslandProto remain enabled; old sandbox scenes disabled only, not deleted. |

## Timing Decision

Older specs mention `initialDelay=5s`, `shrinkInterval=10s`, `warning=1s`, `fade=0.5s`.

Current V8.3 scene values:

- initial delay: 16s
- shrink interval: 20s
- warning duration: 2.2s
- fade duration: 0.85s

This is intentionally slower for the current one-player/screenshot prototype. The old values made the scene feel like a short tech demo and amplified the user's complaint that the round ended before the stage could breathe. Rebalance should happen after a real two-client run, not from single-player feel alone.

## Next Build Priority

The next stage pass should not be decoration-first. It should address the biggest remaining design gap:

1. Make the outer route feel like a deliberate long lane, not just empty distance.
2. Add readable obstacle clusters that create attack, dodge, and pulse opportunities before the center.
3. Keep center accessible but not optimal as a straight-line sprint.
4. Preserve safety: no rejected rounded/chamfer gameplay meshes, no active presentation proxies, no script edits.

## Verification Needed After Next Pass

- Scene validation: 0 issues.
- Console errors/warnings: 0.
- Rejected mesh references: 0.
- Play 5s: P1 grounded on stable spawn deck.
- Play 20s+: first warning/collapse does not strand the player.
- Multi-angle screenshots: one overview, one gameplay camera, one low-angle approach.

## V8.9 Metric Check

Direct center-rush check:

| Start | Direct lanes blocked | Direct distance |
|---|---:|---:|
| SW | 5 / 5 | 24.1m |
| SE | 5 / 5 | 24.1m |
| NW | 5 / 5 | 24.1m |
| NE | 5 / 5 | 24.1m |

Interpretation:

- The current issue is no longer that the center is physically reachable on a clean straight line.
- All four starts now have direct center lanes interrupted by a mix of outer slalom blocks, bridge cover, mid-gates, bump posts, and final-throat blockers.
- The remaining unknown is whether these interruptions create fun 2P incidents or only visual density. That requires ParrelSync / MPPM playtest or user video.

Scene scale:

- Active renderer bounds: about `62.76m x 62.76m`.
- Vertical silhouette range: about `8.47m`.
- V8.7 route pressure pass: `56` renderers, `24` colliders.
- V8.8 split balcony pass: `28` renderers, `20` colliders.

Latest screenshots:

- Gameplay camera: `Assets/Screenshots/v8_9_center_shadow_lift_play_19s.png`
- Overview: `Assets/Screenshots/v8_9_overview_route_pressure.png`

## V8.10 Critic Follow-Up

External critic feedback:

- The stage is technically wider, but the gameplay camera still made center rush look too inviting.
- Mid-route 2P incidents were still unproven; many conflicts might happen only at final throat.
- Visual density was borderline, especially repeated cyan strips and rectangular support noise.

Implemented response:

- Added `V8_10_ReadableSwitchbackAndIncidentPass`.
- One diagonal switchback gate per corner approach now breaks the visually direct line before center.
- Each approach now has a mid-route incident pocket with staggered cheek blockers and a pulse/body-bump post.
- Retinted 12 small cyan strips to mint so cyan reads more like hinge/objective language.
- Adjusted `CM_Player3rdPerson` OrbitalFollow radius from `11.2` to `13.2` to show spawn deck, risky bend, and center objective in one frame.

V8.10 verification:

- Direct center-rush lanes remain `5 / 5` blocked from SW / SE / NW / NE.
- V8.10 blockers near spawn: `0`.
- Console errors/warnings: `0`.
- EditMode tests: `9 / 9` passed.
- Play check around 20s: P1 grounded on `V8_3_SpawnDeck_Floor_-1_-1`.

Latest V8.10 screenshots:

- Gameplay camera: `Assets/Screenshots/v8_10_camera_radius_13_2_play_20s.png`
- Overview: `Assets/Screenshots/v8_10_overview_switchback_incidents.png`
