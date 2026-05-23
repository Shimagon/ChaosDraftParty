# ChaosDraftParty Knowledge Index

> このファイルは **AI (Claude A / B / Codex / 将来の AI) が読む単一 index**。
> 蓄積された全ナレッジ (User 明示ルール / Unity・MCP 罠 / AAA defacto recipe / process パターン / 進行中 state / セッション履歴 / 日報) の場所と要約。
> User 「ナレッジがたまってる場所、MCP の失敗とかよかったところ、そういう経験を引き継がせたい」(2026-05-24)

凡例: ★ = 重要度 (★1 補助 〜 ★5 絶対必読)

---

## 🚨 1. 絶対遵守ルール (★5、User 明示 R1-R11)

すべて `~\.claude\projects\C--Users-taise-Documents-GitHub-ChaosDraftParty\memory\` 配下。
統合版が `RULES.md` (repo 内)、Codex も AGENTS.md 経由で読む。

| ID | file | 内容 (User 原文) | 日付 |
|---|---|---|---|
| R1 | `feedback_visual_aaa_deep_dive_mandatory.md` | 「visual / camera / VFX は AAA 動画解析 deep dive 無しに実装禁止」 | 2026-05-23 |
| R2 | `feedback_research_psychology_industry_pro_view_mandatory.md` | 「新機能着手前に必ず心理学・業界 defacto・プロ視点を並列調査」 | 2026-05-19 |
| R3 | `feedback_ai_review_loop_mandatory.md` | 「AI review loop は commit 前に絶対省略しない」 | 2026-05-19 |
| R4 | `feedback_iterative_plan_review_loop_mandatory.md` | 「計画 → 計画 review → 実装 → 実装 review、OK まで反復」 | 2026-05-19 |
| R5 | `feedback_autonomous_no_confirmation.md` | 「全部許可するから俺に聞かないで、yes 推すのめんどくさい」 | 2026-05-21 |
| R6 | `feedback_ask_user_when_unclear.md` | 「不安・不明・判断つかないことは即 User にわかりやすく聞く」(視覚体感のみ) | 2026-05-19 |
| R7 | `feedback_ask_for_drawing_when_unclear.md` | 「認識ズレ感じたら User に絵で説明依頼」 | 2026-04-30 |
| R8 | `feedback_aaa_emulation_priority.md` | 「AAA タイトルの実装手法を積極的に盗む」(Genshin/Splatoon/Mario) | 2026-05-06 |
| R9 | `feedback_shader_pixel_first.md` | 「思い込みで proposal するな、まず実測 (pixel-level)」 | 2026-04-29 |
| R10 | `feedback_research_first_then_root_solution.md` | 「表層修正より徹底調査 → 根本解決」 | 2026-05-19 |
| (補) | `feedback_workflow_parallel_review_default.md` | 「並列 agent + AI review loop が default」 | 2026-04-30 |

---

## 🪤 2. Unity / MCP 罠 (★4-5、踏むと数時間〜数日溶ける)

### A. プロジェクト固有罠 (チェックリスト)

| ★ | file | 内容 |
|---|---|---|
| ★5 | `.claude/skills/chaosdraftparty-gotchas/SKILL.md` | **既知罠 42 件** (Unity MCP / execute_code / Material / Prefab / NGO 等)。新規スクリプトで罠踏んだら末尾に追記し commit |
| ★5 | `memory/feedback_enabled_false_does_not_block_method_call.md` | **2026-05-23 真因**: `enabled=false` は public method 外部呼出を阻止しない。defensive guard `if (!enabled) return;` 必須。Camera ガクッ 5 周誤診の元凶 |
| ★4 | `docs/ai_playbook/gotchas/unity6_urp17_pitfalls.md` | URP 17 系の罠 (Lit shader signature 等) |

### B. Unity / Mecanim / Cinemachine 罠

| ★ | file | 内容 |
|---|---|---|
| ★5 | `memory/feedback_bone_mesh_hierarchy.md` | bone と mesh の親子関係は別 hierarchy。Transform 同じでも SkinnedMR の rootBone が違うと挙動分離 |
| ★5 | `memory/feedback_mecanim_hips_lateupdate.md` | Mecanim Hips Y 補正は `anim.bodyPosition` shift で効かない、LateUpdate で Hips.position.y 直書きが defacto |
| ★4 | `memory/feedback_avatar_rebuild_caution.md` | humanoid Avatar 再構築は user 手動 bone 編集を破壊。muscle 正規化 + Animator が Edit でも bone 改竄 |
| ★4 | `memory/feedback_urp17_lit_signature.md` | URP 17 で LitPassFragment は `void + out param` 形式。HLSL shader 直書きで magenta 出たらこれ疑う |
| ★4 | `memory/feedback_mesh_pivot_offset_correction.md` | localScale で mesh が動く時は bounds.center + rotation を疑う (Blink 修正で User 高評価) |

### C. MCP 使い方の罠 (Claude が踏んだ実例)

`execute_code` 系の典型失敗 (Claude が何度も踏んだ):
- `using ...` directive は冒頭に書けない (method body 扱い)
- `Object.DestroyImmediate` は ambiguous → `UnityEngine.Object.DestroyImmediate` フル指定
- 長時間 busy-wait は Unity main thread freeze (実例: 2026-05-21 Foot 実測で 600 frame busy-wait → freeze)
- `EditorApplication.update` callback + 早期 break で安全に
- Prefab Mode + Play Mode 衝突ダイアログ (Image #17 系) は User 手動操作必要
- Material の `_Surface = 1` だけじゃ Transparent にならない、`_SrcBlend / _DstBlend / _ZWrite / RenderType tag` まで設定必要 (2026-05-23 砂埃 Material 修正で発見)

---

## 🎯 3. AAA defacto 普遍 Recipe (★4-5、Unity プロジェクト横断応用可)

### A. memory recipe (本プロジェクト発祥、他作品でも応用可)

| ★ | file | 内容 |
|---|---|---|
| ★5 | `memory/recipe_camera_occluder_fade.md` | 3D TPS Camera Occluder Dither Fade 完成 recipe。anticipationStart 0.3 / fadeOut 0.15 / fadeIn 0.30 / holdTime 0.25 / sphereRadius 0.6 / targetUpOffset 1.0 (CORE FIX) + 8 反復で学んだ罠 |
| ★5 | `memory/project_occlusion_dither_v1.md` | OcclusionDither v1 確定構成 (Bayer 4x4 + URP Lit ベース、maxDither 0.75 / 元 mat 完全保持) |

### B. docs/ai_playbook/recipes/ (横断的に整理)

| ★ | file | 内容 |
|---|---|---|
| ★5 | `docs/ai_playbook/recipes/scale_with_mesh_pivot_offset.md` | Unity localScale で mesh が動く罠 — pivot offset 補正 recipe (本作含む全 Unity プロジェクトで応用可) |
| ★5 | `docs/ai_playbook/recipes/spring_damper_for_squash.md` | Spring-Damper 物理で Squash & Stretch — 普遍レシピ。semi-implicit Euler、velocity 継承、AAA defacto 値 (Yoshi / Kirby / LocoRoco 系) |
| ★4 | `docs/ai_playbook/recipes/eggybot_face_expression.md` | Mesh Swap 表情 (Mario Odyssey defacto) — 5 mouth shape + 拡張 procedure |
| ★4 | `docs/ai_playbook/recipes/camera_occluder_fade.md` | recipe_camera_occluder_fade.md と同内容 (repo 側) |

### C. docs/ai_playbook/patterns/

| ★ | file | 内容 |
|---|---|---|
| ★5 | `docs/ai_playbook/patterns/parallel_agent_research.md` | 並列 agent 3 個 + 多角検証の defacto pattern |
| ★4 | `docs/ai_playbook/patterns/anticipation_tick_smooth_tail.md` | Anticipation Tick + Smooth Tail (Disney 12 原則の anticipation pattern) |

---

## 🏗 4. Process / Workflow Skills (★4-5)

`.claude/skills/*/SKILL.md` — 機能別 skill 定義

| ★ | file | 用途 |
|---|---|---|
| ★5 | `.claude/skills/ai-review-loop/SKILL.md` | AI review loop の手順 (unity-reviewer → code-reviser → unity-reviewer、最大 3 周) |
| ★5 | `.claude/skills/chaosdraftparty-gotchas/SKILL.md` | 既知罠 42 件 (上記 #2 と同じ、最重要) |
| ★4 | `.claude/skills/meshy-unity-workflow/SKILL.md` | Meshy AI → Unity 6 URP アセット生成ワークフロー (ステージ装飾、$20/月 1000 credits) |
| ★4 | `.claude/skills/unity-urp-quality-uplift/SKILL.md` | Unity URP キャラが「Lego cube 合体感・Unity 臭」に見える時の改善手順 (product shot クオリティ化) |

### 「良かった process パターン」(2026-04〜05 で実証)

| 出典 | パターン | 効果 |
|---|---|---|
| `memory/feedback_workflow_parallel_review_default.md` | 並列 agent 3 個 + AI review loop が default | 多角検証で見落とし防止 |
| `memory/feedback_ai_review_loop_first_use.md` | unity-reviewer + code-reviser 2 周ループ | HIGH/MEDIUM 5 件を commit 前に除去 |
| `memory/session_2026-04-29_eggybot_v16_journey.md` | 失敗 6 個 → 並列 agent + 実測で真因到達 | 同じ罠を二度踏まない知恵 |
| `memory/session_2026-04-30_eggybot_v16_step5_complete.md` | Mecanim T-pose 解決 + AI review loop 導入 | 失敗 10〜12 + LateUpdate 一発解決 |

---

## 📦 5. 進行中 / 重要 State (★3-4、時期次第で変化)

| file | 内容 |
|---|---|
| `memory/project_eggybot_v16_clip_v16final.md` | EGGYBOT v16 SDF clip 確定構成 (判定球 center=(0,0.50,0)、radius=0.336、bounds.center は罠) |
| `memory/project_audio_day1_pending.md` | Day 1 Audio (UIAudio + BGM) 一時保留 (Camera 酔い対応優先、再開手順含む) |
| `MEMORY.md` 「進行中」セクション | 次セッション継続用の最新 state |

---

## 📜 6. セッション履歴 (★2-3、時系列、過去の試行錯誤の経緯)

時系列 (新 → 古):

| file | 内容 |
|---|---|
| `memory/session_2026-05-22_foot_v3e_and_face_3stage.md` | Foot v3e ✅ + 表情 3 段階 + Day 4/5 + Codex 導入 (User 「結構いいね」) |
| `memory/session_2026-05-21_foot_v2_failed_handoff.md` | Foot v2 までの経緯 (A/B'/v2 失敗記録、参考用) |
| `memory/session_2026-05-20_plan_a_play_test_pending.md` | Plan A 完了 (BodyDip + CC 持ち上げ) — Play test NG → B' へ移行済 |
| `memory/session_2026-05-07_outline_failures_and_plan_A.md` | Outline 失敗 2 回 (Sobel/OSS 撤退、M3 以降保留) |
| `memory/session_2026-05-06_dither_iterations.md` | Dither + Camera Quality 反復 (30+ 候補のうち何が効き何が効かなかったか全記録) |
| `memory/session_2026-05-06_camera_quality_pending.md` | Camera Quality 酔い対策 継続記録 |
| `memory/session_2026-04-30_eggybot_v16_step5_complete.md` | Mecanim T-pose 完全解決 + AI review loop 導入 |
| `memory/session_2026-04-29_eggybot_v16_journey.md` | EGGYBOT v16 SDF clip 修正 (失敗 6 個 → 並列 agent + 実測で真因到達) |

---

## 📊 7. 日報 (★3、各日の作業要約)

`docs/reports/YYYY-MM-DD.md` — 形式は固定 (概要 / HEAD 進捗 / 解決 / 実装 / review 戦績 / 残課題 / まとめ)。

過去日報 (古い順):
- `docs/reports/2026-04-26.md`
- `docs/reports/2026-04-27.md`
- `docs/reports/2026-05-04.md`
- `docs/reports/2026-05-05.md`
- `docs/reports/2026-05-07.md`
- `docs/reports/2026-05-19.md` ← Spring 物理移行 「天才」評価
- `docs/reports/2026-05-22.md` ← Foot v3e + 表情 3 段階
- `docs/reports/2026-05-23.md` ← **Camera ガクッ 5 周誤診 → 真因到達** (Unity 罠 enabled=false)

新規日報書く時は過去の format に合わせる。

---

## 📚 8. プロジェクト規約 (常時 ON)

| file | 内容 |
|---|---|
| `RULES.md` | 絶対遵守 R1-R10 統合、技術ルール、commit format、口調メモ、History |
| `CLAUDE.md` | プロジェクト概要、Must/Must Not、月次マイルストーン、Skill/Minigame/RuleCard 表 |
| `AGENTS.md` | Codex 固有規約 (役割 / 制限) |
| `docs/RESOURCES.md` | Meshy / Synty / Mixamo / Kenney 運用ルール |
| `docs/claude-b-handoff.md` | Claude B (sub worktree) 起動指示書 |
| `docs/handoff-claude-to-codex.md` | Claude のメソッド全集を Codex へ引き継ぐ書 |
| `docs/KNOWLEDGE-INDEX.md` | **本ファイル**、全ナレッジの index |
| `.claude/rules/architecture.md` | MVS / VContainer / MessagePipe / UniTask |
| `.claude/rules/csharp-unity.md` | C# / Unity 命名規約 |
| `.claude/rules/performance.md` | GC / Draw call / 物理 |
| `.claude/rules/serialization.md` | FormerlySerializedAs / Unity serialization 罠 |
| `.claude/rules/unity-specifics.md` | Editor vs Runtime / Lifecycle / `?.` 罠 |

---

## 🎯 各 AI が最初に読むべき順序

### Claude A (main worktree) 起動時

```
1. RULES.md (R1-R10 確認)
2. memory MEMORY.md (進行中セクション)
3. 最新 session_*.md (引き継ぎ確認)
4. 最新日報 docs/reports/*.md
5. 必要に応じて memory feedback_*.md / recipe_*.md
```

### Claude B (stage worktree) 起動時

```
1. docs/claude-b-handoff.md (役割 / 担当 file)
2. RULES.md
3. memory MEMORY.md + 全 feedback_*.md
4. CLAUDE.md
5. .claude/rules/*.md
```

### Codex 起動時

```
1. docs/handoff-claude-to-codex.md (Claude のメソッド全集)
2. RULES.md
3. AGENTS.md (Codex 固有)
4. docs/KNOWLEDGE-INDEX.md (本ファイル)
5. memory MEMORY.md + 全 feedback_*.md
6. .claude/skills/chaosdraftparty-gotchas/SKILL.md (既知罠)
7. CLAUDE.md
8. .claude/rules/*.md
9. 直近 3 つの session_*.md と日報
```

---

## 🧠 ナレッジ追加 / 更新の流れ (User 明示ルール準拠)

新しい教訓 / 罠 / recipe を学んだら **必ず永続化**:

1. **User 明示ルール / 失敗パターン** → `memory/feedback_*.md` + `MEMORY.md` index 追加
2. **Unity / MCP 罠** → `.claude/skills/chaosdraftparty-gotchas/SKILL.md` 末尾に追記
3. **普遍 recipe** (横断応用可) → `memory/recipe_*.md` or `docs/ai_playbook/recipes/`
4. **進行中 state** → `memory/project_*.md`
5. **セッション引き継ぎ** → `memory/session_YYYY-MM-DD_*.md`
6. **日報** → `docs/reports/YYYY-MM-DD.md`

更新したら本ファイル (KNOWLEDGE-INDEX.md) も追記する。Codex / 将来 Claude が読めるように。

---

**作成: Claude (Opus 4.7)、2026-05-24**
**理由: User 「ナレッジがたまってる場所、MCP の失敗とかよかったところ、そういう経験を引き継がせたい」**
