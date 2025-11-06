param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string]$modulePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# 0) Resolve module path and derive names
$moduleFull = (Resolve-Path -LiteralPath $modulePath).Path
$moduleDir = [System.IO.DirectoryInfo]$moduleFull
$moduleName = $moduleDir.Name
Write-Host "Module: $moduleName"
Write-Host "Module folder: $moduleFull"

# 1) Repo root = three parents up (…/…/..): <repo>/src/Modules/<ModuleName> -> <repo>
$repoRoot = $moduleDir.Parent.Parent.Parent.FullName
if (-not (Test-Path -LiteralPath $repoRoot)) { throw "Repo root not found: $repoRoot" }
Write-Host "Repo root: $repoRoot"

# 2) Solution (.slnx) at repo root
$slnx = Get-ChildItem -LiteralPath $repoRoot -Filter *.slnx -File -ErrorAction Stop | Select-Object -First 1
$solutionPath = $slnx.FullName
Write-Host "Solution (.slnx): $solutionPath"