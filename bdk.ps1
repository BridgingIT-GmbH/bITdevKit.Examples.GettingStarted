
#!/usr/bin/env pwsh
# BDK CLI Launcher (PowerShell) - C# Script Version
# Runs the BDK CLI tool using dotnet-script

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ToolPath = Join-Path $ScriptDir ".bdk/cli/bdk-cli.csx"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK (dotnet) was not found on PATH" -ForegroundColor Red
    Write-Host "Install it from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Prefer the repo-local tool (.config/dotnet-tools.json), fall back to a global install
$ErrorActionPreference = "Continue"
dotnet dotnet-script --version *> $null
$hasLocal = ($LASTEXITCODE -eq 0)
if (-not $hasLocal) {
    [Console]::Error.WriteLine("dotnet-script not found, running 'dotnet tool restore'...")
    dotnet tool restore | ForEach-Object { [Console]::Error.WriteLine($_) }
    dotnet dotnet-script --version *> $null
    $hasLocal = ($LASTEXITCODE -eq 0)
}
$ErrorActionPreference = "Stop"

if ($hasLocal) {
    $Runner = "dotnet"
    $RunnerArgs = @("dotnet-script")
}
elseif (Get-Command dotnet-script -ErrorAction SilentlyContinue) {
    $Runner = "dotnet-script"
    $RunnerArgs = @()
}
else {
    Write-Host "ERROR: dotnet-script is not available and 'dotnet tool restore' did not provide it" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check that .config/dotnet-tools.json lists dotnet-script and that NuGet is reachable," -ForegroundColor Yellow
    Write-Host "or install it globally:" -ForegroundColor Yellow
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
& $Runner @RunnerArgs $ToolPath -- @Arguments
exit $LASTEXITCODE
