param(
  [Parameter(Mandatory=$true)][string]$Command,
  [string]$SolutionPath = (Join-Path $PSScriptRoot '..' 'BridgingIT.DevKit.Examples.GettingStarted.sln'),
  [string]$ProjectPath
)

$ErrorActionPreference = 'Stop'

function Invoke-Dotnet([string[]]$Arguments){
  Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
  & dotnet @Arguments
  if ($LASTEXITCODE -ne 0){ throw "dotnet command failed ($LASTEXITCODE)" }
}

# Common logger args as array
$logArgs = @('--nologo','/property:GenerateFullPaths=true','/consoleloggerparameters:NoSummary')

switch ($Command.ToLowerInvariant()) {
  'restore'      { if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('restore',$SolutionPath) + $logArgs) }
  'build'        { $SolutionPath = (Resolve-Path $SolutionPath).Path; if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('build',$SolutionPath) + $logArgs) }
  'build-release'{ $SolutionPath = (Resolve-Path $SolutionPath).Path; if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('build',$SolutionPath,'-c','Release') + $logArgs) }
  'build-nr'    { $SolutionPath = (Resolve-Path $SolutionPath).Path; if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('build',$SolutionPath,'--no-restore') + $logArgs) }
  'pack'        { $SolutionPath = (Resolve-Path $SolutionPath).Path; if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('pack',$SolutionPath,'-c','Release') + $logArgs) }
  'clean'        { if (-not (Test-Path $SolutionPath)) { throw "Solution not found: $SolutionPath" }; Invoke-Dotnet (@('clean',$SolutionPath) + $logArgs) }
  'tool-restore' { Invoke-Dotnet @('tool','restore') }
  'format-check' { Invoke-Dotnet @('format',$SolutionPath,'--verify-no-changes') }
  'format-apply' { Invoke-Dotnet @('format',$SolutionPath) }
  'vulnerabilities' { Invoke-Dotnet @('list',$SolutionPath,'package','--vulnerable') }
  'vulnerabilities-deep' { Invoke-Dotnet @('list',$SolutionPath,'package','--vulnerable','--include-transitive') }
  'outdated'     { Invoke-Dotnet @('list',$SolutionPath,'package','--outdated') }
  'outdated-json' {
    $tmpDir = Join-Path (Join-Path $PSScriptRoot '..') '.tmp/compliance'
    if(-not (Test-Path $tmpDir)){ New-Item -ItemType Directory -Force -Path $tmpDir | Out-Null }
    $outFile = Join-Path $tmpDir ("outdated_" + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.json')
    Write-Host "Collecting outdated packages (JSON) -> $outFile" -ForegroundColor Cyan
    # dotnet list outdated does not have native JSON; capture text then transform rudimentary structure
    $raw = & dotnet list $SolutionPath package --outdated
    if($LASTEXITCODE -ne 0){ throw 'dotnet list outdated failed' }
    $lines = $raw -split "`r?`n"
    $pkgs = @()
    foreach($l in $lines){
      if($l -match '>(\s*)(?<name>[^\s]+)\s+(?<current>\S+)\s+(?<wanted>\S+)\s+(?<latest>\S+)'){ $pkgs += [pscustomobject]@{ name=$Matches.name; current=$Matches.current; wanted=$Matches.wanted; latest=$Matches.latest } }
    }
    $pkgs | ConvertTo-Json -Depth 4 | Set-Content -Path $outFile -Encoding UTF8
    Write-Host ("Outdated packages captured: {0}" -f $pkgs.Count) -ForegroundColor Green
  }
  'analyzers'    { Invoke-Dotnet @('build',$SolutionPath,'-warnaserror','/p:RunAnalyzers=true','/p:EnableNETAnalyzers=true','/p:AnalysisLevel=latest') }
  'project-build'   { if (-not $ProjectPath) { throw 'ProjectPath required for project-build' }; Invoke-Dotnet (@('build',$ProjectPath) + $logArgs) }
  'project-publish' { if (-not $ProjectPath) { throw 'ProjectPath required for project-publish' }; Invoke-Dotnet (@('publish',$ProjectPath) + $logArgs) }
  'project-watch'   { if (-not $ProjectPath) { throw 'ProjectPath required for project-watch' }; Invoke-Dotnet @('watch','run','--project',$ProjectPath,'--nologo') }
  'project-run'     { if (-not $ProjectPath) { throw 'ProjectPath required for project-run' }; Invoke-Dotnet (@('run','--project',$ProjectPath) + $logArgs) }
  'project-watch-fast' { if (-not $ProjectPath) { throw 'ProjectPath required for project-watch-fast' }; Invoke-Dotnet @('watch','run','--project',$ProjectPath,'--nologo','--no-restore') }
  default { throw "Unknown dotnet command: $Command" }
}
