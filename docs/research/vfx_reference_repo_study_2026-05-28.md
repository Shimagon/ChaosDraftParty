# VFX Reference Repo Study

Date: 2026-05-28
Purpose: turn professional Unity VFX references into concrete Shrinking Island upgrades without copying blindly.

## Local Reference Cache

All cloned repos are kept outside the Unity asset tree under `.codex/refs/vfx/`.

| Reference | Local path | Status | Useful for |
|---|---|---|---|
| Unity-Technologies/VisualEffectGraph-Samples | `.codex/refs/vfx/VisualEffectGraph-Samples` | Cloned, HEAD `bcd8004` | VFX layering, meteor impact sequencing, output event helpers, flipbooks, smoke, sparks, portal distortion |
| keijiro/SdfVfxSamples | `.codex/refs/vfx/SdfVfxSamples` | Cloned, HEAD `2f4fac1` | SDF-constrained trails/particles on authored shapes |
| keijiro/Skinner | `.codex/refs/vfx/Skinner` | Cloned, HEAD `4f1b865` | character motion trails from mesh vertices, GPU-driven movement emphasis |
| keijiro/Smrvfx | `.codex/refs/vfx/Smrvfx` | Cloned, HEAD `1a6f214` | built-in VFX Graph skinned mesh sampling |
| keijiro/SonarFx | `.codex/refs/vfx/SonarFx` | Cloned, HEAD `7d48202` | full-screen pulse/wave read over existing surfaces |
| keijiro/KinoFeedback2 | `.codex/refs/vfx/KinoFeedback2` | Cloned, HEAD `690fa65` | frame feedback, afterimage, accumulated energy look |
| keijiro/Kino | `.codex/refs/vfx/Kino` | Cloned, HEAD `2e0dff7` | post-process color language, overlay, streak, sharpen/glitch ideas |
| z4gon black-hole/fire/mesh-deformation/water | Web only | Git/codeload returned 404 despite public GitHub pages | README implementation patterns only |

## License Notes

- Unity VisualEffectGraph-Samples: Unity Companion License. Use only in Unity-dependent projects and preserve license notes.
- Keijiro Skinner/SonarFx: MIT.
- Keijiro Smrvfx/Kino/KinoFeedback2: Unlicense.
- z4gon pages are reference-only for now because repo download failed from Git/codeload. Do not copy source from cached pages.

## What The Good References Have In Common

1. They never rely on one particle emitter.
   Good effects are stacks: anticipation, contact flash, shaped geometry, smoke/dust, sparks/streaks, distortion, lighting/post, and timed camera response.

2. The effect has a readable silhouette first.
   Meteor impact and portal examples work because the player can read the main shape before noticing noise/detail.

3. Motion timing is authored.
   The impact frame does not just "play particles"; it compresses time into a short sequence: charge, hit, overshoot, decay.

4. Glow is controlled by HDR color plus bloom.
   Bloom carries energy, but the base color still has an authored palette. Random neon over the whole scene creates asset-store smell.

5. Advanced effects still need game grammar.
   SDF trails, screen pulses, and distortion are only useful if they explain gameplay state: launch, danger, collapse, recovery, hit.

## Shrinking Island Translation

### BouncePadImpact

Target feel: the pad is a recovery machine, not a trampoline primitive.

Layers:
- pre-contact idle pulse: low-amplitude scale + emissive ring breath
- contact compression: pad squashes 2-3 frames before launch
- launch frame: radial ring flash at pad plane
- direction read: short cyan/mint streaks pointing toward the center objective
- body response: camera impulse stays small and vertical, player squash/velocity carries the force
- decay: sparks fade upward/forward, no default white particle balls

Reference mapping:
- z4gon black-hole README: screen distortion + Fresnel/glow idea, but reduced to a small pad-local ripple.
- Unity Meteorite: impact split into anticipation/main impact/adds.
- KinoFeedback2: optional very short afterimage/echo on hard launch, not global permanent post effect.

### TileCollapseImpact

Target feel: the island is an authored stage mechanism, not a disabled cube.

Layers:
- warning: warm edge frame already exists; keep restrained material tint
- structural crack/read: thin embedded orange line or rim ticks, not full red material swap
- collapse frame: tile sinks/tilts already exists; add dust/smoke at edges
- debris: few low-poly chips that inherit tile palette and fade quickly
- after-space: void/fog below should make the gap feel intentional

Reference mapping:
- Unity Meteorite ground impact: separate ground contact from secondary particles.
- z4gon fire README: flipbook smoke/sparks, color-over-life, drag/wind, normal-lit smoke if we go VFX Graph.
- SdfVfxSamples: future option for particles constrained to tile edge/ring shapes.

### StageAtmosphere

Target feel: authored party-game void instead of default Unity environment.

Layers:
- skybox gradient and fog stay warm/stylized
- subtle global grade and bloom support gameplay colors
- avoid over-dark cinematic look; readability beats mood
- no decorative random props unless their silhouette says something useful

Reference mapping:
- Kino overlay/recolor: use as concept for a restrained global color grade, not as an imported HDRP package.
- Unity official VFX docs/samples: post, sky, fog, and VFX must be tuned together.

## Do Not Import Blindly

- Do not drop full HDRP sample projects into `Assets/_Project`; this project is URP.
- Do not use Skinner/Smrvfx for the player until there is a gameplay reason. Character vertex trails can quickly become unreadable in a party camera.
- Do not add big global glitch/feedback unless it is a one-shot hit confirmation. Persistent screen effects damage readability.
- Do not copy z4gon source until the repo can be fetched normally and its license is confirmed.
- Do not buy VFX packs yet. First build one small authored `BouncePadImpact` and one `TileCollapseImpact`; then compare against paid packs.

## Next Implementation Pass

1. Build a local Shrinking Island VFX lab scene/prefab set, not a full sample import.
2. Replace the current launch particle with a custom layered pad impact:
   - ring mesh or particle ring
   - directed streaks
   - tiny sparks
   - pad-local ripple material if URP camera opaque texture is enabled
3. Upgrade tile fade with edge dust/debris and a stronger collapse beat.
4. Verify in default gameplay camera, low side angle, and Console.
5. Keep `SI_ClosedBox` for stage structure until Blender/ProBuilder side faces are proven.
