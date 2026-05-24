# RULES.md — ChaosDraftParty 開発の絶対ルール

> このファイルは **Claude A (main worktree) / Claude B (sub worktree) / OpenAI Codex CLI** 全員が読む単一参照。
> User 「あなたに渡してるルールはなに? テキストでどっかに書いといて。Codex にも読ませる」(2026-05-23)

User 明示ルールが累積してるので、本ファイル末尾の **History** で時系列も確認可能。

---

## 🚨 絶対遵守ルール (これを破ると User が怒る)

### R1. visual / camera / VFX は AAA 動画解析 deep dive 無しに実装禁止

User 明示 (2026-05-23、Day 4/5 撤回時): 「ちゃんとルールを絶対に守ってね」「ルール覚えてる? 調べたりした?」

- 各タスク受けた瞬間に **並列 agent 3 個** で AAA 動画解析調査を起動
- Mario Odyssey / Splatoon / Genshin / Apex / Hat in Time の動画解析、酔い対策 best practice、Cinemachine 3.x 公式 doc を最低 5 ソース確認
- agent の警告 (「過剰の懸念」「酔いリスク」) を **絶対無視しない**
- 「軽い修正だから」「すぐ commit したいから」で skip 禁止

実例: Day 4 Particle で Default-Particle 白丸 → User「砂埃クオリティ低い」叱責。Day 5 FOV Punch + Camera Impulse で User「画面揺れ不採用」叱責。両方 agent の警告を聞き流したのが原因。**二度と踏まない**。

memory: [`feedback_visual_aaa_deep_dive_mandatory.md`](C:/Users/taise/.claude/projects/C--Users-taise-Documents-GitHub-ChaosDraftParty/memory/feedback_visual_aaa_deep_dive_mandatory.md)

### R2. 新機能着手前に必ず心理学・業界 defacto・プロ視点を並列調査

User 明示 (2026-05-19): 「visual/UX/アニメ系は調査せず実装 NG」

- R1 と同根。表情アニメ直走り失敗を受けて設定された
- 並列 agent (defacto / 心理学 / プロ視点) で多角検証 → 計画 → 実装

memory: `feedback_research_psychology_industry_pro_view_mandatory.md`

### R3. AI review loop は commit 前に絶対省略しない

User 明示 (2026-05-19): 「絶対ループさせるのを挟んで」

- pending changes → `unity-reviewer` (Claude) → `code-reviser` → `unity-reviewer` を最大 3 周
- PASS w/ nits 以上 + HIGH 0 件 になるまで commit しない
- MEDIUM 1 件以下なら commit OK、それでも修正可能なら修正してから
- skill 詳細: `.claude/skills/ai-review-loop/SKILL.md`

memory: `feedback_ai_review_loop_mandatory.md`

### R4. 計画 → 計画 review → 実装 → 実装 review の二重ループを OK まで反復

User 明示 (2026-05-19): 「OK が出るまで繰り返す」

- 計画段階で並列 agent + review、実装後も AI review
- 視覚 / 体感 OK は User Play test 必須

memory: `feedback_iterative_plan_review_loop_mandatory.md`

### R5. AskUserQuestion / yes 確認は機械的判断ではやめる

User 明示 (2026-05-21): 「全部許可するから俺に聞かないで, yes 推すのめんどくさい」

| 判断 | Claude Autonomous | User 必要 |
|---|---|---|
| commit / push (review PASS なら) | ✅ | ❌ |
| 並列 agent + 実測算出値の適用 | ✅ | ❌ |
| 新規 script 作成 / prefab 編集 (機械的) | ✅ | ❌ |
| アプローチ選択 (A/B/C 等の機械評価) | ✅ | ❌ |
| **視覚デザイン変更** | ❌ | ✅ User Play test |
| **shader / VFX / scene 美術** | ❌ | ✅ |
| **ゲーム性 / バランス / 操作感** | ❌ | ✅ |
| **新ライブラリ導入 (有料 $20+)** | ❌ | ✅ 予算判断 |

例外: 破壊操作 / 設計の根本転換 / シリーズ反復で同じ NG が続く → User に聞く。

memory: `feedback_autonomous_no_confirmation.md`

### R6. 不安・不明・判断つかないことは即 User にわかりやすく聞く

User 明示 (2026-05-19): 「すぐに俺にわかりやすく聞くこと」

R5 と矛盾しない: **視覚 / 体感 / ゲーム性 / 予算判断** に限定して User に聞く。
機械判断は Claude 即決。

memory: `feedback_ask_user_when_unclear.md`

### R7. 認識ズレを感じたら User に「絵で説明して」と依頼

User 明示 (2026-04-30): shader rim vs 「淵」混同で時間ロス

- 用語が曖昧、または User の言葉と Claude の認識がズレてる時、**絵 / スクショ** を依頼
- 後から User に再説明させない、最初に聞く

memory: `feedback_ask_for_drawing_when_unclear.md`

### R8. AAA タイトルの実装手法を積極的に盗む

User 明示 (2026-05-06): Genshin / FF14 / モンハン / Hat in Time / Splatoon / Mario Odyssey

- M1 暫定より M3 本格を前倒し可
- 自作 vs AAA 流用の選択は AAA defacto 優先

memory: `feedback_aaa_emulation_priority.md`

### R9. 思い込みで proposal するな、まず実測

User 明示 (2026-04-29): shader / mesh / bone の挙動が想定と違う時:

1. 数値を取る前に修正案を出さない
2. Unity MCP `execute_code` で実測 (vertex position / bone Y / shader uniform 等)
3. 並列 Agent 3 個で多角検証

memory: `feedback_shader_pixel_first.md`

### R10. 表層修正より徹底調査 → 根本解決

User 明示 (2026-05-19): Spring 物理移行で「天才」評価

- 「動いてる中で同 issue 再発」=表層修正、根本解決必要
- Why を調べる → AAA defacto / 物理理論 → 根本実装

memory: `feedback_research_first_then_root_solution.md`

### R11. User が「調べて」と言わなくても、設計判断前は能動的に並列調査

User 明示 (2026-05-24): 「あんたは俺が調べてって言わないと調べないもんな」

R1 (visual deep dive) / R2 (心理学+業界) の精神を「操作系 / 設計判断 / 将来拡張」全般に拡張。Codex が並列 3 agent で構造的考察を最初から提示 → Claude の能動性不足が顕在化したのが契機 (handoff: `docs/handoffs/codex_to_claude_2026-05-24_controller_camera.md`)。

調査必須トリガ (User 明示なしで自動発動):

1. **操作系の変更** — 入力 mapping / ボタン配置 / 移動感 / 入力 latency
2. **設計の根本選択** — 体の向き / カメラ追従 / damping 値 / 物理感覚
3. **将来拡張の判断** — 「後で武器追加した時に大変か」みたいな User の不安
4. **失敗 retrospect** — User が「これダメ」と返した時は値再調整の前に「なぜダメだったか」の構造的調査
5. **defacto の存在しそうな領域** — TPS / FPS / アクション / カジュアル / パーティで「みんなどうしてるか」が効く話

やり方: 並列 Explore agent 3 個 + WebSearch + 公式 doc。AAA タイトル名 5-10 個 + Xbox XAG / PS5 アクセシビリティ + Cinemachine/Unity 公式 doc を根拠に、最終レポート 400 字 + 推奨 1 + 代替 1 + 技術リスクで返す。

memory: `feedback_research_before_user_asks.md`

---

## 🔧 技術ルール (ChaosDraftParty 特有)

### Engine

- Unity **6 LTS** + URP
- C# (`using` のスタイルは既存 file に合わせる)
- Network: **Netcode for GameObjects (NGO) 2.x** + Unity Relay
- Camera: **Cinemachine 3.x**
- Input: **New Input System** (`PlayerControls.inputactions` generated C#)
- Animation: Mixamo retarget + LateUpdate PoseLock pattern

### Folder

```
Assets/_Project/
├─ Scripts/Core/        # PlayerController 他、本体実装
├─ Scripts/Skills/      # M3 着手予定
├─ Scripts/Minigames/   # M3
├─ Scripts/Tests/       # EditMode 中心
├─ Prefabs/Player/      # EggybotPlayer.prefab
├─ Prefabs/Stage/       # ステージ装飾 (Claude B 担当)
├─ Scenes/              # Sandbox_v21, Stage_*
├─ ScriptableObjects/   # Skill/Minigame/RuleCard/Season/Character
├─ Art/Materials/       # URP material
├─ ThirdParty/Kenney/   # CC0
└─ ThirdParty/...       # Synty (買ったら) / Meshy 生成
```

### Must

1. **ホスト権威型ネットワーク**。Client は input 送信 + 補間表示のみ
2. **決定論物理に手を出さない**。Rigidbody は爆弾等のごく最小限
3. **同期対象は最小化**。見た目は client 側で再現
4. **バランス値は ScriptableObject / GameConfig に集約**
5. **ParrelSync で 2 client 動作確認** してから commit

### Must Not

1. スキル無限ネスト、物理連鎖、決定論リプレイ
2. スマホセンサ / VR / モバイル対応 (PC Steam 専用)
3. ホストマイグレーション / 切断再参加 (v10 送り)
4. キャラ大規模自作 (Synty + Mixamo 既製品活用)
5. **Meshy で キャラ 生成禁止** (Synty + Mixamo 固定、装飾 / props のみ Meshy)

### Camera 哲学 (2026-05-23 確定、Mario / Splatoon defacto)

- FOV 60° (TPS defacto)
- OrbitalFollow.PositionDamping (0.15, 0.25, 0.15)
- RotationDamping (0.2, 0.2, 0.2)
- Noise OFF, Roll OFF, Look-Ahead OFF, Deoccluder OFF (CameraOccluderFader 単独)
- Brain.DefaultBlend = Cut (ease 排除)
- 速度感は VFX + Player anim で出す、**camera ではない**
- cameraMinWorldY pitch clamp 維持 (床貫通防止)

memory: `feedback_visual_aaa_deep_dive_mandatory.md`

### Foot v3e (確定、2026-05-21)

- EggybotFootGroundClamp.LateUpdate world.y clamp (Mario / Fall Guys defacto)
- bone は触らない、foot.position のみ補正
- 固定 offset は禁忌 (Animator 駆動下では絶対脆い)

memory: `session_2026-05-21_foot_v2_failed_handoff.md`

### 役割分担 (Claude A / B / Codex)

| | Claude A (main) | Claude B (stage worktree) | Codex |
|---|---|---|---|
| 担当 | Player / Camera / VFX / Foot / 表情 | Stage / Asset / Skill SO / docs / test | 3rd reviewer (read-only) |
| Branch | `main` (or `develop`) | `feat/stage-*` | 編集しない |
| Unity | Editor #1 | Editor #2 (別 Library) | MCP read-only |

衝突回避: `docs/claude-b-handoff.md` の file 担当表参照。

---

## 📝 commit / push ルール

### commit message format

```
feat(scope): 短い要約 (50字以内)

詳細 (背景 / 真因 / 修正内容 / AI review 結果 / 学び)

Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

scope: `eggybot` `camera` `vfx` `foot` `face` `skill` `minigame` `stage` `audio` `network` `docs` `chore` 等。

### type

- `feat:` 新機能
- `fix:` バグ修正
- `revert:` 撤回 (Day 4/5 撤回等)
- `refactor:` リファクタ
- `docs:` ドキュメント
- `test:` テスト
- `chore:` 雑務 (Codex 設定等)

### NEVER

- `--no-verify` (hook skip 禁止)
- `--force` push (main 直 force 禁止)
- `git reset --hard` (User 明示時のみ)
- 機密 commit (`.env` 系)

---

## 📂 必須参照ドキュメント

| ファイル | 内容 |
|---|---|
| `RULES.md` (本ファイル) | 全ルール統合 |
| `CLAUDE.md` | プロジェクト概要、技術スタック、Must/Must Not |
| `AGENTS.md` | Codex 固有規約 (役割 / 制限) |
| `.claude/rules/architecture.md` | MVS / VContainer / MessagePipe / UniTask |
| `.claude/rules/csharp-unity.md` | C# / Unity 命名規約 |
| `.claude/rules/performance.md` | GC / Draw call / 物理 |
| `.claude/rules/serialization.md` | FormerlySerializedAs / Unity 罠 |
| `.claude/rules/unity-specifics.md` | Editor vs Runtime / Lifecycle |
| `.claude/skills/ai-review-loop/SKILL.md` | review loop 手順 |
| `.claude/skills/chaosdraftparty-gotchas/SKILL.md` | 既知罠 42 件 |
| `docs/RESOURCES.md` | Meshy / Synty / Mixamo 運用 |
| `docs/claude-b-handoff.md` | Claude B 起動指示書 |

memory file 全部 (`C:/Users/taise/.claude/projects/C--Users-taise-Documents-GitHub-ChaosDraftParty/memory/`):
- `MEMORY.md` (index)
- `feedback_*.md` (User 明示ルール 16 個)
- `project_*.md` (進行中)
- `session_*.md` (引き継ぎ)
- `recipe_*.md` (普遍 recipe)

---

## 🛑 異常時の対処

### Unity MCP freeze

`mcp__UnityMCP__execute_code` で long task (Play モード busy-wait 等) → main thread 占有 → MCP 応答停止。

対処:
1. Bash で `sleep 5` 待つ
2. `editor_state` check
3. それでもダメなら User に「Unity Editor 復活させて」依頼

予防: `EditorApplication.update` の callback で連続実測する時は **frame 数指定 + 早期 break**。

### Prefab Mode + Play Mode 衝突

Image #17 ダイアログ「Risk of unwanted modifications」表示時、User 操作 (Exit Prefab Mode) 必須。

### commit 競合 (Claude A / B 並列開発)

- file 担当を分ける (R5 + handoff doc)
- main 直 push 禁止 → feature branch → PR (or merge)
- manifest.json は A 専属

---

## 🎯 各 AI の動き方 (要約)

### Claude A (main worktree)

User Play test を頻繁にする Player / Camera / VFX / 体感系。User の即時フィードバックがある。

### Claude B (stage worktree)

User 試遊と独立な裏方 (Stage / Asset / Skill SO / test / docs)。User 待ち時間ゼロ化が目標。

### Codex CLI

両 worktree から呼ぶ 3rd reviewer。read-only sandbox 必須 (`codex exec --sandbox read-only`)。
ChatGPT Plus $20/月 (User 契約済、API key 不要)。

詳細: `AGENTS.md`

---

## 📅 History (User 明示ルール時系列、time 抜粋)

| 日付 | ルール |
|---|---|
| 2026-04-29 | 思い込み禁止、まず実測 (R9) |
| 2026-04-30 | 並列 agent + AI review loop が default |
| 2026-04-30 | 認識ズレ感じたら絵で説明依頼 (R7) |
| 2026-05-06 | AAA タイトル defacto 積極的に盗む (R8) |
| 2026-05-19 | 新機能着手前に心理学・defacto・プロ視点並列調査 (R2) |
| 2026-05-19 | AI review loop は commit 前に絶対省略しない (R3) |
| 2026-05-19 | 計画 → 計画 review → 実装 → 実装 review 反復 (R4) |
| 2026-05-19 | 不安・不明は即 User に聞く (R6、視覚体感のみ) |
| 2026-05-19 | 表層修正より徹底調査 → 根本解決 (R10) |
| 2026-05-21 | AskUserQuestion / yes 確認は機械判断でやめる (R5) |
| 2026-05-23 | visual / camera / VFX は AAA 動画解析 deep dive 無し実装禁止 (R1) |
| 2026-05-24 | User が「調べて」と言わなくても、設計判断前は能動的に並列調査 (R11) ← 最新 |

---

## 💬 User の口調 (参考、Claude が User の機嫌を読む時)

- 短い、フランク、口語的
- 怒ると「不採用で」「やめちゃって」「もう一回調べろ」「徹底的に調べて」
- 喜ぶと「天才」「結構いいね」
- 質問形式が省略多い ("これ何?" "どう?")
- 「うざい」「めんどくさい」と感じたら **即対処** (permission prompt 等)

→ ルール厳守 + 体感優先 + autonomous で動く = 信頼回復。

---

**最終更新: 2026-05-23 (User 「Codex にも読ませる」依頼で作成)**
