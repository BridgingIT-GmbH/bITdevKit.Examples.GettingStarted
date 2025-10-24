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
  [Parameter()] [switch] $FailFast,
  [Parameter()] [switch] $NonInteractive
)

$ErrorActionPreference = 'Stop'
function Fail([string] $Msg, [int] $Code=1){ Write-Error $Msg; exit $Code }
function Step([string] $Msg){ Write-Host "-- $Msg" -ForegroundColor DarkCyan }
function Section([string] $Msg){ Write-Host "`n=== $Msg ===" -ForegroundColor Cyan }

# Dot-source shared helpers
$helpersPath = Join-Path $PSScriptRoot 'tasks-helpers.ps1'
if (Test-Path $helpersPath) { . $helpersPath } else { Fail "Helper script not found: $helpersPath" 10 }

function Build-TestProjectPath([string] $ModuleName, [string] $kind){
  $proj = "tests/Modules/$ModuleName/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' })/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' }).csproj"
  if (-not (Test-Path $proj)) { Fail "Test project not found: $proj" 60 }
  return $proj
}

function Run-TestsForModule([string] $ModuleName){
  $proj = Build-TestProjectPath $ModuleName $Kind
  Section "Testing $Kind for $ModuleName"
  $args = @('test', $proj)
  if (-not $Module) {
    $Module = Select-DevKitModule -Title "Select Test Module" -AllowAll
  if ($null -eq $Module) { exit 0 }
  }
  if ($Coverage) { $args += @('--collect:XPlat Code Coverage','--results-directory','./.tmp/tests/coverage') }
  Step "dotnet $($args -join ' ')"
  dotnet $args
  if ($LASTEXITCODE -ne 0) {
    if ($FailFast) { Fail "Tests failed for module $ModuleName" $LASTEXITCODE } else { Write-Host "Tests failed for module $ModuleName (continuing)" -ForegroundColor Red }
  }
}

function Help(){ @'
Usage: pwsh -File .vscode/tasks-tests.ps1 <unit|integration|help> [-Module <Name>|All] [-Coverage] [-FailFast] [-NonInteractive]
Env Vars: TEST_MODULE=CoreModule | TEST_MODULE=All
Examples:
  pwsh -File .vscode/tasks-tests.ps1 unit -Module CoreModule
  TEST_MODULE=All pwsh -File .vscode/tasks-tests.ps1 integration -Coverage
  pwsh -File .vscode/tasks-tests.ps1 unit -NonInteractive
'@ | Write-Host }

if ($Kind -eq 'help'){ Help; exit 0 }

[string[]]$available = Get-DevKitModules -Root (Split-Path $PSScriptRoot -Parent)
if (-not $available -or $available.Count -eq 0){ Fail 'No modules discovered.' 50 }

$isVsCodeHost = ($Host.Name -eq 'Visual Studio Code Host')
$effectiveNonInteractive = $NonInteractive -or $isVsCodeHost
$selection = Select-DevKitModule -Available $available -Requested $Module -EnvVarName 'TEST_MODULE' -AllowAll -NonInteractive:$effectiveNonInteractive
Write-Host "Resolved Test Module: $selection" -ForegroundColor Green

if ($selection -eq 'All') {
  foreach ($m in $available) { Run-TestsForModule $m }
} else {
  Run-TestsForModule $selection
}
exit 0
