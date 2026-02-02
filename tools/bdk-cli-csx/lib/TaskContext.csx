// BDK CLI - Task Context Module
/// <summary>
/// Context passed to task execution handlers
/// Contains all dependencies and configuration needed by tasks
/// </summary>
public class TaskContext
{
    public BdkConfig Config { get; set; } = null!;
    public DotnetCli DotnetCli { get; set; } = null!;
    public CommandExecutor Executor { get; set; } = null!;
    public string SolutionFile { get; set; } = "";
}
