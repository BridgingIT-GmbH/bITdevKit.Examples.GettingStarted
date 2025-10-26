param(
  [Parameter(Mandatory=$true)][string]$Command,
  [string]$SolutionPath = (Join-Path $PSScriptRoot '..' 'BridgingIT.DevKit.Examples.GettingStarted.sln')
)
$ErrorActionPreference='Stop'

function Ensure-LicenseTool {
  Write-Host 'Restoring local dotnet tools (manifest)...' -ForegroundColor DarkCyan
  & dotnet tool restore | Out-Null
  $toolList = & dotnet tool list --local 2>&1
  if($LASTEXITCODE -ne 0){ throw 'Local tool manifest not found or restore failed.' }
  if(-not ($toolList -match 'nuget-license')){
    throw 'nuget-license missing from local manifest. Add with: dotnet tool install nuget-license --local'
  }
}

switch($Command.ToLowerInvariant()){
  'licenses' { # https://github.com/sensslen/nuget-license
    Ensure-LicenseTool
    $outDir = Join-Path (Join-Path $PSScriptRoot '..') '.tmp/compliance'
    if(-not (Test-Path $outDir)){ New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $mdFile = Join-Path $outDir "licenses_$timestamp.md"
    $jsonFile = Join-Path $outDir "licenses_$timestamp.json"
    Write-Host "Generating license report -> $mdFile" -ForegroundColor Cyan
    # Prefer nuget-license tool; generates SPDX-focused license information
    $nlOutput = & dotnet tool run nuget-license -i $SolutionPath -t -o JsonPretty 2>&1
    $parseSource = ($nlOutput -join "`n")
    if(-not ($parseSource.TrimStart() -match '^[\[{]')){ throw "nuget-license did not return JSON output. Raw: $parseSource" }
    try { $data = $parseSource | ConvertFrom-Json } catch { throw 'Failed to parse nuget-license JSON output.' }
    if(-not ($data -is [System.Collections.IEnumerable])){ throw 'nuget-license JSON unexpected shape (expected array).' }
    $rows = @('| Package | Version | License | LicenseUrl |','|---------|---------|---------|-----------|')
    $licenseStats = @{}
    $jsonList = @()
    foreach($pkg in $data){
      $name = $pkg.PackageId
      $ver = $pkg.PackageVersion
      $licRaw = $pkg.License
      $licUrl = $pkg.LicenseUrl
      if(-not $licRaw){ $licRaw = '(unknown)' }
      if(-not $licUrl){ $licUrl = '(none)' }
      # If license text is embedded (large multi-line EULA), classify
      $lic = if($licRaw.Length -gt 120 -or $licRaw -match "\n") { '(Embedded License Text)' } else { $licRaw }
      $rows += "| $name | $ver | $lic | $licUrl |"
      if($licenseStats.ContainsKey($lic)){ $licenseStats[$lic]++ } else { $licenseStats[$lic] = 1 }
      $jsonList += [pscustomobject]@{ package=$name; version=$ver; license=$lic; licenseUrl=$licUrl }
    }
    # Add summary section
    $total = $jsonList.Count
    $unknownCount = ($jsonList | Where-Object { $_.license -eq '(unknown)' }).Count
    $summaryLines = @("","## License Summary","Total packages: $total","Unknown licenses: $unknownCount","Top licenses:")
    foreach($key in ($licenseStats.Keys | Sort-Object)){
      $count = $licenseStats[$key]
      $summaryLines += "  - ${key}: ${count}"
    }
    ($rows + $summaryLines) -join "`n" | Set-Content -Path $mdFile -Encoding UTF8
    $jsonObj = [pscustomobject]@{ generated = (Get-Date).ToString('o'); total = $total; unknown = $unknownCount; licenses = $licenseStats; packages=$jsonList }
    $jsonObj | ConvertTo-Json -Depth 6 | Set-Content -Path $jsonFile -Encoding UTF8
    Write-Host 'License reports created with nuget-license:' -ForegroundColor Green
    Write-Host "  MD:   $mdFile" -ForegroundColor Green
    Write-Host "  JSON: $jsonFile" -ForegroundColor Green
  }
  default { throw "Unknown compliance command: $Command" }
}
