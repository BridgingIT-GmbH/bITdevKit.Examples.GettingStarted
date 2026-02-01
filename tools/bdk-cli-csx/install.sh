#!/usr/bin/env bash
# BDK CLI - Installation Helper
# Installs dotnet-script and sets up PATH

set -e

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  BDK CLI (C# Script) - Installation Helper                ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK is not installed"
    echo ""
    echo "Please install .NET SDK first:"
    echo "  https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Check if dotnet-script is already installed
if dotnet tool list --global | grep -q "dotnet-script"; then
    INSTALLED_VERSION=$(dotnet tool list --global | grep dotnet-script | awk '{print $2}')
    echo "✓ dotnet-script is already installed (version $INSTALLED_VERSION)"
else
    echo "📦 Installing dotnet-script..."
    dotnet tool install -g dotnet-script
    echo ""
fi

# Check PATH configuration
DOTNET_TOOLS_PATH="$HOME/.dotnet/tools"
SHELL_RC=""

if [[ -n "${BASH_VERSION:-}" ]]; then
    SHELL_RC="$HOME/.bashrc"
elif [[ -n "${ZSH_VERSION:-}" ]]; then
    SHELL_RC="$HOME/.zshrc"
fi

if [[ ":$PATH:" != *":$DOTNET_TOOLS_PATH:"* ]]; then
    echo ""
    echo "⚠️  .NET tools directory is not in your PATH"
    echo ""
    
    if [[ -n "$SHELL_RC" ]] && [[ -f "$SHELL_RC" ]]; then
        read -p "Would you like to add it to $SHELL_RC? [y/N] " -n 1 -r
        echo ""
        
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo "" >> "$SHELL_RC"
            echo "# Add .NET Core SDK tools to PATH" >> "$SHELL_RC"
            echo "export PATH=\"\$PATH:\$HOME/.dotnet/tools\"" >> "$SHELL_RC"
            
            echo "✓ Added to $SHELL_RC"
            echo ""
            echo "Please run: source $SHELL_RC"
            echo "Or restart your terminal"
        else
            echo "Skipped. You can manually add this to your shell profile:"
            echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
        fi
    else
        echo "Please add this to your shell profile:"
        echo "  export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    fi
    
    # Temporarily add to current session
    export PATH="$PATH:$DOTNET_TOOLS_PATH"
else
    echo "✓ .NET tools directory is in your PATH"
fi

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Installation Complete!                                    ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "Test the installation:"
echo "  ./bdk-cli.sh version"
echo "  ./bdk-cli.sh --help"
echo ""
echo "Get started:"
echo "  ./bdk-cli.sh              (interactive mode)"
echo "  ./bdk-cli.sh build        (direct execution)"
echo ""
