<#!
.SYNOPSIS
  Shared common functions for DevKit task scripts (settings, logging, module discovery).
.DESCRIPTION
  Provides reusable PowerShell functions:
    Load-Settings           -> loads and merges .env files automatically
    Get-Setting            -> gets any configuration setting
    Get-OutputDirectory    -> gets OUTPUT_DIRECTORY setting
    Get-ArtifactsDirectory -> gets ARTIFACTS_DIRECTORY setting
    Write-Section          -> formatted section header output
    Get-Modules            -> discovers modules under src/Modules (excludes Common/Shared patterns)
    Select-Module          -> resolves a module name (supports 'All') via env var, argument or interactive menu
.NOTES
  Environment variables:
    TEST_MODULE  -> preferred module name or 'All' for test tasks
    EF_MODULE    -> preferred module name for EF tasks
#>

$script:SettingsLoaded = $false

# ============ Environment Configuration ============

function Load-Settings {
  if ($script:SettingsLoaded) { return }

  $root = Split-Path -Path $PSScriptRoot -Parent

  # Array of .env file paths (order matters: later files override earlier ones)
  $envPaths = @(
    (Join-Path $root '.env'),
    (Join-Path $PSScriptRoot '.env')
  )

  # Load and merge all .env files (last setting wins)
  foreach ($envPath in $envPaths) {
    if (-not (Test-Path $envPath)) {
      if ($envPath -eq $envPaths[-1]) {
        throw "Configuration file not found: $envPath. DevKit requires .env to be present in the .bdk directory."
      }
      continue
    }

    $isDevKit = $envPath -eq $envPaths[-1]
    $source = if ($isDevKit) { 'DevKit' } else { 'repository' }
    # Write-Debug "[env] Loading $source settings from: $envPath"

    foreach ($line in Get-Content $envPath) {
      $line = $line.Trim()
      if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
      $parts = $line -split '=', 2
      if ($parts.Length -ne 2) { continue }
      $key = $parts[0].Trim()
      $val = $parts[1].Trim().Trim("'""")
      if (-not [string]::IsNullOrEmpty($key)) {
        [System.Environment]::SetEnvironmentVariable($key, $val, 'Process')
        if ($isDevKit) {
          # Write-Debug "[env] $key=$val"
        }
      }
    }
  }

  $script:SettingsLoaded = $true
}

function Get-Setting {
  param(
    [Parameter(Mandatory = $true)] [string] $Key,
    [string] $Default
  )

  $value = [System.Environment]::GetEnvironmentVariable($Key, 'Process')
  if ([string]::IsNullOrEmpty($value)) {
    if ($PSBoundParameters.ContainsKey('Default')) {
      Write-Warn "[setting] $Key not set, using default: $Default"
      return $Default
    }
    Write-Warn "[setting] WARNING: Setting '$Key' not configured"
    return $null
  }
  return $value
}

function Get-OutputDirectory {
  $value = [System.Environment]::GetEnvironmentVariable('OUTPUT_DIRECTORY', 'Process')
  if ([string]::IsNullOrEmpty($value)) {
    throw "Setting 'OUTPUT_DIRECTORY' not configured. Ensure .bdk/.env or root .env contains OUTPUT_DIRECTORY=<value>"
  }
  return $value
}

function Get-ArtifactsDirectory {
  $value = [System.Environment]::GetEnvironmentVariable('ARTIFACTS_DIRECTORY', 'Process')
  if ([string]::IsNullOrEmpty($value)) {
    throw "Setting 'ARTIFACTS_DIRECTORY' not configured. Ensure .bdk/.env or root .env contains ARTIFACTS_DIRECTORY=<value>"
  }
  return $value
}

# ============ Logging Functions ============

function Fail([string] $Message, [int] $Code = 1) { Write-Error $Message; exit $Code }

function Write-Section([string] $Msg) {
  Write-Host "`n=== $Msg ===" -ForegroundColor Cyan
}

function Write-Step([string] $Msg) {
  Write-Host " => $Msg" -ForegroundColor DarkCyan
}

function Write-Debug([string] $Msg) {
  Write-Host $Msg -ForegroundColor DarkGray
}

function Write-Info([string] $Msg) {
  Write-Host $Msg -ForegroundColor Green
}

function Write-Warn([string] $Msg) {
  Write-Host $Msg -ForegroundColor DarkYellow
}

function Write-Error([string] $Msg) {
  Write-Host $Msg -ForegroundColor Red
}

# ============ .NET Tools ============

function Ensure-DotNetTools {
  Write-Step "Restoring dotnet tools"
  dotnet tool restore | Out-Null
  if ($LASTEXITCODE -ne 0) {
    throw "Tools restore failed (exit code: $LASTEXITCODE)"
  }
}

function Ensure-DotNetRestore([string] $SolutionPath) {
  Write-Step "Restoring packages"
  dotnet restore $SolutionPath | Out-Null
  if ($LASTEXITCODE -ne 0) {
    throw "Packages restore failed (exit code: $LASTEXITCODE)"
  }
}

# ============ Module Discovery & Selection ============

function Get-Modules {
  param(
    [string] $Root = (Split-Path $PSScriptRoot -Parent)
  )
  $modulesRoot = Join-Path $Root 'src/Modules'
  if (-not (Test-Path $modulesRoot)) { return @() }
  $names = Get-ChildItem -Path $modulesRoot -Directory | ForEach-Object { $_.Name }
  $filtered = $names | Where-Object { $_ -notmatch '^(?:Common|Shared)$' }
  [string[]]$filtered
}

function Ensure-SpectreConsoleAvailable {
  param([switch] $Quiet)
  $moduleName = 'PwshSpectreConsole'
  $env:IgnoreSpectreEncoding = $true
  if (Get-Module -ListAvailable -Name $moduleName) {
    # if (-not $Quiet) { Write-Info "SpectreConsole module available." }
  }
  else {
    if (-not $Quiet) { Write-Warn "SpectreConsole module not found. Installing (CurrentUser)..." }
    try {
      Install-Module -Name $moduleName -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
      if (-not $Quiet) { Write-Info "SpectreConsole module installation successful." }
    }
    catch {
      Write-Error "Failed to install SpectreConsole module: $($_.Exception.Message)."; throw "SpectreConsole install failed"
    }
  }
  try { Import-Module $moduleName -Force; return $true } catch { Write-Error "Failed to import SpectreConsole module: $($_.Exception.Message)."; throw "SpectreConsole import failed" }
}

function Read-Selection {
  param(
    [string] $Title,
    [string[]] $Choices,
    [switch] $EnableSearch,
    [switch] $AddCancel,
    [int] $PageSize = 10
  )
  if (-not (Ensure-SpectreConsoleAvailable -Quiet)) {
    throw "SpectreConsole module required for interactive selection but not available."
  }
  if ($AddCancel) { $Choices += 'Cancel' }
  $s = Read-SpectreSelection -Title $Title -Choices $Choices -EnableSearch:$EnableSearch -PageSize $PageSize -Color DeepSkyBlue3
  if (-not $s -or $s -eq 'Cancel') { Write-Step 'Cancelled.'; return $null }
  return $s
}

function Select-Module {
  param(
    [string[]] $Available,
    [string] $Requested,
    [string] $EnvVarName = 'TEST_MODULE',
    [switch] $AllowAll
  )
  if (-not $Available -or $Available.Count -eq 0) { throw 'No modules discovered.' }

  $envValue = if ($EnvVarName) { (Get-Item -Path Env:$EnvVarName -ErrorAction SilentlyContinue).Value } else { $null }
  $selection = if ($Requested) { $Requested } elseif ($envValue) { $envValue } else { $null }
  if ($selection -and $AllowAll -and $selection -ieq 'All') { return 'All' }
  if ($selection -and $selection -in $Available) { return $selection }
  if ($selection -and $selection -notin $Available) { Write-Warn "Requested module '$selection' not in available set: $($Available -join ', '). Ignoring."; $selection = $null }

  $choices = @()
  if ($AllowAll) { $choices += 'All' }
  $choices += $Available
  $choices += 'Cancel'
  $selected = Read-Selection -Title 'Select Module' -Choices $choices -EnableSearch
  if (-not $selected -or $selected -eq 'Cancel') { throw [System.OperationCanceledException] 'Module selection cancelled.' }
  if ($AllowAll -and $selected -eq 'All') { return 'All' }
  if ($selected -in $Available) { return $selected }
  throw "Unexpected selection '$selected'"
}

function Select-Rid {
  param([string[]]$Rids = @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-musl-x64', 'linux-musl-arm64', 'linux-arm', 'linux-arm64'))
  if (-not $Rids) { return $null }
  $selection = Read-Selection -Title 'Select target (blank for framework-dependent)' -Choices ($Rids + 'Framework-Dependent' + 'Cancel') -EnableSearch -PageSize 15
  if (-not $selection -or $selection -eq 'Cancel') { return $null }
  if ($selection -eq 'Framework-Dependent') { return $null }
  return $selection
}

function Open-File {
  param([string]$Path)
  if (-not $Path) { return }
  if (-not (Test-Path $Path)) { Write-Error "File not found: $Path"; return }
  try {
    if ($IsWindows) {
      Start-Process -FilePath $Path -ErrorAction Stop
    }
    else {
      # prefer xdg-open, fallback to open (macOS)
      if (Get-Command xdg-open -ErrorAction SilentlyContinue) {
        & xdg-open $Path 2>$null
      }
      elseif (Get-Command open -ErrorAction SilentlyContinue) {
        & open $Path 2>$null
      }
      else {
        Write-Error "No known opener found for this platform. File located at: $Path"
      }
    }
    Write-Info "Opened: $Path"
  }
  catch {
    Write-Error "Error opening: $($_.Exception.Message)"
  }
}

function Select-Pid($title) {
  Import-Module PwshSpectreConsole -ErrorAction Stop
  $procs = Get-Process | Where-Object { $_.Id -gt 0 }
  if ($script:DotNetOnly) { $procs = $procs | Where-Object { $_.ProcessName -match 'dotnet|' } }
  $procs = $procs | Sort-Object ProcessName, Id
  $rows = @()
  foreach ($p in $procs) {
    $label = "$($p.ProcessName) (#$($p.Id))"
    if ($rows -notcontains $label) { $rows += $label }
  }
  if (-not $rows) { Write-Host 'No matching processes found.' -ForegroundColor Yellow; return $null }
  $choices = $rows + 'Cancel'
  $sel = Read-Selection -Title $title -Choices $choices -EnableSearch -PageSize 15
  Write-Host "Raw selection: '$sel'" -ForegroundColor DarkGray
  if ([string]::IsNullOrWhiteSpace($sel) -or $sel -eq 'Cancel') { return $null }
  if ($sel -match '\(#(\d+)\)$') { Write-Host "Selected PID: $($Matches[1])" -ForegroundColor DarkGray; return [int]$Matches[1] }
  Write-Host "Could not parse PID from selection: $sel" -ForegroundColor Yellow
  return $null
}

function Ensure-DockerImage([string] $Image) {
  Write-Step "Ensuring docker image '$Image' is available"
  Write-Debug "docker images --format '{{.Repository}}:{{.Tag}}'"
  $exists = docker images --format '{{.Repository}}:{{.Tag}}' | Where-Object { $_ -eq $Image }
  if (-not $exists) { Write-Step "Pulling image $Image"; docker pull $Image | Out-Null }
}

function Resolve-Path-Safely([string] $Path) {
  $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
  return $resolved
}

function Ensure-Path([string] $Path) {
  $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
  New-Item -ItemType Directory -Force -Path $resolved | Out-Null
  return $resolved
}

function Clean-Path([string] $Path) {
  $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
  if (Test-Path $resolved) {
    Remove-Item -Path $resolved -Recurse -Force
    Write-Info "Cleaned path: $resolved"
  }
}