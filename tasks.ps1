<#!
.SYNOPSIS
  Standalone DevKit task runner (Spectre Console) independent of VS Code tasks.json.
.DESCRIPTION
  Provides interactive menus (categories -> tasks) for common solution tasks:
    Build / Clean / Tool Restore
    Tests (Unit, Integration, All)
    EF Migrations (list/add/remove/apply/undo/status/reset/script/removeall/info)
    Docker (build/run/stop/remove/compose up/down)
    Security & Formatting (vulnerabilities, outdated, format check/apply, analyzers)
    Coverage, OpenAPI lint, Misc utilities.
  Uses subordinate scripts (tasks-*.ps1) where specialized logic exists.
.PARAMETER Task
  Optional task key to run directly (e.g. build, test-unit, ef-add).
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

$helpersPath = Join-Path $PSScriptRoot '.vscode/tasks-helpers.ps1'
if (Test-Path $helpersPath) { . $helpersPath }

function Ensure-Spectre {
  if (-not (Get-Module -ListAvailable -Name PwshSpectreConsole)) {
    Install-Module -Name PwshSpectreConsole -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
  }
  Import-Module PwshSpectreConsole -Force
}
Ensure-Spectre

function Read-Selection($title,[string[]]$choices){
  $choices += 'Cancel'
  $s = Read-SpectreSelection -Title $title -Choices $choices -EnableSearch -PageSize 15
  if (-not $s -or $s -eq 'Cancel') { return $null }
  return $s
}

function Invoke-DotnetScript([string]$cmd,[string]$projectPath){
  $script = Join-Path $PSScriptRoot '.vscode/tasks-dotnet.ps1'
  $sol = Join-Path $PSScriptRoot 'BridgingIT.DevKit.Examples.GettingStarted.sln'
  $args = @('-NoProfile','-File', $script, '-Command', $cmd, '-SolutionPath', $sol)
  if ($projectPath) { $args += @('-ProjectPath', $projectPath) }
  & pwsh $args
}

function Invoke-Coverage { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-coverage.ps1') }
function Invoke-CoverageHtml { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-coverage.ps1') -Html }
function Invoke-OpenApiLint { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-openapi.ps1') lint }
function Invoke-Misc([string]$cmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-misc.ps1') $cmd }
function Invoke-Docker([string]$mode){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-docker.ps1') $mode }
function Invoke-Ef([string]$efCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-ef.ps1') $efCmd }
function Invoke-Diagnostics([string]$diagCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-diagnostics.ps1') -Command $diagCmd }
function Invoke-Compliance([string]$compCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-compliance.ps1') -Command $compCmd }

function Invoke-Test([string]$kind,[switch]$All){
  if ($All) { $env:TEST_MODULE='All' }
  & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-tests.ps1') $kind
  if ($All) { Remove-Item Env:TEST_MODULE -ErrorAction SilentlyContinue }
}

$tasks = [ordered]@{
  'build' = @{ Label='Build Solution'; Script={ Invoke-DotnetScript 'build' } }
  'build-release' = @{ Label='Build Solution (Release)'; Script={ Invoke-DotnetScript 'build-release' } }
  'build-nr' = @{ Label='Build Solution (No Restore)'; Script={ Invoke-DotnetScript 'build-nr' } }
  'pack' = @{ Label='Pack Solution (Release)'; Script={ Invoke-DotnetScript 'pack' } }
  'restore' = @{ Label='Restore Solution Packages'; Script={ Invoke-DotnetScript 'restore' } }
  'clean' = @{ Label='Clean Solution'; Script={ Invoke-DotnetScript 'clean' } }
  'tool-restore' = @{ Label='Restore Dotnet Tools'; Script={ Invoke-DotnetScript 'tool-restore' } }
  'test-unit' = @{ Label='Tests Unit (select)'; Script={ Invoke-Test 'unit' } }
  'test-int' = @{ Label='Tests Integration (select)'; Script={ Invoke-Test 'integration' } }
  'test-unit-all' = @{ Label='Tests Unit All'; Script={ Invoke-Test 'unit' -All } }
  'test-int-all' = @{ Label='Tests Integration All'; Script={ Invoke-Test 'integration' -All } }
  'coverage' = @{ Label='Coverage (all)'; Script={ Invoke-Coverage } }
  'coverage-html' = @{ Label='Coverage Report (HTML)'; Script={ Invoke-CoverageHtml } }
  'coverage-all-html' = @{ Label='Coverage (Tests All -> HTML Report)'; Script={ Invoke-Test 'unit' -All; Invoke-Test 'integration' -All; Invoke-CoverageHtml } }
  'ef-info' = @{ Label='EF DbContext Info'; Script={ Invoke-Ef 'info' } }
  'ef-list' = @{ Label='EF Migrations List'; Script={ Invoke-Ef 'list' } }
  'ef-add' = @{ Label='EF Migration Add'; Script={ Invoke-Ef 'add' } }
  'ef-remove' = @{ Label='EF Migration Remove'; Script={ Invoke-Ef 'remove' } }
  'ef-removeall' = @{ Label='EF Migrations Remove All'; Script={ Invoke-Ef 'removeall' } }
  'ef-apply' = @{ Label='EF Apply Migrations'; Script={ Invoke-Ef 'apply' } }
  'ef-update' = @{ Label='EF Update Database'; Script={ Invoke-Ef 'update' } }
  'ef-recreate' = @{ Label='EF Recreate Database'; Script={ Invoke-Ef 'recreate' } }
  'ef-undo' = @{ Label='EF Undo Migration'; Script={ Invoke-Ef 'undo' } }
  'ef-status' = @{ Label='EF Migration Status'; Script={ Invoke-Ef 'status' } }
  'ef-reset' = @{ Label='EF Reset (Squash)'; Script={ Invoke-Ef 'reset' } }
  'ef-script' = @{ Label='EF Export SQL Script'; Script={ Invoke-Ef 'script' } }
  'docker-build-run' = @{ Label='Docker Build & Run'; Script={ Invoke-Docker 'docker-build-run' } }
  # 'docker-build' = @{ Label='Docker Build'; Script={ Invoke-Docker 'docker-build' } }
  'docker-build-debug' = @{ Label='Docker Build'; Script={ Invoke-Docker 'docker-build-debug' } }
  'docker-build-release' = @{ Label='Docker Build (Release)'; Script={ Invoke-Docker 'docker-build-release' } }
  'docker-run' = @{ Label='Docker Run'; Script={ Invoke-Docker 'docker-run' } }
  'docker-stop' = @{ Label='Docker Stop'; Script={ Invoke-Docker 'docker-stop' } }
  'docker-remove' = @{ Label='Docker Remove'; Script={ Invoke-Docker 'docker-remove' } }
  'compose-up' = @{ Label='Docker Compose Up'; Script={ Invoke-Docker 'compose-up' } }
  'compose-up-pull' = @{ Label='Docker Compose Up & Pull'; Script={ Invoke-Docker 'compose-up'; Invoke-Docker 'compose-up' } }
  'compose-down' = @{ Label='Docker Compose Down'; Script={ Invoke-Docker 'compose-down' } }
  'compose-down-clean' = @{ Label='Docker Compose Down & Clean'; Script={ Invoke-Docker 'compose-down-clean' } }
  'vulnerabilities' = @{ Label='List Vulnerable Packages'; Script={ Invoke-DotnetScript 'vulnerabilities' } }
  'vulnerabilities-deep' = @{ Label='List Vulnerable Packages (Transitive)'; Script={ Invoke-DotnetScript 'vulnerabilities-deep' } }
  'outdated' = @{ Label='List Outdated Packages'; Script={ Invoke-DotnetScript 'outdated' } }
  'outdated-json' = @{ Label='Outdated Packages (JSON Export)'; Script={ Invoke-DotnetScript 'outdated-json' } }
  'format-check' = @{ Label='Format Check'; Script={ Invoke-DotnetScript 'format-check' } }
  'format-apply' = @{ Label='Format Apply'; Script={ Invoke-DotnetScript 'format-apply' } }
  'analyzers' = @{ Label='Analyzers Report'; Script={  Invoke-DotnetScript 'analyzers' } }
  'analyzers-export' = @{ Label='Analyzers Export (SARIF + Summary)'; Script={ Invoke-DotnetScript 'analyzers-export' } }
  'server-build' = @{ Label='Server Project Build'; Script={ Invoke-DotnetScript 'project-build' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish' = @{ Label='Server Project Publish'; Script={ Invoke-DotnetScript 'project-publish' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish-release' = @{ Label='Server Project Publish (Release)'; Script={ Invoke-DotnetScript 'project-publish-release' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish-sc' = @{ Label='Server Project Publish (Release, Single-File)'; Script={ Invoke-DotnetScript 'project-publish-sc' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-watch' = @{ Label='Server Project Watch Run'; Script={ Invoke-DotnetScript 'project-watch' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-run-dev' = @{ Label='Server Project Run Dev'; Script={ Invoke-DotnetScript 'project-run' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-watch-fast' = @{ Label='Server Project Watch Fast'; Script={ Invoke-DotnetScript 'project-watch-fast' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'pack-modules' = @{ Label='Pack Modules (Release)'; Script={ Invoke-DotnetScript 'pack-modules' } }
  'openapi-lint' = @{ Label='OpenAPI Lint'; Script={ Invoke-OpenApiLint } }
  'misc-clean' = @{ Label='Misc Clean Workspace'; Script={ Invoke-Misc 'clean' } }
  'misc-digest' = @{ Label='Misc Digest Sources'; Script={ Invoke-Misc 'digest' } }
  'misc-repl' = @{ Label='Misc C# REPL'; Script={ Invoke-Misc 'repl' } }
  'bench' = @{ Label='Diagnostics Benchmarks'; Script={ Invoke-Diagnostics 'bench' } }
  'bench-select' = @{ Label='Diagnostics Benchmarks (Project)'; Script={ Invoke-Diagnostics 'bench-select' } }
  'trace-flame' = @{ Label='Diagnostics Trace (Flame)'; Script={ Invoke-Diagnostics 'trace-flame' } }
  'trace-cpu' = @{ Label='Diagnostics Trace (CPU SampleProfiler)'; Script={ Invoke-Diagnostics 'trace-cpu' } }
  'trace-gc' = @{ Label='Diagnostics Trace (GC Focus)'; Script={ Invoke-Diagnostics 'trace-gc' } }
  'speedscope-view' = @{ Label='Diagnostics Speedscope View'; Script={ Invoke-Diagnostics 'speedscope-view' } }
  'dump-heap' = @{ Label='Diagnostics Heap Dump'; Script={ Invoke-Diagnostics 'dump-heap' } }
  'gc-stats' = @{ Label='Diagnostics GC Stats'; Script={ Invoke-Diagnostics 'gc-stats' } }
  'aspnet-metrics' = @{ Label='Diagnostics ASP.NET Core Metrics'; Script={ Invoke-Diagnostics 'aspnet-metrics' } }
  'diag-quick' = @{ Label='Diagnostics Quick Set (CPU + GC + ASP.NET)'; Script={ Invoke-Diagnostics 'quick' } }
  'coverage-open' = @{ Label='Coverage Report (HTML)'; Script={ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.vscode/tasks-coverage.ps1') -Html -Open } }
  'licenses' = @{ Label='Generate License Reports'; Script={ Invoke-Compliance 'licenses' } }
}

$categories = [ordered]@{
  'Build & Maintenance' = @('restore','build','build-release','build-nr','pack','pack-modules','clean','tool-restore','format-check','format-apply','analyzers','analyzers-export','server-build','server-publish','server-publish-release','server-publish-sc','server-watch','server-run-dev','server-watch-fast')
  'Testing & Quality'   = @('test-unit','test-int','test-unit-all','test-int-all','coverage','coverage-html','coverage-open','coverage-all-html')
  'EF & Persistence'    = @('ef-info','ef-list','ef-add','ef-remove','ef-removeall','ef-apply','ef-update','ef-recreate','ef-undo','ef-status','ef-reset','ef-script')
  'Publishing & Packaging' = @('server-publish','server-publish-release','server-publish-sc','pack','pack-modules')
  'Docker & Containers' = @('docker-build-run','docker-build-debug','docker-build-release','docker-run','docker-stop','docker-remove','compose-up','compose-up-pull','compose-down','compose-down-clean')
  'Security & Compliance' = @('vulnerabilities','vulnerabilities-deep','outdated','outdated-json','licenses')
  'API & Spec' = @('openapi-lint')
  'Utilities'  = @('misc-clean','misc-digest','misc-repl')
  'Performance & Diagnostics' = @('bench','bench-select','trace-flame','trace-cpu','trace-gc','dump-heap','gc-stats','aspnet-metrics','diag-quick','speedscope-view')
}

function Run-Task([string]$key){
  if (-not $tasks.Contains($key)) { Write-Host "Unknown task '$key'" -ForegroundColor Red; return }
  $def = $tasks[$key]
  # Write-Host "=== $($def.Label) ===" -ForegroundColor Cyan
  $def.Label | Format-SpectrePadded -Padding 0 | Format-SpectrePanel -Expand
  & $def.Script
  if ($LASTEXITCODE -ne 0) { Write-Host "Task '$key' failed (exit $LASTEXITCODE)" -ForegroundColor Red } else { Write-Host "Task '$key' completed." -ForegroundColor Green }
}

if ($Task) { Run-Task $Task; exit $LASTEXITCODE }

# Get-SpectreImage -ImagePath "bITDevKit_Logo_dark.png" -MaxWidth 30

$message = "BridgingIT.DevKit.Examples.GettingStarted"
$image = Get-SpectreImage -ImagePath "bITDevKit_Logo_dark.png" -MaxWidth 30
@($message, $image) | Format-SpectreRows | Format-SpectrePanel -Expand

while ($true) {
  $cat = Read-Selection 'Select Task Category' $categories.Keys
  if (-not $cat) { break }
  $taskKeys = $categories[$cat]
  # Present labels only; map back by index to avoid Spectre style parsing collisions
  $choices = $taskKeys | ForEach-Object { $tasks[$_].Label }
  $selection = Read-Selection "Select Task ($cat)" $choices
  if (-not $selection) { continue }
  $selectedIndex = $choices.IndexOf($selection)
  if ($selectedIndex -lt 0) { Write-Host 'Invalid selection mapping.' -ForegroundColor Red; continue }
  $selectedKey = $taskKeys[$selectedIndex]
  Run-Task $selectedKey
}

Write-Host 'Exiting' -ForegroundColor DarkGray
