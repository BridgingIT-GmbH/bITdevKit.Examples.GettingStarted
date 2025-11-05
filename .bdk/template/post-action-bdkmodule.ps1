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

# 2) Build expected project paths
$srcModulesDir = [System.IO.Path]::Combine($repoRoot, "src", "Modules", $moduleName)
$testsModulesDir = [System.IO.Path]::Combine($repoRoot, "tests", "Modules", $moduleName)

$srcProjects = @(
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Application", "$moduleName.Application.csproj"),
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Domain", "$moduleName.Domain.csproj"),
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Infrastructure", "$moduleName.Infrastructure.csproj"),
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Presentation", "$moduleName.Presentation.csproj")
) | Where-Object { Test-Path -LiteralPath $_ }

$testProjects = @(
  [System.IO.Path]::Combine($testsModulesDir, "$moduleName.IntegrationTests", "$moduleName.IntegrationTests.csproj"),
  [System.IO.Path]::Combine($testsModulesDir, "$moduleName.UnitTests", "$moduleName.UnitTests.csproj")
) | Where-Object { Test-Path -LiteralPath $_ }

$projects = $srcProjects + $testProjects

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

# Load XML and set up namespace manager
[xml]$xml = Get-Content -LiteralPath $solutionPath

$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
# Read the default namespace from the root element
$defaultNs = $xml.DocumentElement.NamespaceURI
if ([string]::IsNullOrEmpty($defaultNs)) {
  # No namespace – fallback to simple selection
  $useNs = $false
}
else {
  $useNs = $true
  $ns.AddNamespace('s', $defaultNs)
}

function Get-Or-Create-RootFolder {
  param(
    [xml]$doc,
    [System.Xml.XmlNamespaceManager]$nsMgr,
    [bool]$useNs,
    [string]$name
  )
  if ($useNs) {
    $node = $doc.SelectSingleNode("/s:Solution/s:Folder[@Name='$name']", $nsMgr)
  }
  else {
    $node = $doc.SelectSingleNode("/Solution/Folder[@Name='$name']")
  }
  if (-not $node) {
    $node = $doc.CreateElement("Folder", $doc.DocumentElement.NamespaceURI)
    $null = $node.SetAttribute("Name", $name)
    $null = $doc.DocumentElement.AppendChild($node)
  }
  return $node
}

# Ensure folders
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/src/"
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/src/Modules/"
$moduleFolderName = "/src/Modules/$moduleName/"
$moduleFolder = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name $moduleFolderName

# Add files
$files = @(
  "src/Modules/$moduleName/$moduleName-Customers-API.http",
  "src/Modules/$moduleName/$moduleName-README.md",
  "src/Modules/$moduleName/Directory.Build.props"
)

foreach ($f in $files) {
  $path = $f -replace "\\", "/"
  if ($useNs) {
    $existing = $moduleFolder.SelectSingleNode("s:File[@Path='$path']", $ns)
  }
  else {
    $existing = $moduleFolder.SelectSingleNode("File[@Path='$path']")
  }
  if (-not $existing) {
    $fileNode = $xml.CreateElement("File", $xml.DocumentElement.NamespaceURI)
    $null = $fileNode.SetAttribute("Path", $path)
    $null = $moduleFolder.AppendChild($fileNode)
    Write-Host "Added File: $path"
  }
  else {
    Write-Host "File already present: $path"
  }
}

$xml.Save((Resolve-Path -LiteralPath $solutionPath))
Write-Host "SLNX updated."