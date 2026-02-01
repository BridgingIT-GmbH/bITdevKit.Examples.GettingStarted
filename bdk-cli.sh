#!/usr/bin/env bash
# BDK CLI Launcher (Bash) - C# Script Version
# Runs the BDK CLI tool using dotnet-script

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOOL_PATH="$SCRIPT_DIR/tools/bdk-cli-csx/bdk-cli.csx"

# Add .NET tools to PATH if not already present
DOTNET_TOOLS_PATH="$HOME/.dotnet/tools"
if [[ -d "$DOTNET_TOOLS_PATH" ]] && [[ ":$PATH:" != *":$DOTNET_TOOLS_PATH:"* ]]; then
    export PATH="$PATH:$DOTNET_TOOLS_PATH"
fi

# Check if dotnet-script is installed
if ! command -v dotnet-script &> /dev/null; then
    if ! dotnet tool list --global | grep -q "dotnet-script"; then
        echo "ERROR: dotnet-script is not installed"
        echo ""
        echo "To install dotnet-script:"
        echo "  dotnet tool install -g dotnet-script"
        echo ""
        echo "After installation, add to your shell profile:"
        echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
        exit 1
    fi
fi

# Check if the tool exists
if [[ ! -f "$TOOL_PATH" ]]; then
    echo "ERROR: BDK CLI not found at: $TOOL_PATH"
    exit 1
fi

# Execute with dotnet-script, passing all arguments
# Use -- to separate dotnet-script options from script arguments
exec dotnet script "$TOOL_PATH" -- "$@"
