# Claude → Codex 引き継ぎ書 (User 「Codex に試してみたい」、2026-05-24)

> このファイルは Claude (Anthropic) が普段やってる **メソッド / 記録場所 / workflow** を全部 Codex に引き継ぐためのもの。
> User 「あなたがいつもどのようなやり方で、何をどこに記載してるかとか全部引き継いでみたい」

---

## 🎯 最初に読む

順番に必ず読む:

1. **`RULES.md`** ← 絶対遵守 R1-R10、技術ルール、commit format、全部統合
2. **`docs/KNOWLEDGE-INDEX.md`** ← **全ナレッジ蓄積場所の index** (MCP 失敗 / 成功パターン / recipe / セッション履歴 / 日報 すべての場所と要約)
3. **`AGENTS.md`** ← Codex 固有規約 (役割 / 制限)
4. **`CLAUDE.md`** ← プロジェクト概要、Must/Must Not、月次マイルストーン
5. **`.claude/skills/chaosdraftparty-gotchas/SKILL.md`** ← **既知罠 42 件** (MCP / Unity / Material / Prefab / NGO)、必読
6. **`.claude/rules/*.md`** ← architecture / csharp-unity / performance / serialization / unity-specifics
7. **`docs/RESOURCES.md`** ← Meshy / Synty / Mixamo / Kenney 運用
8. **memory** (`C:\Users\taise\.claude\projects\C--Users-taise-Documents-GitHub-ChaosDraftParty\memory\`):
   - `MEMORY.md` (index)
   - `feedback_*.md` 全部 (User 明示ルール 12 個)
   - `session_*.md` 直近 3 つ (引き継ぎ)
   - `recipe_*.md` (普遍 recipe)
   - `project_*.md` (進行中 state)
9. 直近 3 つの **日報** `docs/reports/YYYY-MM-DD.md` (最新の文脈)

---

## 🧠 Claude のメソッド全集

### A. 並列 agent 起動 (新機能 / 調査 / 修正の defacto)

R1 / R2 / R9 / R10 厳守:
- **3 個並列** で投入 (1 個じゃ視点不足、5 個以上は過剰)
- 各 agent に **明確な役割** (実測 / defacto 調査 / 代替アプローチ)
- WebSearch / WebFetch で **5+ ソース確認** 必須
- AAA タイトル動画解析、GDC talks、Unity 公式 doc、Reddit / forum 苦情

典型 3 agent pattern (Camera / VFX / Skill 等の visual 系):
```
Agent A: Unity MCP execute_code で実測 (時系列、frame 単位、bone Y / camera pos / Animator state)
Agent B: 構成解析 (script 読み + Cinemachine setting + Animator Controller)
Agent C: 業界 best practice (AAA defacto、罠 top 10、修正パターン)
```

各 agent prompt の必須要素:
- 背景 (なぜこの調査が必要か)
- 既存 file path (具体的に)
- 「思い込み禁止、まず実測」「○○ を絶対に守って」
- 出力 format (markdown table)
- 制約 (500 単語以内、5+ ソース、推測は明記)

```
Agent({
  subagent_type: "general-purpose",
  description: "○○調査",
  prompt: "...",
  run_in_background: true
})
```

### B. AI review loop (R3 厳守、commit 前必須)

各 commit 前に `.claude/skills/ai-review-loop/SKILL.md` 通り回す:

```
1. git diff で対象確定
2. Task("unity-reviewer", ...) で 1 周目
3. PASS w/ nits + HIGH 0 → ループ終了 → commit OK
4. ISSUE あれば即修正 (MEDIUM / LOW 数件なら autonomous で)
5. HIGH 残るなら code-reviser 起動 (2 周目)
6. 最大 3 周で打ち切り
```

unity-reviewer の警告 (HIGH / MEDIUM) は **絶対無視しない**。今回 (2026-05-23) Day 4/5 で
agent C の「過剰の懸念」警告を無視して User 不満出した実例あり (memory R1)。

### C. memory システム

User account 共通 (`~\.claude\projects\C--Users-taise-Documents-GitHub-ChaosDraftParty\memory\`)。
Claude も Codex も同じ memory を読む。

#### file 命名規則

| prefix | 内容 | 例 |
|---|---|---|
| `feedback_*.md` | User 明示ルール / 失敗パターン | `feedback_visual_aaa_deep_dive_mandatory.md` |
| `project_*.md` | 進行中の重要 state | `project_audio_day1_pending.md` |
| `session_*.md` | session 引き継ぎ (compact 用) | `session_2026-05-21_foot_v2_failed_handoff.md` |
| `recipe_*.md` | 普遍 recipe (Unity 全般応用可) | `recipe_camera_occluder_fade.md` |
| `MEMORY.md` | index (1 行/file、~150 字) | (本ファイル) |

#### frontmatter format

```yaml
---
name: short-kebab-case-slug
description: 1 行要約 (検索 hit 用、具体的に)
metadata:
  type: feedback / project / reference / session / recipe
---
```

#### feedback body の template

```markdown
# ルール: ___ (1 行)

User 明示 (YYYY-MM-DD、文脈): 「User の発言原文」

## Why
- なぜこのルール? (失敗 / 事故 / 教訓)

## How to apply
- いつ適用? どう判定?

## 例
- 守れた事例 / 守れなかった事例

## 関連 memory
- [[link-to-other-feedback]]
```

#### MEMORY.md への登録

新規 feedback file 作成したら必ず MEMORY.md にも 1 行追加:

```markdown
- [短いタイトル](feedback_xxx.md) — User 明示ルール (YYYY-MM-DD): 「User 原文」。具体的内容
```

### D. 日報 (`docs/reports/YYYY-MM-DD.md`)

毎日の終わりに書く。format:

```markdown
# 日報 YYYY-MM-DD

## 概要
1-3 行で今日のハイライト

## HEAD 進捗 (N commit)
```
hash subject
hash subject
```

## 1. 解決した問題
- 真因
- 経緯 (誤診 → 正解の時系列 table)
- 学び

## 2. 実装したもの
- AAA defacto / 業界根拠
- 値の根拠

## 3. AI review loop 戦績
| commit | 判定 | 修正 |

## 残課題 (次セッション)

## まとめ
1-3 行で総括。

---
**作成: Claude (Opus 4.7)、YYYY-MM-DD**
```

過去日報を参考: `docs/reports/2026-05-19.md`, `2026-05-22.md`, `2026-05-23.md`

### E. commit format

```
type(scope): 50 字以内の要約

詳細 (背景 / 真因 / 修正 / AI review 結果 / 学び)

Co-Authored-By: Codex <noreply@openai.com>
（Claude の場合は Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>）
```

type: `feat:` `fix:` `revert:` `refactor:` `docs:` `test:` `chore:`
scope: `eggybot` `camera` `vfx` `foot` `face` `skill` `minigame` `stage` `audio` `network` `docs` `chore`

### F. Unity MCP 使い方

Codex CLI でも Unity MCP 使える (AGENTS.md 参照、ただし read-only 推奨)。

主要 tool (Claude が頻用):
- `mcp__UnityMCP__execute_code` - C# 直接実行、Unity Editor 内で任意の処理 (実測 / 修正)
- `mcp__UnityMCP__manage_scene` - scene load / save
- `mcp__UnityMCP__manage_prefabs` - prefab 開く / 保存
- `mcp__UnityMCP__manage_editor` - Play / Stop / undo / redo
- `mcp__UnityMCP__manage_gameobject` - GameObject CRUD
- `mcp__UnityMCP__refresh_unity` - script compile / asset refresh
- `mcp__UnityMCP__read_console` - error / warning 確認

#### execute_code の罠

- `using ...` directive は **method body として実行** されるので冒頭に書けない (compile error)
- `Object.DestroyImmediate` は ambiguous (`UnityEngine.Object.DestroyImmediate` でフル指定)
- 長時間 busy-wait は **Unity main thread freeze** (前回失敗実例、`EditorApplication.update` callback + 早期 break で安全に)
- `using UnityEditor` も同様、`UnityEditor.AssetDatabase` フル指定推奨

### G. 異常時対処

| 症状 | 対処 |
|---|---|
| Unity MCP freeze | sleep 5 → editor_state check → ダメなら User に Editor 復活依頼 |
| Prefab Mode + Play Mode 衝突ダイアログ | User に「Exit Prefab Mode」依頼 (Image #17 と同じ) |
| commit 競合 (Claude A / B 並列) | file 担当を分ける、main 直 push 禁止 |
| MCP disconnect | reconnect 待ち、ToolSearch で deferred tool 再 load |
| 「直してるのに直らない」5 周以上 | **enabled = false の外部呼出** など構造的バグ疑う (2026-05-23 実例) |

### H. User 口調 / 機嫌読み (重要)

- 短い、フランク、口語的
- 怒ると「不採用で」「やめちゃって」「もう一回調べろ」「徹底的に調べて」「ルール覚えてる?」「理解してない?」
- 喜ぶと「天才」「結構いいね」「お、なおった」
- 質問形式が省略多い ("これ何?" "どう?")
- 「うざい」「めんどくさい」と感じたら **即対処** (permission prompt 等)

User が**怒ってる時**:
- 即謝罪 + 真剣に受け止める
- 自分の失敗の原因を **正直に認める** (取り繕わない)
- 「理解してない可能性ある」と認めて R7 (絵で説明依頼) も活用
- 並列 agent + 実測で **construction not assumption** で再アタック

User が**喜んでる時**:
- memory に永続化 (R1-R10 のどれが効いたか記録)
- 学びを抽出して次に活かす

### I. 役割分担 (Claude A / B / Codex)

| | Claude A | Claude B | Codex |
|---|---|---|---|
| 担当 | Player / Camera / VFX / Foot / 表情 | Stage / Asset / Skill SO / docs / test | 3rd reviewer |
| Branch | `main` | `feat/stage-*` | (編集なし) |
| Unity | Editor #1 | Editor #2 (別 Library) | MCP read-only |
| User 試遊頻度 | 高 | 低 (User 待ち時間ゼロ目標) | 0 |

User が Codex メイン使うなら、Claude A の役割を Codex が引き継ぐ可能性。その場合は本ファイル + RULES.md 厳守で。

### J. 実例: 「Camera ガクッ問題 5 周誤診」の教訓

5 commit (fa7b816 / 3921192 / 54ac521 / 10546bf / 8dc2594) で **camera 関連を全部 disable / Damping 調整** したが直らず。
6 commit 目 (b940db0) で並列 agent が **「`enabled=false` でも外部呼出で発火するバグ」** を指摘 → 即修正で User 「お、なおった」。

学び:
- 「何回 disable しても直らない時は **外部呼出経路** を疑う」
- `enabled = false` は Unity の **Update / OnEnable / OnDisable サイクルのみ制御**、public method の外部呼出は阻止しない
- **defensive guard** (`if (!enabled) return;`) を public method 冒頭に追加が defacto
- 5 周誤診のコスト大 = 早期に **R1 / R9 (実測 + deep dive)** 適用が結果的に最速

詳細: `docs/reports/2026-05-23.md`

---

## 🛠 Codex 起動推奨手順

```powershell
cd C:\Users\taise\Documents\GitHub\ChaosDraftParty

# 初回 (User 操作済)
codex login

# 試運転 (read-only で安全に)
codex --sandbox read-only
# 対話プロンプト:
> RULES.md と AGENTS.md と docs/handoff-claude-to-codex.md と CLAUDE.md を読んで、現在の状況を把握してください。
> その後 memory MEMORY.md と全 feedback_*.md を読んで、ルール R1-R10 を理解してください。

# 本格作業時 (workspace-write)
codex exec --sandbox workspace-write "次のタスク: ..."
```

---

## 📂 file 担当区分 (Codex / Claude A / B)

### Codex メインで使う場合 (User 意向)

User が Codex を Claude A の代替として使うなら、Codex が触る file:
- Player / Camera / VFX / Foot / 表情 (Claude A 旧担当)
- Stage / Asset / Skill SO / docs / test (Claude B 旧担当)
- 全部 OK

ただし:
- **AGENTS.md 「Codex 固有ルール」厳守** (commit は User と合意のみ、Unity Editor Play モード制御は慎重)
- **MCP script_apply_edits は使わない** (Claude 専有想定だったが、Codex メインなら使う、ただし AI review loop 必須)

### Claude A / B 並列で使う場合

`docs/claude-b-handoff.md` の file 担当表参照。

---

## 🎯 Codex への期待 (Claude 視点)

- **autonomous で長時間 task 実行** (Claude より session 寿命長い?)
- **Cloud Task 並列** (Claude にない機能)
- **token 効率** (Claude より低コスト?)
- **AI review 3rd reviewer** として両 Claude をサポート

ただし Claude の強み:
- **Unity MCP 連携が成熟** (Codex も使えるが Claude が defacto)
- **memory システム** (Claude 標準、Codex は手動で再構築)
- **並列 sub-agent** (Task tool、Codex も似た機能あり)
- **慎重な multi-file refactor** (Claude が得意領域)

→ **User の判断**: Codex に乗り換える / 併用する / 役割分担する。

---

## 💡 引き継ぎ確認 checklist (Codex が読了後)

- [ ] RULES.md 読了、R1-R10 言える
- [ ] memory MEMORY.md + 全 feedback_*.md 読了
- [ ] AGENTS.md の Codex 固有ルール理解
- [ ] CLAUDE.md の Must / Must Not 理解
- [ ] 過去日報 (2026-05-19 / 22 / 23) で文脈把握
- [ ] Unity MCP tool 使い方確認
- [ ] commit format / Co-Authored-By 設定
- [ ] User 口調メモ (怒り / 喜び / 急ぎサイン) 把握
- [ ] 並列 agent / AI review loop / R1 deep dive workflow 理解

完了したら User に「準備 OK、何やる?」と一言。

---

**作成: Claude (Opus 4.7)、2026-05-24**
**理由: User 「あなたがいつもどのようなやり方で、何をどこに記載してるかとか全部引き継いでみたい」**
