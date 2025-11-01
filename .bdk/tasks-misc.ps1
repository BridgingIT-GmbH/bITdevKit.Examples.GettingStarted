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
  # Optional process id for non-interactive kill-dotnet
  [int] $ProcessId,
  [switch] $ForceKill,

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
# Write-Host "Executing command: $Command" -ForegroundColor Yellow
$ErrorActionPreference = 'Stop'

# function Write-Section([string] $Text){ Write-Host "`n=== $Text ===" -ForegroundColor Magenta }
function Fail([string] $Msg, [int] $Code=1){ Write-Error $Msg; exit $Code }

# Reused from diagnostics script (with minor fallback enhancements):
${script:DotNetOnly} = $false
function Select-Pid($title){
  try {
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
  } catch {
    # Fallback to manual numeric selection if Spectre unavailable
    Write-Host ('Spectre selection unavailable; falling back to manual input. Reason: {0}' -f $_.Exception.Message) -ForegroundColor Yellow
    $procs = Get-Process | Where-Object { $_.Id -gt 0 }
    if($script:DotNetOnly){ $procs = $procs | Where-Object { $_.ProcessName -match 'dotnet|Presentation.Web.Server' } }
    $procs = $procs | Sort-Object ProcessName,Id
    if(-not $procs){ Write-Host 'No matching processes found.' -ForegroundColor Yellow; return $null }
    Write-Host 'Index | ProcessName | PID' -ForegroundColor Cyan
    for($i=0;$i -lt $procs.Count;$i++){ Write-Host ("{0,5} | {1,-25} | {2}" -f $i,$procs[$i].ProcessName,$procs[$i].Id) -ForegroundColor DarkGray }
    $raw = Read-Host 'Enter index to select (or blank to cancel)'
    if([string]::IsNullOrWhiteSpace($raw)){ return $null }
    if(-not ([int]::TryParse($raw,[ref]$idx)) -or $idx -lt 0 -or $idx -ge $procs.Count){ Write-Host 'Invalid index.' -ForegroundColor Red; return $null }
    return $procs[$idx].Id
  }
}

function Run-CSharpRepl() {
  # Write-Section 'Starting C# REPL'
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
  # Write-Section 'Combining sources'

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
  function Write-Progress-Info    { param([string]$message) Write-Host "-- $message" -ForegroundColor Cyan }
  function Write-Progress-Warning { param([string]$message) Write-Host "-- $message" -ForegroundColor Yellow }
  function Write-Progress-Success { param([string]$message) Write-Host "$message" -ForegroundColor Green }
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

function Open-BrowserUrl() {
  param(
    [string]$title,
    [string]$url
  )
  # Write-Section $title
  try {
    Write-Host "Launching browser: $url" -ForegroundColor Cyan
    Start-Process $url
    Write-Host "$title opened." -ForegroundColor Green
  } catch {
    Write-Host "Failed to open $title ($url): $($_.Exception.Message)" -ForegroundColor Yellow
  }
}

function Show-MinVer() {
  # Write-Section 'MinVer Semantic Version'
  dotnet tool restore | Out-Null
  if($LASTEXITCODE -ne 0){ Fail 'dotnet tool restore failed.' 200 }
  # Write-Host 'Running: dotnet minver -v d -p preview.0' -ForegroundColor Cyan
  dotnet minver -v d -p preview.0
  $exit = $LASTEXITCODE
  if($exit -ne 0){ Fail "MinVer failed (exit $exit)" 201 }
}

function Help() {
@'
Usage: pwsh -File .vscode/tasks-misc.ps1 <command> [options]

Commands:
  digest                               Generate consolidated markdown documentation per project (.g.md).
  clean|cleanup                        Remove build/output artifact directories (bin/obj/node_modules/etc.).
  remove-headers                       Remove License headers from all C# files in src/ and tests/.
  repl|shell                           Run C# REPL (dotnet tool csharprepl) after tool restore.
  kill-dotnet                          Terminate a dotnet process (interactive selection or direct -ProcessId). No confirmation.
  browser-seq                          Open SEQ logging dashboard (http://localhost:15349) in default browser.
  browser-server-docker                Open Server (Docker container) http://localhost:8080 in default browser.
  show-minver                          Display semantic version computed by MinVer (pre-release tag: preview.0).
  docs-update                          Download latest DevKit docs (markdown) from upstream repository into ./devkit/docs.
  help|?                               Show this help.

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

remove-headers Operation:
  Scans all C# files in src/ and tests/ directories.
  Removes the 4-line MIT-License header comment block from the beginning of each file.
  Preserves all code and other comments.

Examples:
  pwsh -File .vscode/tasks-misc.ps1 digest
  pwsh -File .vscode/tasks-misc.ps1 digest -OutputDirectory ./.tmp/docs -StripComments true -StripEmptyLines true
  pwsh -File .vscode/tasks-misc.ps1 clean
  pwsh -File .vscode/tasks-misc.ps1 remove-headers
  pwsh -File .vscode/tasks-misc.ps1 repl
  pwsh -File .vscode/tasks-misc.ps1 kill-dotnet               # interactive selection (no confirmation)
  pwsh -File .vscode/tasks-misc.ps1 kill-dotnet -ProcessId 1234  # direct kill (no confirmation)

New Commands:
  show-minver|minver|version|show-version  Display semantic version computed by MinVer (pre-release tag: preview.0)

Exit Codes:
  0 success, non-zero on failure.

Notes:
  Output *.g.md files are appended with project name and placed in OutputDirectory.
  .gitignore updated with the OutputDirectory (top-level) when UpdateGitIgnore = true.
  Adjust patterns or extend commands by adding new functions and routing entries.
  REPL launches interactive csharprepl; VS Code task must not run in background to allow input.
'@ | Write-Host
}

function Clean-Workspace() {
  # Write-Section 'Cleaning workspace build artifacts'
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

function Handle-MiscCommand([string]$cmd){
  $key = ($cmd ?? '').ToLowerInvariant()
  switch ($key) {
    'digest' { Combine-Sources; return }
    'clean' { Clean-Workspace; return }
    'cleanup' { Clean-Workspace; return }
    'remove-headers' { Remove-FileHeaders; return }
    'repl' { Run-CSharpRepl; return }
    'shell' { Run-CSharpRepl; return }
    'kill-dotnet' { Kill-DotNetProcess; return }
    'browser-devkit-docs' { Open-BrowserUrl 'Opening DevKit Docs' 'https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs'; return }
    'browser-seq' { Open-BrowserUrl 'Opening Seq Dashboard' 'http://localhost:15349'; return }
    'browser-adminneo' { Open-BrowserUrl 'Opening AdminNeo Dashboard' 'http://localhost:18089'; return }
    'browser-server-kestrel' { Open-BrowserUrl 'Opening Server (Kestrel HTTPS)' 'https://localhost:5001/scalar'; return }
    'browser-server-docker' { Open-BrowserUrl 'Opening Server (Docker HTTP)' 'http://localhost:8080/scalar'; return }
    'show-minver' { Show-MinVer; return }
    'docs-update' { Update-DevKitDocs; return }
    'help' { Help; return }
    '?' { Help; return }
    default { Write-Host "Unknown misc command '$cmd'" -ForegroundColor Red; Help; exit 10 }
  }
}

function Kill-DotNetProcess() {
  # Write-Section 'Kill .NET Process'
  # Non-interactive direct path if provided
  # If ProcessId provided non-interactively, skip selection & confirmation when -ForceKill used.
  $selectedPid = $null
  if($ProcessId -gt 0){
    Write-Host ("Non-interactive target PID specified: {0}" -f $ProcessId) -ForegroundColor Cyan
    $selectedPid = $ProcessId
  }
  if(-not $selectedPid){
    # Use shared Select-Pid logic scoped to dotnet processes
    $script:DotNetOnly = $true
    $selectedPid = Select-Pid 'Select .NET process to KILL'
    if(-not $selectedPid){ Write-Host 'Kill operation cancelled or no process selected.' -ForegroundColor Yellow; $global:LASTEXITCODE=0; return }
  }
  $proc = Get-Process -Id $selectedPid -ErrorAction SilentlyContinue
  if(-not $proc){ Write-Host ("Process with PID {0} no longer exists." -f $selectedPid) -ForegroundColor Yellow; $global:LASTEXITCODE=0; return }
  Write-Host ("Target: {0} (PID {1})" -f $proc.ProcessName,$selectedPid) -ForegroundColor Cyan
  # Confirmation omitted per user request; proceed immediately.
  try {
  Stop-Process -Id $selectedPid -Force -ErrorAction Stop
  Write-Host ("Process {0} terminated." -f $selectedPid) -ForegroundColor Green
    $global:LASTEXITCODE=0
  } catch {
    $errMsg = $_.Exception.Message
  Write-Host ("Failed to terminate PID {0}: {1}" -f $selectedPid, $errMsg) -ForegroundColor Red
    $global:LASTEXITCODE=5
  }
}

function Remove-FileHeaders() {
  # Remove MIT license headers from all C# files in src/ and tests/
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $srcPath = Join-Path $repoRoot 'src'
  $testsPath = Join-Path $repoRoot 'tests'

  Write-Host "Removing file headers from C# files..." -ForegroundColor Cyan

  $filesToProcess = @()
  if (Test-Path $srcPath) {
    $filesToProcess += @(Get-ChildItem -Path $srcPath -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue)
  }
  if (Test-Path $testsPath) {
    $filesToProcess += @(Get-ChildItem -Path $testsPath -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue)
  }

  if ($filesToProcess.Count -eq 0) {
    Write-Host "No C# files found in src/ or tests/" -ForegroundColor Yellow
    return
  }

  Write-Host "Found $($filesToProcess.Count) C# files" -ForegroundColor Cyan

  $headerPattern = '^\s*//\s*MIT-License\s*$|^\s*//\s*Copyright\s+BridgingIT|^\s*//\s*Use\s+of\s+this\s+source\s+code|^\s*//\s*found\s+in\s+the\s+LICENSE'
  $filesModified = 0
  $i = 0

  foreach ($file in $filesToProcess) {
    $i++
    Write-Progress -Activity 'Removing headers' -Status "Processing $i of $($filesToProcess.Count)" -PercentComplete (($i / $filesToProcess.Count) * 100)

    try {
      $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
      $originalLength = $content.Length

      # Split into lines to identify header
      $lines = $content -split "`n"
      $headerEndIndex = 0

      # Look for the license header pattern (typically 4 lines + blank line)
      if ($lines.Count -gt 5) {
        # Check if first 4 lines match the header pattern
        $hasHeader = $false
        if ($lines[0] -match 'MIT-License' -and $lines[1] -match 'Copyright\s+BridgingIT') {
          $hasHeader = $true
          $headerEndIndex = 4
          # Skip one more blank line if present
          if ($headerEndIndex + 1 -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$headerEndIndex + 1])) {
            $headerEndIndex += 1
          }
        }

        if ($hasHeader -and $headerEndIndex -gt 0) {
          # Remove header lines and rejoin
          $newLines = $lines[($headerEndIndex + 1)..$($lines.Count - 1)]
          $newContent = ($newLines -join "`n").TrimStart()

          if ($newContent.Length -lt $originalLength) {
            Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8 -Force
            $filesModified++
            Write-Host "  Removed header: $($file.FullName.Replace($repoRoot, '.'))" -ForegroundColor Green
          }
        }
      }
    }
    catch {
      Write-Host "  Error processing $($file.FullName): $($_.Exception.Message)" -ForegroundColor Red
    }
  }

  Write-Progress -Activity 'Removing headers' -Completed
  Write-Host "`nFile header removal complete. Modified: $filesModified files" -ForegroundColor Green
}

function Update-DevKitDocs() {
  # Write-Section 'Updating DevKit Docs (markdown)'
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $targetRoot = Join-Path $repoRoot '.bdk/docs'
  New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

  $apiBase = 'https://api.github.com/repos/BridgingIT-GmbH/bITdevKit/contents/docs'
  $branchRef = 'main'
  $headers = @{ 'User-Agent' = 'bITdevKit-DocsSyncScript'; 'Accept' = 'application/vnd.github.v3+json' }

  function Get-DirectoryItems([string]$apiUrl){
    try { return Invoke-RestMethod -Uri ($apiUrl + '?ref=' + $branchRef) -Headers $headers -ErrorAction Stop }
    catch { Write-Host ("Failed to list '{0}': {1}" -f $apiUrl,$_.Exception.Message) -ForegroundColor Yellow; return @() }
  }

  $downloaded = [System.Collections.Generic.List[string]]::new()
  $failed = [System.Collections.Generic.List[string]]::new()

  function Sync-Directory([string]$apiUrl){
    $items = Get-DirectoryItems $apiUrl
    foreach($item in $items){
      # if($item.type -eq 'dir'){
      #   Sync-Directory $item.url
      # }
      if($item.type -eq 'file' -and $item.name -like '*.md'){
        $relative = ($item.path -replace '^docs/','')
        $localPath = Join-Path $targetRoot $relative
        $localDir = Split-Path $localPath -Parent
        New-Item -ItemType Directory -Force -Path $localDir | Out-Null
        try {
          $content = Invoke-RestMethod -Uri $item.download_url -Headers $headers -ErrorAction Stop
          # If content returns as an object (rare), cast to string
          if($content -isnot [string]){ $content = [string]$content }
          Set-Content -Path $localPath -Value $content -Encoding UTF8
          $downloaded.Add($relative) | Out-Null
        } catch {
          Write-Host ("Failed to download {0}: {1}" -f $relative, $_.Exception.Message) -ForegroundColor Yellow
          $failed.Add($relative) | Out-Null
        }
      }
    }
  }

  $rootApi = $apiBase
  Invoke-SpectreCommandWithStatus -Title 'Downloading latest DevKit docs...' -ScriptBlock {
    Sync-Directory $rootApi
  } -Spinner dots

  Write-Host ("Downloaded {0} markdown files to {1}" -f $downloaded.Count, $targetRoot) -ForegroundColor Green
  if($failed.Count -gt 0){ Write-Host ("Failed: {0}" -f ($failed -join ', ')) -ForegroundColor Yellow }
}

Handle-MiscCommand $Command