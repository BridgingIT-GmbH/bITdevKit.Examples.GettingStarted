// BDK CLI - Task Registry Module
/// <summary>
/// Defines all available tasks and their execution handlers
/// </summary>

/// <summary>
/// Represents a single task that can be executed
/// </summary>
public class BdkTask
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public Func<TaskContext, Task<ExecutionResult>> Execute { get; set; } = null!;
}

/// <summary>
/// Registry of all available BDK tasks organized by category
/// </summary>
public static class TaskRegistry
{
    public static List<BdkTask> GetAllTasks()
    {
        return new List<BdkTask>
        {
            // ===== Build & Maintenance =====
            new BdkTask
            {
                Key = "build",
                Label = "Build Solution",
                Description = "Build the entire solution",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildAsync()
            },
            new BdkTask
            {
                Key = "clean",
                Label = "Clean Solution",
                Description = "Clean build artifacts",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.CleanAsync()
            },
            new BdkTask
            {
                Key = "restore",
                Label = "Restore Packages",
                Description = "Restore NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.RestoreAsync()
            },
            new BdkTask
            {
                Key = "format",
                Label = "Format Code",
                Description = "Format code using dotnet format",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync()
            },
            new BdkTask
            {
                Key = "format-check",
                Label = "Format Check",
                Description = "Verify code formatting",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync(verify: true)
            },

            // ===== Testing =====
            new BdkTask
            {
                Key = "test",
                Label = "Run All Tests",
                Description = "Run all unit and integration tests",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync()
            },
            new BdkTask
            {
                Key = "test-unit",
                Label = "Run Unit Tests",
                Description = "Run unit tests only",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync("Category=unit")
            },
            new BdkTask
            {
                Key = "test-integration",
                Label = "Run Integration Tests",
                Description = "Run integration tests only",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync("Category=integration")
            },

            // ===== Utilities =====
            new BdkTask
            {
                Key = "version",
                Label = "Show .NET Version",
                Description = "Display .NET SDK version",
                Category = "Utilities",
                Execute = async (ctx) => await ctx.DotnetCli.VersionAsync()
            }
        };
    }

    public static Dictionary<string, List<BdkTask>> GetTasksByCategory()
    {
        return GetAllTasks()
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
