<#!
.SYNOPSIS
  Miscellaneous development automation tasks.

.DESCRIPTION
  Provides wrapper functions for scripts like CombineSources.ps1 so they can be invoked via VS Code tasks.

.EXAMPLES
  pwsh -File .vscode/tasks-misc.ps1 combine-sources -OutputDirectory ./.tmp/combined
  pwsh -File .vscode/tasks-misc.ps1 help
#>
param(
  # Command routing (added wrapper around original script logic)
  [Parameter(Position=0)] [string] $Command = 'help',

  # === Original CombineSources.ps1 parameters (kept defaults) ===
  [string]$OutputDirectory = './.tmp',
  [string[]]$ExcludePatterns = @(
    'docs?',
    'obj',
    'bin',
    'debug',
    'release',
    'packages',
    'node_modules',
    'filesystem',
    'logs'
  ),
  [bool]$StripHeaderComments = $true,
  [bool]$StripRegions = $true,
  [bool]$StripComments = $false,
  [bool]$StripEmptyLines = $false,
  [bool]$StripUsings = $true,
  [bool]$CombineUsings = $true,
  [bool]$StripAttributes = $false,
  [bool]$SkipGeneratedFiles = $true,
  [bool]$UpdateGitIgnore = $true
)
Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'

function Write-Section([string] $Text){ Write-Host "`n=== $Text ===" -ForegroundColor Magenta }
function Fail([string] $Msg, [int] $Code=1){ Write-Error $Msg; exit $Code }

function Run-CSharpRepl() {
  Write-Section 'Starting C# REPL'
  Write-Host 'Restoring dotnet tools...' -ForegroundColor Cyan
  dotnet tool restore | Out-Null
  if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool restore failed.' 91 }
  # Use dotnet tool run to ensure proper shim invocation even if PATH not updated
  $toolListRaw = dotnet tool list --local 2>$null
  $toolList = ($toolListRaw | Out-String)
  if ($LASTEXITCODE -ne 0) { Fail 'dotnet tool list failed.' 92 }
  if ($toolList -notmatch 'csharprepl') {
    Write-Host 'WARNING: csharprepl not detected in tool list output, attempting direct run anyway...' -ForegroundColor Yellow
  }
  Write-Host 'Launching csharprepl (Ctrl+C to exit) ...' -ForegroundColor Cyan
  dotnet tool run csharprepl
}

function Combine-Sources() {
  Write-Section 'Combining sources'

  # NOTE: Because this script now in .vscode, we map gitignore path to repository root (parent of PSScriptRoot)
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

  # Statistics tracking
  $stats = @{
    ProjectsProcessed  = 0
    ProjectsSkipped    = 0
    TotalSourceFiles   = 0
    TotalMarkdownFiles = 0
    OriginalSize       = 0
    FinalSize          = 0
    TotalUsings        = 0
    FilesSkipped       = 0
    StartTime          = Get-Date
  }

  # Logging helpers (original)
  function Write-Progress-Info    { param([string]$message) Write-Host "[INFO] $message" -ForegroundColor Cyan }
  function Write-Progress-Warning { param([string]$message) Write-Host "[WARN] $message" -ForegroundColor Yellow }
  function Write-Progress-Success { param([string]$message) Write-Host "[SUCCESS] $message" -ForegroundColor Green }
  function Write-ProgressBar      { param([int]$Current,[int]$Total,[string]$Activity,[string]$Status); $percentComplete = [math]::Min(100, ($Current / $Total * 100)); Write-Progress -Activity $Activity -Status $Status -PercentComplete $percentComplete }

  # Using statement handling
  function Get-UsingStatements   { param([string]$content); [regex]::Matches($content,'(?m)^using\s+([^;]+);') | ForEach-Object { $_.Groups[1].Value.Trim() } }
  function Remove-UsingStatements{ param([string]$content); $content -replace '(?m)^using\s+[^;]+;\r?\n?', '' }
  function Format-UsingStatements{ param([string[]]$usings)
    $uniqueUsings = $usings | Select-Object -Unique | Sort-Object
    $systemUsings = $uniqueUsings | Where-Object { $_ -like 'System*' } | Sort-Object
    $microsoftUsings = $uniqueUsings | Where-Object { $_ -like 'Microsoft*' } | Sort-Object
    $otherUsings = $uniqueUsings | Where-Object { -not ($_ -like 'System*' -or $_ -like 'Microsoft*') } | Sort-Object
    $result = ''
    if($systemUsings){ $result += ($systemUsings | ForEach-Object { "using $_;" }) -join "`n"; $result += "`n`n" }
    if($microsoftUsings){ $result += ($microsoftUsings | ForEach-Object { "using $_;" }) -join "`n"; $result += "`n`n" }
    if($otherUsings){ $result += ($otherUsings | ForEach-Object { "using $_;" }) -join "`n" }
    $result.Trim()
  }

  # Code cleanup
  function Remove-CodeElements {
    param(
      [string]$content,
      [bool]$stripRegions,
      [bool]$stripComments,
      [bool]$stripEmptyLines,
      [bool]$stripAttributes,
      [bool]$stripHeaderComments
    )
    if($stripHeaderComments){
      $content = $content -replace '(?sm)^/\*.*?\*/',''
      $content = $content -replace '(?m)^//.*MIT.*$\n?',''
      $content = $content -replace '(?m)^//.*[Ll]icense.*$\n?',''
      $content = $content -replace '(?m)^//.*[Cc]opyright.*$\n?',''
      $content = $content -replace '(?m)^//.*[Aa]ll [Rr]ights.*$\n?',''
      $content = $content -replace '(?m)^//.*[Gg]overned by.*$\n?',''
      $content = $content -replace '(?m)^//.*[Uu]se of this source.*$\n?',''
      $content = $content -replace '^\s*\n',''
    }
    if($stripRegions){ $content = $content -replace '(?m)^\s*#region.*$\n?',''; $content = $content -replace '(?m)^\s*#endregion.*$\n?','' }
    if($stripComments){ $content = $content -replace '(?m)^\s*//.*$\n?',''; $content = $content -replace '(?s)/\*.*?\*/','' }
    if($stripEmptyLines){ $content = $content -replace '(?m)^\s*$\n','' }
    if($stripAttributes){ $content = $content -replace '(?m)^\s*\[.*?\]\r?\n','' }
    $content.Trim()
  }

  function Should-ProcessFile {
    param([System.IO.FileInfo]$file,[bool]$skipGenerated,[string[]]$excludePatterns)
    foreach($pattern in $excludePatterns){ if($file.FullName -match $pattern){ $stats.FilesSkipped++; Write-Progress-Info "Skipping file: $($file.FullName) (matched pattern: $pattern)"; return $false } }
    if($skipGenerated){ $generatedPatterns='\.g\.cs$','\.designer\.cs$','\.generated\.cs$','TemporaryGeneratedFile','\.AssemblyInfo\.cs$'; foreach($gp in $generatedPatterns){ if($file.Name -match $gp){ $stats.FilesSkipped++; Write-Progress-Info "Skipping generated file: $($file.Name)"; return $false } } }
    return $true
  }

  function Get-RelativePath { param([string]$fullPath,[string]$basePath)
    # Use a char array for TrimStart to avoid multi-character string binding error
    $relative = $fullPath.Substring($basePath.Length)
    return $relative.TrimStart(@([char]'\',[char]'/'))
  }

  Write-Progress-Info 'Starting documentation generation...'
  Write-Progress-Info "Output directory: $OutputDirectory"
  $OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
  New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

  # Find all .csproj files (working directory assumed repository root when task runs)
  $projects = Get-ChildItem -Recurse -Filter '*.csproj'
  $projectCount = $projects.Count
  Write-Progress-Info "Found $projectCount projects to process"

  $projects | ForEach-Object -Begin { $currentProject=0 } -Process {
    $currentProject++
    Write-ProgressBar -Current $currentProject -Total $projectCount -Activity 'Processing Projects' -Status "Project $currentProject of $projectCount"
    $projectFile = $_
    $projectDir  = $projectFile.Directory
    $projectName = $projectFile.BaseName
    Write-Progress-Info "`nProcessing project: $projectName"
    $shouldExclude = $false
    foreach($pattern in $ExcludePatterns){ if($projectFile.FullName -match $pattern){ $shouldExclude=$true; Write-Progress-Warning "Skipping $projectName (matched exclude pattern: $pattern)"; $stats.ProjectsSkipped++; break } }
    if($shouldExclude){ return }
    $stats.ProjectsProcessed++
    $markdown = "# $projectName`n`n"
    $allUsings=@(); $processedFiles=@()
    $sourceFiles = @( Get-ChildItem -Path $projectDir -Recurse -Filter '*.cs' | Where-Object { Should-ProcessFile -file $_ -skipGenerated $SkipGeneratedFiles -excludePatterns $ExcludePatterns } )
    $razorFiles  = @( Get-ChildItem -Path $projectDir -Recurse -Filter '*.razor' | Where-Object { Should-ProcessFile -file $_ -skipGenerated $SkipGeneratedFiles -excludePatterns $ExcludePatterns } )
    $totalFiles = $sourceFiles.Count + $razorFiles.Count; $currentFile=0
    Write-Progress-Info "Processing $totalFiles source files..."
    $sourceFiles | ForEach-Object {
      $currentFile++; Write-ProgressBar -Current $currentFile -Total $totalFiles -Activity 'Processing Source Files' -Status "File $currentFile of $totalFiles"
      $content = Get-Content $_.FullName -Raw; $stats.OriginalSize += $content.Length
      if($CombineUsings -or $StripUsings){ $usings = Get-UsingStatements -content $content; $stats.TotalUsings += $usings.Count; if($CombineUsings){ $allUsings += $usings }; $content = Remove-UsingStatements -content $content }
      $content = Remove-CodeElements -content $content -stripRegions $StripRegions -stripComments $StripComments -stripEmptyLines $StripEmptyLines -stripAttributes $StripAttributes -stripHeaderComments $StripHeaderComments
      $processedFiles += @{ Path = Get-RelativePath $_.FullName $projectDir.FullName; Content=$content; Type='cs' }
      $stats.TotalSourceFiles++
    }
    $razorFiles | ForEach-Object {
      $currentFile++; Write-ProgressBar -Current $currentFile -Total $totalFiles -Activity 'Processing Source Files' -Status "File $currentFile of $totalFiles"
      $content = Get-Content $_.FullName -Raw; $stats.OriginalSize += $content.Length
      $content = Remove-CodeElements -content $content -stripRegions $StripRegions -stripComments $StripComments -stripEmptyLines $StripEmptyLines -stripAttributes $StripAttributes -stripHeaderComments $StripHeaderComments
      $processedFiles += @{ Path = Get-RelativePath $_.FullName $projectDir.FullName; Content=$content; Type='razor' }
      $stats.TotalSourceFiles++
    }
    if($CombineUsings -and -not $StripUsings -and $allUsings.Count -gt 0){ $combinedUsings = Format-UsingStatements -usings $allUsings; $markdown += "## Global Usings`n`n"; $markdown += '```csharp'; $markdown += "`n"; $markdown += $combinedUsings; $markdown += "`n"; $markdown += '```' }
    Write-Progress-Info 'Processing markdown files...'
    $generatedMdName = "${projectName}.g.md"
    Get-ChildItem -Path $projectDir -Recurse -Filter '*.md' | Where-Object { $_.Name -ne $generatedMdName -and (Should-ProcessFile -file $_ -skipGenerated $false -excludePatterns $ExcludePatterns) } | ForEach-Object {
      $mdContent = Get-Content $_.FullName -Raw
      $relativePath = Get-RelativePath $_.FullName $projectDir.FullName
      $markdown += "`n`n## Documentation: $relativePath`n`n"
      $markdown += $mdContent; $stats.TotalMarkdownFiles++
    }
    foreach($file in $processedFiles){ $markdown += "`n`n## Source: $($file.Path)`n`n"; $markdown += '```csharp'; $markdown += "`n"; $markdown += $file.Content; $markdown += "`n"; $markdown += '```' }
    $outputFile = Join-Path $OutputDirectory $generatedMdName
    $markdown | Out-File $outputFile -Encoding UTF8
    $stats.FinalSize += (Get-Item $outputFile).Length
    Write-Progress-Success "Generated documentation for $projectName"
  } -End { Write-Progress -Activity 'Processing Projects' -Completed }

  # Update .gitignore (mapped to repo root)
  if($UpdateGitIgnore){
    $gitignorePath = Join-Path $repoRoot '.gitignore'
    $outputDirName = Split-Path $OutputDirectory -Leaf
    $ignoreEntry = "/$outputDirName/"
    if(Test-Path $gitignorePath){ $gitignoreContent = Get-Content $gitignorePath; if($gitignoreContent -notcontains $ignoreEntry){ Add-Content $gitignorePath "`n$ignoreEntry"; Write-Progress-Info "Added /$outputDirName/ to .gitignore" } }
    else { Set-Content $gitignorePath $ignoreEntry; Write-Progress-Info "Created .gitignore with /$outputDirName/" }
  }

  # Final statistics report (original formatting)
  $endTime = Get-Date; $duration = $endTime - $stats.StartTime
  $report = @"
Generation Statistics Report
==========================
Duration: $($duration.ToString('hh\:mm\:ss'))

Projects
--------
Total Projects Found: $projectCount
Projects Processed:  $($stats.ProjectsProcessed)
Projects Skipped:    $($stats.ProjectsSkipped)

Files
-----
Source Files:        $($stats.TotalSourceFiles)
Markdown Files:      $($stats.TotalMarkdownFiles)
Files Skipped:       $($stats.FilesSkipped)
Total Files:         $($stats.TotalSourceFiles + $stats.TotalMarkdownFiles)
Total Using Stmts:   $($stats.TotalUsings)

Size
----
Original Size:       $([math]::Round($stats.OriginalSize / 1KB, 2)) KB
Final Size:          $([math]::Round($stats.FinalSize / 1KB, 2)) KB
Size Reduction:      $( if ($stats.OriginalSize -eq 0) { if ($stats.FinalSize -eq 0) { '0.0' } else { 'N/A' } } else { [math]::Round(100 - ($stats.FinalSize / $stats.OriginalSize * 100), 1) } )%

Output Location:     $OutputDirectory
"@
  Write-Host $report -ForegroundColor Cyan
  Write-Progress-Success 'Documentation generation complete!'
}

function Help() {
@'
Usage: pwsh -File .vscode/tasks-misc.ps1 <command> [options]

Commands:
  combine-sources      Generate consolidated markdown documentation per project (.g.md).
  clean                Remove build/output artifact directories (bin/obj/node_modules/etc.).
  repl                 Run C# REPL (dotnet tool csharprepl) after tool restore.
  help                 Show this help.

combine-sources Parameters (defaults shown):
  -OutputDirectory <path>        (default: ./.tmp)
  -ExcludePatterns <patterns[]>  (default: docs?, obj, bin, debug, release, packages, node_modules, filesystem, logs)
  -StripHeaderComments <bool>    (default: true)
  -StripRegions <bool>           (default: true)
  -StripComments <bool>          (default: false)
  -StripEmptyLines <bool>        (default: false)
  -StripUsings <bool>            (default: true)
  -CombineUsings <bool>          (default: true)
  -StripAttributes <bool>        (default: false)
  -SkipGeneratedFiles <bool>     (default: true)
  -UpdateGitIgnore <bool>        (default: true)

clean Operation:
  Recursively finds directories named in the pattern list:
    bin, obj, bld, Backup, _UpgradeReport_Files, Debug, Release, ipch, node_modules, .tmp
  Removes them deepest-first (single pass) and skips .git.

Examples:
  pwsh -File .vscode/tasks-misc.ps1 combine-sources
  pwsh -File .vscode/tasks-misc.ps1 combine-sources -OutputDirectory ./.tmp/docs -StripComments true -StripEmptyLines true
  pwsh -File .vscode/tasks-misc.ps1 clean
  pwsh -File .vscode/tasks-misc.ps1 repl

Exit Codes:
  0 success, non-zero on failure.

Notes:
  Output *.g.md files are appended with project name and placed in OutputDirectory.
  .gitignore updated with the OutputDirectory (top-level) when UpdateGitIgnore = true.
  Adjust patterns or extend commands by adding new functions and routing entries.
  REPL launches interactive csharprepl; VS Code task must not run in background to allow input.
'@ | Write-Host
}

# Command routing (missing earlier)
function Clean-Workspace() {
  Write-Section 'Cleaning workspace build artifacts'
  $root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  Write-Host "Root: $root" -ForegroundColor Cyan
  $patterns = @('bin','obj','bld','Backup','_UpgradeReport_Files','Debug','Release','ipch','node_modules','.tmp')
  # Gather candidate directories (silently continue on access issues)
  $raw = Get-ChildItem -Path $root -Recurse -Directory -Force -ErrorAction SilentlyContinue |
    Where-Object { $patterns -contains $_.Name -and $_.FullName -notmatch '\\.git\\' }
  if(-not $raw) { Write-Host 'No matching artifact directories found.' -ForegroundColor Yellow; return }
  # Sort by depth descending so deepest folders removed first preventing second pass necessity
  $candidates = $raw | Sort-Object { $_.FullName.Split([char]'\').Length } -Descending
  # De-duplicate paths that may have been collected after parents deleted mid-loop
  $unique = [System.Collections.Generic.HashSet[string]]::new(); $ordered = @()
  foreach($c in $candidates){ if($unique.Add($c.FullName)){ $ordered += $c } }
  Write-Host ("Found {0} artifact directories to remove" -f $ordered.Count) -ForegroundColor Cyan
  $i = 0
  foreach($dir in $ordered) {
    $i++
    Write-Progress -Activity 'Cleaning artifacts' -Status "$i / $($ordered.Count): $($dir.FullName)" -PercentComplete (($i / $ordered.Count) * 100)
    if(-not (Test-Path -LiteralPath $dir.FullName)) { continue }
    try {
      Write-Host "Removing: $($dir.FullName)" -ForegroundColor DarkGray
      Remove-Item -LiteralPath $dir.FullName -Recurse -Force -ErrorAction Stop
    }
    catch {
      Write-Host "Failed to remove: $($dir.FullName) -> $($_.Exception.Message)" -ForegroundColor Yellow
    }
  }
  Write-Progress -Activity 'Cleaning artifacts' -Completed
  Write-Host 'Workspace clean complete.' -ForegroundColor Green
}

switch ($Command.ToLower()) {
  'combine-sources' { Combine-Sources }
  'clean' { Clean-Workspace }
  'repl' { Run-CSharpRepl }
  'help' { Help }
  default { Write-Host "Unknown command '$Command'" -ForegroundColor Red; Help }
}