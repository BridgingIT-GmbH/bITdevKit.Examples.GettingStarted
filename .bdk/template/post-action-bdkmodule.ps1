param(
  [Parameter(Mandatory = $true, Position = 0)]
  [string]$modulePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Resolve module path and module name
$moduleFull = (Resolve-Path -LiteralPath $modulePath).Path
$moduleDir = [System.IO.DirectoryInfo]$moduleFull
$moduleName = $moduleDir.Name
Write-Host "Module: $moduleName"
Write-Host "Module folder: $moduleFull"

# Repo root = three parents up (…/…/..)
# <repo>/src/Modules/<ModuleName> -> <repo>
$repoRoot = $moduleDir.Parent.Parent.Parent.FullName
if (-not (Test-Path -LiteralPath $repoRoot)) { throw "Repo root not found: $repoRoot" }
Write-Host "Repo root: $repoRoot"

# Solution = first .slnx at repo root
$slnx = Get-ChildItem -LiteralPath $repoRoot -Filter *.slnx -File -ErrorAction Stop | Select-Object -First 1
$solutionPath = $slnx.FullName
Write-Host "Solution (.slnx): $solutionPath"

# Project paths (all under src/Modules/<ModuleName>)
$moduleSrcDir = [System.IO.Path]::Combine($repoRoot, "src", "Modules", $moduleName)
$projects = @(
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.Application", "$moduleName.Application.csproj"),
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.Domain", "$moduleName.Domain.csproj"),
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.Infrastructure", "$moduleName.Infrastructure.csproj"),
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.Presentation", "$moduleName.Presentation.csproj"),
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.IntegrationTests", "$moduleName.IntegrationTests.csproj"),
  [System.IO.Path]::Combine($moduleSrcDir, "$moduleName.UnitTests", "$moduleName.UnitTests.csproj")
) | Where-Object { Test-Path -LiteralPath $_ }

# Add projects via dotnet
foreach ($p in $projects) {
  Write-Host "dotnet sln add: $p"
  & dotnet sln $solutionPath add $p
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "dotnet sln add failed for $p (exit $LASTEXITCODE)."
  }
}

# Add 3 files into /src/Modules/<ModuleName>/ in .slnx
$files = @(
  "src/Modules/$moduleName/$moduleName-Customers-API.http",
  "src/Modules/$moduleName/$moduleName-README.md",
  "src/Modules/$moduleName/Directory.Build.props"
)

[xml]$xml = Get-Content -LiteralPath $solutionPath

function Ensure-Folder([xml]$doc, [string]$name) {
  $n = $doc.Solution.Folder | Where-Object { $_.Name -eq $name }
  if (-not $n) {
    $n = $doc.CreateElement("Folder")
    $null = $n.SetAttribute("Name", $name)
    $null = $doc.Solution.AppendChild($n)
  }
  $n
}

$null = Ensure-Folder $xml "/src/"
$null = Ensure-Folder $xml "/src/Modules/"
$moduleFolderName = "/src/Modules/$moduleName/"
$moduleFolder = $xml.Solution.Folder | Where-Object { $_.Name -eq $moduleFolderName }
if (-not $moduleFolder) {
  $moduleFolder = $xml.CreateElement("Folder")
  $null = $moduleFolder.SetAttribute("Name", $moduleFolderName)
  $null = $xml.Solution.AppendChild($moduleFolder)
}

foreach ($f in $files) {
  $path = $f -replace "\\", "/"
  if (-not ($moduleFolder.File | Where-Object { $_.Path -eq $path })) {
    $fileNode = $xml.CreateElement("File")
    $null = $fileNode.SetAttribute("Path", $path)
    $null = $moduleFolder.AppendChild($fileNode)
    Write-Host "Added File: $path"
  }
}

$xml.Save((Resolve-Path -LiteralPath $solutionPath))
Write-Host "SLNX updated."