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
  [Parameter(Position = 0)] [string] $Command = 'help',
  [Parameter()] [string] $SpecificationPath = 'src/Presentation.Web.Server/wwwroot/openapi.json',
  [Parameter()] [string] $RulesetPath = '.spectral.yaml',
  [Parameter()] [string] $FailSeverity = 'error',
  [Parameter()] [string] $Format = 'stylish',
  [Parameter()] [string] $DockerImage = 'stoplight/spectral:latest',
  [Parameter()] [string] $HttpBaseUrl = 'https://localhost:5001',
  [Parameter()] [string] $HttpOutputType = 'OneFilePerTag'
)

# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

# Load configuration
$commonScriptsPath = Join-Path $PSScriptRoot "tasks-common.ps1"
if (Test-Path $commonScriptsPath) { . $commonScriptsPath }
Load-Settings

$OutputDirectory = Join-Path $Root (Get-OutputDirectory) 'openapi'

function Lint-OpenApi() {
  # Write-Section 'Spectral OpenAPI Lint'
  $specPath = Resolve-Path
  $rulesPath = Join-Path $Root $RulesetPath

  # Validate severity & format
  $validSeverities = 'error', 'warn', 'info', 'hint', 'off'
  if ($validSeverities -notcontains $FailSeverity.ToLower()) { Fail "Invalid -FailSeverity '$FailSeverity'. Valid: $($validSeverities -join ', ')" 3 }
  $validFormats = 'stylish', 'json', 'text'
  if ($validFormats -notcontains $Format.ToLower()) { Fail "Invalid -Format '$Format'. Valid: $($validFormats -join ', ')" 4 }

  Ensure-DockerImage -Image $DockerImage

  Write-Step "Spec path: $specPath"
  Write-Step "Fail severity: $FailSeverity"
  Write-Step "Output format: $Format"

  # Construct docker run arguments. Mount repo root to /work for access to spec & ruleset
  # Use $() to delimit variable before colon to avoid parser treating ':' as part of variable name
  $dockerArgs = @('run', '--rm', '-v', "$($Root):/work", $DockerImage, 'lint', "/work/$SpecificationPath", '--format', $Format, '--fail-severity', $FailSeverity)
  if ($rulesPath) { $dockerArgs += @('-r', "/work/$RulesetPath") }

  Write-Debug "docker $($dockerArgs -join ' ')"
  docker @dockerArgs
  if ($LASTEXITCODE -ne 0) { Fail "Spectral lint failed (exit code $LASTEXITCODE)." $LASTEXITCODE }
  Write-Info 'OpenAPI lint succeeded with no violations above threshold.'
}

function Generate-KiotaClient {
  param(
    [Parameter(Mandatory)][ValidateSet('CSharp', 'TypeScript')] [string]$Language,
    [string]$ClientClassName = 'ApiClient',
    [string]$Namespace = 'OpenApi.Client'
  )
  # Write-Section "Kiota Generate ($Language)"
  Ensure-DotNetTools
  $specPath = Resolve-Path

  # Output directory selection
  $outDir = if ($Language -eq 'CSharp') { Join-Path $OutputDirectory 'dotnet' }
  else { Join-Path $OutputDirectory 'typescript' }
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  # Build Kiota args
  if ($Language -eq 'CSharp') {
    $kiotaArgs = @('kiota', 'generate',
      '-d', $specPath,
      '-l', 'CSharp',
      '-o', $outDir,
      '--log-level', 'Error',
      '-c', $ClientClassName,
      '-n', $Namespace)
  }
  else {
    # typescript
    $kiotaArgs = @('kiota', 'generate',
      '-d', $specPath,
      '-l', 'TypeScript',
      '-o', $outDir,
      '--log-level', 'Error',
      '-c', $ClientClassName)
  }

  # Import-Module PwshSpectreConsole -ErrorAction Stop
  # @( "Kiota Generation",
  #   "Spec: $specPath",
  #   "Language: $Language",
  #   "Output: $outDir",
  #   "ClientClass: $ClientClassName",
  #   "Namespace: $Namespace"
  # ) | Format-SpectreRows | Format-SpectrePanel -Expand -Color "DeepSkyBlue3"

  Clean-Path $outDir
  Write-Debug "dotnet $($kiotaArgs -join ' ')"
  & dotnet @kiotaArgs
  if ($LASTEXITCODE -ne 0) { Fail "Kiota generation failed ($LASTEXITCODE)" $LASTEXITCODE }

  # Post-generation brief summary
  $fileCount = (Get-ChildItem -Path $outDir -Recurse -File | Measure-Object).Count
  Write-Info "Kiota $Language client generated ($fileCount files)."
}

function Generate-HttpRequests {
  # Write-Section 'Generating .http request files'
  Ensure-DotNetTools
  $specPath = Resolve-Path
  $outDir = Join-Path $OutputDirectory 'http'
  New-Item -ItemType Directory -Force -Path $outDir | Out-Null

  Write-Step "Spec: $specPath"
  Write-Step "Output: $outDir"
  Write-Step "BaseUrl: $HttpBaseUrl"
  Write-Step "OutputType: $HttpOutputType"
  # httpgenerator usage: dotnet httpgenerator generate -i <spec> -b <base-url> -o <out> -t <type> -f
  $genArgs = @('httpgenerator', $specPath,
    '--base-url', $HttpBaseUrl,
    '--output', $outDir,
    '--authorization-header', 'Bearer TOKEN',
    '--output-type', $HttpOutputType)

  Clean-Path $outDir
  Write-Debug "dotnet $($genArgs -join ' ')"
  & dotnet @genArgs
  if ($LASTEXITCODE -ne 0) { Fail "httpgenerator failed ($LASTEXITCODE)" $LASTEXITCODE }
  $httpFiles = Get-ChildItem -Path $outDir -Filter '*.http' -File -Recurse
  $count = ($httpFiles | Measure-Object).Count
  Write-Info "Generated $count .http file(s) in $outDir"
}

function Resolve-Path() {
  $path = Join-Path $Root $SpecificationPath
  if (-not (Test-Path -LiteralPath $path)) {
    Fail "Path not found: $SpecificationPath" 21
  }
  return $path
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
  'lint' { Lint-OpenApi }
  'client-dotnet' { Generate-KiotaClient -Language CSharp }
  'client-typescript' { Generate-KiotaClient -Language TypeScript }
  'http-requests' { Generate-HttpRequests }
  'help' { Help }
  default { Write-Host "Unknown command '$Command'" -ForegroundColor Red; Help; exit 1 }
}

exit 0