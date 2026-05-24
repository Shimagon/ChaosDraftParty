# Codex Account 2 Bootstrap

Date: 2026-05-25
Purpose: New paid Codex account should continue ChaosDraftParty development with the same project context.

## What Transfers / What Does Not

Does transfer if using the same Windows user:

- Repo files in `C:\Users\taise\Documents\GitHub\ChaosDraftParty`
- Project docs and handoffs
- Local Codex config: `C:\Users\taise\.codex\config.toml`
- Local Codex memory: `C:\Users\taise\.codex\memories\chaosdraftparty_codex_trial.md`
- PowerShell aliases/functions already configured in the user's profile

Does not automatically transfer:

- This exact chat thread context
- Cloud-side Codex task history from the old account
- Any account-specific auth/session state

Therefore the new account must explicitly read this file plus the normal project handoffs at startup.

## Current Best Startup Command

For this user, repeated approval prompts are disliked. Use the existing `co` PowerShell function if available:

```powershell
cd C:\Users\taise\Documents\GitHub\ChaosDraftParty
co
```

`co` should expand to:

```powershell
codex --dangerously-bypass-approvals-and-sandbox
```

Continuation:

```powershell
co -c
```

Expected expansion:

```powershell
codex --dangerously-bypass-approvals-and-sandbox resume --last
```

If `co` is missing, use:

```powershell
cd C:\Users\taise\Documents\GitHub\ChaosDraftParty
codex --dangerously-bypass-approvals-and-sandbox
```

## Login / Account Switch

To switch the Codex CLI auth to the new paid account:

```powershell
codex logout
codex login
codex login status
```

Login opens the browser/device auth flow. Sign in with the new paid OpenAI account.

If the user wants to keep both old and new accounts side-by-side, use a second `CODEX_HOME` instead of overwriting `C:\Users\taise\.codex\auth.json`.

Example side-by-side setup:

```powershell
$env:CODEX_HOME = "C:\Users\taise\.codex-account2"
New-Item -ItemType Directory -Force $env:CODEX_HOME
Copy-Item C:\Users\taise\.codex\config.toml $env:CODEX_HOME\config.toml
Copy-Item C:\Users\taise\.codex\memories $env:CODEX_HOME\memories -Recurse -Force
codex login
codex --dangerously-bypass-approvals-and-sandbox
```

For normal use, a single account switch with `codex logout` / `codex login` is simpler.

## First Prompt For New Codex Account

Paste this into the new Codex session:

```text
あなたは ChaosDraftParty の新しい Codex セッションです。

まず以下を順番に読んで、プロジェクト規約・User好み・直近の操作感調整を復元してください。

1. RULES.md
2. CLAUDE.md
3. AGENTS.md
4. docs/KNOWLEDGE-INDEX.md
5. docs/handoff-claude-to-codex.md
6. docs/handoffs/codex_to_claude_2026-05-24_controller_camera.md
7. docs/handoffs/codex_account2_bootstrap_2026-05-25.md
8. C:\Users\taise\.codex\memories\chaosdraftparty_codex_trial.md
9. .claude/skills/chaosdraftparty-gotchas/SKILL.md
10. .claude/rules/*.md
11. docs/reports/2026-05-23.md

読んだら、以下だけ返してください:
- 「準備OK」
- 今の最重要注意点 5 個
- 次にUserへ確認すべき Play test 項目 3 個

まだコード編集やUnity操作はしないでください。
```

## First Real Task Prompt Template

After the new Codex says ready:

```text
次のタスク: <ここに作業を書く>

必ず守る:
- RULES.md R1-R10
- visual / camera / VFX / game feel は調査・実測してから
- まず git status を見て、既存の未コミット変更を壊さない
- 変更は小さく、ちょこちょこ commit
- Unity console の error/warning を確認
- 操作感で悪かったこと・良かったことを docs/handoffs または Codex memory に記録
```

## Current Project State To Remember

Most important recent result:

- Camera motion sickness was fixed.
- Dust VFX became acceptable/cute.
- Fall gravity change felt good.
- First automatic gamepad camera assist was bad and must remain OFF by default.
- Explicit camera reset is now the safer path.

Relevant latest commits:

```text
741c085 docs: CodexからClaudeへの操作感引き継ぎを追加
4db3c47 fix(camera): 自動追従を切りカメラリセットを追加
2e10892 feat(camera): プロコン向け半自動カメラを追加
bf5530b fix(player): 落下重力を締める
d35096c fix(camera): ジャンプ時の縦揺れを抑制
1e2d2fa fix(camera): ダッシュとジャンプの揺れを停止
```

## Dirty Worktree Warning

There are many unrelated dirty/untracked art/material/CharacterLab files in the worktree.

Do not revert them.
Do not stage them unless explicitly asked.
Only stage files touched for the current task.

## Preferred Operating Style

User preference:

- Japanese casual responses.
- Avoid permission prompts; user prefers bypass session via `co`.
- Be autonomous, but do not be reckless.
- For camera / VFX / game feel, do not assume: research, measure, then implement.
- Commit often in small units.
- Record lessons because the user explicitly wants good/bad outcomes carried forward.

## Next Good Task Candidates

1. Playtest Pro Controller with current explicit camera reset:
   - right stick sensitivity
   - `R` / right shoulder camera reset
   - jump + dash + camera reset
2. If right stick still slow:
   - tune only `gamepadYawSpeed` / `gamepadPitchSpeed`
3. If reset feels bad:
   - tune `cameraResetYawSpeed`
4. Do not re-enable `gamepadCameraAssist` without a settings UI and stricter gating.

