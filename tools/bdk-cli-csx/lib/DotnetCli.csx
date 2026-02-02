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
}
