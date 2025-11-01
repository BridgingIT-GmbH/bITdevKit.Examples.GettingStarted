# bdk.ps1
# Bootstrap for .bdk/bdk-cli.ps1

$ErrorActionPreference = 'Stop'

$target = Join-Path $PSScriptRoot '.bdk\bdk-cli.ps1'

if (-not (Test-Path $target)) {
  Write-Error "Cannot find bdk-cli.ps1 at $target"
  exit 2
}

& $target @args
exit $LASTEXITCODE