param(
  [Parameter(Mandatory = $true)][string]$Command,
  [string]$SolutionPath # auto-resolved if omitted
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

$OutputDirectory = Join-Path $Root (Get-OutputDirectory)

function Invoke-Dotnet([string[]]$Arguments) {
  Write-Debug "dotnet $($Arguments -join ' ')"
  & dotnet @Arguments
  if ($LASTEXITCODE -ne 0) { throw "dotnet command failed ($LASTEXITCODE)" }
}

function Copy-PublishOutput {
  param(
    [string]$Root,
    [string]$Rid # optional for RID-specific publish
  )
  try {
    if (-not $Root) { return }
    # Determine publish folder (RID-specific or generic)
    $publishDir = if ($Rid) {
      Join-Path $Root "src/Presentation.Web.Server/bin/Release/net9.0/$Rid/publish" # TODO: resolve this better
    }
    else {
      Join-Path $Root "src/Presentation.Web.Server/bin/Release/net9.0/publish" # TODO: resolve this better
    }
    if (-not (Test-Path $publishDir)) { Write-Error "Publish output not found: $publishDir"; return }
    $destRoot = Join-Path $OutputDirectory 'publish'
    New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $dest = Join-Path $destRoot $stamp
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Write-Step "Copying publish output -> $dest"
    Copy-Item (Join-Path $publishDir '*') -Destination $dest -Recurse -Force
    # Write summary JSON
    $files = Get-ChildItem -Path $dest -Recurse -File | Select-Object -ExpandProperty FullName
    $summary = [ordered]@{ project = $Root; rid = $Rid; source = $publishDir; destination = $dest; fileCount = $files.Count; timestamp = $stamp }
    $summary | ConvertTo-Json -Depth 5 | Out-File (Join-Path $dest 'publish_summary.json') -Encoding UTF8
    Write-Info "Publish output copied ($($files.Count) files)."
  }
  catch {
    Write-Error "Failed to copy publish output: $($_.Exception.Message)"
  }
}

function Resolve-Solution([string]$explicit) {
  $solutions = Get-ChildItem -Path $Root -Filter '*.slnx' -File
  if (-not $solutions -or $solutions.Count -eq 0) {
    throw "No .slnx files found under $Root"
  }

  # Single solution: return directly (explicit ignored if different)
  if ($solutions.Count -eq 1) {
    if ($explicit -and (Test-Path $explicit) -and ((Resolve-Path $explicit).Path -ne $solutions[0].FullName)) {
      Write-Warn "Explicit solution differs; using discovered single solution."
    }
    return $solutions[0].FullName
  }

  # Multiple solutions: always prompt
  # Write-Host "Multiple solutions detected (${($solutions.Count)})." -ForegroundColor Cyan
  $choices = $solutions | ForEach-Object { $_.FullName }
  $selected = Read-Selection -Title 'Select Solution (.slnx)' -Choices ($choices + 'Cancel') -EnableSearch -PageSize 15
  if (-not $selected -or $selected -eq 'Cancel') { throw 'Solution selection cancelled.' }
  # Write-Host "Selected solution: $selected" -ForegroundColor Green
  return $selected
}

# Resolve once for commands needing a solution
$needsSolutionCommands = @(
  'restore', 'build', 'build-release', 'build-nr', 'pack', 'clean',
  'format-check', 'format-apply', 'vulnerabilities', 'vulnerabilities-deep',
  'outdated', 'outdated-json', 'update-packages', 'update-packages-devkit', 'analyzers', 'analyzers-export',
  'licenses', 'coverage', 'coverage-html', 'roslynator-analyze', 'roslynator-loc', 'roslynator-lloc'
)
if ($needsSolutionCommands -contains $Command.ToLowerInvariant()) {
  try {
    $SolutionPath = Resolve-Solution $SolutionPath
  }
  catch {
    Write-Error "Solution resolution failed: $($_.Exception.Message)"
    exit 10
  }
  Write-Info "Using solution: $SolutionPath"
}

# Common logger args as array
$defaultArgs = @('--nologo', '/property:GenerateFullPaths=true', '/consoleloggerparameters:NoSummary')

switch ($Command.ToLowerInvariant()) {
  'restore' { Invoke-Dotnet (@('restore', $SolutionPath) + $defaultArgs) }
  'build' { Invoke-Dotnet (@('build', $SolutionPath) + $defaultArgs) }
  'build-release' { Invoke-Dotnet (@('build', $SolutionPath, '-c', 'Release') + $defaultArgs) }
  'build-nr' { Invoke-Dotnet (@('build', $SolutionPath, '--no-restore') + $defaultArgs) }
  'pack' { Invoke-Dotnet (@('pack', $SolutionPath, '-c', 'Release') + $defaultArgs) }
  'clean' { Invoke-Dotnet (@('clean', $SolutionPath) + $defaultArgs) }
  'format-check' { Invoke-Dotnet @('format', $SolutionPath, '--verify-no-changes') }
  'format-apply' { Invoke-Dotnet @('format', $SolutionPath) }
  'tool-restore' { Invoke-Dotnet @('tool', 'restore') }
  'vulnerabilities' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--vulnerable') }
  'vulnerabilities-deep' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--vulnerable', '--include-transitive') }
  'outdated' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--outdated') }
  'outdated-json' {
    Ensure-DotNetTools
    $OutputDirectoryCompliance = Join-Path (Join-Path $OutputDirectory 'compliance')
    if (-not (Test-Path $OutputDirectoryCompliance)) { New-Item -ItemType Directory -Force -Path $OutputDirectoryCompliance | Out-Null }
    $outFile = Join-Path $OutputDirectoryCompliance ("outdated_" + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.json')
    Write-Step "Collecting outdated packages (JSON) -> $outFile"
    Write-Debug "dotnet list $SolutionPath package --outdated"
    $raw = & dotnet list $SolutionPath package --outdated
    if ($LASTEXITCODE -ne 0) { throw 'dotnet list outdated failed' }
    # dotnet list outdated does not have native JSON; capture text then transform rudimentary structure
    $lines = $raw -split "`r?`n"
    $pkgs = @()
    foreach ($l in $lines) {
      if ($l -match '>(\s*)(?<name>[^\s]+)\s+(?<current>\S+)\s+(?<wanted>\S+)\s+(?<latest>\S+)') { $pkgs += [pscustomobject]@{ name = $Matches.name; current = $Matches.current; wanted = $Matches.wanted; latest = $Matches.latest } }
    }
    $pkgs | ConvertTo-Json -Depth 4 | Set-Content -Path $outFile -Encoding UTF8
    Write-Info ("Outdated packages captured: {0}" -f $pkgs.Count)
  }
  'update-packages' {
    # Use dotnet-outdated global tool to update package versions. https://github.com/dotnet-outdated/dotnet-outdated
    Ensure-DotNetTools
    Write-Step 'Running dotnet-outdated (auto upgrade within constraints)...'
    Write-Debug "dotnet outdated $SolutionPath --upgrade"
    & dotnet outdated $SolutionPath --upgrade
    if ($LASTEXITCODE -ne 0) { Write-Error 'dotnet-outdated reported issues or applied updates with non-zero exit code.' }
    Write-Info'dotnet-outdated execution complete.'
  }
  'update-packages-devkit' {
    # Use dotnet-outdated global tool to update package versions. https://github.com/dotnet-outdated/dotnet-outdated
    Ensure-DotNetTools
    Write-Step 'Running dotnet-outdated (auto upgrade devkit within constraints)...'
    Write-Debug "dotnet outdated $SolutionPath --upgrade -inc 'BridgingIT.DevKit'"
    & dotnet outdated $SolutionPath --upgrade -inc 'BridgingIT.DevKit'
    if ($LASTEXITCODE -ne 0) { Write-Error 'dotnet-outdated reported issues or applied updates with non-zero exit code.'  }
    Write-Info 'dotnet-outdated execution complete.'
  }
  'analyzers' { Invoke-Dotnet @('build', $SolutionPath, '-warnaserror', '/p:RunAnalyzers=true', '/p:EnableNETAnalyzers=true', '/p:AnalysisLevel=latest') }
  'project-build' { if (-not $SolutionPath) { throw 'Path required for project-build' }; Invoke-Dotnet (@('build', $SolutionPath) + $defaultArgs) }
  'project-watch' { if (-not $SolutionPath) { throw 'Path required for project-watch' }; Invoke-Dotnet @('watch', 'run', '--project', $SolutionPath, '--nologo') }
  'project-run' { if (-not $SolutionPath) { throw 'Path required for project-run' }; Invoke-Dotnet (@('run', '--project', $SolutionPath) + $defaultArgs) }
  'project-watch-fast' { if (-not $SolutionPath) { throw 'Path required for project-watch-fast' }; Invoke-Dotnet @('watch', 'run', '--project', $SolutionPath, '--nologo', '--no-restore') }
  'project-publish' {
    if (-not $SolutionPath) { throw 'Path required for project-publish' }
    $outDir = Join-Path $OutputDirectory 'publish'
    $rid = Select-Rid
    if (-not $rid) { Write-Error 'Self-contained publish requires a RID; cancelled.'; break }
    Write-Step "Publishing for target: $rid"
    $publishArgs = @('publish', $SolutionPath, '-o', $outdir) + $defaultArgs
    if ($rid) { $publishArgs += @('-r', $rid, '--self-contained', 'true') }
    Clean-Path $outDir
    Invoke-Dotnet $publishArgs
    Write-Info "publish completed"
    # Copy-PublishOutput $Root -Rid $rid
  }
  'project-publish-release' {
    if (-not $SolutionPath) { throw 'Path required for project-publish-release' }
    $outDir = Join-Path $OutputDirectory 'publish'
    $rid = Select-Rid
    if (-not $rid) { Write-Error 'Self-contained publish requires a RID; cancelled.'; break }
    $publishArgs = @('publish', $SolutionPath, '-c', 'Release', '-o', $outdir) + $defaultArgs
    if ($rid) { $publishArgs += @('-r', $rid, '--self-contained', 'true') }
    Write-Step "Publishing release for target: $rid"
    Clean-Path $outDir
    Invoke-Dotnet $publishArgs
    Write-Info "publish completed"
    # Copy-PublishOutput $Root -Rid $rid
  }
  'project-publish-sc' {
    if (-not $SolutionPath) { throw 'Path required for project-publish-sc' }
    # Self-contained single-file publish reusing Select-Rid helper
    $outDir = Join-Path $OutputDirectory 'publish'
    $rid = Select-Rid
    if (-not $rid) { Write-Error 'Self-contained publish requires a RID; cancelled.'; break }
    Write-Step "Publishing self-contained single-file for target: $rid"
    Clean-Path $outDir
    $publishArgs = @('publish', $SolutionPath, '-c', 'Release', '-r', $rid, '--self-contained', 'true', '/p:PublishSingleFile=true', '/p:PublishTrimmed=false', '-o', $outdir) + $defaultArgs
    Invoke-Dotnet $publishArgs
    Write-Info "publish self-contained completed"
    # Copy-PublishOutput $Root -Rid $rid
  }
  'pack-projects' {
    # Packs each module project (Domain/Application/Infrastructure/Presentation) into output directory
    $modulesPath = Join-Path $Root 'src/Modules'
    if (-not (Test-Path $modulesPath)) { throw "Modules path not found: $modulesPath" }
    $outDir = Join-Path $OutputDirectory 'packages'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $projects = Get-ChildItem -Path $modulesPath -Recurse -Filter '*.csproj' | Where-Object { $_.FullName -notmatch 'Tests' -and $_.Name -match 'CoreModule\.(Domain|Application|Infrastructure|Presentation)\.csproj' }
    if (-not $projects) { Write-Warn 'No module projects found to pack.'; break }
    Clean-Path $outDir
    foreach ($p in $projects) {
      Write-Step "Packing module project: $($p.FullName)"
      Invoke-Dotnet @('pack', $p.FullName, '-c', 'Release', '-o', $outDir)
    }
    Write-Info "Packages written to $outDir"
  }
  'analyzers-export' {
    # Uses solution (resolved earlier)
    $outDir = Join-Path $OutputDirectory 'analyzers'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $sarifFile = Join-Path $outDir "analyzers_$stamp.sarif"
    Write-Step "Running analyzers export -> $sarifFile"
    Write-Debug "dotnet build $SolutionPath -warnaserror /p:RunAnalyzers=true /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest /p:ErrorLog=$sarifFile"
    & dotnet build $SolutionPath -warnaserror /p:RunAnalyzers=true /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest /p:ErrorLog=$sarifFile
    if ($LASTEXITCODE -ne 0) { throw 'Analyzer build failed' }
    # Produce summary JSON (count diagnostics by id/severity) simple parse of SARIF
    $sarifJson = Get-Content -Raw -Path $sarifFile | ConvertFrom-Json
    $issues = @()
    if ($sarifJson.runs) {
      foreach ($run in $sarifJson.runs) { if ($run.results) { $issues += $run.results } }
    }
    $summary = $issues | Group-Object -Property ruleId | ForEach-Object { [pscustomobject]@{ ruleId = $_.Name; count = $_.Count } }
    $summaryFile = Join-Path $outDir "analyzers_summary_$stamp.json"
    $summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryFile -Encoding UTF8
    Write-Info "Analyzer summary written: $summaryFile"
  }
  'licenses' {
    # Generate license report using nuget-license (moved from tasks-compliance.ps1)
    Ensure-DotNetTools
    $outDir = Join-Path $OutputDirectory 'compliance'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $mdFile = Join-Path $outDir "licenses_$timestamp.md"
    $jsonFile = Join-Path $outDir "licenses_$timestamp.json"
    Write-Step "Generating license report"
    # Write-Host "Generating license report -> $mdFile" -ForegroundColor Cyan

    $nlOutput = & dotnet tool run nuget-license -i $SolutionPath -t -o JsonPretty 2>&1
    $parseSource = ($nlOutput -join "`n")
    if (-not ($parseSource.TrimStart() -match '^[\[{]')) { throw "nuget-license did not return JSON output. Raw: $parseSource" }
    try { $data = $parseSource | ConvertFrom-Json } catch { throw 'Failed to parse nuget-license JSON output.' }
    if (-not ($data -is [System.Collections.IEnumerable])) { throw 'nuget-license JSON unexpected shape (expected array).' }

    $rows = @('| Package | Version | License | LicenseUrl |', '|---------|---------|---------|-----------|')
    $licenseStats = @{ }
    $jsonList = @()
    foreach ($pkg in $data) {
      $name = $pkg.PackageId
      $ver = $pkg.PackageVersion
      $licRaw = $pkg.License
      $licUrl = $pkg.LicenseUrl
      if (-not $licRaw) { $licRaw = '(unknown)' }
      if (-not $licUrl) { $licUrl = '(none)' }
      $lic = if (($licRaw) -and ($licRaw.Length -gt 120 -or $licRaw -match "`n")) { '(Embedded License Text)' } else { $licRaw }
      $rows += "| $name | $ver | $lic | $licUrl |"
      if ($licenseStats.ContainsKey($lic)) { $licenseStats[$lic]++ } else { $licenseStats[$lic] = 1 }
      $jsonList += [pscustomobject]@{ package = $name; version = $ver; license = $lic; licenseUrl = $licUrl }
    }

    $total = $jsonList.Count
    $unknownCount = ($jsonList | Where-Object { $_.license -eq '(unknown)' }).Count
    $summaryLines = @("", "## License Summary", "Total packages: $total", "Unknown licenses: $unknownCount", "Top licenses:")
    foreach ($key in ($licenseStats.Keys | Sort-Object)) {
      $count = $licenseStats[$key]
      $summaryLines += "  - ${key}: ${count}"
    }

    ($rows + $summaryLines) -join "`n" | Set-Content -Path $mdFile -Encoding UTF8
    $jsonObj = [pscustomobject]@{ generated = (Get-Date).ToString('o'); total = $total; unknown = $unknownCount; licenses = $licenseStats; packages = $jsonList }
    $jsonObj | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonFile -Encoding UTF8

    Write-Info 'License reports created with nuget-license:'
    Write-Info "  Markdown:   $mdFile"
    Write-Info "  JSON    : $jsonFile"

    Open-File $mdFile
  }
  'coverage' {
    # Ensure plain coverage (no HTML) exists: runs tests with coverage and writes coverage.cobertura.xml files
    $resultsRoot = Join-Path $OutputDirectory 'coverage'
    if (-not (Test-Path $resultsRoot)) { New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $runDir = Join-Path $resultsRoot ("run_$timestamp")
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    Write-Step "Running tests with coverage -> $runDir"

    try {
      Invoke-Dotnet @('test', $SolutionPath, '--collect:"XPlat Code Coverage"', '--results-directory', $runDir, '--settings:.runsettings')
    }
    catch {
      Write-Error 'dotnet test failed.'
      exit 1
    }

    $coverageFiles = Get-ChildItem -Recurse -Path $runDir -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles) { Write-Error 'No coverage.cobertura.xml files found.'; exit 2 }
    Write-Info ("Found {0} coverage file(s) under {1}" -f $coverageFiles.Count, $runDir)
  }
  'coverage-html' {
    # Run coverage and generate HTML report using reportgenerator
    $resultsRoot = Join-Path $OutputDirectory 'coverage'
    if (-not (Test-Path $resultsRoot)) { New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $runDir = Join-Path $resultsRoot ("run_$timestamp")
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    Write-Step "Running tests with coverage -> $runDir"

    try {
      Invoke-Dotnet @('test', $SolutionPath, '--collect:"XPlat Code Coverage"', '--results-directory', $runDir, '--settings:.runsettings')
    }
    catch {
      Write-Error 'dotnet test failed.'
      exit 1
    }

    $coverageFiles = Get-ChildItem -Recurse -Path $runDir -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles) { Write-Error 'No coverage.cobertura.xml files found.'; exit 2 }

    # Generate HTML using reportgenerator tool (ensure tools restored)
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    $reportRoot = Join-Path $runDir 'report'
    if (-not (Test-Path $reportRoot)) { New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null }
    $reportsArg = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
    $reportTypes = 'HtmlInline_AzurePipelines;MarkdownSummaryGithub'
    Write-Step "Generating HTML report -> $reportRoot"
    Write-Debug "dotnet tool run reportgenerator -- -reports:$reportsArg -targetdir:$reportRoot -reporttypes:$reportTypes"
    & dotnet tool run reportgenerator -- "-reports:$reportsArg" "-targetdir:$reportRoot" "-reporttypes:$reportTypes"
    if ($LASTEXITCODE -ne 0) { Write-Error 'Report generation failed'; exit 3 }

    $indexFile = Join-Path $reportRoot 'index.html'
    if (Test-Path $indexFile) {
      Write-Info "Report generated at: $indexFile"
      Open-File $indexFile
    }
    else {
      Write-Warn 'Report generation completed but index.html not found.'
    }
  }
  'roslynator-analyze' {
    Write-Step "Running Roslynator Analyze on solution: $SolutionPath"
    Ensure-DotNetTools
    Write-Debug "dotnet roslynator analyze $SolutionPath"
    & dotnet roslynator analyze $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Error 'Roslynator analyze completed with warnings or errors.' }
  }
  'roslynator-loc' {
    Write-Step "Running Roslynator LOC (Lines of Code) on solution: $SolutionPath"
    Ensure-DotNetTools
    Write-Debug "dotnet roslynator loc $SolutionPath"
    & dotnet roslynator loc $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Error 'Roslynator loc completed with warnings or errors.' }
  }
  'roslynator-lloc' {
    Write-Step "Running Roslynator LLOC (Logical Lines of Code) on solution: $SolutionPath"
    Ensure-DotNetTools
    Write-Debug "dotnet roslynator lloc $SolutionPath"
    & dotnet roslynator lloc $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Error 'Roslynator lloc completed with warnings or errors.' }
  }

  default { throw "Unknown dotnet command: $Command" }
}
