param(
  [Parameter(Mandatory = $true)][string]$Command
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

function Export-BenchmarkSummary {
  param(
    [string]$BenchmarkProjectPath
  )
  try {
    if (-not $BenchmarkProjectPath) { return }
    $projectDir = Split-Path $BenchmarkProjectPath -Parent
    $artifactRoot = Join-Path $projectDir 'BenchmarkDotNet.Artifacts' 'results'
    if (-not (Test-Path $artifactRoot)) {
      $artifactRoot = Join-Path $Root 'BenchmarkDotNet.Artifacts' 'results'
    }
    if (-not (Test-Path $artifactRoot)) { Write-Host "No BenchmarkDotNet results directory found (checked project + solution): $artifactRoot" -ForegroundColor Yellow; return }
    $outDir = Join-Path $Root (Get-OutputDirectory) 'benchmarks'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $sessionDir = Join-Path $outDir $timestamp
    New-Item -ItemType Directory -Force -Path $sessionDir | Out-Null
    $copied = @()
    Get-ChildItem $artifactRoot -File | ForEach-Object {
      Copy-Item $_.FullName -Destination (Join-Path $sessionDir $_.Name) -Force
      $copied += $_.Name
    }
    # Build JSON summary from github md report if present
    $githubMd = Get-ChildItem $sessionDir -Filter '*-report-github.md' | Select-Object -First 1
    $csv = Get-ChildItem $sessionDir -Filter '*-report.csv' | Select-Object -First 1
    $summary = [ordered]@{
      project            = $BenchmarkProjectPath
      timestamp          = $timestamp
      artifacts          = $copied
      csvPath            = $csv?.FullName
      markdownGithubPath = $githubMd?.FullName
    }
    $summaryFile = Join-Path $sessionDir 'summary.json'
    $summary | ConvertTo-Json -Depth 5 | Out-File -FilePath $summaryFile -Encoding UTF8
    Write-Host "Benchmark summary exported: $summaryFile" -ForegroundColor Green
  }
  catch {
    Write-Host "Failed exporting benchmark summary: $($_.Exception.Message)" -ForegroundColor Yellow
  }
}

${script:DotNetOnly} = $false

switch ($Command.ToLowerInvariant()) {
  'bench' {
    # Run benchmark project if exists
    $benchProj = Get-ChildItem -Recurse -Filter '*Benchmarks.csproj' | Select-Object -First 1
    if (-not $benchProj) { Write-Host 'No benchmark project (*.Benchmarks.csproj) found.' -ForegroundColor Yellow; break }
    Write-Host "Attempting benchmark run: $($benchProj.FullName)" -ForegroundColor Cyan
    try {
      $env:DOTNET_EnableDiagnostics = 0
      & dotnet run --project $benchProj.FullName -c Release -- --filter '*' --anyCategories "*"
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      if ($LASTEXITCODE -ne 0) { throw 'BenchmarkDotNet failed' }
      Write-Host 'Benchmarks completed.' -ForegroundColor Green
      Export-BenchmarkSummary -BenchmarkProjectPath $benchProj.FullName
    }
    catch {
      Write-Host "Benchmark run failed: $($_.Exception.Message). Falling back to simple performance smoke (build + run)." -ForegroundColor Yellow
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      & dotnet build $benchProj.FullName -c Release
      if ($LASTEXITCODE -ne 0) { Write-Host 'Fallback build failed.' -ForegroundColor Red; break }
      & dotnet run --project $benchProj.FullName -c Release --no-build
      Write-Host 'Fallback benchmark smoke complete.' -ForegroundColor Green
    }
  }
  'trace-flame' {
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $durationsMap = [ordered]@{ '10 sec' = '00:00:10'; '30 sec' = '00:00:30'; '1 min' = '00:01:00'; '5 min' = '00:05:00' }
    $duration = $durationsMap['10 sec']
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; $choice = Read-SpectreSelection -Title 'Select flame trace duration' -Choices ($durationsMap.Keys + 'Cancel') -EnableSearch -PageSize 10; if ($choice -and $choice -ne 'Cancel') { $duration = $durationsMap[$choice] }; if ($choice -eq 'Cancel') { Write-Host 'Flame trace cancelled.' -ForegroundColor Yellow; break } } catch { Write-Host 'Spectre selection unavailable; using default 10 sec.' -ForegroundColor Yellow }
    $procId = Select-Pid 'Select process to trace (flame)'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' (Get-OutputDirectory) 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $fileBase = "trace_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    $traceFile = Join-Path $outDir "$fileBase.nettrace"
    $speedBase = Join-Path $outDir $fileBase
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; @( 'Flame Trace', 'PID: ' + $procId, 'Duration: ' + $duration, 'Providers: SampleProfiler', 'Artifacts Base: ' + $fileBase ) | Format-SpectreRows | Format-SpectrePanel -Expand -Color "DeepSkyBlue3" } catch {}
    Write-Host "Collecting flame trace (SampleProfiler, $duration) for PID $procId ..." -ForegroundColor Cyan
    & dotnet-trace collect --process-id $procId --providers Microsoft-DotNETCore-SampleProfiler:1 --duration $duration -o $traceFile
    if ($LASTEXITCODE -ne 0) { Write-Host 'SampleProfiler provider failed, retrying with default trace config (cpu+gc)...' -ForegroundColor Yellow; & dotnet-trace collect --process-id $procId --duration $duration -o $traceFile; if ($LASTEXITCODE -ne 0) { throw 'Trace collection failed (fallback also failed)' } }
    Write-Host 'Converting to speedscope...' -ForegroundColor Cyan
    & dotnet-trace convert --format SpeedScope $traceFile -o $speedBase
    if ($LASTEXITCODE -ne 0) { throw 'Trace conversion failed' }
    Write-Host "Trace complete: $traceFile" -ForegroundColor Green
    $speedFile = $speedBase + '.speedscope.json'
    if (Test-Path "$speedFile.speedscope.json") { Move-Item -Force "$speedFile.speedscope.json" $speedFile }
    Write-Host "Speedscope file: $speedFile" -ForegroundColor Green
    if (-not $env:TRACE_NO_VIEW) {
      try { $resolvedSpeed = (Resolve-Path $speedFile).Path; if (-not (Test-Path $resolvedSpeed)) { throw "Speedscope file not found: $resolvedSpeed" }; if (Get-Command npx -ErrorAction SilentlyContinue) { Write-Host "Opening speedscope (npx) -> $resolvedSpeed" -ForegroundColor Cyan; & npx speedscope "$resolvedSpeed"; if ($LASTEXITCODE -ne 0) { Write-Host 'npx speedscope returned non-zero; fallback to browser.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } else { Write-Host 'npx not available; opening speedscope.app and folder.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } catch { Write-Host "Speedscope auto-open failed: $($_.Exception.Message)" -ForegroundColor Yellow }
    }
    else { Write-Host 'TRACE_NO_VIEW set; skipping auto-open.' -ForegroundColor DarkYellow }
  }
  'trace-cpu' {
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $durationsMap = [ordered]@{ '10 sec' = '00:00:10'; '30 sec' = '00:00:30'; '1 min' = '00:01:00'; '5 min' = '00:05:00' }
    $duration = $durationsMap['10 sec']
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; $choice = Read-SpectreSelection -Title 'Select trace duration' -Choices ($durationsMap.Keys + 'Cancel') -EnableSearch -PageSize 10; if ($choice -and $choice -ne 'Cancel') { $duration = $durationsMap[$choice] }; if ($choice -eq 'Cancel') { Write-Host 'CPU trace cancelled.' -ForegroundColor Yellow; break } } catch { Write-Host 'Spectre selection unavailable; using default 10 sec.' -ForegroundColor Yellow }
    $procId = Select-Pid 'Select process for CPU trace'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' (Get-OutputDirectory) 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $fileBase = "cpu_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    $traceFile = Join-Path $outDir "$fileBase.nettrace"
    $speedBase = Join-Path $outDir $fileBase
    $extended = $false
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; $provChoice = Read-SpectreSelection -Title 'Include extended runtime providers?' -Choices @('No', 'Yes', 'Cancel') -EnableSearch -PageSize 5; if ($provChoice -eq 'Cancel') { Write-Host 'CPU trace cancelled.' -ForegroundColor Yellow; break }; if ($provChoice -eq 'Yes') { $extended = $true } } catch { Write-Host 'Provider selection skipped (Spectre unavailable).' -ForegroundColor Yellow }
    $providers = 'Microsoft-DotNETCore-SampleProfiler:1,System.Runtime:4'
    if ($extended) { $providers += ',Microsoft-DotNETCore-EventSource:5' }
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; @( 'CPU Trace', 'PID: ' + $procId, 'Duration: ' + $duration, 'Providers: ' + $providers, 'Extended: ' + $extended, 'Artifacts Base: ' + $fileBase ) | Format-SpectreRows | Format-SpectrePanel -Expand -Color "DeepSkyBlue3" } catch {}
    Write-Host "Collecting CPU trace ($providers, $duration) for PID $procId ..." -ForegroundColor Cyan
    & dotnet-trace collect --process-id $procId --providers $providers --duration $duration -o $traceFile
    if ($LASTEXITCODE -ne 0) { throw 'CPU trace collection failed' }
    Write-Host 'Converting to speedscope...' -ForegroundColor Cyan
    & dotnet-trace convert --format SpeedScope $traceFile -o $speedBase
    if ($LASTEXITCODE -ne 0) { throw 'CPU trace conversion failed' }
    Write-Host "CPU trace complete: $traceFile" -ForegroundColor Green
    $speedFile = $speedBase + '.speedscope.json'
    if (Test-Path "$speedFile.speedscope.json") { Move-Item -Force "$speedFile.speedscope.json" $speedFile }
    Write-Host "Speedscope file: $speedFile" -ForegroundColor Green
    if (-not $env:TRACE_NO_VIEW) {
      try { $resolvedSpeed = (Resolve-Path $speedFile).Path; if (-not (Test-Path $resolvedSpeed)) { throw "Speedscope file not found: $resolvedSpeed" }; if (Get-Command npx -ErrorAction SilentlyContinue) { Write-Host "Opening speedscope (npx) -> $resolvedSpeed" -ForegroundColor Cyan; & npx speedscope "$resolvedSpeed"; if ($LASTEXITCODE -ne 0) { Write-Host 'npx speedscope returned non-zero; fallback to browser.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } else { Write-Host 'npx not available; opening speedscope.app and folder.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } catch { Write-Host "Speedscope auto-open failed: $($_.Exception.Message)" -ForegroundColor Yellow }
    }
    else { Write-Host 'TRACE_NO_VIEW set; skipping auto-open.' -ForegroundColor DarkYellow }
  }
  'trace-gc' {
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $durationsMap = [ordered]@{ '10 sec' = '00:00:10'; '30 sec' = '00:00:30'; '1 min' = '00:01:00'; '5 min' = '00:05:00' }
    $duration = $durationsMap['10 sec']
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; $choice = Read-SpectreSelection -Title 'Select GC trace duration' -Choices ($durationsMap.Keys + 'Cancel') -EnableSearch -PageSize 10; if ($choice -and $choice -ne 'Cancel') { $duration = $durationsMap[$choice] }; if ($choice -eq 'Cancel') { Write-Host 'GC trace cancelled.' -ForegroundColor Yellow; break } } catch { Write-Host 'Spectre selection unavailable; using default 10 sec.' -ForegroundColor Yellow }
    $procId = Select-Pid 'Select process for GC-focused trace'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' (Get-OutputDirectory) 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $fileBase = "gc_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    $traceFile = Join-Path $outDir "$fileBase.nettrace"
    $speedBase = Join-Path $outDir $fileBase
    try { Import-Module PwshSpectreConsole -ErrorAction Stop; @( 'GC Trace', 'PID: ' + $procId, 'Duration: ' + $duration, 'Providers: SampleProfiler + System.Runtime', 'Artifacts Base: ' + $fileBase ) | Format-SpectreRows | Format-SpectrePanel -Expand -Color "DeepSkyBlue3" } catch {}
    Write-Host "Collecting GC-focused trace (SampleProfiler + System.Runtime, $duration) for PID $procId ..." -ForegroundColor Cyan
    & dotnet-trace collect --process-id $procId --providers Microsoft-DotNETCore-SampleProfiler:1, System.Runtime:4 --duration $duration -o $traceFile
    if ($LASTEXITCODE -ne 0) { throw 'GC trace collection failed' }
    Write-Host 'Converting to speedscope...' -ForegroundColor Cyan
    & dotnet-trace convert --format SpeedScope $traceFile -o $speedBase
    if ($LASTEXITCODE -ne 0) { throw 'GC trace conversion failed' }
    Write-Host "GC trace complete: $traceFile" -ForegroundColor Green
    $speedFile = $speedBase + '.speedscope.json'
    if (Test-Path "$speedFile.speedscope.json") { Move-Item -Force "$speedFile.speedscope.json" $speedFile }
    Write-Host "Speedscope file: $speedFile" -ForegroundColor Green
    if (-not $env:TRACE_NO_VIEW) {
      try { $resolvedSpeed = (Resolve-Path $speedFile).Path; if (-not (Test-Path $resolvedSpeed)) { throw "Speedscope file not found: $resolvedSpeed" }; if (Get-Command npx -ErrorAction SilentlyContinue) { Write-Host "Opening speedscope (npx) -> $resolvedSpeed" -ForegroundColor Cyan; & npx speedscope "$resolvedSpeed"; if ($LASTEXITCODE -ne 0) { Write-Host 'npx speedscope returned non-zero; fallback to browser.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } else { Write-Host 'npx not available; opening speedscope.app and folder.' -ForegroundColor Yellow; Start-Process 'https://www.speedscope.app'; Start-Process explorer.exe (Split-Path $resolvedSpeed -Parent) } } catch { Write-Host "Speedscope auto-open failed: $($_.Exception.Message)" -ForegroundColor Yellow }
    }
    else { Write-Host 'TRACE_NO_VIEW set; skipping auto-open.' -ForegroundColor DarkYellow }
  }
  'speedscope-view' {
    # Select a speedscope json file and open visualization
    $diagDir = Join-Path $PSScriptRoot '..' (Get-OutputDirectory) 'diagnostics'
    if (-not (Test-Path $diagDir)) { Write-Host "Diagnostics directory not found: $diagDir" -ForegroundColor Yellow; break }
    $profiles = Get-ChildItem $diagDir -Recurse -Filter '*.speedscope.json'
    if (-not $profiles) { Write-Host 'No speedscope profiles found.' -ForegroundColor Yellow; break }
    Import-Module PwshSpectreConsole -ErrorAction Stop
    $map = [ordered]@{}
    $labels = @()
    foreach ($p in ($profiles | Sort-Object LastWriteTime -Descending)) {
      $full = (Resolve-Path $p.FullName).Path
      $rel = $full.Substring($Root.Length).TrimStart('\\')
      $label = "$($p.LastWriteTime.ToString('HH:mm:ss')) | $($p.Name)"
      $map[$label] = $full
      $labels += $label
    }
    $sel = Read-SpectreSelection -Title 'Select Speedscope Profile' -Choices ($labels + 'Cancel') -EnableSearch -PageSize 20
    if (-not $sel -or $sel -eq 'Cancel') { Write-Host 'Speedscope view cancelled.' -ForegroundColor Yellow; break }
    if (-not $map.Contains($sel)) { Write-Host 'Invalid selection.' -ForegroundColor Red; break }
    $profile = $map[$sel]
    Write-Host "Selected profile: $profile" -ForegroundColor Cyan
    # Try npx speedscope first (requires node + speedscope)
    $opened = $false
    try {
      if (Get-Command npx -ErrorAction SilentlyContinue) {
        Write-Host 'Launching speedscope via npx (local web UI)...' -ForegroundColor Cyan
        & npx speedscope $profile
        if ($LASTEXITCODE -eq 0) { $opened = $true }
      }
    }
    catch { Write-Host "npx speedscope failed: $($_.Exception.Message)" -ForegroundColor Yellow }
    if (-not $opened) {
      Write-Host 'Opening speedscope profile in default browser (https://www.speedscope.app)...' -ForegroundColor Cyan
      try {
        Start-Process 'https://www.speedscope.app' # user can manually load file
        # Also open folder in Explorer for convenience
        $folder = Split-Path $profile -Parent
        Start-Process explorer.exe $folder
        Write-Host 'Speedscope site and folder opened.' -ForegroundColor Green
      }
      catch { Write-Host "Browser/folder open failed: $($_.Exception.Message)" -ForegroundColor Yellow }
    }
  }
  'dump-heap' {
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $procId = Select-Pid 'Select process for heap dump'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' (Get-OutputDirectory) 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $dumpFile = Join-Path $outDir "heap_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss').dmp"
    Write-Host "Creating heap dump for PID $procId ..." -ForegroundColor Cyan
    & dotnet-dump collect --process-id $procId --type full -o $dumpFile
    if ($LASTEXITCODE -ne 0) { throw 'Heap dump failed' }
    Write-Host "Heap dump created: $dumpFile" -ForegroundColor Green
  }
  'gc-stats' {
    # Collect GC stats via dotnet-counters https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-9.0#view-metrics-with-dotnet-counters
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $procId = Select-Pid 'Select process for GC stats'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    Write-Host "Sampling GC counters for PID $procId (5s) ..." -ForegroundColor Cyan
    # & dotnet-counters monitor --process-id $procId --counters "System.Runtime[gc-heap-size;time-in-gc]" --refresh-interval 1 --duration 5
    & dotnet-counters monitor --process-id $procId --counters "System.Runtime" --refresh-interval 1 --duration 5
    if ($LASTEXITCODE -ne 0) { throw 'GC stats collection failed' }
    Write-Host 'GC sampling complete.' -ForegroundColor Green
  }
  'aspnet-metrics' {
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $procId = Select-Pid 'Select ASP.NET Core process for metrics'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    Write-Host "Monitoring ASP.NET Core counters for PID $procId (10s) ..." -ForegroundColor Cyan
    $counterGroups = @('Microsoft.AspNetCore.Hosting')
    $countersArg = $counterGroups -join ' '
    & dotnet-counters monitor --process-id $procId --counters $countersArg --refresh-interval 1 --duration 10
    if ($LASTEXITCODE -ne 0) { throw 'ASP.NET metrics collection failed' }
    Write-Host 'ASP.NET metrics sampling complete.' -ForegroundColor Green
  }
  'quick' {
    # Combined diagnostics: CPU trace + GC trace + ASP.NET metrics (best effort)
    Ensure-DotNetTools
    $script:DotNetOnly = $true
    $procId = Select-Pid 'Select process for QUICK diagnostics'
    if (-not $procId) { Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' '.tmp' 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $errors = @()
    # CPU trace (5s)
    try {
      $cpuFile = Join-Path $outDir "cpuQuick_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss').nettrace"
      Write-Host "[Quick] CPU trace (5s) for PID $procId" -ForegroundColor Cyan
      & dotnet-trace collect --process-id $procId --providers Microsoft-DotNETCore-SampleProfiler:1 --duration 00:00:05 -o $cpuFile
      if ($LASTEXITCODE -ne 0) { throw 'CPU trace failed' }
      Write-Host "[Quick] CPU trace saved: $cpuFile" -ForegroundColor Green
    }
    catch { $errors += $_.Exception.Message; Write-Host "[Quick] CPU trace error: $($_.Exception.Message)" -ForegroundColor Yellow }
    # GC trace (5s)
    try {
      $gcFile = Join-Path $outDir "gcQuick_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss').nettrace"
      Write-Host "[Quick] GC trace (5s) for PID $procId" -ForegroundColor Cyan
      & dotnet-trace collect --process-id $procId --providers Microsoft-DotNETCore-SampleProfiler:1, System.Runtime:4 --duration 00:00:05 -o $gcFile
      if ($LASTEXITCODE -ne 0) { throw 'GC trace failed' }
      Write-Host "[Quick] GC trace saved: $gcFile" -ForegroundColor Green
    }
    catch { $errors += $_.Exception.Message; Write-Host "[Quick] GC trace error: $($_.Exception.Message)" -ForegroundColor Yellow }
    # ASP.NET metrics (6s) limited group
    try {
      Write-Host "[Quick] ASP.NET metrics (6s) for PID $procId" -ForegroundColor Cyan
      & dotnet-counters monitor --process-id $procId --counters "Microsoft.AspNetCore.Hosting" --refresh-interval 1 --duration 6
      if ($LASTEXITCODE -ne 0) { throw 'ASP.NET metrics failed' }
      Write-Host '[Quick] ASP.NET metrics sampling complete.' -ForegroundColor Green
    }
    catch { $errors += $_.Exception.Message; Write-Host "[Quick] ASP.NET metrics error: $($_.Exception.Message)" -ForegroundColor Yellow }
    if ($errors.Count -gt 0) {
      Write-Host "Quick diagnostics finished with $($errors.Count) error(s)." -ForegroundColor Yellow
      foreach ($e in $errors) { Write-Host " - $e" -ForegroundColor DarkYellow }
    }
    else {
      Write-Host 'Quick diagnostics completed successfully.' -ForegroundColor Green
    }
  }
  'bench-select' {
    # Enumerate benchmark projects and allow Spectre selection (force user selection even if single project)
    $benchProjects = Get-ChildItem -Recurse -Filter '*Benchmarks.csproj' -File 2>$null
    if (-not $benchProjects) { Write-Host 'No benchmark projects (*.Benchmarks.csproj) found.' -ForegroundColor Yellow; break }
    Import-Module PwshSpectreConsole -ErrorAction Stop
    $projectMap = [ordered]@{}
    $labels = @()
    $i = 0
    foreach ($p in ($benchProjects | Sort-Object FullName)) {
      $full = (Resolve-Path $p.FullName).Path
      if ($full.StartsWith($Root)) { $rel = $full.Substring($Root.Length).TrimStart('\\') } else { $rel = Split-Path $full -Leaf }
      # Avoid Spectre markup brackets by using parentheses for index
      $label = "$rel"
      $projectMap[$label] = $full
      $labels += $label
      $i++
    }
    $selected = Read-SpectreSelection -Title 'Select Benchmark Project' -Choices ($labels + 'Cancel') -EnableSearch -PageSize 15
    if (-not $selected -or $selected -eq 'Cancel') { Write-Warn 'Benchmark selection cancelled.'; $LASTEXITCODE = 0; break }
    if (-not $projectMap.Contains($selected)) { Write-Error 'Invalid selection mapping.'; $LASTEXITCODE = 1; break }
    $projPath = $projectMap[$selected]
    Write-Info "Running selected benchmarks: $projPath"
    try {
      $env:DOTNET_EnableDiagnostics = 0
      & dotnet run --project "$projPath" -c Release -- --filter '*' --anyCategories '*'
      $runExit = $LASTEXITCODE
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      if ($runExit -ne 0) { throw "Benchmark run failed (exit $runExit)" }
      Write-Info 'Selected benchmarks completed.'
      Export-BenchmarkSummary -BenchmarkProjectPath $projPath
    }
    catch {
      Write-Error "Benchmark run failed: $($_.Exception.Message)"
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      $LASTEXITCODE = 1
    }
  }
  default { throw "Unknown diagnostics command: $Command" }
}
