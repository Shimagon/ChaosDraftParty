#!/bin/zsh
# EGGYBOT 一発 preview：Blender GUI 開かずに build + 6 方向 Cycles render → Finder で開く
#
# Usage:
#   tools/blender/eggybot_preview.sh
#
# Output:
#   tools/blender/preview/preview_0{1..6}_*.png
#   Finder で preview/ ディレクトリが自動で開く

set -e

SCRIPT_DIR="$(cd "$(dirname "${(%):-%N}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BLENDER="/Applications/Blender.app/Contents/MacOS/Blender"

echo "[1/3] Building EGGYBOT + rendering 6 angles..."
"$BLENDER" --background \
    --python "$SCRIPT_DIR/build_eggybot.py" \
    --python "$SCRIPT_DIR/render_eggybot_previews.py" 2>&1 | \
    grep -E "(Rendered:|Error|Traceback|=== EGGYBOT)" || true

PREVIEW_DIR="$SCRIPT_DIR/preview"
echo "[2/3] Preview PNGs at: $PREVIEW_DIR"
ls -lh "$PREVIEW_DIR"/*.png 2>/dev/null | awk '{print "  " $9, $5}'

echo "[3/3] Opening preview folder in Finder..."
open "$PREVIEW_DIR"
