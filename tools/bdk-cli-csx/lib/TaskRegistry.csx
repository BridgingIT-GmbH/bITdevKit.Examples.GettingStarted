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
            new() {
                Key = "clean",
                Label = "Clean Solution",
                Description = "Clean build artifacts",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.CleanAsync()
            },
            new() {
                Key = "restore",
                Label = "Restore Packages",
                Description = "Restore NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.RestoreAsync()
            },
            new() {
                Key = "build",
                Label = "Build Solution",
                Description = "Build entire solution (Debug)",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildAsync()
            },
            new() {
                Key = "build-release",
                Label = "Build Release",
                Description = "Build solution in Release configuration",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildReleaseAsync()
            },
            new() {
                Key = "build-nr",
                Label = "Build NoRestore",
                Description = "Build without restoring packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.BuildNoRestoreAsync()
            },
            new() {
                Key = "pack",
                Label = "Pack",
                Description = "Create NuGet packages for entire solution",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.PackAsync()
            },
            new() {
                Key = "pack-projects",
                Label = "Pack Projects",
                Description = "Create NuGet packages for all projects",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.PackProjectsAsync()
            },
            new() {
                Key = "tool-restore",
                Label = "Restore Tools",
                Description = "Restore dotnet tools",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.ToolRestoreAsync()
            },
            new() {
                Key = "server-build",
                Label = "Build Server",
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
            new() {
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
            new() {
                Key = "server-publish",
                Label = "Publish Server",
                Description = "Publish web server with config, RID, and single-file options",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = ctx.Config.DotnetPublishProject ?? "";
                    if (string.IsNullOrEmpty(project))
                    {
                        AnsiConsole.MarkupLine("[red]Error: DOTNET_PUBLISH_PROJECT not configured in .env[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var config = await Prompts.SelectConfigurationAsync("Debug");
                    if (string.IsNullOrEmpty(config))
                    {
                        AnsiConsole.MarkupLine("[yellow]Configuration selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var singleFile = await Prompts.SelectSingleFileAsync(false);
                    if (singleFile == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]Single-file selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[cyan]Publishing server:[/] {project}");
                    AnsiConsole.MarkupLine($"[dim]Configuration:[/] [cyan]{config}[/]");
                    AnsiConsole.MarkupLine($"[dim]RID:[/] [cyan]{rid}[/]");
                    AnsiConsole.MarkupLine($"[dim]Single-file:[/] [cyan]{singleFile.Value}[/]");
                    
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, config, rid, singleFile.Value, "");
                }
            },
            new() {
                Key = "publish-project",
                Label = "Publish Project",
                Description = "Publish a project with config, RID, and single-file options",
                Category = "Build & Maintenance",
                Execute = async (ctx) => 
                {
                    var project = await Prompts.SelectProjectAsync(ctx, "Select a project to publish:");
                    if (string.IsNullOrEmpty(project))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var config = await Prompts.SelectConfigurationAsync("Debug");
                    if (string.IsNullOrEmpty(config))
                    {
                        AnsiConsole.MarkupLine("[yellow]Configuration selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var rid = await Prompts.SelectRidAsync("linux-x64");
                    if (string.IsNullOrEmpty(rid))
                    {
                        AnsiConsole.MarkupLine("[yellow]RID selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    var singleFile = await Prompts.SelectSingleFileAsync(false);
                    if (singleFile == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]Single-file selection cancelled[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }
                    
                    AnsiConsole.MarkupLine($"[cyan]Publishing project:[/] {project}");
                    AnsiConsole.MarkupLine($"[dim]Configuration:[/] [cyan]{config}[/]");
                    AnsiConsole.MarkupLine($"[dim]RID:[/] [cyan]{rid}[/]");
                    AnsiConsole.MarkupLine($"[dim]Single-file:[/] [cyan]{singleFile.Value}[/]");
                    
                    return await ctx.DotnetCli.PublishProjectRidAsync(project, config, rid, singleFile.Value, "");
                }
            },
            new() {
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
            new() {
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
            new() {
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
            new() {
                Key = "update-packages",
                Label = "Update All Packages",
                Description = "List and update all NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.UpdatePackagesAsync()
            },
            new() {
                Key = "update-packages-devkit",
                Label = "Update DevKit Packages",
                Description = "Update bITdevKit NuGet packages",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.UpdatePackagesDevkitAsync()
            },
            new() {
                Key = "format-apply",
                Label = "Format Apply",
                Description = "Apply code formatting to solution",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync(verify: false)
            },
            new() {
                Key = "format-check",
                Label = "Format Check",
                Description = "Verify code formatting",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.FormatAsync(verify: true)
            },
            new() {
                Key = "analyzers",
                Label = "Analyzers",
                Description = "Run Roslyn analyzers",
                Category = "Build & Maintenance",
                Execute = async (ctx) => await ctx.DotnetCli.AnalyzersAsync()
            },
            new() {
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
            new() {
                Key = "test",
                Label = "Run All Tests",
                Description = "Run all unit and integration tests",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync()
            },
            new() {
                Key = "test-unit",
                Label = "Run Unit Tests",
                Description = "Run unit tests only",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync("Category=unit")
            },
            new() {
                Key = "test-integration",
                Label = "Run Integration Tests",
                Description = "Run integration tests only",
                Category = "Testing",
                Execute = async (ctx) => await ctx.DotnetCli.TestAsync("Category=integration")
            },
            new() {
                Key = "test-unit-module",
                Label = "Run Unit Tests (Module)",
                Description = "Run unit tests for selected module",
                Category = "Testing",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to run unit tests:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    return await ctx.DotnetCli.TestModuleAsync(module, "unit");
                }
            },
            new() {
                Key = "test-integration-module",
                Label = "Run Integration Tests (Module)",
                Description = "Run integration tests for selected module",
                Category = "Testing",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to run integration tests:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    return await ctx.DotnetCli.TestModuleAsync(module, "integration");
                }
            },
            
            // ===== Utilities =====
            new BdkTask
            {
                Key = "version",
                Label = "Show .NET Version",
                Description = "Display .NET SDK version",
                Category = "Utilities",
                Execute = async (ctx) => await ctx.DotnetCli.VersionAsync()
            },
            
            // ===== EF & Persistence =====
            new BdkTask
            {
                Key = "ef-info",
                Label = "EF Info",
                Description = "Show DbContext info",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module for EF info:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfInfoAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-list",
                Label = "EF List Migrations",
                Description = "List migrations",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to list migrations:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfListAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-add",
                Label = "EF Add Migration",
                Description = "Add new migration",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module for migration:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var migrationName = await Prompts.PromptTextAsync("Enter migration name (blank = auto timestamp):", "");
                    if (string.IsNullOrEmpty(migrationName))
                        migrationName = "Migration_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    
                    return await ctx.DotnetCli.EfAddAsync(module, dbContext, migrationName);
                }
            },
            new BdkTask
            {
                Key = "ef-remove",
                Label = "EF Remove Migration",
                Description = "Remove last migration",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to remove migration:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfRemoveAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-removeall",
                Label = "EF Remove All Migrations",
                Description = "Delete all migration files",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to remove all migrations:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return ctx.DotnetCli.EfRemoveAll(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-apply",
                Label = "EF Apply Migrations",
                Description = "Update database (apply migrations)",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to apply migrations:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfApplyAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-update",
                Label = "EF Update Database",
                Description = "Update database (alias for apply)",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to update database:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfApplyAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-recreate",
                Label = "EF Recreate Database",
                Description = "Drop and recreate database",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to recreate database:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfRecreateAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-undo",
                Label = "EF Undo Migration",
                Description = "Undo last migration",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to undo migration:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfUndoAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-status",
                Label = "EF Migration Status",
                Description = "Show migration status",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module for migration status:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfStatusAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-reset",
                Label = "EF Reset Migrations",
                Description = "Squash migrations into new baseline",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to reset migrations:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    return await ctx.DotnetCli.EfResetAsync(module, dbContext);
                }
            },
            new BdkTask
            {
                Key = "ef-script",
                Label = "EF Export SQL Script",
                Description = "Export schema as SQL script",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to export script:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var defaultOutput = $".tmp/ef/efscript_{module.ToLower()}.sql";
                    var outputPath = await Prompts.PromptTextAsync("Output path:", defaultOutput);
                    return await ctx.DotnetCli.EfScriptAsync(module, dbContext, outputPath);
                }
            },
            new BdkTask
            {
                Key = "ef-bundle",
                Label = "EF Export Bundle",
                Description = "Export migration bundle",
                Category = "EF & Persistence",
                Execute = async (ctx) => 
                {
                    var module = await Prompts.SelectModuleForTaskAsync(ctx, "Select module to export bundle:");
                    if (string.IsNullOrEmpty(module))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    ctx.AvailableDbContexts = Prompts.DiscoverDbContexts(module);
                    var dbContext = await Prompts.SelectDbContextForTaskAsync(ctx, "Select DbContext:");
                    if (string.IsNullOrEmpty(dbContext))
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    
                    var defaultOutput = $".tmp/ef/efbundle_{module.ToLower()}.exe";
                    var outputPath = await Prompts.PromptTextAsync("Output path:", defaultOutput);
                    return await ctx.DotnetCli.EfBundleAsync(module, dbContext, outputPath);
                }
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
