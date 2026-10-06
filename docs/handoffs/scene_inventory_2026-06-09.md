# Scene Inventory - 2026-06-09

Purpose: reduce confusion about "which scene is the main Shrinking Island scene?" without deleting anything.

## Current Active Scene

- `Assets/_Project/Scenes/ShrinkingIslandProto.unity`
- Build Settings index: `5`
- Loaded scene count: `1`
- Active scene root count at check time: `24`

This is the scene used for the V8.5 Shrinking Island work.

## Build Settings Scenes

Keep as product flow:

- `Assets/_Project/Scenes/Boot.unity` - index 0
- `Assets/_Project/Scenes/MainMenu.unity` - index 1
- `Assets/_Project/Scenes/Lobby.unity` - index 2
- `Assets/_Project/Scenes/Match.unity` - index 3
- `Assets/_Project/Scenes/Results.unity` - index 4

Keep as current prototype / progress-report scene:

- `Assets/_Project/Scenes/ShrinkingIslandProto.unity` - index 5

Disabled in Build Settings on 2026-06-09 20:46 JST:

- `Assets/_Project/Scenes/Sandbox_v19_player_controller_test.unity` - index 6
- `Assets/_Project/Scenes/Sandbox_v21_crouch_collider_test.unity` - index 7

These scene files were not deleted. They are still available in `Assets/_Project/Scenes/`, but they no longer ship through the enabled Build Settings path.

## Other Scene Assets Found

- `Assets/_Project/Scenes/CharacterLab.unity`
- `Assets/_Project/Scenes/Sandbox_ShrinkingIsland_v1.unity`
- `Assets/_Project/Scenes/Sandbox_v16_humanoid.unity`
- `Assets/_Project/Scenes/Sandbox_v18_crouch_sequence.unity`
- `Assets/_Project/Scenes/Sandbox_v20_run_tuning.unity`

Do not delete automatically. Recommended handling:

- Keep `CharacterLab.unity` as an art/character inspection lab if still useful.
- Keep `Sandbox_ShrinkingIsland_v1.unity` only as historical backup until V8.5 is accepted.
- Keep `Sandbox_v16/v18/v19/v20/v21` only if they still serve specific controller/crouch/run tests.
- Once the user confirms, remove old sandbox scenes from Build Settings first, then archive/delete scene files in a separate cleanup pass.

## Current Recommendation

For today's progress report and recording:

1. Open `Assets/_Project/Scenes/ShrinkingIslandProto.unity`.
2. Use latest deck: `docs/presentations/2026-06-09_progress_shimada_v8_5.pptx`.
3. Use latest gameplay screenshot: `Assets/Screenshots/v8_5_gameplay_player_proxies_visible.png`.

No scene deletion was performed.
