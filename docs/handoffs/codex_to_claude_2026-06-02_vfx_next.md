# Codex -> Claude Handoff: Shrinking Island VFX Next

Date: 2026-06-02
Owner next: Claude
Status: Do not commit yet. Worktree is dirty.

## User Direction

The user wants to try Claude's new feature for a while.

Core visual rule remains non-negotiable:

> Unity感を消す.

Every Shrinking Island visual/VFX/camera/material decision should be checked against whether it still looks like default Unity or like an authored party-game world.

## Current State

- Shrinking Island is playable enough for visual/game-feel iteration.
- Stage side-face regressions were fixed by reverting gameplay surfaces to `SI_ClosedBox`.
- Do not use rejected stage meshes:
  - `SI_TopChamferBox`
  - `SI_RoundedTilePrism`
- Accepted scene expectation:
  - `SI_TopChamferBox = 0`
  - `SI_RoundedTilePrism = 0`
  - `SI_ClosedBox = 192`
- Falling limb stretch bug was fixed:
  - `EggybotFootGroundClamp` skips while airborne / below clamp band.
  - sandbox respawn restores foot/body squash and dip offsets.
- Sandbox camera was retuned so the center objective is visible at Play start.
- Meshy center goal beacon is imported under:
  - `Assets/_Project/ThirdParty/Meshy/Generated/CenterBeacon/`
- Warning/fade VFX exist:
  - `Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileWarningVfx.prefab`
  - `Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileFadeBurstVfx.prefab`
- Current tile warning is intentionally warm hazard tint, not debug red.

## Reference Repos Cloned By Codex

Local reference cache only. Do not import whole projects blindly.

Path:

```text
.codex/refs/vfx/
```

Contents:

- `VisualEffectGraph-Samples`
- `SdfVfxSamples`
- `Skinner`
- `Smrvfx`
- `SonarFx`
- `KinoFeedback2`
- `Kino`

Study memo:

```text
docs/research/vfx_reference_repo_study_2026-05-28.md
```

Important license note:

- Unity VFX samples: Unity Companion License.
- Keijiro Skinner/SonarFx: MIT.
- Keijiro Smrvfx/Kino/KinoFeedback2: Unlicense.
- z4gon pages were reference-only; git/codeload/API returned 404, so do not copy source from them.

## What Codex Learned

Professional VFX quality comes from layered timing, not from one prettier particle.

For each effect, separate:

1. anticipation
2. readable impact shape
3. ring / line / silhouette
4. smoke / dust / debris detail
5. glow / bloom / grade support
6. camera/audio/body response
7. fast decay

Default Unity smell appears when a single ParticleSystem is dropped onto a primitive with random bright color.

## Next Priority

### 1. Build `BouncePadImpact`

Goal:

Make the recovery pad feel like a designed launch mechanism, not a trampoline.

Layers to add:

- subtle idle pulse / emissive breathing
- 2-3 frame pad compression before launch
- radial ring flash on launch frame
- short cyan/mint streaks pointing toward the center objective
- small sparks/debris that fade quickly
- existing small Cinemachine impulse, no excessive camera shake

Implementation target:

- Existing script: `Assets/_Project/Scripts/Minigames/ShrinkingIsland/BouncePad.cs`
- Existing launch particle hook:
  - `_launchParticle`
  - `_launchParticlePrefab`
- Prefer a local prefab under:
  - `Assets/_Project/Prefabs/Minigames/ShrinkingIsland/`
- Keep it readable from default gameplay camera.

### 2. Build `TileCollapseImpact`

Goal:

Make tiles feel like authored stage machinery collapsing, not cubes being disabled.

Layers to add:

- existing warm edge warning remains
- edge dust/smoke when Fading begins
- a few low-poly chips that inherit tile palette and fade quickly
- strengthen the sink/tilt timing if needed
- avoid full red material swap

Implementation target:

- Existing script: `Assets/_Project/Scripts/Minigames/ShrinkingIsland/Tile.cs`
- Existing prefab hooks:
  - `_warningParticlePrefab`
  - `_fadeParticlePrefab`
- Existing prefab:
  - `SI_TileFadeBurstVfx.prefab`

### 3. Verify Like This

Do not accept VFX from Scene view alone.

Required checks:

- Play screenshot from default gameplay camera.
- Low side angle screenshot to catch missing/ugly side faces.
- Top/debug screenshot for readability.
- Unity Console has 0 errors/warnings after VFX changes.
- Confirm no `SI_TopChamferBox` or `SI_RoundedTilePrism` is used in scene.

## Do Not Do

- Do not import the full HDRP sample projects into `Assets/_Project`.
- Do not buy or import paid VFX packs yet.
- Do not add persistent global glitch/feedback effects.
- Do not use character Skinner/Smrvfx trails until there is a gameplay reason.
- Do not use Meshy for characters; props only, and only if gameplay-camera silhouette improves.
- Do not change core gameplay balance before the launch/collapse feedback pass.

## Dirty Worktree Note

The worktree already has many modified/untracked files from prior Shrinking Island, Eggybot, Meshy, VFX, material, and screenshot work. Do not revert unrelated files.

Codex-created/important docs:

- `docs/design/unity_feel_erasure.md`
- `docs/research/vfx_reference_repo_study_2026-05-28.md`
- `docs/decisions/2026-05-25_shrinking_island_fun_design.md`
- `docs/recipes/new_scene_setup.md`

## Suggested First Claude Prompt

```text
docs/handoffs/codex_to_claude_2026-06-02_vfx_next.md を読んで。
Shrinking Island の次の作業は BouncePadImpact と TileCollapseImpact。
Unity感を消すのが最優先。まず現状の prefab/script/scene を実測して、VFX Graph / ParticleSystem / mesh ring のどれで作るか決めて実装して。
commit はまだしないで。
```
