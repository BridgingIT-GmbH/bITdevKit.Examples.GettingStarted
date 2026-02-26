// BDK CLI - .NET CLI Wrapper Module
/// <summary>
/// Wrapper for .NET CLI commands (build, test, restore, format, etc.)
/// </summary>

using Spectre.Console;

public class DotnetCli
{
  private readonly CommandExecutor _executor;
  private readonly BdkConfig _config;
  private readonly string _solutionFile;

  public string SolutionFile => _solutionFile;

  public DotnetCli(CommandExecutor executor, BdkConfig config, string workingDirectory)
  {
    _executor = executor;
    _config = config;
    _solutionFile = FindSolutionFile(workingDirectory);
  }

  /// <summary>
  /// Finds all solution files (.sln and .slnx) in the specified directory
  /// </summary>
  public static List<string> FindAllSolutionFiles(string directory)
  {
    var solutionFiles = new List<string>();

    // Find .sln files
    solutionFiles.AddRange(
        Directory.GetFiles(directory, "*.sln")
            .Select(f => Path.GetFileName(f))
    );

    // Find .slnx files
    solutionFiles.AddRange(
        Directory.GetFiles(directory, "*.slnx")
            .Select(f => Path.GetFileName(f))
    );

    return solutionFiles.OrderBy(f => f).ToList();
  }

  private string FindSolutionFile(string directory)
  {
    var solutionFiles = FindAllSolutionFiles(directory);

    if (solutionFiles.Count == 0)
      return "";

    // Return the first one (alphabetically sorted)
    // In interactive mode, we'll handle selection separately
    return solutionFiles[0];
  }

  public Task<ExecutionResult> VersionAsync()
      => _executor.ExecuteAsync("dotnet", "--version");

  public Task<ExecutionResult> BuildAsync()
      => _executor.ExecuteAsync("dotnet", $"build {_solutionFile}");

  public Task<ExecutionResult> CleanAsync()
      => _executor.ExecuteAsync("dotnet", $"clean {_solutionFile}");

  public Task<ExecutionResult> RestoreAsync()
      => _executor.ExecuteAsync("dotnet", $"restore {_solutionFile}");

  public Task<ExecutionResult> TestAsync(string filter = "")
  {
    var args = $"test {_solutionFile}";
    if (!string.IsNullOrEmpty(filter))
      args += $" --filter \"{filter}\"";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> FormatAsync(bool verify = false)
  {
    var args = $"format {_solutionFile}";
    if (verify)
      args += " --verify-no-changes";
    return _executor.ExecuteAsync("dotnet", args);
  }

  // ===== Additional Build & Maintenance Methods =====

  public Task<ExecutionResult> BuildReleaseAsync()
  {
    var args = $"build {_solutionFile} -c Release";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> BuildNoRestoreAsync()
  {
    var args = $"build {_solutionFile} --no-restore";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> PackAsync(string projectPath = "")
  {
    var target = string.IsNullOrEmpty(projectPath) ? _solutionFile : projectPath;
    return _executor.ExecuteAsync("dotnet", $"pack {target}");
  }

  public Task<ExecutionResult> PackProjectsAsync()
  {
    return _executor.ExecuteAsync("dotnet", $"pack {_solutionFile}");
  }

  public Task<ExecutionResult> ToolRestoreAsync()
  {
    return _executor.ExecuteAsync("dotnet", "tool restore");
  }

  public async Task<ExecutionResult> ToolUpdateAsync()
  {
    var startTime = DateTime.UtcNow;
    var listResult = await _executor.ExecuteAsync("dotnet", "tool list --local", captureOutput: true);
    if (!listResult.Success)
    {
      return new ExecutionResult
      {
        Success = false,
        ExitCode = listResult.ExitCode,
        Output = listResult.Output,
        Error = listResult.Error,
        Duration = DateTime.UtcNow - startTime
      };
    }

    var packageIds = ParseLocalToolPackageIds(listResult.Output);
    if (packageIds.Count == 0)
    {
      AnsiConsole.MarkupLine("[yellow]No local tools found in manifest[/]");
      return new ExecutionResult
      {
        Success = true,
        ExitCode = 0,
        Duration = DateTime.UtcNow - startTime
      };
    }

    var failedTools = new List<string>();
    foreach (var packageId in packageIds)
    {
      AnsiConsole.MarkupLine($"[cyan]Updating tool:[/] {packageId}");
      var result = await _executor.ExecuteAsync("dotnet", $"tool update --local {packageId}");
      if (!result.Success)
      {
        failedTools.Add(packageId);
      }
    }

    if (failedTools.Count > 0)
    {
      AnsiConsole.MarkupLine($"[red]Failed to update {failedTools.Count} tool(s):[/] {string.Join(", ", failedTools)}");
      return new ExecutionResult
      {
        Success = false,
        ExitCode = 1,
        Duration = DateTime.UtcNow - startTime
      };
    }

    AnsiConsole.MarkupLine($"[green]✓ Updated {packageIds.Count} local tool(s)[/]");
    return new ExecutionResult
    {
      Success = true,
      ExitCode = 0,
      Duration = DateTime.UtcNow - startTime
    };
  }

  public Task<ExecutionResult> BuildProjectAsync(string projectPath, string configuration = "Debug", bool noRestore = false)
  {
    var args = $"build {projectPath} -c {configuration}";
    if (noRestore)
      args += " --no-restore";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> PublishProjectAsync(string projectPath, string configuration = "Debug", string outputDir = "", bool singleFile = false)
  {
    var args = $"publish {projectPath} -c {configuration}";
    if (!string.IsNullOrEmpty(outputDir))
      args += $" -o \"{outputDir}\"";
    if (singleFile)
      args += " --self-contained false -p:PublishSingleFile=true";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> PublishProjectRidAsync(string projectPath, string configuration = "Debug", string rid = "", bool singleFile = false, string outputDir = "")
  {
    var args = $"publish {projectPath} -c {configuration}";
    if (!string.IsNullOrEmpty(rid))
    {
      args += $" -r {rid} --self-contained true";
    }
    if (singleFile)
    {
      args += " /p:PublishSingleFile=true /p:PublishTrimmed=false";
    }
    if (!string.IsNullOrEmpty(outputDir))
    {
      args += $" -o \"{outputDir}\"";
    }
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> RunProjectAsync(string projectPath, bool noBuild = false)
  {
    var args = $"run --project {projectPath}";
    if (noBuild)
      args += " --no-build";
    return _executor.ExecuteAsync("dotnet", args);
  }

  public Task<ExecutionResult> WatchProjectAsync(string projectPath)
  {
    return _executor.ExecuteAsync("dotnet", $"watch --project {projectPath} run");
  }

  public Task<ExecutionResult> ListOutdatedPackagesAsync()
  {
    return _executor.ExecuteAsync("dotnet", $"outdated {_solutionFile}");
  }

  public Task<ExecutionResult> UpdateOutdatedPackagesAsync()
  {
    return _executor.ExecuteAsync("dotnet", $"outdated {_solutionFile} --upgrade");
  }

  public Task<ExecutionResult> UpdateOutdatedPackagesDevkitAsync()
  {
    return _executor.ExecuteAsync("dotnet", $"outdated {_solutionFile} --upgrade -inc 'BridgingIT.DevKit'");
  }

  public Task<ExecutionResult> AnalyzersAsync()
  {
    return _executor.ExecuteAsync("dotnet", $"format analyzers {_solutionFile}");
  }

  public Task<ExecutionResult> AnalyzersExportAsync(string reportPath = "")
  {
    var args = $"format analyzers {_solutionFile}";
    if (!string.IsNullOrEmpty(reportPath))
      args += $" --report {reportPath}";
    return _executor.ExecuteAsync("dotnet", args);
  }

  // ===== Module-Specific Test Methods =====

  /// <summary>
  /// Runs tests for a specific module
  /// </summary>
  /// <param name="moduleName">Name of module</param>
  /// <param name="kind">Type of test: "unit" or "integration"</param>
  /// <returns>Execution result</returns>
  public Task<ExecutionResult> TestModuleAsync(string moduleName, string kind)
  {
    var testProjectPath = BuildTestProjectPath(moduleName, kind);
    return _executor.ExecuteAsync("dotnet", $"test {testProjectPath}");
  }

  /// <summary>
  /// Builds test project path for a module
  /// </summary>
  private string BuildTestProjectPath(string moduleName, string kind)
  {
    var testProjectType = kind == "unit" ? "UnitTests" : "IntegrationTests";
    return $"tests/Modules/{moduleName}/{moduleName}.{testProjectType}/{moduleName}.{testProjectType}.csproj";
  }

  private static List<string> ParseLocalToolPackageIds(string output)
  {
    var packageIds = new List<string>();
    if (string.IsNullOrWhiteSpace(output))
      return packageIds;

    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      var trimmed = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmed))
        continue;

      if (trimmed.StartsWith("Package Id", StringComparison.OrdinalIgnoreCase) ||
          trimmed.StartsWith("---", StringComparison.OrdinalIgnoreCase) ||
          trimmed.StartsWith("No tools were found", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      var columns = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (columns.Length < 2)
        continue;

      if (!char.IsDigit(columns[1][0]))
        continue;

      packageIds.Add(columns[0]);
    }

    return packageIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
  }

  // ===== EF Core Methods =====

  /// <summary>
  /// Gets infrastructure project path for a module
  /// </summary>
  private string GetInfrastructureProjectPath(string moduleName)
  {
    return $"src/Modules/{moduleName}/{moduleName}.Infrastructure/{moduleName}.Infrastructure.csproj";
  }

  /// <summary>
  /// Builds EF command arguments
  /// </summary>
  private string[] BuildEfArgs(string moduleName, string dbContext, string startupProject, string[] extraArgs)
  {
    var infraProject = GetInfrastructureProjectPath(moduleName);
    var args = new List<string> { "dotnet", "ef" };
    args.AddRange(extraArgs);
    args.AddRange(new[] { "--project", infraProject, "--startup-project", startupProject, "--no-build", "--verbose", "--context", dbContext });
    return args.ToArray();
  }

  /// <summary>
  /// Shows DbContext info
  /// </summary>
  public Task<ExecutionResult> EfInfoAsync(string moduleName, string dbContext)
  {
    var args = BuildEfArgs(moduleName, dbContext, "src/Presentation.Web.Server/Presentation.Web.Server.csproj", new[] { "dbcontext", "info" });
    return _executor.ExecuteAsync("dotnet", $"ef dbcontext info --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Lists migrations
  /// </summary>
  public Task<ExecutionResult> EfListAsync(string moduleName, string dbContext)
  {
    return _executor.ExecuteAsync("dotnet", $"ef migrations list --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Adds a new migration
  /// </summary>
  public Task<ExecutionResult> EfAddAsync(string moduleName, string dbContext, string migrationName)
  {
    var infraProject = GetInfrastructureProjectPath(moduleName);
    var migrationsDir = $"src/Modules/{moduleName}/{moduleName}.Infrastructure/EntityFramework/Migrations";
    var args = $"ef migrations add {migrationName} --context {dbContext} --project {infraProject} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --output-dir EntityFramework/Migrations --no-build";
    return _executor.ExecuteAsync("dotnet", args);
  }

  /// <summary>
  /// Removes last migration
  /// </summary>
  public Task<ExecutionResult> EfRemoveAsync(string moduleName, string dbContext)
  {
    return _executor.ExecuteAsync("dotnet", $"ef migrations remove --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Removes all migration files
  /// </summary>
  public ExecutionResult EfRemoveAll(string moduleName, string dbContext)
  {
    var migrationsDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "Modules", moduleName, $"{moduleName}.Infrastructure", "EntityFramework", "Migrations");

    if (Directory.Exists(migrationsDir))
    {
      var files = Directory.GetFiles(migrationsDir, "*.cs");
      foreach (var file in files)
      {
        File.Delete(file);
      }
      return new ExecutionResult { Success = true, ExitCode = 0, Duration = TimeSpan.Zero };
    }

    return new ExecutionResult { Success = true, ExitCode = 0, Duration = TimeSpan.Zero };
  }

  /// <summary>
  /// Applies migrations to database
  /// </summary>
  public Task<ExecutionResult> EfApplyAsync(string moduleName, string dbContext)
  {
    return _executor.ExecuteAsync("dotnet", $"ef database update --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Recreates database (drop and migrate)
  /// </summary>
  public async Task<ExecutionResult> EfRecreateAsync(string moduleName, string dbContext)
  {
    await _executor.ExecuteAsync("dotnet", $"ef database drop --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build --force");
    return await _executor.ExecuteAsync("dotnet", $"ef database update --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Undoes last migration
  /// </summary>
  public Task<ExecutionResult> EfUndoAsync(string moduleName, string dbContext)
  {
    return _executor.ExecuteAsync("dotnet", $"ef database update 0 --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Shows migration status
  /// </summary>
  public Task<ExecutionResult> EfStatusAsync(string moduleName, string dbContext)
  {
    return _executor.ExecuteAsync("dotnet", $"ef migrations list --context {dbContext} --project {GetInfrastructureProjectPath(moduleName)} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --no-build");
  }

  /// <summary>
  /// Resets migrations (creates new baseline)
  /// </summary>
  public async Task<ExecutionResult> EfResetAsync(string moduleName, string dbContext)
  {
    var infraProject = GetInfrastructureProjectPath(moduleName);

    EfRemoveAll(moduleName, dbContext);

    return await _executor.ExecuteAsync("dotnet", $"ef migrations add Initial --context {dbContext} --project {infraProject} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --output-dir EntityFramework/Migrations --no-build");
  }

  /// <summary>
  /// Exports SQL script
  /// </summary>
  public async Task<ExecutionResult> EfScriptAsync(string moduleName, string dbContext, string outputPath = "")
  {
    var moduleLower = moduleName.ToLower();
    var output = string.IsNullOrEmpty(outputPath)
      ? Path.Combine(_config.OutputDirectory, "ef", $"efscript_{moduleLower}.sql")
      : outputPath;
    var infraProject = GetInfrastructureProjectPath(moduleName);

    var outputDir = Path.GetDirectoryName(output);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
      Directory.CreateDirectory(outputDir);

    var result = await _executor.ExecuteAsync("dotnet", $"ef migrations script --context {dbContext} --project {infraProject} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --output {output} --idempotent --no-build");

    if (result.Success)
    {
      AnsiConsole.MarkupLine($"[green]✓ Script written:[/] [cyan]{Path.GetFullPath(output)}[/]");
    }

    return result;
  }

  /// <summary>
  /// Exports migration bundle
  /// </summary>
  public async Task<ExecutionResult> EfBundleAsync(string moduleName, string dbContext, string outputPath = "")
  {
    var moduleLower = moduleName.ToLower();
    var output = string.IsNullOrEmpty(outputPath)
      ? Path.Combine(_config.OutputDirectory, "ef", $"efbundle_{moduleLower}.exe")
      : outputPath;
    var infraProject = GetInfrastructureProjectPath(moduleName);

    var outputDir = Path.GetDirectoryName(output);
    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
      Directory.CreateDirectory(outputDir);

    var result = await _executor.ExecuteAsync("dotnet", $"ef migrations bundle --context {dbContext} --project {infraProject} --startup-project src/Presentation.Web.Server/Presentation.Web.Server.csproj --output {output} --no-build");

    if (result.Success)
    {
      AnsiConsole.MarkupLine($"[green]✓ Bundle written:[/] [cyan]{Path.GetFullPath(output)}[/]");
    }

    return result;
  }
}
