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
  [Parameter()] [string] $FailSeverity = 'error', # one of: error|warn|info|hint|off
  [Parameter()] [string] $Format = 'stylish',     # stylish|json|text
  [Parameter()] [string] $DockerImage = 'stoplight/spectral:latest'
)

Write-Host "Executing OpenAPI command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'

function Fail([string] $Message, [int] $Code = 1) { Write-Error $Message; exit $Code }
function Write-Section([string] $Text) { Write-Host "`n=== $Text ===" -ForegroundColor DarkCyan }
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
  Write-Section 'Spectral OpenAPI Lint'

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

function Help() {
@'
Usage: pwsh -File .vscode/tasks-openapi.ps1 <command> [options]

Commands:
  lint    Run Spectral lint against the OpenAPI specification
  help    Show this help text

Parameters:
  -SpecificationPath <path>  Path to OpenAPI spec (default: src/Presentation.Web.Server/wwwroot/openapi.json)
  -RulesetPath <path>        Optional Spectral ruleset (.spectral.yaml) (default: .spectral.yaml if exists)
  -FailSeverity <severity>   error|warn|info|hint|off (default: error). Minimum level that causes non-zero exit.
  -Format <format>           stylish|json|text (default: stylish)
  -DockerImage <image>       Docker image to use (default: stoplight/spectral:latest)

Examples:
  pwsh -File .vscode/tasks-openapi.ps1 lint
  pwsh -File .vscode/tasks-openapi.ps1 lint -FailSeverity warn -Format json
  pwsh -File .vscode/tasks-openapi.ps1 lint -RulesetPath rules/.spectral.yaml

Notes:
  The specification is generated during build. Run the build task first if the spec is missing.
  Customize rules by adding a .spectral.yaml in repository root or provide -RulesetPath.
  Non-zero exit code indicates lint violations at or above FailSeverity or operational failure.
  Add future commands (e.g., bundle, diff) by creating new functions and switch entries.
'@ | Write-Host
}

switch ($Command.ToLower()) {
  'lint' { Lint-OpenApi }
  'help' { Help }
  default { Write-Host "Unknown command '$Command'" -ForegroundColor Red; Help; exit 1 }
}

exit 0