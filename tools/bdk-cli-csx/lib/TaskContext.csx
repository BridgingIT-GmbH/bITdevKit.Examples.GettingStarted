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
    public string SelectedModule { get; set; } = "";
    public string SelectedDbContext { get; set; } = "";
    public List<string> AvailableModules { get; set; } = new();
    public List<string> AvailableDbContexts { get; set; } = new();
}
