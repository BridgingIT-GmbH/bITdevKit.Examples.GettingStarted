#!/usr/bin/env bash
# BDK TUI Launcher (Bash)
# Runs the BDK TUI tool with Bun

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Check if Bun is installed
BUN_PATH="${HOME}/.bun/bin/bun"
if [[ ! -f "$BUN_PATH" ]]; then
    if ! command -v bun &> /dev/null; then
        echo "ERROR: Bun is not installed"
        echo ""
        echo "To install Bun:"
        echo "  curl -fsSL https://bun.sh/install | bash"
        exit 1
    fi
    BUN_PATH="bun"
fi

# Run the TUI tool
TOOL_PATH="$SCRIPT_DIR/tools/bdk-tui/src/index.ts"

if [[ ! -f "$TOOL_PATH" ]]; then
    echo "ERROR: BDK TUI not found at: $TOOL_PATH"
    exit 1
fi

# Execute with Bun, passing all arguments
exec "$BUN_PATH" run "$TOOL_PATH" "$@"
