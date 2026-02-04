#!/usr/bin/env pwsh
# BDK CLI Launcher (PowerShell) - C# Script Version
# Runs the BDK CLI tool using dotnet-script

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ToolPath = Join-Path $ScriptDir "tools/bdk-cli-csx/bdk-cli.csx"

# Check if dotnet-script is installed
$dotnetScriptCheck = dotnet tool list --global | Select-String "dotnet-script"

if (-not $dotnetScriptCheck) {
    Write-Host "ERROR: dotnet-script is not installed" -ForegroundColor Red
    Write-Host ""
    Write-Host "To install dotnet-script:" -ForegroundColor Yellow
    Write-Host "  dotnet tool install -g dotnet-script" -ForegroundColor Cyan
    exit 1
}

# Check if the tool exists
if (-not (Test-Path $ToolPath)) {
    Write-Host "ERROR: BDK CLI not found at: $ToolPath" -ForegroundColor Red
    exit 1
}

# Execute with dotnet-script, using -- to separate script args
# This prevents dotnet-script from intercepting flags like --help
& dotnet script $ToolPath -- @Arguments
exit $LASTEXITCODE
