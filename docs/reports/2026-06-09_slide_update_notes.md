# 2026-06-09 Slide Update Notes

## V8.10 Autonomous Deck

Latest generated deck:

- `docs/presentations/2026-06-09_progress_shimada_v8_10_autonomous.pptx`
- Previous: `docs/presentations/2026-06-09_progress_shimada_v8_9_autonomous.pptx`

Build script:

- `tools/build_progress_pptx_20260609_v8_9.py`

Deck structure:

- 8 slides total.
- Title slide has no page number.
- Slides 2-8 each have exactly one page number.
- Video placeholder pages were removed.
- Closing slide is a simple `ありがとうございました` page.
- Main latest screenshot:
  - `Assets/Screenshots/v8_10_camera_radius_13_2_play_20s.png`
- Structure overview screenshot:
  - `Assets/Screenshots/v8_10_overview_switchback_incidents.png`

Verification:

- Reopened with `python-pptx`.
- Slide count: `8`.
- Page-number check: title `0`, slides 2-8 `1` each.
- Visual image rendering could not be run because LibreOffice / PowerPoint COM helpers were not available in this shell.

## V8.5 Slide Replacement

Use the V8.5 gameplay screenshot when the slide needs to communicate multiplayer intent.

Recommended visual:

- `Assets/Screenshots/v8_5_gameplay_player_proxies_visible.png`

Caption:

V8.5 adds static visual-only player proxies so the route reads as a 2-4 player party-game stage in screenshots. The proxies have no enabled behaviours or colliders.

## V8.3 Slide Replacement

Use V8.3 screenshots instead of the earlier V8 screenshots if there is time to refresh the deck.

Recommended closing slide title:

Thank you

Recommended closing body:

- Next: connect dash / jump / pulse to this Shrinking Island route gauntlet.
- 2P Playtest: check whether bridge pockets and L-gates create real accidents and convincing KOs.

Recommended visual:

- `Assets/Screenshots/v8_3_playmode_grounded_spawn_deck.png`

Small caption:

V8.3 adds stable 4-corner spawn decks, bridge contact pockets, L-shaped central entrances, and slower shrink pacing so players do not rush straight into the center.

## V8.3 Quality Gate Slide

Title:

Shrinking Island now has a longer outer route before the central fight.

Proof bullets:

- 4 stable outer spawn decks added.
- Bridge pockets and L-gates weaken direct center rush.
- Shrink pacing changed from `6s / 14s` to `16s / 20s`.
- Play check: player stayed grounded after 5 seconds.
- Scene validate 0, Console 0, EditMode 9/9 pass.

Recommended visuals:

- Gameplay: `Assets/Screenshots/v8_3_playmode_grounded_spawn_deck.png`
- Overview: `Assets/Screenshots/v8_3_overview_stable_spawn_decks.png`

## V8 Slide Replacement

Use the V8 screenshots instead of the older V7 video placeholders.

Recommended closing slide title:

ありがとうございました

Recommended closing body:

- 次は M3 として、選んだスキルをこのステージで使う 1 ラウンドを見せる。
- 2P Playtest で「事故が起きるか」「納得できる KO になるか」を確認する。

Recommended visual:

- `Assets/Screenshots/screenshot-20260609-192441_1.png`

Small caption:

縮む島は、四隅の島から中央リングへ進む構造に変更。中央へ行くほど接触点が増える。

## V8 Quality Gate Slide

Title:

縮む島を、中央へ進むほど接触するステージに変えた

Proof bullets:

- 外周 28 タイルを四隅の島に再配置
- 中央直行を blocker / gate / bridge で弱めた
- shrink layer は 28 / 20 / 12 / 4 のまま維持
- 全 shrink 段階で床接続は 1 component
- Scene validate 0、Console 0、EditMode 9/9 pass
- Playtest_EggybotPlayer と Play camera を配置済み

Recommended visuals:

- Topdown: `Assets/Screenshots/screenshot-20260609-192441_1.png`
- Play camera: `Assets/Screenshots/screenshot-20260609-193108.png`

## V7 Older Notes

## Replace Page: Next Sprint / Thank You Slide

Use this as the closing update instead of a plain "thank you" slide if a technical closing is needed.

Title:

M3 は「選んだスキルを、縮む島で使う」1 ラウンドを見せる

Main visual:

- `Assets/Screenshots/codex_si_v7_recovery_marks_topdown_2026-06-09.png`

Three callouts:

1. Draft UI
   - round 前に dash / jump / pulse を選ぶ
2. Skill Use
   - S 字ルート、障害物、復帰パッドで事故を作る
3. 2P Playtest
   - 中央喉元でぶつかるか、納得できる KO になるかを見る

Footer line:

次の報告目標: 2P で「事故が起きる」「納得できる KO」が 1 ラウンド内で見える状態にする。

## Replace Page: Quality Gate Slide

Use this if the deck needs a QA/engineering slide.

Title:

縮む島を「中央へ逃げるだけ」から「中央へ行くほど接触する」ステージに変えた

Proof bullets:

- 4 隅スポーンから S 字ルートで中央喉元へ合流
- 最短路は約 42.2m、以前より中央直行感を弱めた
- shrink layer は 28 / 20 / 12 / 4 のまま維持
- Scene validate 0、Console 0、EditMode 9/9 pass
- プレイヤー砂埃 / Footstep / DashTrail は scene player に配線済み

Recommended visual:

- `Assets/Screenshots/codex_si_v7_recovery_marks_gamecam_2026-06-09.png`

## Speaker Notes

初見向けには「縮む島」は、足場が外から消えていく中で、中央の安全地帯へ向かうミニゲームだと先に言う。

今回の変更点は、単に広くしたことではなく、中央へ向かうルートを S 字にして、橋・障害物・復帰パッドで他プレイヤーと接触しやすくしたこと。

未検証として、ParrelSync / MPPM の 2P 同時プレイはまだ残る。次回はそこを動画で見せる。
