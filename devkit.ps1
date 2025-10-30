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

$helpersPath = Join-Path $PSScriptRoot '.devkit/tasks-helpers.ps1'
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
  $script = Join-Path $PSScriptRoot '.devkit/tasks-dotnet.ps1'
  $sol = Join-Path $PSScriptRoot 'BridgingIT.DevKit.Examples.GettingStarted.sln'
  $args = @('-NoProfile','-File', $script, '-Command', $cmd, '-SolutionPath', $sol)
  if ($projectPath) { $args += @('-ProjectPath', $projectPath) }
  & pwsh $args
}

function Invoke-Coverage { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-coverage.ps1') }
function Invoke-CoverageHtml { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-coverage.ps1') -Html }
function Invoke-OpenApiLint { & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-openapi.ps1') lint }
function Invoke-Misc([string]$cmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-misc.ps1') $cmd }
function Invoke-Docker([string]$mode){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-docker.ps1') $mode }
function Invoke-Ef([string]$efCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-ef.ps1') $efCmd }
function Invoke-Diagnostics([string]$diagCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-diagnostics.ps1') -Command $diagCmd }
function Invoke-Compliance([string]$compCmd){ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-compliance.ps1') -Command $compCmd }

function Invoke-Test([string]$kind,[switch]$All){
  if ($All) { $env:TEST_MODULE='All' }
  & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-tests.ps1') $kind
  if ($All) { Remove-Item Env:TEST_MODULE -ErrorAction SilentlyContinue }
}

$tasks = [ordered]@{
  'build'                   = @{ Label='Build';                Desc='Build solution';                Script={ Invoke-DotnetScript 'build' } }
  'build-release'           = @{ Label='Build Release';        Desc='Release build';                 Script={ Invoke-DotnetScript 'build-release' } }
  'build-nr'                = @{ Label='Build NoRestore';      Desc='Build without restore';         Script={ Invoke-DotnetScript 'build-nr' } }
  'pack'                    = @{ Label='Pack';                 Desc='Create NuGet packages';         Script={ Invoke-DotnetScript 'pack' } }
  'restore'                 = @{ Label='Restore';              Desc='Restore packages';              Script={ Invoke-DotnetScript 'restore' } }
  'clean'                   = @{ Label='Clean';                Desc='Clean projects';                Script={ Invoke-DotnetScript 'clean' } }
  'tool-restore'            = @{ Label='Restore Tools';        Desc='Restore dotnet tools';          Script={ Invoke-DotnetScript 'tool-restore' } }
  'test-unit'               = @{ Label='Tests Unit';           Desc='Run selected unit tests';       Script={ Invoke-Test 'unit' } }
  'test-int'                = @{ Label='Tests Integration';    Desc='Run selected integration tests';Script={ Invoke-Test 'integration' } }
  'test-unit-all'           = @{ Label='Tests Unit All';       Desc='Run all unit tests';            Script={ Invoke-Test 'unit' -All } }
  'test-int-all'            = @{ Label='Tests Int All';        Desc='Run all integration tests';     Script={ Invoke-Test 'integration' -All } }
  'coverage'                = @{ Label='Coverage';             Desc='Compute coverage';              Script={ Invoke-Coverage } }
  'coverage-html'           = @{ Label='Coverage HTML';        Desc='Coverage HTML report';          Script={ Invoke-CoverageHtml } }
  'coverage-all-html'       = @{ Label='Coverage All+HTML';    Desc='All tests then HTML';           Script={ Invoke-Test 'unit' -All; Invoke-Test 'integration' -All; Invoke-CoverageHtml } }
  'ef-info'                 = @{ Label='EF Info';              Desc='DbContext info';                Script={ Invoke-Ef 'info' } }
  'ef-list'                 = @{ Label='EF List';              Desc='List migrations';               Script={ Invoke-Ef 'list' } }
  'ef-add'                  = @{ Label='EF Add';               Desc='Add migration';                 Script={ Invoke-Ef 'add' } }
  'ef-remove'               = @{ Label='EF Remove';            Desc='Remove last migration';         Script={ Invoke-Ef 'remove' } }
  'ef-removeall'            = @{ Label='EF RemoveAll';         Desc='Delete migration files';        Script={ Invoke-Ef 'removeall' } }
  'ef-apply'                = @{ Label='EF Apply';             Desc='Apply migrations';              Script={ Invoke-Ef 'apply' } }
  'ef-update'               = @{ Label='EF Update';            Desc='Update database';               Script={ Invoke-Ef 'update' } }
  'ef-recreate'             = @{ Label='EF Recreate';          Desc='Drop & recreate DB';            Script={ Invoke-Ef 'recreate' } }
  'ef-undo'                 = @{ Label='EF Undo';              Desc='Revert to previous migration';  Script={ Invoke-Ef 'undo' } }
  'ef-status'               = @{ Label='EF Status';            Desc='Migrations status';             Script={ Invoke-Ef 'status' } }
  'ef-reset'                = @{ Label='EF Reset';             Desc='Squash migrations';             Script={ Invoke-Ef 'reset' } }
  'ef-script'               = @{ Label='EF Script';            Desc='Export SQL script';             Script={ Invoke-Ef 'script' } }
  'docker-build-run'        = @{ Label='Docker Build+Run';     Desc='Build image & run';             Script={ Invoke-Docker 'docker-build-run' } }
  'docker-build-debug'      = @{ Label='Docker Build Debug';   Desc='Debug image build';             Script={ Invoke-Docker 'docker-build-debug' } }
  'docker-build-release'    = @{ Label='Docker Build Release'; Desc='Release image build';           Script={ Invoke-Docker 'docker-build-release' } }
  'docker-run'              = @{ Label='Docker Run';           Desc='Run container';                 Script={ Invoke-Docker 'docker-run' } }
  'docker-stop'             = @{ Label='Docker Stop';          Desc='Stop container';                Script={ Invoke-Docker 'docker-stop' } }
  'docker-remove'           = @{ Label='Docker Remove';        Desc='Remove container';              Script={ Invoke-Docker 'docker-remove' } }
  'compose-up'              = @{ Label='Compose Up';           Desc='docker compose up';             Script={ Invoke-Docker 'compose-up' } }
  'compose-up-pull'         = @{ Label='Compose Up Pull';      Desc='Up with image pull';            Script={ Invoke-Docker 'compose-up'; Invoke-Docker 'compose-up' } }
  'compose-down'            = @{ Label='Compose Down';         Desc='docker compose down';           Script={ Invoke-Docker 'compose-down' } }
  'compose-down-clean'      = @{ Label='Compose Down Clean';   Desc='Down & clean volumes';          Script={ Invoke-Docker 'compose-down-clean' } }
  'vulnerabilities'         = @{ Label='Security Vulnerabilities';       Desc='List vulnerabilities';          Script={ Invoke-DotnetScript 'vulnerabilities' } }
  'vulnerabilities-deep'    = @{ Label='Security Vulnerabilities Deep';  Desc='Transitive vulnerabilities';    Script={ Invoke-DotnetScript 'vulnerabilities-deep' } }
  'outdated'                = @{ Label='Packages Outdated';    Desc='List outdated packages';        Script={ Invoke-DotnetScript 'outdated' } }
  'outdated-json'           = @{ Label='Packages Outdated JSON';Desc='Export outdated JSON';        Script={ Invoke-DotnetScript 'outdated-json' } }
  'format-check'            = @{ Label='Format Check';         Desc='Check code formatting';              Script={ Invoke-DotnetScript 'format-check' } }
  'format-apply'            = @{ Label='Format Apply';         Desc='Apply code formatting';              Script={ Invoke-DotnetScript 'format-apply' } }
  'analyzers'               = @{ Label='Analyzers';            Desc='Run analyzers';                 Script={ Invoke-DotnetScript 'analyzers' } }
  'analyzers-export'        = @{ Label='Analyzers Export';     Desc='Export analyzer report';        Script={ Invoke-DotnetScript 'analyzers-export' } }
  'server-build'            = @{ Label='Server Build';         Desc='Build web server';              Script={ Invoke-DotnetScript 'project-build' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish'          = @{ Label='Server Publish';       Desc='Publish web server';                Script={ Invoke-DotnetScript 'project-publish' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish-release'  = @{ Label='Server Publish Release'; Desc='Publish web server release';               Script={ Invoke-DotnetScript 'project-publish-release' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-publish-sc'       = @{ Label='Server Publish Single';    Desc='Single-file publish';           Script={ Invoke-DotnetScript 'project-publish-sc' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-watch'            = @{ Label='Server Watch';         Desc='Run & Watch dev server';                   Script={ Invoke-DotnetScript 'project-watch' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'server-run-dev'          = @{ Label='Server Run';           Desc='Run dev server';                Script={ Invoke-DotnetScript 'project-run' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  # 'server-watch-fast'       = @{ Label='Server Watch Fast';    Desc='Fast watch run';                Script={ Invoke-DotnetScript 'project-watch-fast' (Join-Path $PSScriptRoot 'src/Presentation.Web.Server/Presentation.Web.Server.csproj') } }
  'pack-projects'            = @{ Label='Pack Projects';         Desc='Create NuGet packages';          Script={ Invoke-DotnetScript 'pack-projects' } }
  'update-packages'         = @{ Label='Update Packages';      Desc='Update Nuget packages';       Script={ Invoke-DotnetScript 'update-packages' } }
  'openapi-lint'            = @{ Label='OpenAPI Lint';         Desc='Lint OpenAPI specs';            Script={ Invoke-OpenApiLint } }
  'openapi-client-dotnet'   = @{ Label='OpenAPI Client .NET';  Desc='Generate C# client (Kiota)';    Script={ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-openapi.ps1') client-dotnet } }
  'openapi-client-typescript' = @{ Label='OpenAPI Client TS';  Desc='Generate TS client (Kiota)';    Script={ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-openapi.ps1') client-typescript } }
  'misc-clean'              = @{ Label='Workspace Clean';      Desc='Clean workspace';               Script={ Invoke-Misc 'clean' } }
  'misc-digest'             = @{ Label='Sources Digest';       Desc='Generate source digest';        Script={ Invoke-Misc 'digest' } }
  'misc-repl'               = @{ Label='C# REPL';              Desc='Interactive REPL';              Script={ Invoke-Misc 'repl' } }
  'misc-kill-dotnet'        = @{ Label='.NET Kill';            Desc='Kill dotnet process';           Script={ Invoke-Misc 'kill-dotnet' } }
  'misc-browser-seq'        = @{ Label='Browser Seq';          Desc='Open Seq dashboard';            Script={ Invoke-Misc 'browser-seq' } }
  'misc-browser-server-kestrel' = @{ Label='Browser Server HTTPS'; Desc='Open Kestrel site';       Script={ Invoke-Misc 'browser-server-kestrel' } }
  'misc-browser-server-docker'  = @{ Label='Browser Server Docker'; Desc='Open container site';   Script={ Invoke-Misc 'browser-server-docker' } }
  'doc-browser-devkit-docs'    = @{ Label='Browser DevKit Docs';   Desc='Open online DevKit Docs';              Script={ Invoke-Misc 'browser-devkit-docs' } }
  'doc-update-devkit-docs'     = @{ Label='Update DevKit Docs';    Desc='Download online DevKit Docs'; Script={ Invoke-Misc 'docs-update' } }
  'bench'                   = @{ Label='Bench Run';            Desc='Run benchmarks';                Script={ Invoke-Diagnostics 'bench' } }
  'bench-select'            = @{ Label='Bench Run Project';    Desc='Select benchmark project';      Script={ Invoke-Diagnostics 'bench-select' } }
  'trace-flame'             = @{ Label='Trace Flame';          Desc='Flame CPU trace';               Script={ Invoke-Diagnostics 'trace-flame' } }
  'trace-cpu'               = @{ Label='Trace CPU';            Desc='Detailed CPU trace';            Script={ Invoke-Diagnostics 'trace-cpu' } }
  'trace-gc'                = @{ Label='Trace GC';             Desc='GC-focused trace';              Script={ Invoke-Diagnostics 'trace-gc' } }
  'speedscope-view'         = @{ Label='Speedscope View';      Desc='View profiles';                 Script={ Invoke-Diagnostics 'speedscope-view' } }
  'dump-heap'               = @{ Label='Dump heap';            Desc='Create heap dump';              Script={ Invoke-Diagnostics 'dump-heap' } }
  'gc-stats'                = @{ Label='GC Stats';             Desc='Collect GC stats';              Script={ Invoke-Diagnostics 'gc-stats' } }
  'aspnet-metrics'          = @{ Label='ASP.NET Metrics';      Desc='ASP.NET counters';              Script={ Invoke-Diagnostics 'aspnet-metrics' } }
  'diag-quick'              = @{ Label='Diag Quick';           Desc='Quick diagnostics';             Script={ Invoke-Diagnostics 'quick' } }
  'coverage-open'           = @{ Label='Coverage Open';        Desc='Open coverage HTML';            Script={ & pwsh -NoProfile -File (Join-Path $PSScriptRoot '.devkit/tasks-coverage.ps1') -Html -Open } }
  'licenses'                = @{ Label='Licenses';             Desc='Generate license report';       Script={ Invoke-Compliance 'licenses' } }
}
# Compute max label width for aligned output
$TaskLabelWidth = ($tasks.Values | ForEach-Object { $_.Label.Length } | Measure-Object -Maximum).Maximum
function Format-TaskDisplay([hashtable]$t){
  if (-not $t) { return '' }
  return "{0} - {1}" -f ($t.Label.PadRight($TaskLabelWidth)), $t.Desc
}

$categories = [ordered]@{
  'Build & Maintenance' = @('restore','build','build-release','build-nr','pack','pack-projects','update-packages','clean','tool-restore','format-check','format-apply','analyzers','analyzers-export','server-build','server-publish','server-publish-release','server-publish-sc','server-watch','server-run-dev')
  'Testing & Quality'   = @('test-unit','test-int','test-unit-all','test-int-all','coverage','coverage-html','coverage-open','coverage-all-html')
  'EF & Persistence'    = @('ef-info','ef-list','ef-add','ef-remove','ef-removeall','ef-apply','ef-update','ef-recreate','ef-undo','ef-status','ef-reset','ef-script')
  'Publishing & Packaging' = @('server-publish','server-publish-release','server-publish-sc','pack','pack-projects')
  'Docker & Containers' = @('docker-build-run','docker-build-debug','docker-build-release','docker-run','docker-stop','docker-remove','compose-up','compose-up-pull','compose-down','compose-down-clean')
  'Security & Compliance' = @('vulnerabilities','vulnerabilities-deep','outdated','outdated-json','licenses')
  'API & Spec' = @('openapi-lint','openapi-client-dotnet','openapi-client-typescript')
  'Utilities'  = @('misc-clean','misc-digest','misc-repl','misc-kill-dotnet','misc-browser-seq','misc-browser-server-kestrel','misc-browser-server-docker')
  'Performance & Diagnostics' = @('bench','bench-select','trace-flame','trace-cpu','trace-gc','dump-heap','gc-stats','aspnet-metrics','diag-quick','speedscope-view')
  'Documentation'  = @('doc-browser-devkit-docs','doc-update-devkit-docs')
}

function Run-Task([string]$key){
  if (-not $tasks.Contains($key)) { Write-Host "Unknown task '$key'" -ForegroundColor Red; return }
  $def = $tasks[$key]
  (Format-TaskDisplay $def) | Format-SpectrePadded -Padding 0 | Format-SpectrePanel -Expand -Color "DeepSkyBlue3"
  & $def.Script
  if ($LASTEXITCODE -ne 0) { Write-Host "Task '$key' failed (exit $LASTEXITCODE)" -ForegroundColor Red } else { Write-Host "Task '$key' completed." -ForegroundColor Green }
}

if ($Task) { Run-Task $Task; exit $LASTEXITCODE }

$repoName = Split-Path $PSScriptRoot -Leaf
$message = "DevTasks - $repoName"
if ($env:VSCODE_PID -or $env:TERM_PROGRAM -eq 'vscode') {
  $message | Format-SpectrePadded -Padding 0 | Format-SpectrePanel -Expand -Border "Double" -Color "DeepSkyBlue3"
} else {
  try {
    $image = Get-SpectreImage -ImagePath "bITDevKit_Logo_dark.png" -MaxWidth 30
    @($message, $image) | Format-SpectreRows | Format-SpectrePanel -Expand -Border "Double" -Color "DeepSkyBlue3"
  } catch {
    $message | Format-SpectrePadded -Padding 0 | Format-SpectrePanel -Expand -Border "Double" -Color "DeepSkyBlue3"
  }
}

while ($true) {
  $cat = Read-Selection 'Select Task Category' $categories.Keys
  if (-not $cat) { break }
  $taskKeys = $categories[$cat]
  # Present aligned label + description
  $choices = $taskKeys | ForEach-Object { Format-TaskDisplay $tasks[$_] }
  $selection = Read-Selection "Select Task ($cat)" $choices
  if (-not $selection) { continue }
  $selectedIndex = $choices.IndexOf($selection)
  if ($selectedIndex -lt 0) { Write-Host 'Invalid selection mapping.' -ForegroundColor Red; continue }
  $selectedKey = $taskKeys[$selectedIndex]
  Run-Task $selectedKey
}

Write-Host 'Exiting' -ForegroundColor DarkGray
