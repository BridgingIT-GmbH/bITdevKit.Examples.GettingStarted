<#!
.SYNOPSIS
  Entity Framework Core management tasks (multi-module, multi-DbContext).

.DESCRIPTION
  Provides docker-independent EF Core automation via dotnet-ef tool (from manifest). Supports listing migrations,
  adding/removing/applying/undoing migrations, viewing DbContext model info, generating SQL scripts, showing
  applied status, and performing a squash/reset (re-baseline) operation.

.NOTES
  Run via VS Code tasks or manually: pwsh -File .vscode/tasks-ef.ps1 <command> [options]
  Exit codes: 0 success, non-zero failure.
#>
param(
  [Parameter(Position=0)] [string] $Command = 'help',
  [Parameter()] [string] $Module,                # removed default; will be resolved dynamically
  [Parameter()] [string] $DbContext,             # removed default; will be resolved dynamically
  [Parameter()] [string] $StartupProject = 'src/Presentation.Web.Server/Presentation.Web.Server.csproj',
  [Parameter()] [string] $InfrastructureProject = '', # if empty, will infer from Module
  [Parameter()] [string] $MigrationName,
  [Parameter()] [string] $OutputDirectory = './.tmp/ef',
  [Parameter()] [switch] $Force # used for reset confirmation bypass
)

$ErrorActionPreference = 'Stop'
Write-Host "EF Command: $Command" -ForegroundColor Yellow

# maintain script-scoped copies of mutable selection variables so function-local assignment doesn't lose them
$script:Module = $Module
$script:DbContext = $DbContext

# region: Dynamic discovery & selection
function Get-Modules() {
  # Determine modules root relative to repo root (parent of .vscode)
  $repoRoot = Split-Path $PSScriptRoot -Parent
  $modulesRoot = Join-Path $repoRoot 'src/Modules'
  Write-Host "Modules root: $modulesRoot" -ForegroundColor DarkGray
  if (-not (Test-Path $modulesRoot)) { Write-Host 'Modules root not found.' -ForegroundColor Red; return @() }
  $dirs = Get-ChildItem -Path $modulesRoot -Directory | Select-Object -ExpandProperty Name
  $filtered = $dirs | Where-Object { $_ -notmatch '^(?:Common|Shared)$' }
  Write-Host "Discovered modules: $($filtered -join ', ')" -ForegroundColor DarkGray
  return $filtered
}

function Get-DbContexts([string] $ModuleName) {
  if (-not $ModuleName) { return @() }
  $infraDir = "src/Modules/$ModuleName/$ModuleName.Infrastructure"
  if (-not (Test-Path $infraDir)) { return @() }
  Get-ChildItem -Path $infraDir -Recurse -Filter '*DbContext.cs' -File |
    ForEach-Object {
      $content = Get-Content -Path $_.FullName -Raw
      $matches = [regex]::Matches($content, 'class\s+([A-Za-z0-9_]+DbContext)')
      foreach ($m in $matches) { $m.Groups[1].Value }
    } | Sort-Object -Unique
}

function Prompt-Select([string] $Title, [string[]] $Items) {
  if (-not $Items -or $Items.Count -eq 0) { return $null }
  Write-Host "" -ForegroundColor DarkGray
  Write-Host "$Title" -ForegroundColor Cyan
  for ($i=0; $i -lt $Items.Count; $i++) { Write-Host "  [$i] $($Items[$i])" }
  $selection = Read-Host 'Enter index'
  if ($selection -match '^[0-9]+$' -and [int]$selection -ge 0 -and [int]$selection -lt $Items.Count) { return $Items[[int]$selection] }
  Write-Host 'Invalid selection; using first item.' -ForegroundColor DarkYellow
  return $Items[0]
}

function Resolve-ModuleAndContext() {
  # 1) Environment overrides (non-interactive tasks)
  if (-not $script:Module) { $script:Module = $env:EF_MODULE }
  if (-not $script:DbContext) { $script:DbContext = $env:EF_DBCONTEXT }

  $availableModules = Get-Modules
  if (-not $availableModules -or $availableModules.Count -eq 0) { Fail 'No modules discovered under src/Modules.' 101 }

  if (-not $script:Module) {
    # Interactive only if running directly (VS Code tasks won't prompt unless set to interactive). Provide fallback.
    if ($Host.Name -ne 'Visual Studio Code Host') {
      $script:Module = Prompt-Select 'Select Module:' $availableModules
    }
    if (-not $script:Module) { $script:Module = $availableModules[0] }
  } elseif ($availableModules -notcontains $script:Module) {
    Fail "Specified module '$script:Module' not found. Available: $($availableModules -join ', ')" 102
  }

  $availableContexts = Get-DbContexts $script:Module
  if (-not $availableContexts -or $availableContexts.Count -eq 0) { Fail "No DbContext classes discovered for module '$script:Module'." 103 }

  if (-not $script:DbContext) {
    if ($Host.Name -ne 'Visual Studio Code Host') {
      $script:DbContext = Prompt-Select "Select DbContext for module '$script:Module':" $availableContexts
    }
    if (-not $script:DbContext) { $script:DbContext = $availableContexts[0] }
  } elseif ($availableContexts -notcontains $script:DbContext) {
    Fail "Specified DbContext '$script:DbContext' not found in module '$script:Module'. Available: $($availableContexts -join ', ')" 104
  }

  if (-not $script:Module) { Fail 'Module resolution failed (empty after fallback).' 105 }
  if (-not $script:DbContext) { Fail 'DbContext resolution failed (empty after fallback).' 106 }
  Write-Host "Resolved Module: $script:Module" -ForegroundColor Green
  Write-Host "Resolved DbContext: $script:DbContext" -ForegroundColor Green
  # sync back to param vars for any external consumption
  $Module = $script:Module
  $DbContext = $script:DbContext
}
# endregion

Resolve-ModuleAndContext
Write-Host "Post-Resolve Values: Module='$Module' DbContext='$DbContext'" -ForegroundColor DarkGray

function Fail([string] $Msg, [int] $Code=1){ Write-Error $Msg; exit $Code }
function Section([string] $Text){ Write-Host "`n=== $Text ===" -ForegroundColor Cyan }
function Step([string] $Text){ Write-Host "-- $Text" -ForegroundColor DarkCyan }

function Ensure-DotNetTools() {
  Step 'Restoring dotnet tools'
  dotnet tool restore | Out-Null
  if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' $LASTEXITCODE }
}

function Resolve-InfrastructureProject([string] $ModuleName) {
  if ($InfrastructureProject) { return $InfrastructureProject }
  if (-not $ModuleName) { Fail 'Infrastructure project resolution failed: Module name empty.' 107 }
  $infraFolder = Join-Path -Path "src/Modules/$ModuleName" -ChildPath "$ModuleName.Infrastructure"
  $csproj = Join-Path -Path $infraFolder -ChildPath "$ModuleName.Infrastructure.csproj"
  Step "Resolving infrastructure project: $csproj"
  if (-not (Test-Path $csproj)) { Fail "Infrastructure project not found: $csproj" 2 }
  return $csproj
}

function Ensure-OutputDir([string] $Path){ $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path); New-Item -ItemType Directory -Force -Path $resolved | Out-Null; return $resolved }

function Build-EfArgs([string[]] $extra){
  if (-not $script:Module) { Fail 'Build-EfArgs: Module is empty.' 201 }
  $proj = Resolve-InfrastructureProject $script:Module
  Write-Host "EF Args Module: $script:Module | DbContext: $script:DbContext | Project: $proj" -ForegroundColor DarkGray
  return @('dotnet','ef') + $extra + @('--project', $proj, '--startup-project', $StartupProject)
}

function Run-Ef([string[]] $cmdArgs){
  if (-not $cmdArgs -or $cmdArgs.Count -eq 0){ Fail 'EF invocation received empty argument list.' 20 }
  Step "Running: $($cmdArgs -join ' ')"
  & $cmdArgs[0] $cmdArgs[1..($cmdArgs.Length-1)]
  if ($LASTEXITCODE -ne 0){ Fail "Command failed: $($cmdArgs -join ' ')" $LASTEXITCODE }
}

function List-DbContextInfo() {
  Section "DbContext Info ($script:DbContext) Module ($script:Module)"
  if (-not $script:DbContext) { Fail 'List-DbContextInfo: DbContext empty.' 202 }
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('dbcontext','info','--context', $script:DbContext)
  Run-Ef $efArgs
}

function List-Migrations() {
  Section "List Migrations ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations','list','--context', $script:DbContext)
  Run-Ef $efArgs
}

function Add-Migration() {
  Section "Add Migration ($script:DbContext)"
  Ensure-DotNetTools
  if (-not $MigrationName) {
    # Try environment variable fallback (settable via VS Code task env or user input variable)
    $envName = $env:EF_MIGRATION_NAME
    if ($envName) { $MigrationName = $envName }
  }
  if (-not $MigrationName) {
    # Auto-generate a name to keep task non-interactive (timestamp-based)
    $MigrationName = 'Migration_' + (Get-Date -Format 'yyyyMMdd_HHmmss')
    Write-Host "No migration name provided. Using generated name: $MigrationName" -ForegroundColor DarkYellow
  }
  $efArgs = Build-EfArgs @('migrations','add', $MigrationName,'--context', $script:DbContext,'--output-dir','EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Host "Migration '$MigrationName' added." -ForegroundColor Green
}

function Remove-Migration() {
  Section "Remove Last Migration ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations','remove','--context', $script:DbContext)
  Run-Ef $efArgs
  Write-Host 'Last migration removed.' -ForegroundColor Green
}

function Apply-Migrations() {
  Section "Apply (Update Database) ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('database','update','--context', $script:DbContext)
  Run-Ef $efArgs
  Write-Host 'Database updated.' -ForegroundColor Green
}

function Undo-Migration() {
  Section "Undo (Revert to Previous) ($script:DbContext)"
  Ensure-DotNetTools
  # Determine second last migration for revert
  $listArgs = Build-EfArgs @('migrations','list','--context', $script:DbContext)
  Step 'Fetching migrations list'
  $output = (& $listArgs[0] $listArgs[1..($listArgs.Length-1)]) 2>&1
  if ($LASTEXITCODE -ne 0){ Fail 'Failed retrieving migrations list.' $LASTEXITCODE }
  $migrations = $output | Where-Object { $_ -match '^[0-9]{14}_.+' }
  if ($migrations.Count -lt 2){ Fail 'Not enough migrations to undo.' 4 }
  $target = $migrations[-2].Split()[0] # migration id portion
  Step "Target revert migration: $target"
  $undoArgs = Build-EfArgs @('database','update',$target,'--context',$script:DbContext)
  Run-Ef $undoArgs
  Write-Host "Reverted to migration $target" -ForegroundColor Green
}

function Show-MigrationStatus() {
  Section "Migration Status ($script:DbContext)"
  Ensure-DotNetTools
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migrationsDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  $fsMigrations = @()
  if (Test-Path $migrationsDir){ $fsMigrations = Get-ChildItem -Path $migrationsDir -Filter '*.cs' | Where-Object { $_.Name -match '^[0-9]{14}_.+' } | Select-Object -ExpandProperty BaseName }
  $listArgs = Build-EfArgs @('migrations','list','--context',$script:DbContext)
  $output = (& $listArgs[0] $listArgs[1..($listArgs.Length-1)]) 2>&1
  if ($LASTEXITCODE -ne 0){ Fail 'Failed retrieving migrations list.' $LASTEXITCODE }
  $applied = $output | Where-Object { $_ -match '^[0-9]{14}_.+' } | ForEach-Object { ($_ -split '\s+')[0] }
  Write-Host 'Filesystem migrations:' -ForegroundColor Magenta
  $fsMigrations | ForEach-Object { Write-Host "  $_" }
  Write-Host 'Applied migrations:' -ForegroundColor Magenta
  $applied | ForEach-Object { Write-Host "  $_" }
  $pending = $fsMigrations | Where-Object { $applied -notcontains $_ }
  Write-Host 'Pending migrations:' -ForegroundColor Magenta
  if ($pending) { $pending | ForEach-Object { Write-Host "  $_" } } else { Write-Host '  (none)' }
}

function Reset-Migrations() {
  Section "Reset (Squash) Migrations ($script:DbContext)"
  Ensure-DotNetTools
  if (-not $Force){
    $confirmEnv = $env:EF_RESET_CONFIRM
    if ($confirmEnv -and $confirmEnv.ToLower() -eq 'y') {
      Write-Host 'Confirmation provided via EF_RESET_CONFIRM environment variable.' -ForegroundColor DarkYellow
    }
    else {
      Write-Host 'Force flag not supplied and EF_RESET_CONFIRM!=y. Aborting reset to remain non-interactive in task context.' -ForegroundColor Red
      Fail 'Reset cancelled (no confirmation).' 10
    }
  }
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  if (Test-Path $migDir){ Step "Removing migration files in $migDir"; Get-ChildItem -Path $migDir -File -Force | Remove-Item -Force }
  Step 'Adding baseline migration (Initial)'
  $efArgs = Build-EfArgs @('migrations','add','Initial','--context',$script:DbContext,'--output-dir','EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Host 'Baseline migration created.' -ForegroundColor Green
}

function Export-DbContextScript() {
  Section "Export DbContext SQL Script ($script:DbContext)"
  Ensure-DotNetTools
  $outDir = Ensure-OutputDir $OutputDirectory
  $scriptPath = Join-Path $outDir "$script:Module-$script:DbContext-schema.sql"
  $efArgs = Build-EfArgs @('dbcontext','script','--context',$script:DbContext,'--output',$scriptPath)
  Run-Ef $efArgs
  Write-Host "Script written: $scriptPath" -ForegroundColor Green
}

function RemoveAll-Migrations() {
  Section "Remove ALL Migration Files ($script:DbContext)"
  Ensure-DotNetTools
  if (-not $Force) {
    $removeAllConfirm = $env:EF_REMOVEALL_CONFIRM
    if ($removeAllConfirm -and $removeAllConfirm.ToLower() -eq 'y') {
      Write-Host 'Confirmation provided via EF_REMOVEALL_CONFIRM environment variable.' -ForegroundColor DarkYellow
    } else {
      Write-Host 'Force flag not supplied and EF_REMOVEALL_CONFIRM!=y. Aborting remove all (safety).' -ForegroundColor Red
      Fail 'Remove ALL cancelled (no confirmation).' 11
    }
  }
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  if (Test-Path $migDir) {
    Step "Removing all migration files in $migDir"
    Get-ChildItem -Path $migDir -File -Force | Remove-Item -Force
    Write-Host 'All migration files removed.' -ForegroundColor Green
  } else {
    Write-Host 'Migrations directory does not exist; nothing to remove.' -ForegroundColor Yellow
  }
}

function Help() {
@'
Usage: pwsh -File .vscode/tasks-ef.ps1 <command> [options]

Commands:
  info                 Show DbContext info
  list                 List migrations
  add                  Add new migration (prompts if -MigrationName omitted)
  remove               Remove last migration (not applied)
  removeall            Delete ALL migration source files (safety requires EF_REMOVEALL_CONFIRM=y or -Force)
  apply                Update database (apply all pending)
  undo                 Revert database to previous migration
  status               Show applied vs filesystem migrations
  reset                Squash migrations into new baseline (Initial)
  script               Export schema as SQL script
  help                 Show this help

Common Parameters:
  -Module <name>             (optional; can use env EF_MODULE or interactive select)
  -DbContext <name>          (optional; can use env EF_DBCONTEXT or interactive select)
  -StartupProject <path>     (default: src/Presentation.Web.Server/Presentation.Web.Server.csproj)
  -InfrastructureProject <path> override inferred module infrastructure project
  -MigrationName <name>      Name for new migration (add)
  -OutputDirectory <path>    (for script export, default: ./.tmp/ef)
  -Force                     Skip confirmation (reset)
  -Verbose                   Extra logging

Examples:
  pwsh -File .vscode/tasks-ef.ps1 list                # uses interactive/env resolution
  pwsh -File .vscode/tasks-ef.ps1 add -MigrationName AddCustomerIndex
  pwsh -File .vscode/tasks-ef.ps1 undo -Module SomeModule -DbContext SomeModuleDbContext
  pwsh -File .vscode/tasks-ef.ps1 script -Module InventoryModule -OutputDirectory ./.tmp/scripts
  EF_MODULE=InventoryModule EF_DBCONTEXT=InventoryModuleDbContext pwsh -File .vscode/tasks-ef.ps1 apply

Notes:
  Modules & DbContexts discovered dynamically. Provide EF_MODULE / EF_DBCONTEXT env variables for non-interactive tasks.
  Uses dotnet-ef from the tool manifest; tool restore executed automatically.
  Undo chooses second latest migration ID. Reset deletes migration source files (not database) then creates Initial.
  Status parses migrations folder and dotnet ef list output to display pending migrations.
  For complex squashing (including database consolidation), additional manual steps may be required.
'@ | Write-Host
}

switch ($Command.ToLower()) {
  'info' { List-DbContextInfo }
  'list' { List-Migrations }
  'add' { Add-Migration }
  'remove' { Remove-Migration }
  'removeall' { RemoveAll-Migrations }
  'apply' { Apply-Migrations }
  'undo' { Undo-Migration }
  'status' { Show-MigrationStatus }
  'reset' { Reset-Migrations }
  'script' { Export-DbContextScript }
  'help' { Help }
  default { Write-Host "Unknown command '$Command'" -ForegroundColor Red; Help; exit 1 }
}

exit 0