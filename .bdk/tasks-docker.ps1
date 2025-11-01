<#!
.SYNOPSIS
  Multi-task helper script for local development automation.

.DESCRIPTION
  Provides reusable PowerShell functions invoked via `pwsh -File .vscode/task.ps1 <Command> [options]`.
  Designed for VS Code tasks.json entries. Add more functions as needed (e.g., db-migrate, test, etc.).

.EXAMPLES
  pwsh -File .vscode/task.ps1 docker-build-run
  pwsh -File .vscode/task.ps1 docker-stop
  pwsh -File .vscode/task.ps1 docker-remove
  pwsh -File .vscode/task.ps1 help

.NOTES
  Uses exit codes for task success/failure. Writes minimal structured logs.
#>
param(
  [Parameter(Position=0)] [string] $Command = 'help',
  [Parameter()] [string] $ImageTag = '',
  [Parameter()] [string] $ContainerName = '',
  [Parameter()] [string] $Dockerfile = 'src/Presentation.Web.Server/Dockerfile',
  [Parameter()] [string] $ProjectDockerContext = '.',
  [Parameter()] [switch] $NoCache,
  [Parameter()] [string] $Network = '',
  [Parameter()] [int] $HostPort = 8080,
  [Parameter()] [int] $ContainerPort = 8080,
  [Parameter()] [string] $ComposeFile = 'docker-compose.yml',
  [Parameter()] [switch] $Pull
)
# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
$OutputDirectory = Join-Path $Root ".tmp"

# function Write-Section([string] $Text) { Write-Host "`n=== $Text ===" -ForegroundColor Cyan }
function Write-Step([string] $Text) { Write-Host "-- $Text" -ForegroundColor Cyan }
function Write-Command([string] $Text) { Write-Host "$Text" -ForegroundColor DarkGray }
function Fail([string] $Message, [int] $Code = 1) { Write-Error $Message; exit $Code }

function Ensure-Network() {
  param([string] $Name)
  Write-Step "Ensuring docker network '$Name' exists"
  if (-not (docker network ls --format '{{.Name}}' | Select-String -Quiet -Pattern "^$Name$")) {
    docker network create $Name | Out-Null
    Write-Step "Created network $Name"
  }
}

function Docker-Build() {
  param([string] $Tag, [string] $File, [string] $Context, [switch] $NoCache)
  # Write-Section "Docker Build ($Tag)"
  $args = @('build', '-t', $Tag, '-f', $File)
  if ($NoCache) { $args += '--no-cache' }
  $args += $Context
  Write-Command "docker $($args -join ' ')"
  docker @args
  if ($LASTEXITCODE -ne 0) { Fail "Docker build failed." $LASTEXITCODE }
}

function Docker-Run() {
  param(
    [string] $Tag,
    [string] $Name,
    [string] $Network,
    [int] $HostPort,
    [int] $ContainerPort
  )
  # Write-Section "Docker Run ($Name)"
  Write-Step 'Removing any existing container'
  docker stop $Name 2>$null | Out-Null
  docker rm $Name 2>$null | Out-Null

  $logsDir = Join-Path (Get-Location) 'logs'
  if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Force -Path $logsDir | Out-Null }

  $jsonPath = Join-Path $Root ".docker/$Name.json"
  if (Test-Path $jsonPath) {
    Write-Step "Reading settings from $jsonPath"
    $jsonContent = Get-Content $jsonPath -Raw | ConvertFrom-Json
    $envVars += $jsonContent.Environment
  }
  else{
    Write-Host "No settings file found at $jsonPath" -ForegroundColor Red
  }

  $runArgs = @('run','-d','--name', $Name,'-p',"$HostPort`:$ContainerPort",'--network', $Network)
  foreach($e in $envVars){ $runArgs += @('-e', $e) }
  $runArgs += @('-v',"$(Resolve-Path $logsDir):/.logs", $Tag)
  Write-Command "docker $($runArgs -join ' ')"
  docker @runArgs
  if ($LASTEXITCODE -ne 0) { Fail "Docker run failed." $LASTEXITCODE }
  Write-Step "Container running: http://localhost:$HostPort"

  # show concise container details (ID, Name, Status, Ports)
  try {
    $fmt = '{{.ID}};{{.Names}};{{.Status}};{{.Ports}}'
    Write-Step "Container details:"
    Write-Command "docker ps --filter name=$Name --format `"$fmt`""
    $info = docker ps --filter "name=$Name" --format $fmt 2>$null
    if ($info) {
      $cols = $info -split ";"
      Write-Host ("ID: {0}  `nName: {1}  `nStatus: {2}  `nPorts: {3}" -f $cols[0], $cols[1], $cols[2], ($cols[3] -replace '\s+', ' ')) -ForegroundColor Green
    } else {
      Write-Step "No running container matched the name: $Name"
    }
  } catch {
    Write-Step "Failed to list container details: $($_.Exception.Message)"
  }
}

function Docker-Stop() {
  param([string] $Name)
  # Write-Section "Docker Stop ($Name)"
  Write-Command "docker stop $Name"
  docker stop $Name 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Stopped (if existed).' }
}

function Docker-Remove() {
  param(
    [string] $Name,
    [string] $Network
  )
  # Write-Section "Docker Remove ($Name)"
  Write-Step "Removing container (if present): $Name"
  Write-Command "docker rm -f $Name"
  docker rm -f $Name 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Container removed (if existed).' } else { Write-Step 'Container removal attempted (may not have existed).' }

  if ($Network) {
    Write-Step "Attempting to remove network: $Network (if present and not in use)"
    try {
      $exists = docker network ls --format '{{.Name}}' | Select-String -Quiet -Pattern "^$Network$"
      if ($exists) {
        Write-Command "docker network rm -f $Network"
        docker network rm $Network 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
          Write-Step "Network removed: $Network"
        } else {
          Write-Step "Network removal skipped or failed (likely in use): $Network"
        }
      } else {
        Write-Step "Network not found: $Network"
      }
    } catch {
      Write-Step "Network removal encountered an error: $($_.Exception.Message)"
    }
  }
}

function Compose-Up() {
  param([string] $File,[switch] $Pull)
  # Write-Section "docker compose up ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $args = @('compose','-f', $File,'up','-d')
  if ($Pull) { $args = @('compose','-f', $File,'pull'); Write-Command "docker $($args -join ' ')"; docker @args; if ($LASTEXITCODE -ne 0){ Fail 'docker compose pull failed.' $LASTEXITCODE }; $args = @('compose','-f', $File,'up','-d') }
  Write-Command "docker $($args -join ' ')"
  docker @args
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose up failed.' $LASTEXITCODE }
  Write-Step 'Compose stack started.'
}

function Compose-Down-Clean() {
  param([string] $File)
  # Write-Section "docker compose down + prune ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $downArgs = @('compose','-f', $File,'down','--remove-orphans','--volumes')
  Write-Command "docker $($downArgs -join ' ')"
  docker @downArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose down failed.' $LASTEXITCODE }
  # Remove images defined in the compose file (basic parse by 'image:' tokens)
  $images = Select-String -Path $File -Pattern 'image:\s*(\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
  foreach($img in $images) {
    Write-Step "Removing image $img (if present)"
    docker rmi $img 2>$null | Out-Null
  }
  Write-Step 'Compose stack fully cleaned.'
}

function Compose-Down() {
  param([string] $File)
  # Write-Section "docker compose down ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $downArgs = @('compose','-f', $File,'down')
  Write-Command "docker $($downArgs -join ' ')"
  docker @downArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose down failed.' $LASTEXITCODE }
  Write-Step 'Compose stack stopped.'
}

function Read-SpectreSelectionOrNull([string]$title, [string[]]$choices) {
  # Spectre is present; present choices with Cancel as last option and return $null on cancel
  $choices += 'Cancel'
  $sel = Read-SpectreSelection -Title $title -Choices $choices -EnableSearch -PageSize 15
  if (-not $sel -or $sel -eq 'Cancel') { return $null }
  return $sel
}

function Compose-Recreate([string] $ComposeFile) {
  Write-Step "Retrieving running containers..."
  $fmt = '{{.Names}}|{{.Image}}|{{.Status}}'
  $raw = docker ps --format $fmt 2>$null
  if (-not $raw) {
    Write-Step "No running containers found."
    return
  }

  $names = @()
  $choices = @()
  foreach ($line in $raw) {
    $parts = $line -split '\|', 3
    $name = $parts[0]
    $image = if ($parts.Length -ge 2) { $parts[1] } else { '' }
    $status = if ($parts.Length -ge 3) { $parts[2] } else { '' }
    $names += $name
    $choices += ("{0}  ({1}) - {2}" -f $name, $image, $status)
  }

  # Insert an "All" option at the top to recreate all services
  $allLabel = 'All services (recreate all)'
  $choices = @($allLabel) + $choices

  $selection = Read-SpectreSelectionOrNull 'Select container (Cancel to quit)' $choices
  if (-not $selection) { Write-Step 'Cancelled.'; return }

  $idx = $choices.IndexOf($selection)
  if ($idx -lt 0) { Fail "Invalid selection mapping." 2 }

  # If user selected the top "All" option, run compose without service name
  if ($idx -eq 0) {
    Write-Step "Recreating ALL services (compose file: $ComposeFile)"
    $args = @('compose','-f',$ComposeFile,'up','-d','--force-recreate')
    Write-Command "docker $($args -join ' ')"
    docker @args
    if ($LASTEXITCODE -ne 0) { Fail "docker compose recreate (all) failed." $LASTEXITCODE }
    Write-Step "Recreate requested for ALL services"
    return
  }

  # Map selection back to container (offset by 1 because of the All entry)
  $selectedName = $names[$idx - 1]
  $selectedImage = ($raw[$idx - 1] -split '\|')[1]

  # Prefer compose service label provided by docker (com.docker.compose.service)
  $serviceName = $null
  try {
    $label = docker inspect --format '{{ index .Config.Labels "com.docker.compose.service" }}' $selectedName 2>$null
    if ($label) { $label = $label.Trim() }
    if ($label -and $label -ne '<no value>') {
      $serviceName = $label
    }
  } catch {
    $serviceName = $null
  }

  # Fallback: strip CONTAINER_PREFIX_ if present, otherwise take last underscore segment
  if (-not $serviceName) {
    $prefix = $env:CONTAINER_PREFIX
    if ($prefix -and $selectedName.StartsWith("$prefix`_")) {
      $serviceName = $selectedName.Substring($prefix.Length + 1)
    } elseif ($selectedName -match '_') {
      $serviceName = ($selectedName -split '_')[-1]
    } else {
      $serviceName = $selectedName
    }
  }

  Write-Step "Resolved compose service name: $serviceName (from container: $selectedName, image: $selectedImage)"
  Write-Step "Recreating service/container: $serviceName (compose file: $ComposeFile)"
  $args = @('compose','-f',$ComposeFile,'up','-d','--force-recreate',$serviceName)
  Write-Command "docker $($args -join ' ')"
  docker @args
  if ($LASTEXITCODE -ne 0) { Fail "docker compose recreate failed for $serviceName." $LASTEXITCODE }
  Write-Step "Recreate requested for: $serviceName"
}

# Update Help to include the new command
function Help() {
  @'
Usage: pwsh -File .vscode/task.ps1 <command> [options]

Commands:
  docker-build-run   Build image (optionally --NoCache) & run container
  docker-build       Build image only
  docker-build-debug Build image (DEBUG tag) using build arg CONFIG=Debug
  docker-build-release Build image (RELEASE tag) using build arg CONFIG=Release
  docker-run         Run container (assumes image built)
  docker-stop        Stop container
  docker-remove      Remove (force) container
  docker-recreate    Recreate a specific container/service (interactive selection)
  compose-up         docker compose up (uses -ComposeFile, add -Pull to fetch latest)
  compose-down       docker compose down (stop stack, keep volumes & images)
  compose-down-clean docker compose down (remove volumes, orphans) and delete images
  help               Show this help

Common Parameters:
  -ImageTag <tag>            (default: localhost:5500/bdk_gettingstarted-web:latest)
  -ContainerName <name>      (default: bdk_gettingstarted-web)
  -Dockerfile <path>         (default: src/Presentation.Web.Server/Dockerfile)
  -ProjectDockerContext <dir>(default: .)
  -Network <name>            (default: bdk_gettingstarted)
  -HostPort <port>           (default: 8080)
  -ContainerPort <port>      (default: 8080)
  -NoCache                   (skip build cache)
  -ComposeFile <file>        (default: docker-compose.yml)
  -Pull                      (with compose-up: pre-pull images)
'@ | Write-Host
}

switch ($Command.ToLower()) {
  'docker-build-run' {
    Ensure-Network -Name $Network
    Docker-Build -Tag $ImageTag -File $Dockerfile -Context $ProjectDockerContext -NoCache:$NoCache
    Docker-Run -Tag $ImageTag -Name $ContainerName -Network $Network -HostPort $HostPort -ContainerPort $ContainerPort
  }
  'docker-build-debug' {
    $args = @('build','-t',$ImageTag,'-f',$Dockerfile,'--build-arg','CONFIG=Debug')
    if($NoCache){ $args += '--no-cache' }
    $args += $ProjectDockerContext
    Write-Command "docker $($args -join ' ')"
    docker @args
    if($LASTEXITCODE -ne 0){ Fail 'Docker debug build failed.' $LASTEXITCODE }
  }
  'docker-build-release' {
    $args = @('build','-t',$ImageTag,'-f',$Dockerfile,'--build-arg','CONFIG=Release')
    if($NoCache){ $args += '--no-cache' }
    $args += $ProjectDockerContext
    Write-Command "docker $($args -join ' ')"
    docker @args
    if($LASTEXITCODE -ne 0){ Fail 'Docker release build failed.' $LASTEXITCODE }
  }
  'docker-run' {
    Ensure-Network -Name $Network
    Docker-Run -Tag $ImageTag -Name $ContainerName -Network $Network -HostPort $HostPort -ContainerPort $ContainerPort
  }
  'docker-stop' { Docker-Stop -Name $ContainerName }
  'docker-remove' { Docker-Remove -Name $ContainerName -Network $Network }
  'compose-recreate' {
    # Interactive recreate: list running containers and run `docker compose -f <ComposeFile> up -d --force-recreate <name>`
    Compose-Recreate -ComposeFile $ComposeFile
  }
  'compose-up' { Compose-Up -File $ComposeFile -Pull:$Pull }
  'compose-down' { Compose-Down -File $ComposeFile }
  'compose-down-clean' { Compose-Down-Clean -File $ComposeFile }
  default { Help }
}

exit 0