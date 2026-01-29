#!/usr/bin/env pwsh
#Requires -Version 7.0

# BDK TUI Launcher (PowerShell)
# Runs the BDK TUI tool with Bun

$ErrorActionPreference = 'Stop'
$ScriptRoot = Split-Path -Path $MyInvocation.MyCommand.Path -Parent

# Check if Bun is installed
if (-not (Get-Command bun -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Bun is not installed" -ForegroundColor Red
    Write-Host ""
    Write-Host "To install Bun:" -ForegroundColor Yellow
    Write-Host "  Windows: powershell -c `"irm bun.sh/install.ps1 | iex`""
    Write-Host "  Linux/macOS: curl -fsSL https://bun.sh/install | bash"
    exit 1
}

# Run the TUI tool
$ToolPath = Join-Path $ScriptRoot "tools/bdk-tui/src/index.ts"

if (-not (Test-Path $ToolPath)) {
    Write-Host "ERROR: BDK TUI not found at: $ToolPath" -ForegroundColor Red
    exit 1
}

# Execute with Bun, passing all arguments
& bun run $ToolPath @args
exit $LASTEXITCODE
