<#!
.SYNOPSIS
  OpenAPI related development tasks (linting, future bundling/diff, etc.).

.DESCRIPTION
  Provides a docker-based wrapper around the Spectral linter so it can be invoked via VS Code tasks.
  Uses the official stoplight/spectral image. Ensures the spec exists (build must have generated it).

.EXAMPLES
  pwsh -File .vscode/tasks-openapi.ps1 lint
  pwsh -File .vscode/tasks-openapi.ps1 lint -SpecificationPath src/Presentation.Web.Server/wwwroot/openapi.json -FailSeverity warn
  pwsh -File .vscode/tasks-openapi.ps1 help

.NOTES
  Exit codes: 0 success, non-zero failure (including lint rule violations >= FailSeverity).
  Requires Docker daemon running locally.
#>
param(
  [Parameter(Position=0)] [string] $Command = 'help',
  [Parameter()] [string] $SpecificationPath = 'src/Presentation.Web.Server/wwwroot/openapi.json',
  [Parameter()] [string] $RulesetPath = '.spectral.yaml',
  [Parameter()] [string] $FailSeverity = 'error',
  [Parameter()] [string] $Format = 'stylish',
  [Parameter()] [string] $DockerImage = 'stoplight/spectral:latest',
  [Parameter()] [string] $OutputRoot = '.tmp/openapi',
  [Parameter()] [string] $HttpBaseUrl = 'https://localhost:5001',
  [Parameter()] [string] $HttpOutputType = 'OneFilePerTag'
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'

function Fail([string] $Message, [int] $Code = 1) { Write-Error $Message; exit $Code }
# function Write-Section([string] $Text) { Write-Host "`n=== $Text ===" -ForegroundColor DarkCyan }
function Write-Step([string] $Text) { Write-Host "-- $Text" -ForegroundColor Cyan }

function Ensure-DockerImage([string] $Image) {
  Write-Step "Ensuring docker image '$Image' is available"
  $exists = docker images --format '{{.Repository}}:{{.Tag}}' | Where-Object { $_ -eq $Image }
  if (-not $exists) { Write-Step "Pulling image $Image"; docker pull $Image | Out-Null }
}

function Resolve-Path-Safely([string] $Path) {
  $resolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
  return $resolved
}

function Lint-OpenApi() {
  # Write-Section 'Spectral OpenAPI Lint'

  $specFull = Resolve-Path-Safely $SpecificationPath
  if (-not (Test-Path -LiteralPath $specFull)) { Fail "OpenAPI specification not found: $SpecificationPath (have you built the project?)" 2 }

  $rulesetFull = $null
  if ($RulesetPath -and (Test-Path -LiteralPath $RulesetPath)) { $rulesetFull = Resolve-Path-Safely $RulesetPath }

  # Validate severity & format
  $validSeverities = 'error','warn','info','hint','off'
  if ($validSeverities -notcontains $FailSeverity.ToLower()) { Fail "Invalid -FailSeverity '$FailSeverity'. Valid: $($validSeverities -join ', ')" 3 }
  $validFormats = 'stylish','json','text'
  if ($validFormats -notcontains $Format.ToLower()) { Fail "Invalid -Format '$Format'. Valid: $($validFormats -join ', ')" 4 }

  Ensure-DockerImage -Image $DockerImage

  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  Write-Step "Repository root: $repoRoot"
  Write-Step "Spec path: $specFull"
  if ($rulesetFull) { Write-Step "Ruleset: $rulesetFull" } else { Write-Step 'Ruleset: (none / default rules)' }
  Write-Step "Fail severity: $FailSeverity"
  Write-Step "Output format: $Format"

  # Construct docker run arguments. Mount repo root to /work for access to spec & ruleset
  # Use $() to delimit variable before colon to avoid parser treating ':' as part of variable name
  $dockerArgs = @('run','--rm','-v',"$($repoRoot):/work", $DockerImage, 'lint', "/work/$SpecificationPath", '--format', $Format, '--fail-severity', $FailSeverity)
  if ($rulesetFull) { $dockerArgs += @('-r', "/work/$RulesetPath") }

  Write-Step "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  $exit = $LASTEXITCODE
  if ($exit -ne 0) { Fail "Spectral lint failed (exit code $exit)." $exit }
  Write-Host 'OpenAPI lint succeeded with no violations above threshold.' -ForegroundColor Green
}

function Resolve-Spec() {
  $specFull = Resolve-Path-Safely $SpecificationPath
  if (-not (Test-Path -LiteralPath $specFull)) {
    Fail "Specification not found: $SpecificationPath (expected build to generate it)" 21
  }
  return $specFull
}

function Generate-KiotaClient {
  param(
    [Parameter(Mandatory)][ValidateSet('CSharp','TypeScript')] [string]$Language,
    [string]$ClientClassName = 'ApiClient',
    [string]$Namespace = 'OpenApi.Client'
  )
  # Write-Section "Kiota Generate ($Language)"
  dotnet tool restore | Out-Null
  if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' 91 }
  $specFull = Resolve-Spec

  # Output directory selection
  $root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $baseOut = Join-Path $root $OutputRoot
  $outDir = if ($Language -eq 'CSharp') { Join-Path $baseOut 'dotnet' }
            else { Join-Path $baseOut 'typescript' }
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  # Build Kiota args
  if ($Language -eq 'CSharp') {
    $args = @('kiota','generate',
      '-d', $specFull,
      '-l', 'CSharp',
      '-o', $outDir,
      '--clean-output',
      '-c', $ClientClassName,
      '-n', $Namespace)
  } else {
    $args = @('kiota','generate',
      '-d', $specFull,
      '-l', 'TypeScript',
      '-o', $outDir,
      '--clean-output',
      '-c', $ClientClassName)
  }

  try {
    Import-Module PwshSpectreConsole -ErrorAction Stop
    @( "Kiota Generation",
       "Spec: $specFull",
       "Language: $Language",
       "Output: $outDir",
       "ClientClass: $ClientClassName",
       "Namespace: $Namespace"
     ) | Format-SpectreRows | Format-SpectrePanel -Expand -Color "DeepSkyBlue3" | Out-Null
  } catch { }

  Write-Step "dotnet $($args -join ' ')"
  & dotnet @args
  if ($LASTEXITCODE -ne 0) { Fail "Kiota generation failed ($LASTEXITCODE)" $LASTEXITCODE }

  # Post-generation brief summary
  $fileCount = (Get-ChildItem -Path $outDir -Recurse -File | Measure-Object).Count
  Write-Host "Kiota $Language client generated ($fileCount files)." -ForegroundColor Green
}

function Generate-HttpRequests {
  # Write-Section 'Generating .http request files'
  dotnet tool restore | Out-Null
  if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' 91 }
  $specFull = Resolve-Spec
  $root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $outDir = Join-Path $root $OutputRoot
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null
  Write-Step "Spec: $specFull"
  Write-Step "Output: $outDir"
  Write-Step "BaseUrl: $HttpBaseUrl"
  Write-Step "OutputType: $HttpOutputType"
  # httpgenerator usage: dotnet httpgenerator generate -i <spec> -b <base-url> -o <out> -t <type> -f
  $args = @('httpgenerator', $specFull,
    '--base-url', $HttpBaseUrl,
    '--output', $outDir,
    '--authorization-header', 'Bearer TOKEN',
    '--output-type', $HttpOutputType)
  Write-Step "dotnet $($args -join ' ')"
  & dotnet @args
  if ($LASTEXITCODE -ne 0) { Fail "httpgenerator failed ($LASTEXITCODE)" $LASTEXITCODE }
  $httpFiles = Get-ChildItem -Path $outDir -Filter '*.http' -File -Recurse
  $count = ($httpFiles | Measure-Object).Count
  Write-Host "Generated $count .http file(s) in $outDir" -ForegroundColor Green
}

function Help() {
@'
Usage: pwsh -File .devkit/tasks-openapi.ps1 <command> [options]

Commands:
  lint                Lint OpenAPI (Spectral)
  client-dotnet       Generate C# client (Kiota)
  client-typescript   Generate TypeScript client (Kiota)
  http-requests       Generate .http request files (httpgenerator)
  help                Show help

HTTP Request Generation Defaults:
  Base URL: https://localhost:5001
  Output:   ./tmp/openapi/http
  OutputType: OneFilePerTag
  Flags used: -i (spec) -b (base-url) -o (output) -t (output-type) -f (force overwrite)
'@ | Write-Host
}

switch ($Command.ToLower()) {
  'lint'               { Lint-OpenApi }
  'client-dotnet'      { Generate-KiotaClient -Language CSharp }
  'client-typescript'  { Generate-KiotaClient -Language TypeScript }
  'http-requests'      { Generate-HttpRequests }
  'help'               { Help }
  default              { Write-Host "Unknown command '$Command'" -ForegroundColor Red; Help; exit 1 }
}

exit 0