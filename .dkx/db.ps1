# .dkx/lib/db.ps1
# Cross-platform PS7+ ADO.NET helpers with on-the-fly NuGet loading

using namespace System.IO.Compression

# ------------- Utility: simple log -------------
function Write-Dbg([string] $msg) {
  if ($env:DKX_DB_DEBUG) { Write-Host "[db] $msg" -ForegroundColor DarkGray }
}

# ------------- NuGet loader -------------
function Get-PackageAssetPath {
  param(
    [Parameter(Mandatory)] [string] $PackageId,
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $PreferAssembly,
    [string[]] $FallbackAssemblies = @(),
    [string] $RuntimeHint
  )

  $root = Join-Path $PSScriptRoot "nuget/$PackageId/$Version"
  $nupkg = Join-Path $root "$PackageId.$Version.nupkg"
  if (-not (Test-Path $root)) { New-Item -ItemType Directory -Path $root -Force | Out-Null }

  if (-not (Test-Path $nupkg)) {
    $url = "https://www.nuget.org/api/v2/package/$PackageId/$Version"
    Write-Dbg "Downloading $PackageId $Version"
    Invoke-WebRequest -Uri $url -OutFile $nupkg
  }

  $extract = Join-Path $root "extracted"
  if (-not (Test-Path $extract)) {
    [ZipFile]::ExtractToDirectory($nupkg, $extract)
  }

  # Choose best TFM for current pwsh runtime; default to .NET 8 then 6 then netstandard
  if (-not $RuntimeHint) {
    $RuntimeHint = if ($PSVersionTable.PSVersion.Major -ge 7 -and $PSVersionTable.Patch -ge 0) { 'net8.0' } else { 'net6.0' }
    # Prefer detecting .NET target by environment:
    if ($PSVersionTable.BuildVersion -and $PSVersionTable.GitCommitId) { } # placeholder
  }

  $tfmOrder = @($RuntimeHint, 'net8.0','net6.0','net7.0','netstandard2.1','netstandard2.0')

  foreach ($tfm in $tfmOrder | Select-Object -Unique) {
    $lib = Join-Path $extract "lib/$tfm"
    if (-not (Test-Path $lib)) { continue }
    $cand = Get-ChildItem $lib -Filter "$PreferAssembly.dll" -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cand) { return $cand.FullName }
    foreach ($alt in $FallbackAssemblies) {
      $cand = Get-ChildItem $lib -Filter "$alt.dll" -File -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($cand) { return $cand.FullName }
    }
  }

  throw "Could not locate $PreferAssembly.dll in $PackageId/$Version for any of: $($tfmOrder -join ', ')"
}

function Import-DbProviderAuto {
  param([Parameter(Mandatory)] [string] $ConnectionString)

  $kind = Get-ConnectionKind -ConnectionString $ConnectionString
  switch ($kind) {
    'sqlserver' {
      if (-not ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'Microsoft.Data.SqlClient' })) {
        $dll = Get-PackageAssetPath -PackageId 'Microsoft.Data.SqlClient' -Version '5.2.2' `
          -PreferAssembly 'Microsoft.Data.SqlClient' -FallbackAssemblies @('System.Data.SqlClient')
        Add-Type -Path $dll -ErrorAction Stop
        Write-Dbg "Loaded Microsoft.Data.SqlClient from $dll"
      }
    }
    'postgres' {
      if (-not ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'Npgsql' })) {
        $dll = Get-PackageAssetPath -PackageId 'Npgsql' -Version '8.0.3' -PreferAssembly 'Npgsql'
        Add-Type -Path $dll -ErrorAction Stop
        Write-Dbg "Loaded Npgsql from $dll"
      }
    }
    'sqlite' {
      if (-not ([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'Microsoft.Data.Sqlite' })) {
        $dll = Get-PackageAssetPath -PackageId 'Microsoft.Data.Sqlite' -Version '8.0.7' -PreferAssembly 'Microsoft.Data.Sqlite'
        Add-Type -Path $dll -ErrorAction Stop
        Write-Dbg "Loaded Microsoft.Data.Sqlite from $dll"
      }
    }
    default { throw "Unsupported or unknown connection string (cannot detect provider)." }
  }
  return $kind
}

# ------------- Provider detection -------------
function Get-ConnectionKind {
  param([Parameter(Mandatory)] [string] $ConnectionString)
  $cs = $ConnectionString.Trim()

  # URL-style first
  if ($cs -match '^(?i)sqlserver://') { return 'sqlserver' }
  if ($cs -match '^(?i)(postgres|postgresql|psql)://') { return 'postgres' }
  if ($cs -match '^(?i)sqlite://') { return 'sqlite' }

  # Key=value style
  $lc = $cs.ToLowerInvariant()
  if ($lc -match '(^|;)\s*(server|data\s*source)\s*=' ) { return 'sqlserver' }
  if ($lc -match '(^|;)\s*(host|username|user\s*id|password|port)\s*=' ) { return 'postgres' }
  if ($lc -match '(^|;)\s*(data\s*source|filename|mode)\s*=' ) { return 'sqlite' }

  throw "Unable to detect provider from connection string."
}

# ------------- Core execution -------------
function Invoke-UniversalQuery {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory)] [string] $ConnectionString,
    [Parameter(Mandatory)] [string] $Query,
    [hashtable] $Parameters
  )

  $kind = Import-DbProviderAuto -ConnectionString $ConnectionString

  switch ($kind) {
    'sqlserver' { $conn = [Microsoft.Data.SqlClient.SqlConnection]::new($ConnectionString) }
    'postgres'  { $conn = [Npgsql.NpgsqlConnection]::new($ConnectionString) }
    'sqlite'    { $conn = [Microsoft.Data.Sqlite.SqliteConnection]::new($ConnectionString) }
  }

  $cmd = $conn.CreateCommand()
  $cmd.CommandText = $Query
  if ($Parameters) {
    foreach ($k in $Parameters.Keys) {
      $v = $Parameters[$k]
      switch ($kind) {
        'sqlserver' { $p = [Microsoft.Data.SqlClient.SqlParameter]::new("@$k", $v) }
        'postgres'  { $p = [Npgsql.NpgsqlParameter]::new("@$k", $v) }
        'sqlite'    { $p = [Microsoft.Data.Sqlite.SqliteParameter]::new("@$k", $v) }
      }
      $null = $cmd.Parameters.Add($p)
    }
  }

  $conn.Open()
  try {
    $rdr = $cmd.ExecuteReader()
    $dt = New-Object System.Data.DataTable
    $dt.Load($rdr)
  } finally {
    $conn.Close()
  }
  return $dt
}

# ------------- Helpers -------------
function Get-DbTables {
  [CmdletBinding()]
  param([Parameter(Mandatory)] [string] $ConnectionString)

  $kind = Get-ConnectionKind -ConnectionString $ConnectionString
  switch ($kind) {
    'sqlserver' {
      $q = @"
SELECT s.name AS schema_name, t.name AS table_name
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
ORDER BY s.name, t.name;
"@
    }
    'postgres' {
      $q = @"
SELECT table_schema AS schema_name, table_name
FROM information_schema.tables
WHERE table_type='BASE TABLE' AND table_schema NOT IN ('pg_catalog','information_schema')
ORDER BY table_schema, table_name;
"@
    }
    'sqlite' {
      $q = @"
SELECT '' AS schema_name, name AS table_name
FROM sqlite_master
WHERE type='table' AND name NOT LIKE 'sqlite_%'
ORDER BY name;
"@
    }
  }

  Invoke-UniversalQuery -ConnectionString $ConnectionString -Query $q
}

function Get-DbInfo {
  [CmdletBinding()]
  param([Parameter(Mandatory)] [string] $ConnectionString)

  $kind = Get-ConnectionKind -ConnectionString $ConnectionString
  switch ($kind) {
    'sqlserver' {
      $q = "SELECT @@VERSION AS version, DB_NAME() AS database_name, SUSER_SNAME() AS login_name;"
    }
    'postgres' {
      $q = "SELECT version(), current_database() AS database_name, current_user AS login_name;"
    }
    'sqlite' {
      $q = "SELECT sqlite_version() AS version;"
    }
  }
  Invoke-UniversalQuery -ConnectionString $ConnectionString -Query $q
}

function Drop-Db {
  [CmdletBinding(SupportsShouldProcess)]
  param([Parameter(Mandatory)] [string] $ConnectionString)

  $kind = Get-ConnectionKind -ConnectionString $ConnectionString
  switch ($kind) {
    'sqlserver' {
      # Expect initial connection string to point to the DB; we connect to master to drop
      # Parse database name
      $db = ($ConnectionString -split ';') |
        Where-Object { $_ -match '^(?i)\s*database(\s*name)?\s*=' } |
        ForEach-Object { ($_ -split '=',2)[1].Trim() } |
        Select-Object -First 1
      if (-not $db) { throw "Database name not found in connection string." }

      # Build master connection
      $csMaster = ($ConnectionString -replace '(?i)database(\s*name)?\s*=\s*[^;]*', 'Database=master')
      $q = @"
IF DB_ID(N'$db') IS NOT NULL BEGIN
  ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  DROP DATABASE [$db];
END
"@
      if ($PSCmdlet.ShouldProcess($db, 'DROP DATABASE')) {
        Invoke-UniversalQuery -ConnectionString $csMaster -Query $q | Out-Null
      }
    }
    'postgres' {
      # Need to connect to postgres db to drop target
      $db = ($ConnectionString -split ';') |
        Where-Object { $_ -match '^(?i)\s*database\s*=' } |
        ForEach-Object { ($_ -split '=',2)[1].Trim() } |
        Select-Object -First 1
      if (-not $db) { throw "Database name not found in connection string." }

      $csAdmin = ($ConnectionString -replace '(?i)database\s*=\s*[^;]*', 'Database=postgres')
      $q = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db; DROP DATABASE IF EXISTS ""$db"";"
      Invoke-UniversalQuery -ConnectionString $csAdmin -Query $q -Parameters @{ db = $db } | Out-Null
    }
    'sqlite' {
      # For SQLite: deleting the file is effectively dropping the DB
      $file = ($ConnectionString -split ';') |
        Where-Object { $_ -match '^(?i)\s*(data\s*source|filename)\s*=' } |
        ForEach-Object { ($_ -split '=',2)[1].Trim() } |
        Select-Object -First 1
      if (-not $file) { throw "SQLite file path (Data Source=) not found." }
      if ($PSCmdlet.ShouldProcess($file, 'Remove SQLite database file')) {
        if (Test-Path $file) { Remove-Item -LiteralPath $file -Force }
      }
    }
  }
}