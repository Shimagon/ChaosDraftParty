# AGENTS.md — Codex CLI 用ハンドオフガイド

このファイルは **OpenAI Codex CLI** が project root で自動読込するハンドオフガイド。
Claude Code 用の `CLAUDE.md` と協調動作するための **Codex 固有規約のみ** を記載。

**プロジェクト全体の規約は `CLAUDE.md` および `.claude/rules/*.md` を必ず読むこと。**

---

## このプロジェクトの背景 (1 行)

ChaosDraftParty — Unity 6 LTS + NGO + Relay の 2-4 人 3D TPS パーティゲーム、卒研、2026-04 〜 2026-09 (M1-M6)。

---

## Codex の役割 (Claude Code との分担、2026-05-21 User 確認)

### 案 2: 並列 worker (User Play test / Claude 主処理中の独立タスク)
- balance CSV / ScriptableObject 雛形生成 (SkillDefinition × 10 / RuleCard × 6 等)
- docs / progress report 起草 (`docs/reports/YYYY-MM-DD.md`)
- test 雛形 / EditMode test 生成 (Spring 物理、Foot ジオメトリ等)
- Meshy prompt 量産 (ステージ装飾用、灯籠 / 樹木 / 旗 等)

### 案 1: AI review loop の 3rd reviewer
- 順序: `unity-reviewer` (Claude) → `code-reviser` (Claude) → **`codex exec` (Codex)** で最終 read-only 検証
- 観点: 別視点の null check、boundary case、edge case 漏れ、業界 defacto 違反

---

## Codex 固有ルール (NON-NEGOTIABLE)

1. **`script_apply_edits` / `apply_text_edits` 等の Unity script 編集は Claude 専有**
   - Codex は MCP 経由でも **read-only** 操作のみ (`read_console`, `find_in_file`, `get_sha`, `unity_reflect` 等)
   - 編集が必要なら、`docs/codex_proposals/<topic>.md` に diff 案を出して Claude へ引き継ぐ

2. **commit は Claude + User が握る** — Codex は提案 + 雛形のみ、`git commit` を実行しない

3. **Unity Editor の Play モード in/out は Claude 専有** — Editor 状態の主導は 1 役固定

4. **AI review loop で Codex は read-only sandbox 起動必須**:
   ```powershell
   codex exec --sandbox read-only "<review prompt>"
   ```

5. **AGENTS.md の更新は両 agent で同期** — 片方が更新したら他方に通知する pattern (User 経由 or ファイル diff)

6. **「不安・不明・判断つかないこと」は User に即聞く** (CLAUDE.md 原則と同じ)

7. **思い込みで proposal 禁止、まず実測** (`memory/feedback_shader_pixel_first.md` の原則を Codex も厳守)

---

## 必須参照ドキュメント (Codex も読むこと)

| ファイル | 内容 |
|---|---|
| `CLAUDE.md` | 全規約、技術スタック、Must/Must Not、月次マイルストーン |
| `.claude/rules/architecture.md` | MVS / VContainer / MessagePipe / UniTask |
| `.claude/rules/csharp-unity.md` | C# / Unity 命名規約 |
| `.claude/rules/performance.md` | GC / Draw call / 物理 |
| `.claude/rules/serialization.md` | FormerlySerializedAs / Unity serialization 罠 |
| `.claude/rules/unity-specifics.md` | Editor vs Runtime / Lifecycle / `?.` operator 罠 |
| `.claude/skills/ai-review-loop/SKILL.md` | review ループ手順 (Codex が 3rd reviewer 加わる) |
| `docs/ai_playbook/recipes/` | 普遍 recipe (Spring 物理 / Scale pivot offset / Camera fade 等) |

---

## 試運転 (User の最初の Codex 起動)

```powershell
cd C:\Users\taise\Documents\GitHub\ChaosDraftParty
codex login                                  # 初回のみ ChatGPT Plus account 認証 (ブラウザ)
codex --sandbox read-only                    # 対話モード、read-only で起動 (破壊防止)

# 対話プロンプト例:
> AGENTS.md と CLAUDE.md を読んで、現在の M2 ステータスを 5 行で要約してください。
> docs/reports/2026-05-19.md を踏まえ、M2 残タスクの優先度を提案してください。
```

成功すれば Codex が project context を把握。

---

## 試運転 後の活用フロー (案)

### A. balance CSV 生成
```powershell
codex exec "SkillDefinition 10 個の CD / カテゴリ / 効果説明を docs/balance/skill_cd_table.md に表形式で生成。CLAUDE.md の SkillDefinition 表を踏まえる。"
```

### B. docs 起草
```powershell
codex exec "docs/reports/2026-05-21.md を起草。HEAD ~5 commit を要約、今日の Foot 修正 (Plan B') と Codex 導入を時系列で。"
```

### C. AI review loop 3rd reviewer
Claude が `unity-reviewer` → `code-reviser` を回した後:
```powershell
codex exec --sandbox read-only "直近 commit を review してください。観点: null check、boundary case、edge case、CLAUDE.md 規約違反。HIGH / MEDIUM / LOW + PASS/BLOCK 判定で。"
```

---

## 変更履歴

- **2026-05-21**: 初版。User 指示 (案 1 + 案 2 両方、MCP 両方可) で作成。Codex CLI 0.132.0 / Plus 契約済。
