<#!
.SYNOPSIS
  Wrapper/launcher for VS Code tasks defined in .vscode/tasks.json usable outside VS Code.
.DESCRIPTION
  Parses .vscode/tasks.json and exposes shell/process tasks via an interactive PSMenu (if available) or a
  non-interactive listing. Selecting a task executes the underlying command with its arguments and environment.
  After completion, the menu is shown again until user exits.

.PARAMETER Task
  Optional exact task label to run directly (bypasses menu).
.PARAMETER List
  List all available task labels (alphabetically) then exit.
.PARAMETER NonInteractive
  Force non-interactive mode (no menu). Useful for CI when specifying -Task.

.EXAMPLES
  pwsh -File tasks.ps1                # interactive menu loop
  pwsh -File tasks.ps1 -List          # print all task labels
  pwsh -File tasks.ps1 -Task 'Solution [dotnet build]'  # run specific task once

.NOTES
  Requires tasks.json to be present at .vscode/tasks.json. Only tasks of type 'shell' or 'process' are considered.
#>
param(
  [string] $Task,
  [switch] $List,
  [switch] $NonInteractive
)

$ErrorActionPreference = 'Stop'
$tasksFile = Join-Path $PSScriptRoot '.vscode/tasks.json'
if (-not (Test-Path $tasksFile)) { Write-Error "tasks.json not found at $tasksFile"; exit 1 }

# Try to reuse helper for PSMenu installation if present
$helpersPath = Join-Path $PSScriptRoot '.vscode/tasks-helpers.ps1'
if (Test-Path $helpersPath) { . $helpersPath }

function Load-Tasks {
  $raw = Get-Content -Raw -Path $tasksFile
  # Remove // line comments and /* */ blocks, VS Code allows jsonc style.
  $raw = ($raw -replace '(?m)//.*$','')
  $raw = ($raw -replace '/\*[^*]*\*+(?:[^/*][^*]*\*+)*/','')
  # Remove trailing commas before object/array close.
  $raw = ($raw -replace ',\s*([}\]])','$1')
  $json = $raw | ConvertFrom-Json -Depth 8
  if (-not $json.tasks) { return @() }
  $valid = $json.tasks | Where-Object { $_.type -in @('shell','process') } | ForEach-Object {
    [PSCustomObject]@{
      label = $_.label
      type = $_.type
      command = $_.command
      args = if ($_.args) { $_.args } else { @() }
      options = $_.options
      problemMatcher = $_.problemMatcher
    }
  }
  $valid | Sort-Object label
}

function Resolve-EnvOptions($task) {
  if (-not $task.options -or -not $task.options.env) { return @{} }
  $raw = Get-Content -Raw -Path $tasksFile
  $raw = ($raw -replace '(?m)//.*$','')
  $raw = ($raw -replace '/\*[^*]*\*+(?:[^/*][^*]*\*+)*/','')
  $raw = ($raw -replace ',\s*([}\]])','$1')
  $json = $raw | ConvertFrom-Json -Depth 8
  $envMap = @{}
  foreach ($kv in $task.options.env.GetEnumerator()) {
    $value = $kv.Value
    if ($value -is [string] -and $value -match '^\$\{input:(?<id>[^}]+)\}$') {
      $id = $Matches.id
      $inputDef = $json.inputs | Where-Object { $_.id -eq $id }
      if ($inputDef) { $value = $inputDef.default }
    }
    $envMap[$kv.Key] = $value
  }
  return $envMap
}

function Invoke-Task($task) {
  Write-Host "=== Running Task: $($task.label) ===" -ForegroundColor Cyan
  $envMap = Resolve-EnvOptions $task
  if ($envMap.Count -gt 0) {
    Write-Host "Environment overrides:" -ForegroundColor DarkGray
    foreach ($k in $envMap.Keys) { Write-Host "  $k=$($envMap[$k])" -ForegroundColor DarkGray }
  }
  $originalEnv = @{}
  foreach ($k in $envMap.Keys) {
    $originalEnv[$k] = (Get-Item -Path Env:$k -ErrorAction SilentlyContinue).Value
    Set-Item -Path Env:$k -Value $envMap[$k]
  }
  # Expand placeholders in args (currently supports ${workspaceFolder})
  $workspace = $PSScriptRoot
  [string[]]$expandedArgs = @()
  foreach ($a in $task.args) {
    if ($a -is [string]) {
      $ea = $a -replace '\$\{workspaceFolder\}',$workspace
      $expandedArgs += $ea
    } else { $expandedArgs += $a }
  }
  # Auto-append -NonInteractive for known devkit scripts needing stable behavior outside VS Code.
  $scriptPathArg = $expandedArgs | Select-Object -First 3 | Select-Object -Last 1
  $joinedArgs = ($expandedArgs -join ' ')
  if ($joinedArgs -match '\.vscode/tasks-tests.ps1' -and ($expandedArgs -notcontains '-NonInteractive')) { $expandedArgs += '-NonInteractive' }
  if ($joinedArgs -match '\.vscode/tasks-ef.ps1' -and ($expandedArgs -notcontains '-NonInteractive')) { $expandedArgs += '-NonInteractive' }
  $cmdLinePreview = $task.command + ' ' + ($expandedArgs -join ' ')
  Write-Host "Command: $cmdLinePreview" -ForegroundColor DarkGray
  try {
    if ($task.type -eq 'process') {
      $output = & $task.command @expandedArgs 2>&1
    } else {
      $output = & $task.command @expandedArgs 2>&1
    }
    $exitCode = $LASTEXITCODE
  } catch {
    Write-Error "Task execution failed: $($_.Exception.Message)"; $exitCode = 1
  } finally {
    foreach ($k in $envMap.Keys) {
      if ($originalEnv.ContainsKey($k)) {
        Set-Item -Path Env:$k -Value $originalEnv[$k]
      } else {
        Remove-Item Env:$k -ErrorAction SilentlyContinue
      }
    }
  }
  if ($output) { $output | ForEach-Object { Write-Host $_ } }
  if ($exitCode -ne 0) { Write-Host "Task Exit Code: $exitCode" -ForegroundColor Red } else { Write-Host "Task Exit Code: $exitCode" -ForegroundColor Green }
  return $exitCode
}

function Show-MenuLoop($tasks) {
  if (-not $tasks -or $tasks.Count -eq 0) { Write-Host 'No tasks available.' -ForegroundColor Red; return }
  $usePSMenu = $false
  if (Get-Command Show-Menu -ErrorAction SilentlyContinue) { $usePSMenu = $true } elseif (Get-Module -ListAvailable -Name PSMenu) { Import-Module PSMenu -Force; $usePSMenu = $true }
  elseif (Get-Module -ListAvailable -Name PSMenu) { Import-Module PSMenu -Force; $usePSMenu = $true }
  elseif (Get-Command Install-Module -ErrorAction SilentlyContinue) {
    try { Install-Module -Name PSMenu -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop; Import-Module PSMenu -Force; $usePSMenu = $true } catch { Write-Host 'PSMenu unavailable; falling back to numeric selection.' -ForegroundColor DarkYellow }
  }
  while ($true) {
    Write-Host "Select a task (Ctrl+C to exit):" -ForegroundColor Green
    $labels = $tasks | Select-Object -ExpandProperty label
  $indexed = for ($i=0; $i -lt $labels.Count; $i++){ "[$i] $($labels[$i])" }
    $choiceLabel = $null
    if ($usePSMenu) {
      $choiceLabel = Show-Menu -MenuItems $indexed
    } else {
      for ($i=0; $i -lt $labels.Count; $i++){ Write-Host "  [$i] $($labels[$i])" }
      $raw = Read-Host 'Enter choice'
      if ($raw -match '^[0-9]+$') { $idx = [int]$raw; if ($idx -ge 0 -and $idx -lt $labels.Count) { $choiceLabel = $indexed[$idx] } }
    }
    if (-not $choiceLabel) { Write-Host 'No selection; exiting.' -ForegroundColor DarkYellow; return }
    if ($choiceLabel -match '^\[(?<idx>\d+)\]') {
      $idx = [int]$Matches.idx
      $selected = $tasks[$idx]
      Invoke-Task $selected | Out-Null
      # loop resumes
    }
  }
}

$allTasks = Load-Tasks
if ($List) {
  $allTasks | Select-Object -ExpandProperty label | ForEach-Object { $_ } ; exit 0
}

if ($Task) {
  $match = $allTasks | Where-Object { $_.label -eq $Task }
  if (-not $match) { Write-Error "Task '$Task' not found."; Write-Host 'Available:'; $allTasks | Select-Object -ExpandProperty label; exit 2 }
  Invoke-Task $match | Out-Null; exit $LASTEXITCODE
}

if ($NonInteractive) {
  Write-Host 'NonInteractive mode requires -Task parameter.' -ForegroundColor Red; exit 3
}

Show-MenuLoop -tasks $allTasks
