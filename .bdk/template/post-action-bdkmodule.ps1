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
Write-Output "Module: $moduleName"
Write-Output "Module folder: $moduleFull"

# 1) Repo root = three parents up (…/…/..): <repo>/src/Modules/<ModuleName> -> <repo>
$repoRoot = $moduleDir.Parent.Parent.Parent.FullName
$webServerCsproj = Join-Path $repoRoot "src/Presentation.Web.Server/Presentation.Web.Server.csproj"
if (-not (Test-Path -LiteralPath $repoRoot)) { throw "Repo root not found: $repoRoot" }
if (-not (Test-Path -LiteralPath $webServerCsproj)) { throw "Web.Server project not found: $webServerCsproj" }
Write-Output "Repo root: $repoRoot"
Write-Output "Web.Server project: $webServerCsproj"

# 2) Solution (.slnx) at repo root
$slnx = Get-ChildItem -LiteralPath $repoRoot -Filter *.slnx -File -ErrorAction Stop | Select-Object -First 1
$solutionPath = $slnx.FullName
Write-Output "Solution (.slnx): $solutionPath"

# 3) Project paths
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

$serverReferenceProjects = @(
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Infrastructure", "$moduleName.Infrastructure.csproj"),
  [System.IO.Path]::Combine($srcModulesDir, "$moduleName.Presentation", "$moduleName.Presentation.csproj")
) | Where-Object { Test-Path -LiteralPath $_ }

$allProjects = $srcProjects + $testProjects

# 4) Add projects via dotnet CLI
foreach ($p in $allProjects) {
  Write-Host "dotnet sln add: $p"
  & dotnet sln $solutionPath add $p
  if ($LASTEXITCODE -ne 0) {
    Write-Warning "dotnet sln add failed for $p (exit $LASTEXITCODE)."
  }
}

# 5) Add 3 files under /src/Modules/<ModuleName>/ in .slnx (namespace-aware)
[xml]$xml = Get-Content -LiteralPath $solutionPath

$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$defaultNs = $xml.DocumentElement.NamespaceURI
$useNs = -not [string]::IsNullOrEmpty($defaultNs)
if ($useNs) { $ns.AddNamespace('s', $defaultNs) }

function Get-Or-Create-RootFolder {
  param(
    [xml]$doc,
    [System.Xml.XmlNamespaceManager]$nsMgr,
    [bool]$useNs,
    [string]$name
  )
  $node = if ($useNs) {
    $doc.SelectSingleNode("/s:Solution/s:Folder[@Name='$name']", $nsMgr)
  }
  else {
    $doc.SelectSingleNode("/Solution/Folder[@Name='$name']")
  }
  if (-not $node) {
    $node = $doc.CreateElement("Folder", $doc.DocumentElement.NamespaceURI)
    $null = $node.SetAttribute("Name", $name)
    $null = $doc.DocumentElement.AppendChild($node)
  }
  return $node
}

# Ensure src and tests folders
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/src/"
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/src/Modules/"
$srcModuleFolderName = "/src/Modules/$moduleName/"
$srcModuleFolder = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name $srcModuleFolderName

$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/tests/"
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name "/tests/Modules/"
$testsModuleFolderName = "/tests/Modules/$moduleName/"
$null = Get-Or-Create-RootFolder -doc $xml -nsMgr $ns -useNs $useNs -name $testsModuleFolderName

# Add file entries in src module folder
$files = @(
  "src/Modules/$moduleName/$moduleName-Customers-API.http",
  "src/Modules/$moduleName/$moduleName-README.md",
  "src/Modules/$moduleName/Directory.Build.props"
)

foreach ($f in $files) {
  $path = ($f -replace '\\', '/')
  $existing = if ($useNs) {
    $srcModuleFolder.SelectSingleNode("s:File[@Path='$path']", $ns)
  }
  else {
    $srcModuleFolder.SelectSingleNode("File[@Path='$path']")
  }
  if (-not $existing) {
    $fileNode = $xml.CreateElement("File", $xml.DocumentElement.NamespaceURI)
    $null = $fileNode.SetAttribute("Path", $path)
    $null = $srcModuleFolder.AppendChild($fileNode)
    Write-Host "Added File: $path"
  }
  else {
    Write-Host "File already present: $path"
  }
}

$xml.Save((Resolve-Path -LiteralPath $solutionPath))
Write-Host "SLNX updated."

# 6) Add ProjectReferences from Presentation.Web.Server to module Infrastructure and Presentation
$refs = @(
  Join-Path $srcModulesDir "..\Modules\$moduleName.Infrastructure\$moduleName.Infrastructure.csproj",
  Join-Path $srcModulesDir "..\Modules\$moduleName.Presentation\$moduleName.Presentation.csproj"
)

Write-Output "Adding ProjectReferences to $serverReferenceProjects"
# if ((Test-Path -LiteralPath $webServerCsproj)) {
  foreach ($r in $serverReferenceProjects) {
    Write-Host "dotnet add $webServerCsproj reference $r"
    & dotnet add $webServerCsproj reference $r
    # if ($LASTEXITCODE -ne 0) {
    #   Write-Warning "dotnet add reference failed for $r (exit $LASTEXITCODE)."
    # }
  }
# }