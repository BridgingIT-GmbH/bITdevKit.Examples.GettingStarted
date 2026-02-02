// BDK CLI - .NET CLI Wrapper Module
/// <summary>
/// Wrapper for .NET CLI commands (build, test, restore, format, etc.)
/// </summary>
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
            args += $" -o {outputDir}";
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
            args += $" -o {outputDir}";
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

    public Task<ExecutionResult> UpdatePackagesAsync()
    {
        return _executor.ExecuteAsync("dotnet", $"outdated {_solutionFile}");
    }

    public Task<ExecutionResult> UpdatePackagesDevkitAsync()
    {
        return _executor.ExecuteAsync("dotnet", $"outdated {_solutionFile} --framework net10.0");
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
}
