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
  [Parameter(Position = 0)] [string] $Command = 'help',
  [Parameter()] [string] $ImageTag = '',
  [Parameter()] [string] $ContainerName = '',
  [Parameter()] [string] $Dockerfile = '',
  [Parameter()] [string] $ProjectDockerContext = '.',
  [Parameter()] [switch] $NoCache,
  [Parameter()] [string] $Network = '',
  [Parameter()] [int] $HostPort = 8080,
  [Parameter()] [int] $ContainerPort = 8080,
  [Parameter()] [string] $ComposeFile = '',
  [Parameter()] [switch] $Pull,
  [Parameter()] [string] $RegistryHost = ''
)
# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

$OutputDirectory = Join-Path $Root (Get-OutputDirectory) 'docker'

if ( -not $RegistryHost) {
  $RegistryHost = Get-Setting 'REGISTRY_HOST'
}
if ( -not $Network) {
  $Network = Get-Setting 'NETWORK_NAME'
}
if ( -not $ContainerName) {
  $containerPrefix = Get-Setting 'CONTAINER_PREFIX'
  $ContainerName = "${containerPrefix}-web"
}
if ( -not $ImageTag) {
  $ImageTag = "${RegistryHost}/${ContainerName}:latest"
}
if ( -not $Dockerfile) {
  $Dockerfile = Get-Setting 'DOCKER_FILE_PATH'
}
if ( -not $ComposeFile) {
  $ComposeFile = Get-Setting 'DOCKER_COMPOSE_PATH'
}

function Ensure-Network() {
  param([string] $Name)
  Write-Step "Ensuring docker network '$Name' exists"
  if (-not (docker network ls --format '{{.Name}}' | Select-String -Quiet -Pattern "^$Name$")) {
    Write-Debug "docker network create $Name"
    docker network create $Name | Out-Null
    Write-Step "Created network $Name"
  }
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
  else {
    Write-Error "No settings file found at $jsonPath"
  }

  $dockerArgs = @('run', '-d', '--name', $Name, '-p', "$HostPort`:$ContainerPort", '--network', $Network)
  foreach ($e in $envVars) { $dockerArgs += @('-e', $e) }
  $dockerArgs += @('-v', "$(Resolve-Path $logsDir):/.logs", $Tag)
  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail "Docker run failed." $LASTEXITCODE }
  Write-Info "Container running: http://localhost:$HostPort"

  # show concise container details (ID, Name, Status, Ports)
  try {
    $fmt = '{{.ID}};{{.Names}};{{.Status}};{{.Ports}}'
    Write-Step "Container details:"
    Write-Debug "docker ps --filter name=$Name --format `"$fmt`""
    $info = docker ps --filter "name=$Name" --format $fmt 2>$null
    if ($info) {
      $cols = $info -split ";"
      Write-Info ("ID: {0}  `nName: {1}  `nStatus: {2}  `nPorts: {3}" -f $cols[0], $cols[1], $cols[2], ($cols[3] -replace '\s+', ' '))
    }
    else {
      Write-Error "No running container matched the name: $Name"
    }
  }
  catch {
    Write-Error "Failed to list container details: $($_.Exception.Message)"
  }
}

function Docker-Stop() {
  param([string] $Name)
  # Write-Section "Docker Stop ($Name)"
  Write-Debug "docker stop $Name"
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
  Write-Debug "docker rm -f $Name"
  docker rm -f $Name 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Container removed (if existed).' } else { Write-Step 'Container removal attempted (may not have existed).' }

  if ($Network) {
    Write-Step "Attempting to remove network: $Network (if present and not in use)"
    try {
      $exists = docker network ls --format '{{.Name}}' | Select-String -Quiet -Pattern "^$Network$"
      if ($exists) {
        Write-Debug "docker network rm -f $Network"
        docker network rm $Network 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
          Write-Step "Network removed: $Network"
        }
        else {
          Write-Step "Network removal skipped or failed (likely in use): $Network"
        }
      }
      else {
        Write-Step "Network not found: $Network"
      }
    }
    catch {
      Write-Step "Network removal encountered an error: $($_.Exception.Message)"
    }
  }
}

function Docker-Remove-Image() {
  param([string] $Tag)
  # Write-Section "Docker Remove Image ($Tag)"
  Write-Step "Removing image (if present): $Tag"
  Write-Debug "docker rmi -f $Tag"
  docker rmi -f $Tag 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Image removed (if existed).' } else { Write-Step 'Image removal attempted (may not have existed).' }
}

function Compose-Up() {
  param([string] $File, [switch] $Pull)
  # Write-Section "docker compose up ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $dockerArgs = @('compose', '-f', $File, 'up', '-d')
  if ($Pull) { $dockerArgs = @('compose', '-f', $File, 'pull'); Write-Debug "docker $($dockerArgs -join ' ')"; docker @dockerArgs; if ($LASTEXITCODE -ne 0) { Fail 'docker compose pull failed.' $LASTEXITCODE }; $dockerArgs = @('compose', '-f', $File, 'up', '-d') }
  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose up failed.' $LASTEXITCODE }
  Write-Info 'Compose stack started.'
}

function Compose-Down-Clean() {
  param([string] $File)
  # Write-Section "docker compose down + prune ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $dockerArgs = @('compose', '-f', $File, 'down', '--remove-orphans', '--volumes')
  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose down failed.' $LASTEXITCODE }
  # Remove images defined in the compose file (basic parse by 'image:' tokens)
  $images = Select-String -Path $File -Pattern 'image:\s*(\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Sort-Object -Unique
  foreach ($img in $images) {
    Write-Step "Removing image $img (if present)"
    docker rmi $img 2>$null | Out-Null
  }
  Write-Info 'Compose stack fully cleaned.'
}

function Compose-Down() {
  param([string] $File)
  # Write-Section "docker compose down ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $dockerArgs = @('compose', '-f', $File, 'down')
  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose down failed.' $LASTEXITCODE }
  Write-Info 'Compose stack stopped.'
}

function Compose-Recreate([string] $ComposeFile) {
  Write-Step "Retrieving running containers..."
  $fmt = '{{.Names}}|{{.Image}}|{{.Status}}'
  $raw = docker ps --format $fmt 2>$null
  if (-not $raw) {
    Write-Warn "No running containers found."
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

  $selection = Read-Selection 'Select container (Cancel to quit)' $choices -EnableSearch -PageSize 15 -AddCancel
  if (-not $selection -or $selection -eq 'Cancel') { return }

  $idx = $choices.IndexOf($selection)
  if ($idx -lt 0) { Fail "Invalid selection mapping." 2 }

  # If user selected the top "All" option, run compose without service name
  if ($idx -eq 0) {
    Write-Step "Recreating ALL services (compose file: $ComposeFile)"
    $dockerArgs = @('compose', '-f', $ComposeFile, 'up', '-d', '--force-recreate')
    Write-Debug "docker $($dockerArgs -join ' ')"
    docker @dockerArgs
    if ($LASTEXITCODE -ne 0) { Fail "docker compose recreate (all) failed." $LASTEXITCODE }
    Write-Info "Recreate requested for ALL services"
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
  }
  catch {
    $serviceName = $null
  }

  # Fallback: strip CONTAINER_PREFIX_ if present, otherwise take last underscore segment
  if (-not $serviceName) {
    $prefix = $env:CONTAINER_PREFIX
    if ($prefix -and $selectedName.StartsWith("$prefix`_")) {
      $serviceName = $selectedName.Substring($prefix.Length + 1)
    }
    elseif ($selectedName -match '_') {
      $serviceName = ($selectedName -split '_')[-1]
    }
    else {
      $serviceName = $selectedName
    }
  }

  Write-Step "Resolved compose service name: $serviceName (from container: $selectedName, image: $selectedImage)"
  Write-Step "Recreating service/container: $serviceName (compose file: $ComposeFile)"
  $dockerArgs = @('compose', '-f', $ComposeFile, 'up', '-d', '--force-recreate', $serviceName)
  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail "docker compose recreate failed for $serviceName." $LASTEXITCODE }
  Write-Info "Recreate requested for: $serviceName"
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
  -ImageTag <tag>            (default: localhost:5500/bit_devkit_gettingstarted-web:latest)
  -ContainerName <name>      (default: bit_devkit_gettingstarted-web)
  -Dockerfile <path>         (default: src/Presentation.Web.Server/Dockerfile)
  -ProjectDockerContext <dir>(default: .)
  -Network <name>            (default: bit_devkit_gettingstarted)
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

    $dockerArgs = @('build', '-t', $ImageTag, '-f', $Dockerfile, '--build-arg', 'CONFIG=Debug')
    if ($NoCache) { $dockerArgs += '--no-cache' }
    $dockerArgs += $ProjectDockerContext
    Write-Debug "docker $($dockerArgs -join ' ')"
    docker @dockerArgs
    if ($LASTEXITCODE -ne 0) { Fail 'Docker debug build failed.' $LASTEXITCODE }

    Docker-Run -Tag $ImageTag -Name $ContainerName -Network $Network -HostPort $HostPort -ContainerPort $ContainerPort
  }
  'docker-build-debug' {
    $dockerArgs = @('build', '-t', $ImageTag, '-f', $Dockerfile, '--build-arg', 'CONFIG=Debug')
    if ($NoCache) { $dockerArgs += '--no-cache' }
    $dockerArgs += $ProjectDockerContext
    Write-Debug "docker $($dockerArgs -join ' ')"
    docker @dockerArgs
    if ($LASTEXITCODE -ne 0) { Fail 'Docker debug build failed.' $LASTEXITCODE }
  }
  'docker-build-release' {
    $dockerArgs = @('build', '-t', $ImageTag, '-f', $Dockerfile, '--build-arg', 'CONFIG=Release')
    if ($NoCache) { $dockerArgs += '--no-cache' }
    $dockerArgs += $ProjectDockerContext
    Write-Debug "docker $($dockerArgs -join ' ')"
    docker @dockerArgs
    if ($LASTEXITCODE -ne 0) { Fail 'Docker release build failed.' $LASTEXITCODE }
  }
  'docker-run' {
    Ensure-Network -Name $Network
    Docker-Run -Tag $ImageTag -Name $ContainerName -Network $Network -HostPort $HostPort -ContainerPort $ContainerPort
  }
  'docker-stop' {
    Docker-Stop -Name $ContainerName
  }
  'docker-remove' {
    Docker-Stop -Name $ContainerName
    Docker-Remove -Name $ContainerName -Network $Network
  }
  'docker-remove-image' {
    Docker-Stop -Name $ContainerName
    Docker-Remove -Name $ContainerName -Network $Network
    Docker-Remove-Image -Tag $ImageTag
  }
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
