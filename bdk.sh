#!/usr/bin/env bash
# BDK CLI Launcher (Bash) - C# Script Version
# Runs the BDK CLI tool using dotnet-script

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOOL_PATH="$SCRIPT_DIR/.bdk/cli/bdk-cli.csx"

# Add .NET tools to PATH if not already present
DOTNET_TOOLS_PATH="$HOME/.dotnet/tools"
if [[ -d "$DOTNET_TOOLS_PATH" ]] && [[ ":$PATH:" != *":$DOTNET_TOOLS_PATH:"* ]]; then
    export PATH="$PATH:$DOTNET_TOOLS_PATH"
fi

if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK (dotnet) was not found on PATH" >&2
    echo "Install it from https://dotnet.microsoft.com/download" >&2
    exit 1
fi

# Prefer the repo-local tool (.config/dotnet-tools.json), fall back to a global install
if ! dotnet dotnet-script --version &> /dev/null; then
    echo "dotnet-script not found, running 'dotnet tool restore'..." >&2
    dotnet tool restore >&2 || true
fi

if dotnet dotnet-script --version &> /dev/null; then
    RUNNER=(dotnet dotnet-script)
elif command -v dotnet-script &> /dev/null; then
    RUNNER=(dotnet-script)
else
    echo "ERROR: dotnet-script is not available and 'dotnet tool restore' did not provide it" >&2
    echo "" >&2
    echo "Check that .config/dotnet-tools.json lists dotnet-script and that NuGet is reachable," >&2
    echo "or install it globally:" >&2
    echo "  dotnet tool install -g dotnet-script" >&2
    echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\"" >&2
    exit 1
fi

# Check if the tool exists
if [[ ! -f "$TOOL_PATH" ]]; then
    echo "ERROR: BDK CLI not found at: $TOOL_PATH"
    exit 1
fi

# Execute with dotnet-script, passing all arguments
# Use -- to separate dotnet-script options from script arguments
exec "${RUNNER[@]}" "$TOOL_PATH" -- "$@"
