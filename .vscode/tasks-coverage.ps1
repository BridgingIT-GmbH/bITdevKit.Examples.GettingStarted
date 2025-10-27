param(
  [string]$Solution = 'BridgingIT.DevKit.Examples.GettingStarted.sln',
  [string]$ResultsDir = './.tmp/tests/coverage',
  [switch]$Html,
  [switch]$Open
)
$ErrorActionPreference = 'Stop'
function Write-Section($t){ Write-Host "`n=== $t ===" -ForegroundColor Cyan }
Write-Section "Coverage Run"
$fullResults = (Resolve-Path $ResultsDir -ErrorAction SilentlyContinue)
if(-not $fullResults){ New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null; $fullResults = Resolve-Path $ResultsDir }
Write-Host "Results dir: $fullResults" -ForegroundColor DarkCyan
# Run all tests with coverage collector
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$runDir = Join-Path $fullResults "run_$timestamp"
New-Item -ItemType Directory -Force -Path $runDir | Out-Null
$logFile = Join-Path $runDir 'test.log'
Write-Host 'Executing dotnet test with coverage...' -ForegroundColor Green
& dotnet test $Solution --collect:"XPlat Code Coverage" --results-directory $runDir --settings:.runsettings | Tee-Object -FilePath $logFile
if($LASTEXITCODE -ne 0){ Write-Host 'dotnet test failed.' -ForegroundColor Red; exit 1 }
# Find coverage files (cobertura or json)
$coverageFiles = Get-ChildItem -Recurse -Path $runDir -Filter 'coverage.cobertura.xml'
if(-not $coverageFiles){ Write-Host 'No coverage.cobertura.xml files found.' -ForegroundColor Yellow; exit 2 }
Write-Host ("Found {0} coverage file(s)" -f $coverageFiles.Count)
$reportRoot = Join-Path $runDir 'report'
if($Html){
  Write-Section 'Generating HTML report'
  dotnet tool restore | Out-Null
  $reportPaths = ($coverageFiles | Select-Object -ExpandProperty FullName) -join ';'
  # Ensure target directory exists
  if(-not (Test-Path $reportRoot)){ New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null }
  $reportTypes = 'HtmlInline_AzurePipelines;MarkdownSummaryGithub'
  Write-Host "Invoking reportgenerator with reports=$reportPaths" -ForegroundColor DarkGray
  # Use dotnet tool run to ensure shim path resolution
  $rgCmd = @('tool','run','reportgenerator','--',"-reports:$reportPaths","-targetdir:$reportRoot","-reporttypes:$reportTypes")
  & dotnet $rgCmd
  if($LASTEXITCODE -ne 0){ Write-Host 'Report generation failed' -ForegroundColor Red; exit 3 }
  $indexFile = Join-Path $reportRoot 'index.html'
  Write-Host "Report generated at: $indexFile" -ForegroundColor Green
  if($Open -and (Test-Path $indexFile)){
    Write-Section 'Opening HTML report'
    try {
      if($IsWindows){ Start-Process $indexFile } else { & xdg-open $indexFile }
    } catch { Write-Host "Failed to open report: $($_.Exception.Message)" -ForegroundColor Yellow }
  }
}
Write-Host 'Coverage processing complete.' -ForegroundColor Green
