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
  [Parameter()] [string] $ImageTag = 'localhost:5500/bdk_gettingstarted-web:latest',
  [Parameter()] [string] $ContainerName = 'bdk_gettingstarted-web',
  [Parameter()] [string] $Dockerfile = 'src/Presentation.Web.Server/Dockerfile',
  [Parameter()] [string] $ProjectDockerContext = '.',
  [Parameter()] [switch] $NoCache,
  [Parameter()] [string] $Network = 'bdk_gettingstarted',
  [Parameter()] [int] $HostPort = 8080,
  [Parameter()] [int] $ContainerPort = 8080,
  [Parameter()] [string] $ComposeFile = 'docker-compose.yml',
  [Parameter()] [switch] $Pull
)
Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'

function Write-Section([string] $Text) { Write-Host "`n=== $Text ===" -ForegroundColor Cyan }
function Write-Step([string] $Text) { Write-Host "-- $Text" -ForegroundColor DarkCyan }
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
  Write-Section "Docker Build ($Tag)"
  $args = @('build', '-t', $Tag, '-f', $File)
  if ($NoCache) { $args += '--no-cache' }
  $args += $Context
  Write-Step "docker $($args -join ' ')"
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
  Write-Section "Docker Run ($Name)"
  Write-Step 'Removing any existing container'
  docker stop $Name 2>$null | Out-Null
  docker rm $Name 2>$null | Out-Null

  $logsDir = Join-Path (Get-Location) 'logs'
  if (-not (Test-Path $logsDir)) { New-Item -ItemType Directory -Force -Path $logsDir | Out-Null }

  $envVars = @(
    'ASPNETCORE_ENVIRONMENT=Development'
    'Modules__CoreModule__ConnectionStrings__Default=Server=mssql,1433;Initial Catalog=bit_devkit_gettingstarted;User Id=sa;Password=Abcd1234!;Trusted_Connection=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;'
    'JobScheduling__Quartz__quartz.dataSource.default.connectionString=Server=mssql,1433;Initial Catalog=bit_devkit_gettingstarted;User Id=sa;Password=Abcd1234!;Trusted_Connection=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;'
    'Authentication__Authority=http://localhost:8080'
  )

  $runArgs = @('run','-d','--name', $Name,'-p',"$HostPort`:$ContainerPort",'--network', $Network)
  foreach($e in $envVars){ $runArgs += @('-e', $e) }
  $runArgs += @('-v',"$(Resolve-Path $logsDir):/.logs", $Tag)
  Write-Step "docker $($runArgs -join ' ')"
  docker @runArgs
  if ($LASTEXITCODE -ne 0) { Fail "Docker run failed." $LASTEXITCODE }
  Write-Step "Container running: http://localhost:$HostPort"
}

function Docker-Stop() {
  param([string] $Name)
  Write-Section "Docker Stop ($Name)"
  docker stop $Name 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Stopped (if existed).' }
}

function Docker-Remove() {
  param([string] $Name)
  Write-Section "Docker Remove ($Name)"
  docker rm -f $Name 2>$null | Out-Null
  if ($LASTEXITCODE -eq 0) { Write-Step 'Removed (if existed).' }
}

function Compose-Up() {
  param([string] $File,[switch] $Pull)
  Write-Section "docker compose up ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $args = @('compose','-f', $File,'up','-d')
  if ($Pull) { $args = @('compose','-f', $File,'pull'); Write-Step "docker $($args -join ' ')"; docker @args; if ($LASTEXITCODE -ne 0){ Fail 'docker compose pull failed.' $LASTEXITCODE }; $args = @('compose','-f', $File,'up','-d') }
  Write-Step "docker $($args -join ' ')"
  docker @args
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose up failed.' $LASTEXITCODE }
  Write-Step 'Compose stack started.'
}

function Compose-Down-Clean() {
  param([string] $File)
  Write-Section "docker compose down + prune ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $downArgs = @('compose','-f', $File,'down','--remove-orphans','--volumes')
  Write-Step "docker $($downArgs -join ' ')"
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
  Write-Section "docker compose down ($File)"
  if (-not (Test-Path $File)) { Fail "Compose file not found: $File" }
  $downArgs = @('compose','-f', $File,'down')
  Write-Step "docker $($downArgs -join ' ')"
  docker @downArgs
  if ($LASTEXITCODE -ne 0) { Fail 'docker compose down failed.' $LASTEXITCODE }
  Write-Step 'Compose stack stopped.'
}

function Help() {
  @'
Usage: pwsh -File .vscode/task.ps1 <command> [options]

Commands:
  docker-build-run   Build image (optionally --NoCache) & run container
  docker-build       Build image only
  docker-run         Run container (assumes image built)
  docker-stop        Stop container
  docker-remove      Remove (force) container
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
  'docker-build' {
    Docker-Build -Tag $ImageTag -File $Dockerfile -Context $ProjectDockerContext -NoCache:$NoCache
  }
  'docker-run' {
    Ensure-Network -Name $Network
    Docker-Run -Tag $ImageTag -Name $ContainerName -Network $Network -HostPort $HostPort -ContainerPort $ContainerPort
  }
  'docker-stop' { Docker-Stop -Name $ContainerName }
  'docker-remove' { Docker-Remove -Name $ContainerName }
  'compose-up' { Compose-Up -File $ComposeFile -Pull:$Pull }
  'compose-down' { Compose-Down -File $ComposeFile }
  'compose-down-clean' { Compose-Down-Clean -File $ComposeFile }
  default { Help }
}

exit 0