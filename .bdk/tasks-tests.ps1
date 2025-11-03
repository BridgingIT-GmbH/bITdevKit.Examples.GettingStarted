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
  [Parameter(Position = 0)] [ValidateSet('unit', 'integration', 'help')] [string] $Kind = 'help',
  [Parameter()] [string] $Module,
  [Parameter()] [switch] $Coverage,
  [Parameter()] [switch] $FailFast
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

$OutputDirectory = Join-Path $Root (Get-OutputDirectory) 'test'

function Run-ModuleTests([string] $ModuleName) {
  $proj = Build-TestProjectPath $ModuleName $Kind
  Write-Step "Testing $Kind for $ModuleName"
  $dotnetArgs = @('test', $proj)
  if ($Coverage) { $dotnetArgs += @('--collect:XPlat Code Coverage', '--results-directory', (Join-Path $OutputDirectory 'coverage')) }
  Write-Debug "dotnet $($dotnetArgs -join ' ')"
  dotnet $dotnetArgs
  if ($LASTEXITCODE -ne 0) {
    if ($FailFast) { Fail "Tests failed for module $ModuleName" $LASTEXITCODE } else { Write-Error "Tests failed for module $ModuleName (continuing)" }
  }
}

function Build-TestProjectPath([string] $ModuleName, [string] $kind) {
  $proj = "tests/Modules/$ModuleName/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' })/$ModuleName.$(if($kind -eq 'unit'){ 'UnitTests' } else { 'IntegrationTests' }).csproj"
  if (-not (Test-Path $proj)) { Fail "Test project not found: $proj" 60 }
  return $proj
}


function Help() {
  @'
Usage: pwsh -File .vscode/tasks-tests.ps1 <unit|integration|help> [-Module <Name>|All] [-Coverage] [-FailFast]
Env Vars: TEST_MODULE=CoreModule | TEST_MODULE=All
Examples:
  pwsh -File .vscode/tasks-tests.ps1 unit -Module CoreModule
  TEST_MODULE=All pwsh -File .vscode/tasks-tests.ps1 integration -Coverage
'@ | Write-Host
}

if ($Kind -eq 'help') { Help; exit 0 }

[string[]]$available = Get-Modules -Root (Split-Path $PSScriptRoot -Parent)
if (-not $available -or $available.Count -eq 0) { Fail 'No modules discovered.' 50 }

$selection = Select-Module -Available $available -Requested $Module -EnvVarName 'TEST_MODULE' -AllowAll
Write-Info "Resolved Test Module: $selection"

if ($selection -eq 'All') {
  foreach ($m in $available) { Run-ModuleTests $m }
}
else {
  Run-ModuleTests $selection
}
exit 0
