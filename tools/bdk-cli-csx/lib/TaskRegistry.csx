// BDK CLI - Task Registry Module
/// <summary>
/// Defines all available tasks and their execution handlers
/// </summary>

using Spectre.Console;

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
                Key = "build",
                Label = "Build Solution",
                Description = "Build entire solution (Debug)",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildAsync()
            },
            new BdkTask
            {
                Key = "build-release",
                Label = "Build Release",
                Description = "Build solution in Release configuration",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildReleaseAsync()
            },
            new BdkTask
            {
                Key = "build-nr",
                Label = "Build NoRestore",
                Description = "Build without restoring packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildNoRestoreAsync()
            },
            new BdkTask
            {
                Key = "pack",
                Label = "Pack",
                Description = "Create NuGet packages for entire solution",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.PackAsync()
            },
            new BdkTask
            {
                Key = "pack-projects",
                Label = "Pack Projects",
                Description = "Create NuGet packages for all projects",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.PackProjectsAsync()
            },
            new BdkTask
            {
                Key = "tool-restore",
                Label = "Restore Tools",
                Description = "Restore dotnet tools",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.ToolRestoreAsync()
            },
            new BdkTask
            {
                Key = "server-build",
                Label = "Server Build",
                Description = "Build web server project",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    AnsiConsole.MarkupLine($"[dim]Building server project: {project}[/]");
                    return await ctx.DotnetCli.BuildProjectAsync(project, "Debug", false);
                }
            },
            new BdkTask
            {
                Key = "build-project",
                Label = "Build Project",
                Description = "Build a specific project",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to build:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    return await ctx.DotnetCli.BuildProjectAsync(project, "Debug", false);
                }
            },
            new BdkTask
            {
                Key = "server-publish",
                Label = "Publish Server",
                Description = "Publish web server (Debug) with RID selection",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing server project: {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Debug", rid, false, "");
                }
            },
            new BdkTask
            {
                Key = "publish-project",
                Label = "Publish Project",
                Description = "Publish a specific project with RID selection",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to publish:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing project: {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Debug", rid, false, "");
                }
            },
            new BdkTask
            {
                Key = "server-publish-release",
                Label = "Publish Server (Release)",
                Description = "Publish web server (Release) with RID selection",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing server project (Release): {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Release", rid, false, "");
                }
            },
            new BdkTask
            {
                Key = "publish-project-release",
                Label = "Publish Project (Release)",
                Description = "Publish a project (Release) with RID selection",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to publish:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing project (Release): {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Release", rid, false, "");
                }
            },
            new BdkTask
            {
                Key = "server-publish-sc",
                Label = "Publish Server Single",
                Description = "Publish web server as single-file with RID",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing server project (single-file): {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Release", rid, true, "");
                }
            },
            new BdkTask
            {
                Key = "publish-project-sc",
                Label = "Publish Project Single",
                Description = "Publish a project as single-file with RID",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to publish:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[dim]Publishing project (single-file): {project}[/]");
                    AnsiConsole.MarkupLine($"[dim]Target RID: {rid}[/]");
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, "Release", rid, true, "");
                }
            },
            new BdkTask
            {
                Key = "server-run-dev",
                Label = "Run Server",
                Description = "Run web server in development mode",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    AnsiConsole.MarkupLine($"[dim]Running server project: {project}[/]");
                    return await ctx.DotnetCli.RunProjectAsync(project, false);
                }
            },
            new BdkTask
            {
                Key = "run-project",
                Label = "Run Project",
                Description = "Run a specific project",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to run:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    return await ctx.DotnetCli.RunProjectAsync(project, false);
                }
            },
            new BdkTask
            {
                Key = "server-watch",
                Label = "Watch Server",
                Description = "Watch and hot-reload web server",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    AnsiConsole.MarkupLine($"[dim]Watching server project: {project}[/]");
                    return await ctx.DotnetCli.WatchProjectAsync(project);
                }
            },
            new BdkTask
            {
                Key = "update-packages",
                Label = "Update All Packages",
                Description = "List and update all NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.UpdatePackagesAsync()
            },
            new BdkTask
            {
                Key = "update-packages-devkit",
                Label = "Update DevKit Packages",
                Description = "Update bITdevKit NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.UpdatePackagesDevkitAsync()
            },
            new BdkTask
            {
                Key = "format-apply",
                Label = "Format Apply",
                Description = "Apply code formatting to solution",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync(verify: false)
            },
            new BdkTask
            {
                Key = "format-check",
                Label = "Format Check",
                Description = "Verify code formatting",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync(verify: true)
            },
            new BdkTask
            {
                Key = "analyzers",
                Label = "Analyzers",
                Description = "Run Roslyn analyzers",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.AnalyzersAsync()
            },
            new BdkTask
            {
                Key = "analyzers-export",
                Label = "Analyzers Export",
                Description = "Export analyzer report",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var reportPath = await Prompts.PromptTextAsync("Report path (optional):", "");
                    return await ctx.DotnetCli.AnalyzersExportAsync(reportPath);
                }
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
