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

function Ensure-PSMenuAvailable {
  param([switch] $Quiet)
  $moduleName = 'PSMenu'
  if (Get-Module -ListAvailable -Name $moduleName) {
    if (-not $Quiet) { Write-DevKitInfo "PSMenu module available." }
  } else {
    if (-not $Quiet) { Write-DevKitWarn "PSMenu module not found. Installing (CurrentUser)..." }
    try {
      Install-Module -Name $moduleName -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
      if (-not $Quiet) { Write-DevKitInfo "PSMenu installation successful." }
    } catch {
      Write-DevKitError "Failed to install PSMenu: $($_.Exception.Message). Falling back to basic selection."; return $false
    }
  }
  try { Import-Module $moduleName -Force; return $true } catch { Write-DevKitError "Failed to import PSMenu: $($_.Exception.Message)."; return $false }
}

function Select-DevKitModule {
  param(
    [string[]] $Available,
    [string] $Requested,
    [string] $EnvVarName = 'TEST_MODULE',
    [switch] $AllowAll,
    [switch] $NonInteractive
  )
  if (-not $Available -or $Available.Count -eq 0) { throw [System.Exception] 'No modules discovered.' }

  # Resolve requested precedence: explicit param > env var > (interactive / default)
  $envValue = if ($EnvVarName) { (Get-Item -Path Env:$EnvVarName -ErrorAction SilentlyContinue).Value } else { $null }
  $selection = if ($Requested) { $Requested } elseif ($envValue) { $envValue } else { $null }

  if ($selection -and $AllowAll -and $selection -ieq 'All') { return 'All' }
  if ($selection -and $selection -in $Available) { return $selection }
  if ($selection -and $selection -notin $Available) { Write-DevKitWarn "Requested module '$selection' not in available set: $($Available -join ', '). Ignoring."; $selection = $null }

  if ($NonInteractive) {
    $auto = $Available[0]
    Write-DevKitWarn "NonInteractive mode: auto-selected module '$auto'"; return $auto
  }

  # Try PSMenu interactive selection
  $psmenuOk = Ensure-PSMenuAvailable -Quiet
  if ($psmenuOk) {
    $menuItems = @()
    for ($i=0; $i -lt $Available.Count; $i++){ $menuItems += "[$i] $($Available[$i])" }
    if ($AllowAll) { $menuItems += '[A] All Modules' }
    Write-Host "Use arrow keys / Enter to choose a module:" -ForegroundColor Cyan
    $chosen = Show-Menu -MenuItems $menuItems
  if (-not $chosen) { return $null }
    if ($AllowAll -and $chosen.StartsWith('[A]')) { return 'All' }
    if ($chosen -match '^\[(?<idx>\d+)\]') {
      $idx = [int]$Matches.idx
      if ($idx -ge 0 -and $idx -lt $Available.Count) { return $Available[$idx] }
    }
  return $null
  }

  # Fallback basic prompt selection
  Write-Host 'Select Module:' -ForegroundColor Cyan
  for ($i=0; $i -lt $Available.Count; $i++){ Write-Host "  [$i] $($Available[$i])" }
  if ($AllowAll) { Write-Host '  [A] All' }
  $raw = Read-Host 'Enter choice'
  if ($AllowAll -and $raw -eq 'A') { return 'All' }
  if ($raw -match '^[0-9]+$') {
    $idx=[int]$raw; if ($idx -ge 0 -and $idx -lt $Available.Count) { return $Available[$idx] }
  }
  return $null
}

# Dot-sourced script: no Export-ModuleMember needed.
