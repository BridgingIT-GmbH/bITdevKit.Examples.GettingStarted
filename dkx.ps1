# dkx.ps1
# Bootstrap for .dkx/dkx-cli.ps1

$ErrorActionPreference = 'Stop'

$target = Join-Path $PSScriptRoot '.dkx\dkx-cli.ps1'

if (-not (Test-Path $target)) {
  Write-Error "Cannot find dkx-cli.ps1 at $target"
  exit 2
}

& $target @args
exit $LASTEXITCODE