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
  'outdated'     { Invoke-Dotnet @('list',$SolutionPath,'package','--outdated') }
  'analyzers'    { Invoke-Dotnet @('build',$SolutionPath,'-warnaserror','/p:RunAnalyzers=true','/p:EnableNETAnalyzers=true','/p:AnalysisLevel=latest') }
  'project-build'   { if (-not $ProjectPath) { throw 'ProjectPath required for project-build' }; Invoke-Dotnet (@('build',$ProjectPath) + $logArgs) }
  'project-publish' { if (-not $ProjectPath) { throw 'ProjectPath required for project-publish' }; Invoke-Dotnet (@('publish',$ProjectPath) + $logArgs) }
  'project-watch'   { if (-not $ProjectPath) { throw 'ProjectPath required for project-watch' }; Invoke-Dotnet @('watch','run','--project',$ProjectPath,'--nologo') }
  default { throw "Unknown dotnet command: $Command" }
}
