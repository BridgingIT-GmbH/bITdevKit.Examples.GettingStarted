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

function Select-Rid {
  param([string[]]$Rids)
  if(-not $Rids){ return $null }
  try {
    Import-Module PwshSpectreConsole -ErrorAction Stop
    $selection = Read-SpectreSelection -Title 'Select RID (blank for framework-dependent)' -Choices ($Rids + 'Framework-Dependent' + 'Cancel') -EnableSearch -PageSize 15
    if(-not $selection -or $selection -eq 'Cancel'){ return $null }
    if($selection -eq 'Framework-Dependent'){ return $null }
    return $selection
  } catch {
    Write-Host 'Spectre selection unavailable; proceeding framework-dependent.' -ForegroundColor Yellow
    return $null
  }
}

function Copy-PublishOutput {
  param(
    [string]$ProjectPath,
    [string]$Rid # optional for RID-specific publish
  )
  try {
    if(-not $ProjectPath){ return }
    $projDir = Split-Path $ProjectPath -Parent
    # Determine publish folder (RID-specific or generic)
    $publishDir = if($Rid){ Join-Path $projDir "bin/Release/net9.0/$Rid/publish" } else { Join-Path $projDir "bin/Release/net9.0/publish" }
    if(-not (Test-Path $publishDir)){ Write-Host "Publish output not found: $publishDir" -ForegroundColor Yellow; return }
    $root = Split-Path $PSScriptRoot -Parent
    $destRoot = Join-Path $root '.tmp/publish'
    New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $dest = Join-Path $destRoot $stamp
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Write-Host "Copying publish output -> $dest" -ForegroundColor Cyan
    Copy-Item (Join-Path $publishDir '*') -Destination $dest -Recurse -Force
    # Write summary JSON
    $files = Get-ChildItem -Path $dest -Recurse -File | Select-Object -ExpandProperty FullName
    $summary = [ordered]@{ project=$ProjectPath; rid=$Rid; source=$publishDir; destination=$dest; fileCount=$files.Count; timestamp=$stamp }
    $summary | ConvertTo-Json -Depth 5 | Out-File (Join-Path $dest 'publish_summary.json') -Encoding UTF8
    Write-Host "Publish output copied ($($files.Count) files)." -ForegroundColor Green
  } catch {
    Write-Host "Failed to copy publish output: $($_.Exception.Message)" -ForegroundColor Yellow
  }
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
  'project-publish' {
    if (-not $ProjectPath) { throw 'ProjectPath required for project-publish' }
    $rids = @('win-x64','win-x86','win-arm64','linux-x64','linux-musl-x64','linux-musl-arm64','linux-arm','linux-arm64')
    $rid = Select-Rid -Rids $rids
    $args = @('publish',$ProjectPath) + $logArgs
    if($rid){ $args += @('-r',$rid,'--self-contained','true') }
    Invoke-Dotnet $args
    Copy-PublishOutput -ProjectPath $ProjectPath -Rid $rid
  }
  'project-publish-release' {
    if (-not $ProjectPath) { throw 'ProjectPath required for project-publish-release' }
    $rids = @('win-x64','win-x86','win-arm64','linux-x64','linux-musl-x64','linux-musl-arm64','linux-arm','linux-arm64')
    $rid = Select-Rid -Rids $rids
    $args = @('publish',$ProjectPath,'-c','Release') + $logArgs
    if($rid){ $args += @('-r',$rid,'--self-contained','true') }
    Invoke-Dotnet $args
    Copy-PublishOutput -ProjectPath $ProjectPath -Rid $rid
  }
  'project-publish-sc' {
    if (-not $ProjectPath) { throw 'ProjectPath required for project-publish-sc' }
    # Self-contained single-file publish reusing Select-Rid helper
    $rids = @('win-x64','win-x86','win-arm64','linux-x64','linux-musl-x64','linux-musl-arm64','linux-arm','linux-arm64')
    $rid = Select-Rid -Rids $rids
    if(-not $rid){ Write-Host 'Self-contained publish requires a RID; cancelled.' -ForegroundColor Yellow; break }
    Write-Host "Publishing self-contained single-file for RID: $rid" -ForegroundColor Cyan
    $publishArgs = @('publish',$ProjectPath,'-c','Release','-r',$rid,'--self-contained','true','/p:PublishSingleFile=true','/p:PublishTrimmed=false') + $logArgs
    Invoke-Dotnet $publishArgs
    Write-Host "Self-contained publish complete for RID $rid" -ForegroundColor Green
    Copy-PublishOutput -ProjectPath $ProjectPath -Rid $rid
  }
  'pack-modules' {
    # Packs each module project (Domain/Application/Infrastructure/Presentation) into .tmp/packages
    $root = Split-Path $PSScriptRoot -Parent
    $modulesPath = Join-Path $root 'src/Modules'
    if (-not (Test-Path $modulesPath)) { throw "Modules path not found: $modulesPath" }
    $outDir = Join-Path $root '.tmp/packages'
    if(-not (Test-Path $outDir)){ New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $projects = Get-ChildItem -Path $modulesPath -Recurse -Filter '*.csproj' | Where-Object { $_.FullName -notmatch 'Tests' -and $_.Name -match 'CoreModule\.(Domain|Application|Infrastructure|Presentation)\.csproj' }
    if(-not $projects){ Write-Host 'No module projects found to pack.' -ForegroundColor Yellow; break }
    foreach($p in $projects){
      Write-Host "Packing module project: $($p.FullName)" -ForegroundColor Cyan
      Invoke-Dotnet @('pack',$p.FullName,'-c','Release','-o',$outDir)
    }
    Write-Host "Module packages written to $outDir" -ForegroundColor Green
  }
  'analyzers-export' {
    $root = Split-Path $PSScriptRoot -Parent
    $anDir = Join-Path $root '.tmp/analyzers'
    if(-not (Test-Path $anDir)){ New-Item -ItemType Directory -Force -Path $anDir | Out-Null }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $sarifFile = Join-Path $anDir "analyzers_$stamp.sarif"
    Write-Host "Running analyzers export -> $sarifFile" -ForegroundColor Cyan
    & dotnet build $SolutionPath -warnaserror /p:RunAnalyzers=true /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest /p:ErrorLog=$sarifFile
    if($LASTEXITCODE -ne 0){ throw 'Analyzer build failed' }
    # Produce summary JSON (count diagnostics by id/severity) simple parse of SARIF
    $sarifJson = Get-Content -Raw -Path $sarifFile | ConvertFrom-Json
    $issues = @()
    if($sarifJson.runs){
      foreach($run in $sarifJson.runs){ if($run.results){ $issues += $run.results } }
    }
    $summary = $issues | Group-Object -Property ruleId | ForEach-Object { [pscustomobject]@{ ruleId=$_.Name; count=$_.Count } }
    $summaryFile = Join-Path $anDir "analyzers_summary_$stamp.json"
    $summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryFile -Encoding UTF8
    Write-Host "Analyzer summary written: $summaryFile" -ForegroundColor Green
  }
  'project-watch'   { if (-not $ProjectPath) { throw 'ProjectPath required for project-watch' }; Invoke-Dotnet @('watch','run','--project',$ProjectPath,'--nologo') }
  'project-run'     { if (-not $ProjectPath) { throw 'ProjectPath required for project-run' }; Invoke-Dotnet (@('run','--project',$ProjectPath) + $logArgs) }
  'project-watch-fast' { if (-not $ProjectPath) { throw 'ProjectPath required for project-watch-fast' }; Invoke-Dotnet @('watch','run','--project',$ProjectPath,'--nologo','--no-restore') }
  default { throw "Unknown dotnet command: $Command" }
}
