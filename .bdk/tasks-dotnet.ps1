param(
  [Parameter(Mandatory = $true)][string]$Command,
  [string]$SolutionPath # auto-resolved if omitted
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
$OutputDirectory = Join-Path $Root ".tmp"

function Invoke-Dotnet([string[]]$Arguments) {
  Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
  & dotnet @Arguments
  if ($LASTEXITCODE -ne 0) { throw "dotnet command failed ($LASTEXITCODE)" }
}

function Open-File {
  param([string]$Path)
  if (-not $Path) { return }
  if (-not (Test-Path $Path)) { Write-Host "File not found: $Path" -ForegroundColor Yellow; return }
  try {
    if ($IsWindows) {
      Start-Process -FilePath $Path -ErrorAction Stop
    }
    else {
      # prefer xdg-open, fallback to open (macOS)
      if (Get-Command xdg-open -ErrorAction SilentlyContinue) {
        & xdg-open $Path 2>$null
      }
      elseif (Get-Command open -ErrorAction SilentlyContinue) {
        & open $Path 2>$null
      }
      else {
        Write-Host "No known opener found for this platform. File located at: $Path" -ForegroundColor Yellow
      }
    }
    Write-Host "Opened: $Path" -ForegroundColor Green
  }
  catch {
    Write-Host "Error opening: $($_.Exception.Message)" -ForegroundColor Yellow
  }
}

function Select-Rid {
  param([string[]]$Rids)
  if (-not $Rids) { return $null }
  try {
    Import-Module PwshSpectreConsole -ErrorAction Stop
    $selection = Read-SpectreSelection -Title 'Select RID (blank for framework-dependent)' -Choices ($Rids + 'Framework-Dependent' + 'Cancel') -EnableSearch -PageSize 15
    if (-not $selection -or $selection -eq 'Cancel') { return $null }
    if ($selection -eq 'Framework-Dependent') { return $null }
    return $selection
  }
  catch {
    Write-Host 'Spectre selection unavailable; proceeding framework-dependent.' -ForegroundColor Yellow
    return $null
  }
}

function Copy-PublishOutput {
  param(
    [string]$Root,
    [string]$Rid # optional for RID-specific publish
  )
  try {
    if (-not $Root) { return }
    # Determine publish folder (RID-specific or generic)
    $publishDir = if ($Rid) { Join-Path $Root "bin/Release/net9.0/$Rid/publish" } else { Join-Path $Root "bin/Release/net9.0/publish" }
    if (-not (Test-Path $publishDir)) { Write-Host "Publish output not found: $publishDir" -ForegroundColor Yellow; return }
    $destRoot = Join-Path $Root '.tmp/publish'
    New-Item -ItemType Directory -Force -Path $destRoot | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $dest = Join-Path $destRoot $stamp
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    Write-Host "Copying publish output -> $dest" -ForegroundColor Cyan
    Copy-Item (Join-Path $publishDir '*') -Destination $dest -Recurse -Force
    # Write summary JSON
    $files = Get-ChildItem -Path $dest -Recurse -File | Select-Object -ExpandProperty FullName
    $summary = [ordered]@{ project = $Root; rid = $Rid; source = $publishDir; destination = $dest; fileCount = $files.Count; timestamp = $stamp }
    $summary | ConvertTo-Json -Depth 5 | Out-File (Join-Path $dest 'publish_summary.json') -Encoding UTF8
    Write-Host "Publish output copied ($($files.Count) files)." -ForegroundColor Green
  }
  catch {
    Write-Host "Failed to copy publish output: $($_.Exception.Message)" -ForegroundColor Yellow
  }
}

function Resolve-Solution([string]$explicit) {
  $solutions = Get-ChildItem -Path $Root -Filter '*.sln' -File
  if (-not $solutions -or $solutions.Count -eq 0) {
    throw "No .sln files found under $Root"
  }

  # Single solution: return directly (explicit ignored if different)
  if ($solutions.Count -eq 1) {
    if ($explicit -and (Test-Path $explicit) -and ((Resolve-Path $explicit).Path -ne $solutions[0].FullName)) {
      Write-Host "Explicit solution differs; using discovered single solution." -ForegroundColor DarkYellow
    }
    return $solutions[0].FullName
  }

  # Multiple solutions: always prompt (ignore explicit)
  # Write-Host "Multiple solutions detected (${($solutions.Count)})." -ForegroundColor Cyan
  $choices = $solutions | ForEach-Object { $_.FullName }
  $selected = Read-SpectreSelection -Title 'Select Solution (.sln)' -Choices ($choices + 'Cancel') -EnableSearch -PageSize 15
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
    Write-Host "Solution resolution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 10
  }
  Write-Host "Using solution: $SolutionPath" -ForegroundColor DarkGray
}

# Common logger args as array
$logArgs = @('--nologo', '/property:GenerateFullPaths=true', '/consoleloggerparameters:NoSummary')

switch ($Command.ToLowerInvariant()) {
  'restore' { Invoke-Dotnet (@('restore', $SolutionPath) + $logArgs) }
  'build' { Invoke-Dotnet (@('build', $SolutionPath) + $logArgs) }
  'build-release' { Invoke-Dotnet (@('build', $SolutionPath, '-c', 'Release') + $logArgs) }
  'build-nr' { Invoke-Dotnet (@('build', $SolutionPath, '--no-restore') + $logArgs) }
  'pack' { Invoke-Dotnet (@('pack', $SolutionPath, '-c', 'Release') + $logArgs) }
  'clean' { Invoke-Dotnet (@('clean', $SolutionPath) + $logArgs) }
  'format-check' { Invoke-Dotnet @('format', $SolutionPath, '--verify-no-changes') }
  'format-apply' { Invoke-Dotnet @('format', $SolutionPath) }
  'tool-restore' { Invoke-Dotnet @('tool', 'restore') }
  'vulnerabilities' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--vulnerable') }
  'vulnerabilities-deep' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--vulnerable', '--include-transitive') }
  'outdated' { Invoke-Dotnet @('list', $SolutionPath, 'package', '--outdated') }
  'outdated-json' {
    $OutputDirectoryCompliance = Join-Path (Join-Path $OutputDirectory 'compliance')
    if (-not (Test-Path $OutputDirectoryCompliance)) { New-Item -ItemType Directory -Force -Path $OutputDirectoryCompliance | Out-Null }
    $outFile = Join-Path $OutputDirectoryCompliance ("outdated_" + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.json')
    Write-Host "Collecting outdated packages (JSON) -> $outFile" -ForegroundColor Cyan
    # dotnet list outdated does not have native JSON; capture text then transform rudimentary structure
    $raw = & dotnet list $SolutionPath package --outdated
    if ($LASTEXITCODE -ne 0) { throw 'dotnet list outdated failed' }
    $lines = $raw -split "`r?`n"
    $pkgs = @()
    foreach ($l in $lines) {
      if ($l -match '>(\s*)(?<name>[^\s]+)\s+(?<current>\S+)\s+(?<wanted>\S+)\s+(?<latest>\S+)') { $pkgs += [pscustomobject]@{ name = $Matches.name; current = $Matches.current; wanted = $Matches.wanted; latest = $Matches.latest } }
    }
    $pkgs | ConvertTo-Json -Depth 4 | Set-Content -Path $outFile -Encoding UTF8
    Write-Host ("Outdated packages captured: {0}" -f $pkgs.Count) -ForegroundColor Green
  }
  'update-packages' {
    # Use dotnet-outdated global tool to update package versions. https://github.com/dotnet-outdated/dotnet-outdated
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' 91 }
    Write-Host 'Running dotnet-outdated (auto upgrade within constraints)...' -ForegroundColor Cyan
    # Central Package Management: dotnet-outdated supports --upgrade and --allow-major-version-updates if desired.
    & dotnet outdated $SolutionPath --upgrade
    if($LASTEXITCODE -ne 0){ Write-Host 'dotnet-outdated reported issues or applied updates with non-zero exit code.' -ForegroundColor Yellow }
    Write-Host 'dotnet-outdated execution complete.' -ForegroundColor Green
  }
  'update-packages-devkit' {
    # Use dotnet-outdated global tool to update package versions. https://github.com/dotnet-outdated/dotnet-outdated
    dotnet tool restore | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' 91 }
    Write-Host 'Running dotnet-outdated (auto upgrade devkit within constraints)...' -ForegroundColor Cyan
    # Central Package Management: dotnet-outdated supports --upgrade and --allow-major-version-updates if desired.
    & dotnet outdated $SolutionPath --upgrade -inc 'BridgingIT.DevKit'
    if($LASTEXITCODE -ne 0){ Write-Host 'dotnet-outdated reported issues or applied updates with non-zero exit code.' -ForegroundColor Yellow }
    Write-Host 'dotnet-outdated execution complete.' -ForegroundColor Green
  }
  'analyzers' { Invoke-Dotnet @('build', $SolutionPath, '-warnaserror', '/p:RunAnalyzers=true', '/p:EnableNETAnalyzers=true', '/p:AnalysisLevel=latest') }
  'project-build' { if (-not $SolutionPath) { throw 'Path required for project-build' }; Invoke-Dotnet (@('build', $SolutionPath) + $logArgs) }
  'project-watch' { if (-not $SolutionPath) { throw 'Path required for project-watch' }; Invoke-Dotnet @('watch', 'run', '--project', $SolutionPath, '--nologo') }
  'project-run' { if (-not $SolutionPath) { throw 'Path required for project-run' }; Invoke-Dotnet (@('run', '--project', $SolutionPath) + $logArgs) }
  'project-watch-fast' { if (-not $SolutionPath) { throw 'Path required for project-watch-fast' }; Invoke-Dotnet @('watch', 'run', '--project', $SolutionPath, '--nologo', '--no-restore') }
  'project-publish' {
    if (-not $SolutionPath) { throw 'Path required for project-publish' }
    $rids = @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-musl-x64', 'linux-musl-arm64', 'linux-arm', 'linux-arm64')
    $rid = Select-Rid -Rids $rids
    $args = @('publish', $SolutionPath) + $logArgs
    if ($rid) { $args += @('-r', $rid, '--self-contained', 'true') }
    Invoke-Dotnet $args
    # Copy-PublishOutput $Root -Rid $rid
  }
  'project-publish-release' {
    if (-not $SolutionPath) { throw 'Path required for project-publish-release' }
    $rids = @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-musl-x64', 'linux-musl-arm64', 'linux-arm', 'linux-arm64')
    $rid = Select-Rid -Rids $rids
    $args = @('publish', $SolutionPath, '-c', 'Release') + $logArgs
    if ($rid) { $args += @('-r', $rid, '--self-contained', 'true') }
    Invoke-Dotnet $args
    # Copy-PublishOutput $Root -Rid $rid
  }
  'project-publish-sc' {
    if (-not $SolutionPath) { throw 'Path required for project-publish-sc' }
    # Self-contained single-file publish reusing Select-Rid helper
    $rids = @('win-x64', 'win-x86', 'win-arm64', 'linux-x64', 'linux-musl-x64', 'linux-musl-arm64', 'linux-arm', 'linux-arm64')
    $rid = Select-Rid -Rids $rids
    if (-not $rid) { Write-Host 'Self-contained publish requires a RID; cancelled.' -ForegroundColor Yellow; break }
    Write-Host "Publishing self-contained single-file for RID: $rid" -ForegroundColor Cyan
    $publishArgs = @('publish', $SolutionPath, '-c', 'Release', '-r', $rid, '--self-contained', 'true', '/p:PublishSingleFile=true', '/p:PublishTrimmed=false') + $logArgs
    Invoke-Dotnet $publishArgs
    Write-Host "Self-contained publish complete for RID $rid" -ForegroundColor Green
    # Copy-PublishOutput $Root -Rid $rid
  }
  'pack-projects' {
    # Packs each module project (Domain/Application/Infrastructure/Presentation) into .tmp/packages
    $modulesPath = Join-Path $Root 'src/Modules'
    if (-not (Test-Path $modulesPath)) { throw "Modules path not found: $modulesPath" }
    $outDir = Join-Path $Root '.tmp/packages'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $projects = Get-ChildItem -Path $modulesPath -Recurse -Filter '*.csproj' | Where-Object { $_.FullName -notmatch 'Tests' -and $_.Name -match 'CoreModule\.(Domain|Application|Infrastructure|Presentation)\.csproj' }
    if (-not $projects) { Write-Host 'No module projects found to pack.' -ForegroundColor Yellow; break }
    foreach ($p in $projects) {
      Write-Host "Packing module project: $($p.FullName)" -ForegroundColor Cyan
      Invoke-Dotnet @('pack', $p.FullName, '-c', 'Release', '-o', $outDir)
    }
    Write-Host "Module packages written to $outDir" -ForegroundColor Green
  }
  'analyzers-export' {
    # Uses solution (resolved earlier)
    $anDir = Join-Path $Root '.tmp/analyzers'
    if (-not (Test-Path $anDir)) { New-Item -ItemType Directory -Force -Path $anDir | Out-Null }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $sarifFile = Join-Path $anDir "analyzers_$stamp.sarif"
    Write-Host "Running analyzers export -> $sarifFile" -ForegroundColor Cyan
    & dotnet build $SolutionPath -warnaserror /p:RunAnalyzers=true /p:EnableNETAnalyzers=true /p:AnalysisLevel=latest /p:ErrorLog=$sarifFile
    if ($LASTEXITCODE -ne 0) { throw 'Analyzer build failed' }
    # Produce summary JSON (count diagnostics by id/severity) simple parse of SARIF
    $sarifJson = Get-Content -Raw -Path $sarifFile | ConvertFrom-Json
    $issues = @()
    if ($sarifJson.runs) {
      foreach ($run in $sarifJson.runs) { if ($run.results) { $issues += $run.results } }
    }
    $summary = $issues | Group-Object -Property ruleId | ForEach-Object { [pscustomobject]@{ ruleId = $_.Name; count = $_.Count } }
    $summaryFile = Join-Path $anDir "analyzers_summary_$stamp.json"
    $summary | ConvertTo-Json -Depth 4 | Set-Content -Path $summaryFile -Encoding UTF8
    Write-Host "Analyzer summary written: $summaryFile" -ForegroundColor Green
  }
  'licenses' {
    # Generate license report using nuget-license (moved from tasks-compliance.ps1)
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    $outDir = Join-Path $Root '.tmp/compliance'
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $mdFile = Join-Path $outDir "licenses_$timestamp.md"
    $jsonFile = Join-Path $outDir "licenses_$timestamp.json"
    Write-Host "Generating license report" -ForegroundColor Cyan
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

    Write-Host 'License reports created with nuget-license:' -ForegroundColor Green
    Write-Host "  MD:   $mdFile" -ForegroundColor Green
    Write-Host "  JSON: $jsonFile" -ForegroundColor Green

    # open the generated markdown file
    Open-File $mdFile
  }
  'coverage' {
    # Ensure plain coverage (no HTML) exists: runs tests with coverage and writes coverage.cobertura.xml files
    $resultsRoot = Join-Path $Root '.tmp/tests/coverage'
    if (-not (Test-Path $resultsRoot)) { New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $runDir = Join-Path $resultsRoot ("run_$timestamp")
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    Write-Host "Running tests with coverage -> $runDir" -ForegroundColor Cyan

    try {
      Invoke-Dotnet @('test', $SolutionPath, '--collect:"XPlat Code Coverage"', '--results-directory', $runDir, '--settings:.runsettings')
    }
    catch {
      Write-Host 'dotnet test failed.' -ForegroundColor Red
      exit 1
    }

    $coverageFiles = Get-ChildItem -Recurse -Path $runDir -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles) { Write-Host 'No coverage.cobertura.xml files found.' -ForegroundColor Yellow; exit 2 }
    Write-Host ("Found {0} coverage file(s) under {1}" -f $coverageFiles.Count, $runDir) -ForegroundColor Green
  }
  'coverage-html' {
    # Run coverage and generate HTML report using reportgenerator
    $resultsRoot = Join-Path $Root '.tmp/tests/coverage'
    if (-not (Test-Path $resultsRoot)) { New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $runDir = Join-Path $resultsRoot ("run_$timestamp")
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    Write-Host "Running tests with coverage -> $runDir" -ForegroundColor Cyan

    try {
      Invoke-Dotnet @('test', $SolutionPath, '--collect:"XPlat Code Coverage"', '--results-directory', $runDir, '--settings:.runsettings')
    }
    catch {
      Write-Host 'dotnet test failed.' -ForegroundColor Red
      exit 1
    }

    $coverageFiles = Get-ChildItem -Recurse -Path $runDir -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue
    if (-not $coverageFiles) { Write-Host 'No coverage.cobertura.xml files found.' -ForegroundColor Yellow; exit 2 }

    # Generate HTML using reportgenerator tool (ensure tools restored)
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    $reportRoot = Join-Path $runDir 'report'
    if (-not (Test-Path $reportRoot)) { New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null }
    $reportsArg = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
    $reportTypes = 'HtmlInline_AzurePipelines;MarkdownSummaryGithub'
    Write-Host "Generating HTML report -> $reportRoot" -ForegroundColor Cyan
    & dotnet tool run reportgenerator -- "-reports:$reportsArg" "-targetdir:$reportRoot" "-reporttypes:$reportTypes"
    if ($LASTEXITCODE -ne 0) { Write-Host 'Report generation failed' -ForegroundColor Red; exit 3 }

    $indexFile = Join-Path $reportRoot 'index.html'
    if (Test-Path $indexFile) {
      Write-Host "Report generated at: $indexFile" -ForegroundColor Green
      Open-File $indexFile
    }
    else {
      Write-Host 'Report generation completed but index.html not found.' -ForegroundColor Yellow
    }
  }
  'roslynator-analyze' {
    Write-Host "Running Roslynator Analyze on solution: $SolutionPath" -ForegroundColor Cyan
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    & dotnet roslynator analyze $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Host 'Roslynator analyze completed with warnings or errors.' -ForegroundColor Yellow }
  }
  'roslynator-loc' {
    Write-Host "Running Roslynator LOC (Lines of Code) on solution: $SolutionPath" -ForegroundColor Cyan
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    & dotnet roslynator loc $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Host 'Roslynator loc completed with warnings or errors.' -ForegroundColor Yellow }
  }
  'roslynator-lloc' {
    Write-Host "Running Roslynator LLOC (Logical Lines of Code) on solution: $SolutionPath" -ForegroundColor Cyan
    Invoke-Dotnet @('tool', 'restore') | Out-Null
    & dotnet roslynator lloc $SolutionPath
    if ($LASTEXITCODE -ne 0) { Write-Host 'Roslynator lloc completed with warnings or errors.' -ForegroundColor Yellow }
  }

  default { throw "Unknown dotnet command: $Command" }
}