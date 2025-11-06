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
  [Parameter(Position = 0)] [string] $Command = 'help',
  [Parameter()] [string] $Module,                # module name (can be provided or discovered)
  [Parameter()] [string] $DbContext,             # dbcontext name (discovered after module selection)
  [Parameter()] [string] $StartupProject = 'src/Presentation.Web.Server/Presentation.Web.Server.csproj', # TODO: use EF_STARTUP_PROJECT setting
  [Parameter()] [string] $InfrastructureProject = '', # override inferred infrastructure project
  [Parameter()] [string] $MigrationName
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# maintain script-scoped copies of mutable selection variables so function-local assignment doesn't lose them
$script:Module = $Module
$script:DbContext = $DbContext

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

$OutputDirectory = Join-Path $Root (Get-OutputDirectory) 'db'

function Get-DbContexts([string] $ModuleName) {
  if (-not $ModuleName) { return @() }
  $infraDir = Join-Path $Root "src/Modules/$ModuleName/$ModuleName.Infrastructure"
  if (-not (Test-Path $infraDir)) { return @() }
  $contexts = @(Get-ChildItem -Path $infraDir -Recurse -Filter '*DbContext.cs' -File | ForEach-Object { $_.BaseName } | Sort-Object -Unique)
  return $contexts
}

function Resolve-ModuleAndContext() {
  # Resolve module using shared helper (env variable EF_MODULE honored via -EnvVarName)
  [string[]]$availableModules = Get-Modules -Root (Split-Path $PSScriptRoot -Parent)
  if (-not $availableModules -or $availableModules.Count -eq 0) { Fail 'No modules discovered under src/Modules.' 101 }
  # Write-Host "Discovered Modules: $($availableModules -join ', ')" -ForegroundColor DarkGray
  # Interactive selection via helper unless only one module (then auto-select for speed).
  $script:Module = Select-Module -Available $availableModules -Requested $Module -EnvVarName 'EF_MODULE'
  if (-not $script:Module) { Fail 'Module resolution failed (empty result).' 105 }
  if ($script:Module -eq 'All') { Fail "'All' selection not supported for EF operations." 106 }

  # Deterministic DbContext selection (always first discovered unless explicitly provided)
  $infraDir = "src/Modules/$script:Module/$script:Module.Infrastructure"
  $ctxFiles = @(Get-ChildItem -Path $infraDir -Recurse -Filter '*DbContext.cs' -File -ErrorAction SilentlyContinue)
  if (-not $ctxFiles -or $ctxFiles.Count -eq 0) { Fail "No DbContext files found under $infraDir" 107 }
  $discoveredContexts = @($ctxFiles | ForEach-Object { $_.BaseName } | Sort-Object -Unique)
  # If multiple contexts, allow interactive selection (Spectre) with Cancel
  if ($discoveredContexts.Count -gt 1) {
    if (Get-Command Read-SpectreSelection -ErrorAction SilentlyContinue) {
      try {
        $ctxChoices = $discoveredContexts + 'Cancel'
        $chosenCtx = Read-SpectreSelection -Title "Select DbContext" -Choices $ctxChoices -EnableSearch
        if ($chosenCtx -and $chosenCtx -ne 'Cancel') { $script:DbContext = $chosenCtx }
        elseif ($chosenCtx -eq 'Cancel') { Write-Warn 'DbContext selection cancelled.'; exit 0 }
      }
      catch { Write-Error "DbContext selection failed: $($_.Exception.Message). Using first." }
    }
  }
  if (-not $script:DbContext -and $DbContext) { $script:DbContext = $DbContext }
  if (-not $script:DbContext -and $env:EF_DBCONTEXT) { $script:DbContext = $env:EF_DBCONTEXT }
  if (-not $script:DbContext) { $script:DbContext = $discoveredContexts[0] }
  elseif ($discoveredContexts -notcontains $script:DbContext) {
    Write-Warn "Provided DbContext '$script:DbContext' not in discovered set. Using first."
    $script:DbContext = $discoveredContexts[0]
  }
  if ($script:DbContext.Length -eq 1 -and $discoveredContexts[0].Length -gt 1) {
    Write-Warn "Truncated DbContext '$script:DbContext' corrected to '$($discoveredContexts[0])'"
    $script:DbContext = $discoveredContexts[0]
  }
  $Module = $script:Module
  $DbContext = $script:DbContext
  Write-Step "Resolved Module: $Module"
  Write-Step "Resolved DbContext: $DbContext"
  if (-not $Module -or -not $DbContext) { Write-Error 'Module/DbContext resolution produced empty values.' }
  return
}
# endregion

Resolve-ModuleAndContext
if (-not $Module) { exit 0 }
# Write-Debug "Post-Resolve Values: Module='$Module' DbContext='$DbContext'"

function Resolve-InfrastructureProject([string] $ModuleName) {
  if ($InfrastructureProject) { return $InfrastructureProject }
  if (-not $ModuleName) { Fail 'Infrastructure project resolution failed: Module name empty.' 107 }
  $infraFolder = Join-Path -Path "src/Modules/$ModuleName" -ChildPath "$ModuleName.Infrastructure"
  $csproj = Join-Path -Path $infraFolder -ChildPath "$ModuleName.Infrastructure.csproj"
  Write-Step "Resolving infrastructure project: $csproj"
  if (-not (Test-Path $csproj)) { Fail "Infrastructure project not found: $csproj" 2 }
  return $csproj
}

function Build-EfArgs([string[]] $extra) {
  if (-not $script:Module) { Fail 'Build-EfArgs: Module is empty.' 201 }
  $proj = Resolve-InfrastructureProject $script:Module
  # Write-debug "EF Args Module: $script:Module | DbContext: $script:DbContext | Project: $proj"
  return @('dotnet', 'ef') + $extra + @('--project', $proj, '--startup-project', $StartupProject, '--no-build', '--verbose')
}

function Run-Ef([string[]] $cmdArgs) {
  if (-not $cmdArgs -or $cmdArgs.Count -eq 0) { Fail 'EF invocation received empty argument list.' 20 }
  Write-Debug "$($cmdArgs -join ' ')"
  & $cmdArgs[0] $cmdArgs[1..($cmdArgs.Length - 1)]
  if ($LASTEXITCODE -ne 0) { Fail "Command failed: $($cmdArgs -join ' ')" $LASTEXITCODE }
}

function List-DbContextInfo() {
  Write-Info "DbContext Info ($script:DbContext) Module ($script:Module)"
  if (-not $script:DbContext) { Fail 'List-DbContextInfo: DbContext empty.' 202 }
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('dbcontext', 'info', '--context', $script:DbContext)
  Run-Ef $efArgs
}

function List-Migrations() {
  Write-Info "List Migrations ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations', 'list', '--context', $script:DbContext)
  Run-Ef $efArgs
}

function Add-Migration() {
  Write-Info "Add Migration ($script:DbContext)"
  Ensure-DotNetTools
  if (-not $MigrationName) {
    # Try environment variable fallback (settable via VS Code task env or user input variable)
    $envName = $env:EF_MIGRATION_NAME
    if ($envName) { $MigrationName = $envName }
  }
  if (-not $MigrationName) {
    # Attempt interactive Spectre prompt for migration name; fallback to generated timestamp if cancelled/blank
    if (Get-Command Read-SpectreText -ErrorAction SilentlyContinue) {
      try {
        $inputName = Read-SpectreText -Message 'Enter Migration Name (blank = auto timestamp)' -AllowEmpty
        if ($inputName) { $MigrationName = $inputName }
      }
      catch { Write-Error "Prompt failed: $($_.Exception.Message)" }
    }
  }
  if (-not $MigrationName) {
    $MigrationName = 'Migration_' + (Get-Date -Format 'yyyyMMdd_HHmmss')
    Write-Step "No migration name provided. Using generated name: $MigrationName"
  }
  $efArgs = Build-EfArgs @('migrations', 'add', $MigrationName, '--context', $script:DbContext, '--output-dir', 'EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Info "Migration '$MigrationName' added."
}

function Remove-Migration() {
  Write-Info "Remove Last Migration ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations', 'remove', '--context', $script:DbContext)
  Run-Ef $efArgs
  Write-Info 'Last migration removed.'
}

function Apply-Migrations() {
  Write-Info "Apply (Update Database) ($script:DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('database', 'update', '--context', $script:DbContext)
  Run-Ef $efArgs
  Write-Info 'Database updated.'
}

function Update-DatabaseAlias() {
  Write-Info "Update Database (Alias) ($script:DbContext)"
  Apply-Migrations
}

function Recreate-Database() {
  Write-Info "Recreate Database (Drop + Migrate) ($script:DbContext)"
  Ensure-DotNetTools
  # Drop database
  $dropArgs = Build-EfArgs @('database', 'drop', '--context', $script:DbContext, '--force')
  Run-Ef $dropArgs
  Write-Step 'Database dropped.'
  # Apply migrations
  $updateArgs = Build-EfArgs @('database', 'update', '--context', $script:DbContext)
  Run-Ef $updateArgs
  Write-Info 'Database recreated with latest migrations.'
}

function Undo-Migration() {
  Write-Info "Undo (Revert to Previous) ($script:DbContext)"
  Ensure-DotNetTools
  # Determine second last migration for revert
  $listArgs = Build-EfArgs @('migrations', 'list', '--context', $script:DbContext)
  Step 'Fetching migrations list'
  $output = (& $listArgs[0] $listArgs[1..($listArgs.Length - 1)]) 2>&1
  if ($LASTEXITCODE -ne 0) { Fail 'Failed retrieving migrations list.' $LASTEXITCODE }
  $migrations = $output | Where-Object { $_ -match '^[0-9]{14}_.+' }
  if ($migrations.Count -lt 2) { Fail 'Not enough migrations to undo.' 4 }
  $target = $migrations[-2].Split()[0] # migration id portion
  Write-Step "Target revert migration: $target"
  $undoArgs = Build-EfArgs @('database', 'update', $target, '--context', $script:DbContext)
  Run-Ef $undoArgs
  Write-Info "Reverted to migration $target"
}

function Show-MigrationStatus() {
  Write-Info "Migration Status ($script:DbContext)"
  Ensure-DotNetTools
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migrationsDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  $fsMigrations = @()
  if (Test-Path $migrationsDir) { $fsMigrations = Get-ChildItem -Path $migrationsDir -Filter '*.cs' | Where-Object { $_.Name -match '^[0-9]{14}_.+' } | Select-Object -ExpandProperty BaseName }
  $listArgs = Build-EfArgs @('migrations', 'list', '--context', $script:DbContext)
  $output = (& $listArgs[0] $listArgs[1..($listArgs.Length - 1)]) 2>&1
  if ($LASTEXITCODE -ne 0) { Fail 'Failed retrieving migrations list.' $LASTEXITCODE }
  $applied = $output | Where-Object { $_ -match '^[0-9]{14}_.+' } | ForEach-Object { ($_ -split '\s+')[0] }
  Write-Step 'Filesystem migrations:'
  $fsMigrations | ForEach-Object { Write-Host "  $_" }
  Write-Step 'Applied migrations:'
  $applied | ForEach-Object { Write-Host "  $_" }
  $pending = $fsMigrations | Where-Object { $applied -notcontains $_ }
  Write-Step 'Pending migrations:'
  if ($pending) { $pending | ForEach-Object { Write-Host "  $_" } } else { Write-Host '  (none)' }
}

function Reset-Migrations() {
  Write-Info "Reset (Squash) Migrations ($script:DbContext)"
  Ensure-DotNetTools
  Write-Step 'Proceeding with migration reset (no confirmation).'
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  if (Test-Path $migDir) { Write-Step "Removing migration files in $migDir"; Get-ChildItem -Path $migDir -File -Force | Remove-Item -Force }
  Step 'Adding baseline migration (Initial)'
  $efArgs = Build-EfArgs @('migrations', 'add', 'Initial', '--context', $script:DbContext, '--output-dir', 'EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Info 'Baseline migration created.'
}

function Export-DbContextScript() {
  Write-Info "Export DbContext SQL Script ($script:DbContext)"
  Ensure-DotNetTools
  Ensure-Path $OutputDirectory
  # $outPath = Join-Path $OutputDirectory "$script:Module-$script:DbContext-schema.sql"
  $outPath = Join-Path $OutputDirectory "efscript_$script:Module.sql"
  $efArgs = Build-EfArgs @('migrations', 'script', '--context', $script:DbContext, '--output', $outPath, '--idempotent')
  Run-Ef $efArgs
  Write-Info "Script written: $outPath"
}

function Export-DbContextBundle() {
  Write-Info "Export DbContext Bundle ($script:DbContext)"
  Ensure-DotNetTools
  Ensure-Path $OutputDirectory
  # $outPath = Join-Path $OutputDirectory "$script:Module-$script:DbContext-schema.exe"
  $outPath = Join-Path $OutputDirectory "efbundle_$script:Module.exe"
  $efArgs = Build-EfArgs @('migrations', 'bundle', '--context', $script:DbContext, '--output', $outPath)
  Run-Ef $efArgs
  Write-Info "Script written: $outPath"
}

function RemoveAll-Migrations() {
  Write-Info "Remove ALL Migration Files ($script:DbContext)"
  Ensure-DotNetTools
  Write-Step 'Proceeding with complete migration file removal (no confirmation).'
  $infraProj = Resolve-InfrastructureProject $script:Module
  $migDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  if (Test-Path $migDir) {
    Write-Step "Removing all migration files in $migDir"
    Get-ChildItem -Path $migDir -File -Force | Remove-Item -Force
    Write-Info 'All migration files removed.'
  }
  else {
    Write-Warn 'Migrations directory does not exist; nothing to remove.'
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
  removeall            Delete ALL migration source files (NO confirmation)
  apply                Update database (apply all pending)
  update               Alias for apply (update database)
  recreate             Drop and recreate database (applies all migrations)
  undo                 Revert database to previous migration
  status               Show applied vs filesystem migrations
  reset                Squash migrations into new baseline (Initial) (NO confirmation)
  script               Export schema as SQL script
  help                 Show this help

Common Parameters:
  -Module <name>             (optional; can use env EF_MODULE or interactive select)
  -DbContext <name>          (optional; can use env EF_DBCONTEXT or interactive select)
  -StartupProject <path>     (default: src/Presentation.Web.Server/Presentation.Web.Server.csproj)
  -InfrastructureProject <path> override inferred module infrastructure project
  -MigrationName <name>      Name for new migration (add)
  -OutputDirectory <path>    (for script export, default: ./.tmp/ef)
  (Reset/removeall have no confirmation safeguards; ensure you target the correct module.)
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
  Undo chooses second latest migration ID. Reset deletes migration source files (not database) then creates Initial WITHOUT confirmation.
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
  'update' { Update-DatabaseAlias }
  'recreate' { Recreate-Database }
  'undo' { Undo-Migration }
  'status' { Show-MigrationStatus }
  'reset' { Reset-Migrations }
  'script' { Export-DbContextScript }
  'bundle' { Export-DbContextBundle }
  'help' { Help }
  default { Write-Error "Unknown command '$Command'"; Help; exit 1 }
}

exit 0
