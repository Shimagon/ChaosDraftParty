#!/usr/bin/env bash
# ai-review-loop runner (option, manual / pre-commit hook 用)
#
# 用途:
#   - Claude Code CLI 経由で ai-review-loop skill を起動するラッパー
#   - pre-commit hook / GitHub Action から呼びたくなった時に使う
#   - 通常運用は Claude Code 内で「AI review loop 回して」と頼むだけで OK
#
# 前提:
#   - Claude Code CLI が PATH に入っている
#   - 本リポジトリのルートで実行（cwd 自動検出）
#   - 認証は Claude Code に任せる（ANTHROPIC_API_KEY を直接使わない）
#
# 使い方:
#   ./tools/ai-review-loop/run.sh           # pending 変更をレビュー
#   ./tools/ai-review-loop/run.sh --staged  # staged 変更のみ
#   ./tools/ai-review-loop/run.sh --last    # 直近 commit をレビュー
#
# 出力:
#   標準出力にレポート、exit 0=PASS、exit 1=BLOCK、exit 2=DEFERRED（人間判断）

set -euo pipefail

REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$REPO_ROOT"

MODE="${1:-pending}"
case "$MODE" in
  --staged) DIFF_CMD="git diff --cached --name-only" ;;
  --last)   DIFF_CMD="git diff --name-only HEAD~1 HEAD" ;;
  pending|"") DIFF_CMD="git diff --name-only HEAD" ;;
  *) echo "Unknown mode: $MODE"; exit 2 ;;
esac

CHANGED=$($DIFF_CMD | grep -E '\.(cs|asmdef|asset|prefab|unity|meta)$' || true)

if [ -z "$CHANGED" ]; then
  echo "[ai-review-loop] No reviewable changes. Skipping."
  exit 0
fi

echo "[ai-review-loop] Target files:"
echo "$CHANGED" | sed 's/^/  - /'
echo ""

if ! command -v claude >/dev/null 2>&1; then
  echo "[ai-review-loop] ERROR: claude CLI not found in PATH."
  echo "  Install Claude Code or run the loop interactively from inside Claude Code session."
  exit 2
fi

PROMPT=$(cat <<EOF
Run the ai-review-loop skill on the following changed files (max 2 iterations, headless):

$CHANGED

Mode: $MODE

Output a single final report at the end with:
- Final verdict (PASS / PASS w/ nits / BLOCK)
- HIGH / MEDIUM remaining count
- Recommendation
EOF
)

# headless mode で Claude Code CLI を叩く（-p / --print で 1 ターン実行）
claude -p "$PROMPT" --permission-mode acceptEdits

EXIT=$?
exit $EXIT
