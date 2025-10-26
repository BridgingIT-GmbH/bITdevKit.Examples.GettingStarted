param(
  [Parameter(Mandatory=$true)][string]$Command
)
$ErrorActionPreference='Stop'

function Ensure-Tool($tool,$install){
  if(-not (Get-Command $tool -ErrorAction SilentlyContinue)){
    Write-Host "Installing missing tool: $tool" -ForegroundColor Yellow
    & dotnet tool install --global $install
    if($LASTEXITCODE -ne 0){ throw "Failed to install tool $tool" }
  }
}

${script:DotNetOnly} = $false
function Select-Pid($title){
  Import-Module PwshSpectreConsole -ErrorAction Stop
  $procs = Get-Process | Where-Object { $_.Id -gt 0 }
  if($script:DotNetOnly){ $procs = $procs | Where-Object { $_.ProcessName -match 'dotnet|Presentation.Web.Server' } }
  $procs = $procs | Sort-Object ProcessName,Id
  $rows = @()
  foreach($p in $procs){
    $label = "$($p.ProcessName) (#$($p.Id))"
    if($rows -notcontains $label){ $rows += $label }
  }
  if(-not $rows){ Write-Host 'No matching processes found.' -ForegroundColor Yellow; return $null }
  $choices = $rows + 'Cancel'
  $sel = Read-SpectreSelection -Title $title -Choices $choices -EnableSearch -PageSize 25
  Write-Host "Raw selection: '$sel'" -ForegroundColor DarkGray
  if([string]::IsNullOrWhiteSpace($sel) -or $sel -eq 'Cancel'){ return $null }
  if($sel -match '\(#(\d+)\)$'){ Write-Host "Selected PID: $($Matches[1])" -ForegroundColor DarkGray; return [int]$Matches[1] }
  Write-Host "Could not parse PID from selection: $sel" -ForegroundColor Yellow
  return $null
}

switch($Command.ToLowerInvariant()){
  'bench' {
    # Run benchmark project if exists
    $benchProj = Get-ChildItem -Recurse -Filter '*Benchmarks.csproj' | Select-Object -First 1
    if(-not $benchProj){ Write-Host 'No benchmark project (*.Benchmarks.csproj) found.' -ForegroundColor Yellow; break }
    Write-Host "Attempting benchmark run: $($benchProj.FullName)" -ForegroundColor Cyan
    try {
      $env:DOTNET_EnableDiagnostics=0
      & dotnet run --project $benchProj.FullName -c Release -- --filter '*' --anyCategories "*"
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      if($LASTEXITCODE -ne 0){ throw 'BenchmarkDotNet failed' }
      Write-Host 'Benchmarks completed.' -ForegroundColor Green
    }
    catch {
      Write-Host "Benchmark run failed: $($_.Exception.Message). Falling back to simple performance smoke (build + run)." -ForegroundColor Yellow
      Remove-Item Env:DOTNET_EnableDiagnostics -ErrorAction SilentlyContinue
      & dotnet build $benchProj.FullName -c Release
      if($LASTEXITCODE -ne 0){ Write-Host 'Fallback build failed.' -ForegroundColor Red; break }
      & dotnet run --project $benchProj.FullName -c Release --no-build
      Write-Host 'Fallback benchmark smoke complete.' -ForegroundColor Green
    }
  }
  'trace-flame' {
    Ensure-Tool 'dotnet-counters' 'dotnet-counters'
    Ensure-Tool 'dotnet-trace' 'dotnet-trace'
  $script:DotNetOnly = $true
  $procId = Select-Pid 'Select process to trace (flame)'
  if(-not $procId){ Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' '.tmp' 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
  $fileBase = "trace_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
  $traceFile = Join-Path $outDir "$fileBase.nettrace"
  $speedFile = Join-Path $outDir "$fileBase.speedscope.json"
  Write-Host "Collecting trace for PID $procId ..." -ForegroundColor Cyan
  & dotnet-trace collect --process-id $procId --providers Microsoft-DotNETCore-SampleProfiler:1 --duration 00:00:10 -o $traceFile
    if($LASTEXITCODE -ne 0){
      Write-Host 'SampleProfiler provider failed, retrying with default trace config (cpu+gc)...' -ForegroundColor Yellow
  & dotnet-trace collect --process-id $procId --duration 00:00:10 -o $traceFile
      if($LASTEXITCODE -ne 0){ throw 'Trace collection failed (fallback also failed)' }
    }
    Write-Host 'Converting to speedscope...' -ForegroundColor Cyan
    & dotnet-trace convert --format SpeedScope $traceFile -o $speedFile
    if($LASTEXITCODE -ne 0){ throw 'Trace conversion failed' }
  Write-Host "Trace complete: $traceFile" -ForegroundColor Green
  Write-Host "Speedscope file: $speedFile" -ForegroundColor Green
  }
  'dump-heap' {
    Ensure-Tool 'dotnet-dump' 'dotnet-dump'
  $script:DotNetOnly = $true
  $procId = Select-Pid 'Select process for heap dump'
  if(-not $procId){ Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
    $outDir = Join-Path $PSScriptRoot '..' '.tmp' 'diagnostics'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
  $dumpFile = Join-Path $outDir "heap_${procId}_$(Get-Date -Format 'yyyyMMdd_HHmmss').dmp"
  Write-Host "Creating heap dump for PID $procId ..." -ForegroundColor Cyan
  & dotnet-dump collect --process-id $procId --type full -o $dumpFile
    if($LASTEXITCODE -ne 0){ throw 'Heap dump failed' }
    Write-Host "Heap dump created: $dumpFile" -ForegroundColor Green
  }
  'gc-stats' { # Collect GC stats via dotnet-counters https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics?view=aspnetcore-9.0#view-metrics-with-dotnet-counters
    Ensure-Tool 'dotnet-counters' 'dotnet-counters'
  $script:DotNetOnly = $true
  $procId = Select-Pid 'Select process for GC stats'
  if(-not $procId){ Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
  Write-Host "Sampling GC counters for PID $procId (5s) ..." -ForegroundColor Cyan
  # & dotnet-counters monitor --process-id $procId --counters "System.Runtime[gc-heap-size;time-in-gc]" --refresh-interval 1 --duration 5
  & dotnet-counters monitor --process-id $procId --counters "System.Runtime" --refresh-interval 1 --duration 5
    if($LASTEXITCODE -ne 0){ throw 'GC stats collection failed' }
    Write-Host 'GC sampling complete.' -ForegroundColor Green
  }
    'aspnet-metrics' {
      Ensure-Tool 'dotnet-counters' 'dotnet-counters'
      $script:DotNetOnly = $true
      $procId = Select-Pid 'Select ASP.NET Core process for metrics'
      if(-not $procId){ Write-Host 'No PID selected.' -ForegroundColor Yellow; break }
      Write-Host "Monitoring ASP.NET Core + runtime counters for PID $procId (10s) ..." -ForegroundColor Cyan
      # $counterGroups = @(
      #   'Microsoft.AspNetCore.Hosting[requests-started;requests-completed;current-requests]',
      #   'Microsoft.AspNetCore.Server.Kestrel[connection-queue-length;connections-active;connections-opened;connections-closed]',
      #   'System.Net.Http[requests-started;requests-failed]',
      #   'System.Runtime[cpu-usage;working-set;gc-heap-size;gen-0-gc-count;gen-1-gc-count;gen-2-gc-count;time-in-gc]'
      # )
      $counterGroups = @(
        'Microsoft.AspNetCore.Hosting'
      )
      $countersArg = $counterGroups -join ' '
      & dotnet-counters monitor --process-id $procId --counters $countersArg --refresh-interval 1 --duration 10
      if($LASTEXITCODE -ne 0){ throw 'ASP.NET metrics collection failed' }
      Write-Host 'ASP.NET metrics sampling complete.' -ForegroundColor Green
    }
  default { throw "Unknown diagnostics command: $Command" }
}
