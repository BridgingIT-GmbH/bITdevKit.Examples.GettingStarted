#!/usr/bin/env pwsh
# BDK CLI - Installation Helper
# Installs dotnet-script and sets up PATH

$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════════════════╗"
Write-Host "║  BDK CLI (C# Script) - Installation Helper                ║"
Write-Host "╚════════════════════════════════════════════════════════════╝"
Write-Host ""

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK is not installed"
    Write-Host ""
    Write-Host "Please install .NET SDK first:"
    Write-Host "  https://dotnet.microsoft.com/download"
    exit 1
}

$dotnetVersion = & dotnet --version
Write-Host "✓ .NET SDK found: $dotnetVersion"
Write-Host ""

# Check if dotnet-script is already installed
$dotnetToolList = & dotnet tool list --global
if ($dotnetToolList -match "dotnet-script") {
    $installedVersion = ($dotnetToolList -split "`n" | Where-Object { $_ -match "dotnet-script" } | ForEach-Object { ($_ -split "\s+")[1] })
    Write-Host "✓ dotnet-script is already installed (version $installedVersion)"
}
else {
    Write-Host "📦 Installing dotnet-script..."
    & dotnet tool install -g dotnet-script
    Write-Host ""
}

# Check PATH configuration
$dotnetToolsPath = Join-Path $HOME ".dotnet\tools"
$pathEntries = $env:PATH -split ";"

if (-not ($pathEntries -contains $dotnetToolsPath)) {
    Write-Host ""
    Write-Host "⚠️  .NET tools directory is not in your PATH"
    Write-Host ""

    $profilePath = $PROFILE.CurrentUserAllHosts
    $shouldWriteProfile = $false

    if ($profilePath -and (Test-Path $profilePath)) {
        $response = Read-Host "Would you like to add it to $profilePath? [y/N]"
        if ($response -match "^[Yy]$") {
            $shouldWriteProfile = $true
        }
    }

    if ($shouldWriteProfile) {
        $profileLine = '$env:PATH = "' + '$env:PATH' + ';' + $dotnetToolsPath + '"'
        Add-Content -Path $profilePath -Value ""
        Add-Content -Path $profilePath -Value "# Add .NET Core SDK tools to PATH"
        Add-Content -Path $profilePath -Value $profileLine
        Write-Host "✓ Added to $profilePath"
        Write-Host ""
        Write-Host "Please run: . $profilePath"
        Write-Host "Or restart your terminal"
    }
    else {
        Write-Host "Skipped. You can manually add this to your PowerShell profile:"
        $manualLine = '  $env:PATH = "' + '$env:PATH' + ';' + $dotnetToolsPath + '"'
        Write-Host $manualLine
    }

    # Temporarily add to current session
    $env:PATH = "$env:PATH;$dotnetToolsPath"
}
else {
    Write-Host "✓ .NET tools directory is in your PATH"
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════╗"
Write-Host "║  Installation Complete!                                    ║"
Write-Host "╚════════════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "Test the installation:"
Write-Host "  ./bdk-cli.ps1 version"
Write-Host "  ./bdk-cli.ps1 --help"
Write-Host ""
Write-Host "Get started:"
Write-Host "  ./bdk-cli.ps1              (interactive mode)"
Write-Host "  ./bdk-cli.ps1 build        (direct execution)"
Write-Host ""
