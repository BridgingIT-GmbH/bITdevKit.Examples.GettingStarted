#!/usr/bin/env bash
# bdk.sh
# Bootstrap for running the BDK CLI C# script

set -e

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CSX_SCRIPT="$SCRIPT_DIR/tools/bdk-cli-csx/bdk-cli.csx"

# Check if the C# script exists
if [ ! -f "$CSX_SCRIPT" ]; then
    echo "Error: Cannot find bdk-cli.csx at $CSX_SCRIPT"
    exit 2
fi

# Add dotnet tools to PATH if not already
if ! echo "$PATH" | grep -q "$HOME/.dotnet/tools"; then
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

# Run the C# script with all arguments passed through
dotnet script "$CSX_SCRIPT" "$@"
exit $?
