<#!
.SYNOPSIS
  Dynamic test runner for module unit/integration tests with optional All selection.
.DESCRIPTION
  Discovers modules under src/Modules and runs dotnet test for the selected module's UnitTests or IntegrationTests project.
  Supports environment variable TEST_MODULE for non-interactive selection. Value 'All' tests all modules sequentially.
.NOTES
  Usage examples:
    pwsh -File .vscode/tasks-tests.ps1 unit
    pwsh -File .vscode/tasks-tests.ps1 unit -Module CoreModule
    TEST_MODULE=CoreModule pwsh -File .vscode/tasks-tests.ps1 integration
    TEST_MODULE=All pwsh -File .vscode/tasks-tests.ps1 unit
#>
param(
  [Parameter(Position=0)] [ValidateSet('unit','integration','help')] [string] $Kind = 'help',
  [Parameter()] [string] $Module,
  [Parameter()] [switch] $Coverage,
  [Parameter()] [switch] $FailFast
)

$script:TestModule = $Module

$ErrorActionPreference = 'Stop'
function Fail([string] $Msg, [int] $Code=1){ Write-Error $Msg; exit $Code }
function Step([string] $Msg){ Write-Host "-- $Msg" -ForegroundColor DarkCyan }
function Section([string] $Msg){ Write-Host "`n=== $Msg ===" -ForegroundColor Cyan }

function Get-Modules() {
  $repoRoot = Split-Path $PSScriptRoot -Parent
  $modulesRoot = Join-Path $repoRoot 'src/Modules'
  if (-not (Test-Path $modulesRoot)) { return @() }
  $dirs = @()
  foreach ($d in (Get-ChildItem -Path $modulesRoot -Directory)) { $dirs += $d.Name }
  $filtered = @()
  foreach ($n in $dirs) { if ($n -notmatch '^(?:Common|Shared)$') { $filtered += $n } }
  return $filtered
}

function Resolve-Module() {
  if (-not $script:TestModule) { $script:TestModule = $env:TEST_MODULE }
  [string[]]$available = Get-Modules
  if (-not $available -or $available.Count -eq 0) { Fail 'No modules discovered.' 50 }
  Write-Host "DEBUG available type: $($available.GetType().FullName) Count: $($available.Count)" -ForegroundColor DarkGray
  Write-Host "DEBUG available raw: $available" -ForegroundColor DarkGray
  if (-not $script:TestModule) {
    if ($Host.Name -eq 'Visual Studio Code Host') {
      $script:TestModule = $available[0]
      Write-Host "Auto-selected first module (VS Code non-interactive): $script:TestModule" -ForegroundColor DarkYellow
    } else {
    Write-Host 'Select Module (index or All):' -ForegroundColor Cyan
    $idx = 0
    foreach ($name in $available) {
      $n = $name.ToString()
      Write-Host "  [$idx] $n" -NoNewline
      Write-Host " (len=$($n.Length))" -ForegroundColor DarkGray
      $idx++
    }
    Write-Host '  [A] All'
    $sel = Read-Host 'Enter choice'
    if ($sel -eq 'A') {
      $script:TestModule = 'All'
    } elseif ($sel -match '^[0-9]+$' -and [int]$sel -lt $available.Count) {
      $index = [int]$sel
      $selectedValue = [string]($available[$index])
      Write-Host "Selected index $index -> $selectedValue" -ForegroundColor DarkGray
      $script:TestModule = $selectedValue
    } else {
      $script:TestModule = [string]$available[0]
      Write-Host "Defaulting to first module: $script:TestModule" -ForegroundColor DarkYellow
    }
    }
  }
  if ($script:TestModule -ne 'All' -and $available -notcontains $script:TestModule) { Fail "Specified module '$script:TestModule' not found. Available: $($available -join ', ')" 51 }
  Write-Host "Resolved Test Module: $script:TestModule" -ForegroundColor Green
  $Module = $script:TestModule
  return $available
}

function Build-TestProjectPath([string] $ModuleName, [string] $kind){
  $proj = "tests/Modules/$ModuleName/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' })/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' }).csproj"
  if (-not (Test-Path $proj)) { Fail "Test project not found: $proj" 60 }
  return $proj
}

function Run-TestsForModule([string] $ModuleName){
  $proj = Build-TestProjectPath $ModuleName $Kind
  Section "Testing $Kind for $ModuleName"
  $args = @('test', $proj)
  if ($Coverage) { $args += @('--collect:XPlat Code Coverage','--results-directory','./.tmp/tests/coverage') }
  Step "dotnet $($args -join ' ')"
  dotnet $args
  if ($LASTEXITCODE -ne 0) {
    if ($FailFast) { Fail "Tests failed for module $ModuleName" $LASTEXITCODE } else { Write-Host "Tests failed for module $ModuleName (continuing)" -ForegroundColor Red }
  }
}

function Help(){ @'
Usage: pwsh -File .vscode/tasks-tests.ps1 <unit|integration|help> [-Module <Name>|All] [-Coverage] [-FailFast]
Env Vars: TEST_MODULE=CoreModule | TEST_MODULE=All
Examples:
  pwsh -File .vscode/tasks-tests.ps1 unit -Module CoreModule
  TEST_MODULE=All pwsh -File .vscode/tasks-tests.ps1 integration -Coverage
'@ | Write-Host }

if ($Kind -eq 'help'){ Help; exit 0 }
$all = Resolve-Module
if ($script:TestModule -eq 'All') { foreach ($m in $all) { Run-TestsForModule $m } } else { Run-TestsForModule $script:TestModule }
exit 0
