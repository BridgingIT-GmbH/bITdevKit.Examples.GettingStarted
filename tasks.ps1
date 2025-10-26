<#!
.SYNOPSIS
  Standalone DevKit task runner (Spectre Console) independent of VS Code tasks.json.
.DESCRIPTION
  Provides interactive menus (categories -> actions) for common solution tasks:
    Build / Clean / Tool Restore
    Tests (Unit, Integration, All)
    EF Migrations (list/add/remove/apply/undo/status/reset/script/removeall/info)
    Docker (build/run/stop/remove/compose up/down)
    Security & Formatting (vulnerabilities, outdated, format check/apply, analyzers)
    Coverage, OpenAPI lint, Misc utilities.
  Uses subordinate scripts (tasks-*.ps1) where specialized logic exists.
.PARAMETER Task
  Optional action key to run directly (e.g. build, test-unit, ef-add).
.EXAMPLES
  pwsh -File tasks.ps1              # interactive menus
  pwsh -File tasks.ps1 -Task build  # direct build
  pwsh -File tasks.ps1 -Task ef-add # add EF migration interactively (prompts)
.NOTES
  SpectreConsole required; script will install if missing.
  ANSI unsupported hosts fall back for module selection.
#>
param(
  [string] $Task
)

$ErrorActionPreference = 'Stop'
$env:IgnoreSpectreEncoding = $true

# Load only helper (contains selection utilities); do not dot-source operational scripts to prevent auto execution
$helpersPath = Join-Path $PSScriptRoot '.vscode/tasks-helpers.ps1'
if (Test-Path $helpersPath) { . $helpersPath }

# Attempt to enable VT (ANSI) processing for current Windows console so Spectre can render.
# function Enable-VTSupport {
#   if (-not $IsWindows) { return }
#   $signature = @'
# using System;
# using System.Runtime.InteropServices;
# public static class VTInterop {
#   [DllImport("kernel32.dll", SetLastError=true)] public static extern IntPtr GetStdHandle(int nStdHandle);
#   [DllImport("kernel32.dll", SetLastError=true)] public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
#   [DllImport("kernel32.dll", SetLastError=true)] public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
# }
# '@
#   Add-Type -TypeDefinition $signature -ErrorAction SilentlyContinue | Out-Null
#   $STD_OUTPUT_HANDLE = -11
#   $handle = [VTInterop]::GetStdHandle($STD_OUTPUT_HANDLE)
#   if ($handle -eq [IntPtr]::Zero) { return }
#   if ([VTInterop]::GetConsoleMode($handle, [ref]([uint]$mode = 0))) {
#     $ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004
#     if (($mode -band $ENABLE_VIRTUAL_TERMINAL_PROCESSING) -eq 0) {
#       [void][VTInterop]::SetConsoleMode($handle, ($mode -bor $ENABLE_VIRTUAL_TERMINAL_PROCESSING))
#     }
#   }
# }
# Enable-VTSupport

function Ensure-Spectre {
  if (-not (Get-Module -ListAvailable -Name PwshSpectreConsole)) {
    Install-Module -Name PwshSpectreConsole -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
  }
  Import-Module PwshSpectreConsole -Force
}
Ensure-Spectre

function Read-Selection($title,[string[]]$choices){
  $choices += 'Cancel'
  try {
    # Write-Host "[Diag] Prompting selection: $title" -ForegroundColor DarkGray
    $s = Read-SpectreSelection -Title $title -Choices $choices -EnableSearch -PageSize 15
  } catch {
    Write-Host "Spectre selection failed: $($_.Exception.Message)" -ForegroundColor Red
    # Fallback minimal selection (numbered) to keep interaction (still manual selection, no auto-run)
    for ($i=0; $i -lt $choices.Count; $i++){ Write-Host "[$i] $($choices[$i])" }
    $raw = Read-Host "Enter index (or blank to cancel)"
    if (-not $raw) { return $null }
    if ($raw -notmatch '^[0-9]+$') { Write-Host 'Invalid index.' -ForegroundColor Red; return $null }
    $idx = [int]$raw
    if ($idx -lt 0 -or $idx -ge $choices.Count){ Write-Host 'Index out of range.' -ForegroundColor Red; return $null }
    $s = $choices[$idx]
  }
  if (-not $s) {
    Write-Host '[Diag] Spectre returned empty selection; switching to numeric fallback.' -ForegroundColor DarkYellow
    for ($i=0; $i -lt $choices.Count; $i++){ Write-Host "[$i] $($choices[$i])" }
    $raw = Read-Host "Enter index (or blank to cancel)"
    if (-not $raw) { return $null }
    if ($raw -notmatch '^[0-9]+$') { Write-Host 'Invalid index.' -ForegroundColor Red; return $null }
    $idx = [int]$raw
    if ($idx -lt 0 -or $idx -ge $choices.Count){ Write-Host 'Index out of range.' -ForegroundColor Red; return $null }
    $s = $choices[$idx]
  }
  if ($s -eq 'Cancel') { return $null }
  return $s
}

function Invoke-Build { dotnet build "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" --nologo }
function Invoke-Clean { dotnet clean "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" --nologo }
function Invoke-ToolRestore { dotnet tool restore }
function Invoke-FormatCheck { dotnet format "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" --verify-no-changes }
function Invoke-FormatApply { dotnet format "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" }
function Invoke-Vulnerabilities { dotnet list "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" package --vulnerable }
function Invoke-Outdated { dotnet list "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" package --outdated }
function Invoke-Analyzers { dotnet build "$PSScriptRoot/BridgingIT.DevKit.Examples.GettingStarted.sln" -warnaserror /p:RunAnalyzers=true /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest }
function Invoke-Coverage { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-coverage.ps1') }
function Invoke-OpenApiLint { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-openapi.ps1') lint }
function Invoke-MiscClean { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-misc.ps1') clean }
function Invoke-MiscCombine { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-misc.ps1') combine-sources }

# Docker wrapper functions (script supplies logic)
function Invoke-Docker([string]$mode){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-docker.ps1') $mode }

# EF wrapper (call original script with command; interactive inside)
function Invoke-Ef([string]$efCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-ef.ps1') $efCmd }

# Tests wrapper: delegate to script (script handles selection)
function Invoke-Test([string]$kind,[switch]$All){
  if ($All) { $env:TEST_MODULE='All' }
  & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-tests.ps1') $kind
  if ($All) { Remove-Item Env:TEST_MODULE -ErrorAction SilentlyContinue }
}

$actions = [ordered]@{
  'build' = @{ Label='Build Solution'; Script={ Invoke-Build } }
  'clean' = @{ Label='Clean Solution'; Script={ Invoke-Clean } }
  'tool-restore' = @{ Label='Restore Dotnet Tools'; Script={ Invoke-ToolRestore } }
  'test-unit' = @{ Label='Tests Unit (select)'; Script={ Invoke-Test 'unit' } }
  'test-int' = @{ Label='Tests Integration (select)'; Script={ Invoke-Test 'integration' } }
  'test-unit-all' = @{ Label='Tests Unit All'; Script={ Invoke-Test 'unit' -All } }
  'test-int-all' = @{ Label='Tests Integration All'; Script={ Invoke-Test 'integration' -All } }
  'coverage' = @{ Label='Coverage (all)'; Script={ Invoke-Coverage } }
  'ef-info' = @{ Label='EF DbContext Info'; Script={ Invoke-Ef 'info' } }
  'ef-list' = @{ Label='EF Migrations List'; Script={ Invoke-Ef 'list' } }
  'ef-add' = @{ Label='EF Migration Add'; Script={ Invoke-Ef 'add' } }
  'ef-remove' = @{ Label='EF Migration Remove'; Script={ Invoke-Ef 'remove' } }
  'ef-removeall' = @{ Label='EF Migrations Remove All'; Script={ Invoke-Ef 'removeall' } }
  'ef-apply' = @{ Label='EF Apply Migrations'; Script={ Invoke-Ef 'apply' } }
  'ef-undo' = @{ Label='EF Undo Migration'; Script={ Invoke-Ef 'undo' } }
  'ef-status' = @{ Label='EF Migration Status'; Script={ Invoke-Ef 'status' } }
  'ef-reset' = @{ Label='EF Reset (Squash)'; Script={ Invoke-Ef 'reset' } }
  'ef-script' = @{ Label='EF Export SQL Script'; Script={ Invoke-Ef 'script' } }
  'docker-build-run' = @{ Label='Docker Build & Run'; Script={ Invoke-Docker 'docker-build-run' } }
  'docker-build' = @{ Label='Docker Build'; Script={ Invoke-Docker 'docker-build' } }
  'docker-run' = @{ Label='Docker Run'; Script={ Invoke-Docker 'docker-run' } }
  'docker-stop' = @{ Label='Docker Stop'; Script={ Invoke-Docker 'docker-stop' } }
  'docker-remove' = @{ Label='Docker Remove'; Script={ Invoke-Docker 'docker-remove' } }
  'compose-up' = @{ Label='Docker Compose Up'; Script={ Invoke-Docker 'compose-up' } }
  'compose-up-pull' = @{ Label='Docker Compose Up & Pull'; Script={ Invoke-Docker 'compose-up' ; Invoke-Docker 'compose-up' -Pull } }
  'compose-down' = @{ Label='Docker Compose Down'; Script={ Invoke-Docker 'compose-down' } }
  'compose-down-clean' = @{ Label='Docker Compose Down & Clean'; Script={ Invoke-Docker 'compose-down-clean' } }
  'vulnerabilities' = @{ Label='List Vulnerable Packages'; Script={ Invoke-Vulnerabilities } }
  'outdated' = @{ Label='List Outdated Packages'; Script={ Invoke-Outdated } }
  'format-check' = @{ Label='Format Check'; Script={ Invoke-FormatCheck } }
  'format-apply' = @{ Label='Format Apply'; Script={ Invoke-FormatApply } }
  'analyzers' = @{ Label='Analyzers Report'; Script={ Invoke-Analyzers } }
  'openapi-lint' = @{ Label='OpenAPI Lint'; Script={ Invoke-OpenApiLint } }
  'misc-clean' = @{ Label='Misc Clean Workspace'; Script={ Invoke-MiscClean } }
  'misc-combine' = @{ Label='Misc Combine Sources'; Script={ Invoke-MiscCombine } }
}

$categories = [ordered]@{
  'Solution' = @('build','clean','tool-restore','format-check','format-apply','analyzers')
  'Tests'    = @('test-unit','test-int','test-unit-all','test-int-all','coverage')
  'EF'       = @('ef-info','ef-list','ef-add','ef-remove','ef-removeall','ef-apply','ef-undo','ef-status','ef-reset','ef-script')
  'Docker'   = @('docker-build-run','docker-build','docker-run','docker-stop','docker-remove','compose-up','compose-up-pull','compose-down','compose-down-clean')
  'Security' = @('vulnerabilities','outdated')
  'OpenAPI'  = @('openapi-lint')
  'Misc'     = @('misc-clean','misc-combine')
}

function Run-Action([string]$key){
  if (-not $actions.Contains($key)) { Write-Host "Unknown action '$key'" -ForegroundColor Red; return }
  $def = $actions[$key]
  Write-Host "\n=== $($def.Label) ===" -ForegroundColor Cyan
  & $def.Script
  if ($LASTEXITCODE -ne 0) { Write-Host "Action '$key' failed (exit $LASTEXITCODE)" -ForegroundColor Red } else { Write-Host "Action '$key' completed." -ForegroundColor Green }
}

if ($Task) { Run-Action $Task; exit $LASTEXITCODE }

while ($true) {
  $cat = Read-Selection 'Select Category' $categories.Keys
  if (-not $cat) { break }
  $actionKeys = $categories[$cat]
  # Present labels only; map back by index to avoid Spectre style parsing collisions
  $choices = $actionKeys | ForEach-Object { $actions[$_].Label }
  $selection = Read-Selection "Select Action ($cat)" $choices
  if (-not $selection) { continue }
  $selectedIndex = $choices.IndexOf($selection)
  if ($selectedIndex -lt 0) { Write-Host 'Invalid selection mapping.' -ForegroundColor Red; continue }
  $selectedKey = $actionKeys[$selectedIndex]
  Run-Action $selectedKey
}

Write-Host 'Exiting task runner.' -ForegroundColor DarkGray
