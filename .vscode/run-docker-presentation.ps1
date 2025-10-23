Param()
$ErrorActionPreference = 'Stop'

Write-Host '=== Ensuring docker network bdk_gettingstarted exists ==='
if (-not (docker network ls --format '{{.Name}}' | Select-String -Pattern '^bdk_gettingstarted$')) {
  docker network create bdk_gettingstarted | Out-Null
  Write-Host 'Created docker network bdk_gettingstarted'
}

$imageTag = 'localhost:5500/bdk_gettingstarted-web:latest'
$containerName = 'bdk_gettingstarted-web'
$logsDir = Join-Path (Get-Location) 'logs'

Write-Host '=== Building image ==='
docker build -t $imageTag -f src/Presentation.Web.Server/Dockerfile .

if ($LASTEXITCODE -ne 0) {
  Write-Error 'Image build failed.'
  exit $LASTEXITCODE
}

Write-Host '=== Cleaning existing container (if any) ==='
 docker stop $containerName 2>$null | Out-Null
 docker rm $containerName 2>$null | Out-Null

if (-not (Test-Path $logsDir)) {
  New-Item -ItemType Directory -Force -Path $logsDir | Out-Null
}

Write-Host '=== Running container ==='
docker run `
  --name $containerName `
  -d `
  -p 8080:8080 `
  --network bdk_gettingstarted `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e "Modules__CoreModule__ConnectionStrings__Default=Server=mssql,1433;Initial Catalog=bit_devkit_gettingstarted;User Id=sa;Password=Abcd1234!;Trusted_Connection=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;" `
  -e "JobScheduling__Quartz__quartz.dataSource.default.connectionString=Server=mssql,1433;Initial Catalog=bit_devkit_gettingstarted;User Id=sa;Password=Abcd1234!;Trusted_Connection=False;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False;" `
  -e "Authentication__Authority=http://localhost:8080" `
  $imageTag

$exit = $LASTEXITCODE
if ($exit -ne 0) {
  Write-Error "Container run failed with exit code $exit"
  exit $exit
}

Write-Host '=== Container started successfully ==='
Write-Host "Try: curl http://localhost:8080/api/_system/info"