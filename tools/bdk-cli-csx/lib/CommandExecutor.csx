// BDK CLI - Command Execution Module
// Handles cross-platform process execution with real-time output streaming

/// <summary>
/// Executes external commands with real-time output streaming
/// </summary>
public class CommandExecutor
{
    private readonly string _workingDirectory;

    public CommandExecutor(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public async Task<ExecutionResult> ExecuteAsync(string fileName, string arguments = "", bool captureOutput = false, bool showCommand = true)
    {
        if (showCommand && !captureOutput)
        {
            Console.WriteLine($"[exec] {fileName} {arguments}");
        }

        var startTime = DateTime.UtcNow;
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var processInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = _workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                if (!captureOutput)
                {
                    Console.WriteLine(e.Data);
                }
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                if (!captureOutput)
                {
                    Console.Error.WriteLine(e.Data);
                }
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        var duration = DateTime.UtcNow - startTime;

        return new ExecutionResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString(),
            Duration = duration,
            Success = process.ExitCode == 0
        };
    }
}

/// <summary>
/// Result of a command execution
/// </summary>
public class ExecutionResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}
