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
  [Parameter()] [string] $Module = 'CoreModule',
  [Parameter()] [string] $DbContext = 'CoreModuleDbContext',
  [Parameter()] [string] $StartupProject = 'src/Presentation.Web.Server/Presentation.Web.Server.csproj',
  [Parameter()] [string] $InfrastructureProject = '', # if empty, will infer from Module
  [Parameter()] [string] $MigrationName,
  [Parameter()] [string] $OutputDirectory = './.tmp/ef',
  # Do NOT declare a Verbose switch here; PowerShell supplies -Verbose as a common parameter automatically.
  [Parameter()] [switch] $Force # used for reset confirmation bypass
)

$ErrorActionPreference = 'Stop'
Write-Host "EF Command: $Command" -ForegroundColor Yellow

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
  $path = "src/Modules/$ModuleName/$ModuleName.Infrastructure/$ModuleName.Infrastructure.csproj"
  if (-not (Test-Path $path)) { Fail "Infrastructure project not found: $path" 2 }
  return $path
}

function Ensure-OutputDir([string] $Path){ $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path); New-Item -ItemType Directory -Force -Path $resolved | Out-Null; return $resolved }

function Build-EfArgs([string[]] $extra){ return @('dotnet','ef') + $extra + @('--project', (Resolve-InfrastructureProject $Module), '--startup-project', $StartupProject) }

function Run-Ef([string[]] $cmdArgs){
  if (-not $cmdArgs -or $cmdArgs.Count -eq 0){ Fail 'EF invocation received empty argument list.' 20 }
  Step "Running: $($cmdArgs -join ' ')"
  & $cmdArgs[0] $cmdArgs[1..($cmdArgs.Length-1)]
  if ($LASTEXITCODE -ne 0){ Fail "Command failed: $($cmdArgs -join ' ')" $LASTEXITCODE }
}

function List-DbContextInfo() {
  Section "DbContext Info ($DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('dbcontext','info','--context', $DbContext)
  Run-Ef $efArgs
}

function List-Migrations() {
  Section "List Migrations ($DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations','list','--context', $DbContext)
  Run-Ef $efArgs
}

function Add-Migration() {
  Section "Add Migration ($DbContext)"
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
  $efArgs = Build-EfArgs @('migrations','add', $MigrationName,'--context', $DbContext,'--output-dir','EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Host "Migration '$MigrationName' added." -ForegroundColor Green
}

function Remove-Migration() {
  Section "Remove Last Migration ($DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('migrations','remove','--context', $DbContext)
  Run-Ef $efArgs
  Write-Host 'Last migration removed.' -ForegroundColor Green
}

function Apply-Migrations() {
  Section "Apply (Update Database) ($DbContext)"
  Ensure-DotNetTools
  $efArgs = Build-EfArgs @('database','update','--context', $DbContext)
  Run-Ef $efArgs
  Write-Host 'Database updated.' -ForegroundColor Green
}

function Undo-Migration() {
  Section "Undo (Revert to Previous) ($DbContext)"
  Ensure-DotNetTools
  # Determine second last migration for revert
  $listArgs = Build-EfArgs @('migrations','list','--context', $DbContext)
  Step 'Fetching migrations list'
  $output = (& $listArgs[0] $listArgs[1..($listArgs.Length-1)]) 2>&1
  if ($LASTEXITCODE -ne 0){ Fail 'Failed retrieving migrations list.' $LASTEXITCODE }
  $migrations = $output | Where-Object { $_ -match '^[0-9]{14}_.+' }
  if ($migrations.Count -lt 2){ Fail 'Not enough migrations to undo.' 4 }
  $target = $migrations[-2].Split()[0] # migration id portion
  Step "Target revert migration: $target"
  $undoArgs = Build-EfArgs @('database','update',$target,'--context',$DbContext)
  Run-Ef $undoArgs
  Write-Host "Reverted to migration $target" -ForegroundColor Green
}

function Show-MigrationStatus() {
  Section "Migration Status ($DbContext)"
  Ensure-DotNetTools
  $infraProj = Resolve-InfrastructureProject $Module
  $migrationsDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  $fsMigrations = @()
  if (Test-Path $migrationsDir){ $fsMigrations = Get-ChildItem -Path $migrationsDir -Filter '*.cs' | Where-Object { $_.Name -match '^[0-9]{14}_.+' } | Select-Object -ExpandProperty BaseName }
  $listArgs = Build-EfArgs @('migrations','list','--context',$DbContext)
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
  Section "Reset (Squash) Migrations ($DbContext)"
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
  $infraProj = Resolve-InfrastructureProject $Module
  $migDir = Join-Path (Split-Path $infraProj -Parent) 'EntityFramework/Migrations'
  if (Test-Path $migDir){ Step "Removing migration files in $migDir"; Get-ChildItem -Path $migDir -File -Force | Remove-Item -Force }
  Step 'Adding baseline migration (Initial)'
  $efArgs = Build-EfArgs @('migrations','add','Initial','--context',$DbContext,'--output-dir','EntityFramework/Migrations')
  Run-Ef $efArgs
  Write-Host 'Baseline migration created.' -ForegroundColor Green
}

function Export-DbContextScript() {
  Section "Export DbContext SQL Script ($DbContext)"
  Ensure-DotNetTools
  $outDir = Ensure-OutputDir $OutputDirectory
  $scriptPath = Join-Path $outDir "$Module-$DbContext-schema.sql"
  $efArgs = Build-EfArgs @('dbcontext','script','--context',$DbContext,'--output',$scriptPath)
  Run-Ef $efArgs
  Write-Host "Script written: $scriptPath" -ForegroundColor Green
}

function RemoveAll-Migrations() {
  Section "Remove ALL Migration Files ($DbContext)"
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
  $infraProj = Resolve-InfrastructureProject $Module
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
  -Module <name>             (default: CoreModule)
  -DbContext <name>          (default: CoreModuleDbContext)
  -StartupProject <path>     (default: src/Presentation.Web.Server/Presentation.Web.Server.csproj)
  -InfrastructureProject <path> override inferred module infrastructure project
  -MigrationName <name>      Name for new migration (add)
  -OutputDirectory <path>    (for script export, default: ./.tmp/ef)
  -Force                     Skip confirmation (reset)
  -Verbose                   Extra logging

Examples:
  pwsh -File .vscode/tasks-ef.ps1 list -Module CoreModule -DbContext CoreModuleDbContext
  pwsh -File .vscode/tasks-ef.ps1 add -Module CoreModule -MigrationName AddCustomerIndex
  pwsh -File .vscode/tasks-ef.ps1 undo -Module CoreModule
  pwsh -File .vscode/tasks-ef.ps1 script -Module CoreModule -OutputDirectory ./.tmp/scripts

Notes:
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