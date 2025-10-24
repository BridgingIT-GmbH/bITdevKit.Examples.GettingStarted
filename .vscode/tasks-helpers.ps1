<#!
.SYNOPSIS
  Shared helper functions for DevKit task scripts (module discovery & interactive selection).
.DESCRIPTION
  Provides reusable PowerShell functions:
    Get-DevKitModules      -> discovers modules under src/Modules (excludes Common/Shared patterns)
    Select-DevKitModule    -> resolves a module name (supports 'All') via env var, argument or interactive PSMenu
    Ensure-PSMenuAvailable -> installs/imports PSMenu when interactive selection required
.NOTES
  Environment variables:
    TEST_MODULE  -> preferred module name or 'All' for test tasks
    EF_MODULE    -> preferred module name for EF tasks
  These functions intentionally avoid throwing where graceful fallback improves UX.
#>

$script:DevKitHelpersVersion = '1.0.0'

function Write-DevKitStep([string] $Msg){ Write-Host "-- $Msg" -ForegroundColor DarkCyan }
function Write-DevKitInfo([string] $Msg){ Write-Host $Msg -ForegroundColor Green }
function Write-DevKitWarn([string] $Msg){ Write-Host $Msg -ForegroundColor DarkYellow }
function Write-DevKitError([string] $Msg){ Write-Host $Msg -ForegroundColor Red }

function Get-DevKitModules {
  param(
    [string] $Root = (Split-Path $PSScriptRoot -Parent)
  )
  $modulesRoot = Join-Path $Root 'src/Modules'
  if (-not (Test-Path $modulesRoot)) { return @() }
  $names = Get-ChildItem -Path $modulesRoot -Directory | ForEach-Object { $_.Name }
  $filtered = $names | Where-Object { $_ -notmatch '^(?:Common|Shared)$' }
  [string[]]$filtered
}

function Ensure-SpectreConsoleAvailable {
  param([switch] $Quiet)
  $moduleName = 'PwshSpectreConsole'
  $env:IgnoreSpectreEncoding = $true
  if (Get-Module -ListAvailable -Name $moduleName) {
    if (-not $Quiet) { Write-DevKitInfo "SpectreConsole module available." }
  } else {
    if (-not $Quiet) { Write-DevKitWarn "SpectreConsole module not found. Installing (CurrentUser)..." }
    try {
      Install-Module -Name $moduleName -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
      if (-not $Quiet) { Write-DevKitInfo "SpectreConsole module installation successful." }
    } catch {
      Write-DevKitError "Failed to install SpectreConsole module: $($_.Exception.Message)."; throw [System.Exception] "SpectreConsole install failed"
    }
  }
  try { Import-Module $moduleName -Force; return $true } catch { Write-DevKitError "Failed to import SpectreConsole module: $($_.Exception.Message)."; throw [System.Exception] "SpectreConsole import failed" }
}

function Select-DevKitModule {
  param(
    [string[]] $Available,
    [string] $Requested,
    [string] $EnvVarName = 'TEST_MODULE',
    [switch] $AllowAll
  )
  Write-Host "------------------------------"
  if (-not $Available -or $Available.Count -eq 0) { throw [System.Exception] 'No modules discovered.' }
  if ($Available.Count -eq 1 -and -not $AllowAll) { return $Available[0] }

  # Resolve requested precedence: explicit param > env var > interactive
  $envValue = if ($EnvVarName) { (Get-Item -Path Env:$EnvVarName -ErrorAction SilentlyContinue).Value } else { $null }
  $selection = if ($Requested) { $Requested } elseif ($envValue) { $envValue } else { $null }
  if ($selection -and $AllowAll -and $selection -ieq 'All') { return 'All' }
  if ($selection -and $selection -in $Available) { return $selection }
  if ($selection -and $selection -notin $Available) { Write-DevKitWarn "Requested module '$selection' not in available set: $($Available -join ', '). Ignoring."; $selection = $null }

  # Require SpectreConsole (no fallback)
  if (-not (Ensure-SpectreConsoleAvailable -Quiet)) { throw [System.Exception] 'SpectreConsole module required but not available.' }
  $choices = @()
  if ($AllowAll) { $choices += 'All' }
  $choices += $Available
  $choices += 'Cancel'
  $selected = Read-SpectreSelection -Title 'Select Module' -Choices $choices -EnableSearch
  if (-not $selected -or $selected -eq 'Cancel') { throw [System.OperationCanceledException] 'Module selection cancelled.' }
  if ($AllowAll -and $selected -eq 'All') { return 'All' }
  if ($selected -in $Available) { return $selected }
  throw [System.Exception] "Unexpected selection '$selected'"
}

# Dot-sourced script: no Export-ModuleMember needed.
