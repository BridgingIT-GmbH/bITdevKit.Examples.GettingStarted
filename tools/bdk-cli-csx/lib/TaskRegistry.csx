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
            new BdkTask {
                Key = "coverage",
                Label = "Code Coverage",
                Description = "Run tests with coverage (cobertura)",
                Category = "Testing",
                Execute = async (ctx) =>
                {
                    var startTime = DateTime.Now;
                    var solution = ctx.SolutionPath;
                    if (string.IsNullOrEmpty(solution))
                    {
                        AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }

                    var outDir = Path.Combine(ctx.OutputDir, "coverage");
                    Directory.CreateDirectory(outDir);

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var runDir = Path.Combine(outDir, $"run_{timestamp}");
                    Directory.CreateDirectory(runDir);

                    AnsiConsole.MarkupLine($"[cyan]Running tests with coverage -> {Markup.Escape(runDir)}[/]");

                    var args = $"test \"{solution}\" --collect:\"XPlat Code Coverage\" --results-directory \"{runDir}\" --settings:coverlet.runsettings";
                    var result = await ctx.Executor.ExecuteAsync("dotnet", args);
                    
                    if (result.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]Tests failed[/]");
                        return new ExecutionResult { Success = false, ExitCode = result.ExitCode, Duration = DateTime.Now - startTime };
                    }
                    
                    var coverageFiles = Directory.GetFiles(runDir, "coverage.cobertura.xml", SearchOption.AllDirectories);

                    if (coverageFiles.Length == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No coverage.cobertura.xml files found[/]");
                        return new ExecutionResult { Success = false, ExitCode = 2, Duration = DateTime.Now - startTime };
                    }

                    AnsiConsole.MarkupLine($"[green]Found {coverageFiles.Length} coverage file(s) under {Markup.Escape(runDir)}[/]");
                    
                    return new ExecutionResult 
                    { 
                        Success = true, 
                        ExitCode = 0, 
                        Duration = DateTime.Now - startTime 
                    };
                }
            },
            new BdkTask {
                Key = "coverage-html",
                Label = "Code Coverage (HTML)",
                Description = "Run coverage and generate HTML report",
                Category = "Testing",
                Execute = async (ctx) =>
                {
                    var startTime = DateTime.Now;
                    var solution = ctx.SolutionPath;
                    if (string.IsNullOrEmpty(solution))
                    {
                        AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                        return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                    }

                    var outDir = Path.Combine(ctx.OutputDir, "coverage");
                    Directory.CreateDirectory(outDir);

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var runDir = Path.Combine(outDir, $"run_{timestamp}");
                    Directory.CreateDirectory(runDir);

                    AnsiConsole.MarkupLine($"[cyan]Running tests with coverage -> {Markup.Escape(runDir)}[/]");

                    var args = $"test \"{solution}\" --collect:\"XPlat Code Coverage\" --results-directory \"{runDir}\" --settings:coverlet.runsettings";
                    var result = await ctx.Executor.ExecuteAsync("dotnet", args);
                    
                    if (result.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]Tests failed[/]");
                        return new ExecutionResult { Success = false, ExitCode = result.ExitCode, Duration = DateTime.Now - startTime };
                    }
                    
                    var coverageFiles = Directory.GetFiles(runDir, "coverage.cobertura.xml", SearchOption.AllDirectories);
                    
                    if (coverageFiles.Length == 0)
                    {
                        AnsiConsole.MarkupLine("[red]No coverage.cobertura.xml files found[/]");
                        return new ExecutionResult { Success = false, ExitCode = 2, Duration = DateTime.Now - startTime };
                    }
                    
                    var reportRoot = Path.Combine(runDir, "report");
                    Directory.CreateDirectory(reportRoot);
                    var reportsArg = string.Join(';', coverageFiles);
                    var reportTypes = "HtmlInline_AzurePipelines;MarkdownSummaryGithub";
                    
                    AnsiConsole.MarkupLine($"[cyan]Generating HTML report -> {Markup.Escape(reportRoot)}[/]");
                    
                    args = $"tool run reportgenerator -- -reports:\"{reportsArg}\" -targetdir:\"{reportRoot}\" -reporttypes:{reportTypes}";
                    result = await ctx.Executor.ExecuteAsync("dotnet", args);
                    
                    if (result.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]Report generation failed[/]");
                        return new ExecutionResult { Success = false, ExitCode = result.ExitCode, Duration = DateTime.Now - startTime };
                    }
                    
                    var indexFile = Path.Combine(reportRoot, "index.html");
                    if (File.Exists(indexFile))
                    {
                        Utils.OpenFile(indexFile);
                        return new ExecutionResult 
                        { 
                            Success = true, 
                            ExitCode = 0, 
                            Duration = DateTime.Now - startTime 
                        };
                    }
                    
                    AnsiConsole.MarkupLine("[yellow]Report generation completed but index.html not found[/]");
                    
                    return new ExecutionResult 
                    { 
                        Success = true, 
                        ExitCode = 0, 
                        Duration = DateTime.Now - startTime 
                    };
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
            new BdkTask
            {
                Key = "docs-generate",
                Label = "Generate Documentation",
                Description = "Generate consolidated markdown documentation per project",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.GenerateDocsAsync(ctx)
            },
            new BdkTask
            {
                Key = "clean-ws",
                Label = "Clean Workspace",
                Description = "Remove build/output artifact directories (bin/obj/node_modules/etc.)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.CleanWorkspaceAsync(ctx)
            },
            new BdkTask
            {
                Key = "cleanup",
                Label = "Clean Workspace (Alias)",
                Description = "Remove build/output artifact directories (alias for clean-ws)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.CleanWorkspaceAsync(ctx)
            },
            new BdkTask
            {
                Key = "remove-headers",
                Label = "Remove File Headers",
                Description = "Remove MIT license headers from all C# files in src/ and tests/",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.RemoveFileHeadersAsync(ctx)
            },
            new BdkTask
            {
                Key = "repl",
                Label = "C# REPL",
                Description = "Run C# REPL (csharprepl)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.RunCSharpReplAsync(ctx)
            },
            new BdkTask
            {
                Key = "shell",
                Label = "C# Shell (Alias)",
                Description = "Run C# REPL (alias for repl)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.RunCSharpReplAsync(ctx)
            },
            new BdkTask
            {
                Key = "kill-dotnet",
                Label = "Kill .NET Process",
                Description = "Terminate a dotnet process (interactive selection or direct -ProcessId)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.KillDotnetProcessAsync(ctx)
            },
            new BdkTask
            {
                Key = "minver",
                Label = "Show MinVer",
                Description = "Display semantic version computed by MinVer",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.ShowMinVerAsync(ctx)
            },
            new BdkTask
            {
                Key = "show-minver",
                Label = "Show MinVer (Alias)",
                Description = "Display semantic version (alias for minver)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.ShowMinVerAsync(ctx)
            },
            new BdkTask
            {
                Key = "browser-devkit-docs",
                Label = "Open DevKit Docs",
                Description = "Open DevKit docs",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.OpenBrowserUrlAsync(ctx, "https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs", "DevKit Docs")
            },
            new BdkTask
            {
                Key = "browser-seq",
                Label = "Open SEQ Dashboard",
                Description = "Open SEQ logging dashboard",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.OpenBrowserUrlAsync(ctx, "http://localhost:15349", "SEQ Dashboard")
            },
            new BdkTask
            {
                Key = "browser-adminneo",
                Label = "Open AdminNeo Dashboard",
                Description = "Open AdminNeo dashboard",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.OpenBrowserUrlAsync(ctx, "http://localhost:18089", "AdminNeo Dashboard")
            },
            new BdkTask
            {
                Key = "browser-server-kestrel",
                Label = "Open Server (Kestrel)",
                Description = "Open Server (Kestrel HTTPS)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.OpenBrowserUrlAsync(ctx, "https://localhost:5001/scalar", "Server Kestrel")
            },
            new BdkTask
            {
                Key = "browser-server-docker",
                Label = "Open Server (Docker)",
                Description = "Open Server (Docker HTTP)",
                Category = "Utilities",
                Execute = async (ctx) => await MiscUtils.OpenBrowserUrlAsync(ctx, "http://localhost:8080/scalar", "Server Docker")
            },
            new BdkTask
            {
                Key = "docs-update",
                Label = "Update DevKit Docs",
                Description = "Download latest DevKit docs",
                Category = "Utilities",
                Execute = async (ctx) => 
                {
                    AnsiConsole.MarkupLine("[yellow]Docs update not yet implemented in C# CLI[/]");
                    return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
                }
            },
            
            // ===== Performance & Diagnostics =====
            new BdkTask
            {
                Key = "bench",
                Label = "Run Benchmarks",
                Description = "Run benchmark project (auto-detect)",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.RunBenchmarksAsync(ctx)
            },
            new BdkTask
            {
                Key = "bench-select",
                Label = "Run Selected Benchmarks",
                Description = "Select and run specific benchmark project",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.RunSelectedBenchmarksAsync(ctx)
            },
            new BdkTask
            {
                Key = "trace-flame",
                Label = "Flame Trace",
                Description = "Collect flame graph trace (SampleProfiler)",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CollectFlameTraceAsync(ctx)
            },
            new BdkTask
            {
                Key = "trace-cpu",
                Label = "CPU Trace",
                Description = "Collect CPU performance trace",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CollectCpuTraceAsync(ctx)
            },
            new BdkTask
            {
                Key = "trace-gc",
                Label = "GC Trace",
                Description = "Collect GC-focused performance trace",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CollectGcTraceAsync(ctx)
            },
            new BdkTask
            {
                Key = "dump-heap",
                Label = "Heap Dump",
                Description = "Create memory heap dump of process",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CreateHeapDumpAsync(ctx)
            },
            new BdkTask
            {
                Key = "gc-stats",
                Label = "GC Stats",
                Description = "Monitor GC counters for 5 seconds",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CollectGcStatsAsync(ctx)
            },
            new BdkTask
            {
                Key = "aspnet-metrics",
                Label = "ASP.NET Metrics",
                Description = "Monitor ASP.NET Core counters for 10 seconds",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.CollectAspnetMetricsAsync(ctx)
            },
            new BdkTask
            {
                Key = "diag-quick",
                Label = "Quick Diagnostics",
                Description = "Combined CPU+GC trace + ASP.NET metrics (5s each)",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.RunQuickDiagnosticsAsync(ctx)
            },
            new BdkTask
            {
                Key = "speedscope-view",
                Label = "View Speedscope",
                Description = "Open speedscope profile in viewer",
                Category = "Performance & Diagnostics",
                Execute = async (ctx) => await DiagnosticsUtils.ViewSpeedscopeAsync(ctx)
            },
            
            // ===== Security & Compliance =====
            new BdkTask
            {
                Key = "vulnerabilities",
                Label = "Check Vulnerabilities",
                Description = "List vulnerable packages",
                Category = "Security & Compliance",
                Execute = async (ctx) => await SecurityUtils.ListVulnerablePackagesAsync(ctx)
            },
            new BdkTask
            {
                Key = "vulnerabilities-deep",
                Label = "Check Vulnerabilities (Deep)",
                Description = "List vulnerable packages (including transitive)",
                Category = "Security & Compliance",
                Execute = async (ctx) => await SecurityUtils.ListVulnerablePackagesDeepAsync(ctx)
            },
            new BdkTask
            {
                Key = "outdated",
                Label = "Outdated Packages",
                Description = "List packages with updates available",
                Category = "Security & Compliance",
                Execute = async (ctx) => await SecurityUtils.ListOutdatedPackagesAsync(ctx)
            },
            new BdkTask
            {
                Key = "outdated-json",
                Label = "Outdated Packages (JSON)",
                Description = "Export outdated packages to JSON",
                Category = "Security & Compliance",
                Execute = async (ctx) => await SecurityUtils.ListOutdatedPackagesJsonAsync(ctx)
            },
            new BdkTask
            {
                Key = "licenses",
                Label = "License Report",
                Description = "Generate license report (Markdown + JSON)",
                Category = "Security & Compliance",
                Execute = async (ctx) => await SecurityUtils.GenerateLicenseReportAsync(ctx)
            },
            
            // ===== API & Spec =====
            new BdkTask
            {
                Key = "openapi-lint",
                Label = "Lint OpenAPI",
                Description = "Lint OpenAPI spec with Spectral",
                Category = "API & Spec",
                Execute = async (ctx) => await OpenApiUtils.LintOpenApiAsync(ctx)
            },
            new BdkTask
            {
                Key = "openapi-client-dotnet",
                Label = "Generate C# Client",
                Description = "Generate OpenAPI C# client with Kiota",
                Category = "API & Spec",
                Execute = async (ctx) => await OpenApiUtils.GenerateDotNetClientAsync(ctx)
            },
            new BdkTask
            {
                Key = "openapi-client-typescript",
                Label = "Generate TypeScript Client",
                Description = "Generate OpenAPI TypeScript client with Kiota",
                Category = "API & Spec",
                Execute = async (ctx) => await OpenApiUtils.GenerateTypeScriptClientAsync(ctx)
            },
            new BdkTask
            {
                Key = "openapi-http",
                Label = "Generate HTTP Requests",
                Description = "Generate .http request files from spec",
                Category = "API & Spec",
                Execute = async (ctx) => await OpenApiUtils.GenerateHttpRequestFilesAsync(ctx)
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
            },
            
            // ===== Docker & Containers =====
            new BdkTask
            {
                Key = "docker-image-build-run-debug",
                Label = "Docker Image Build & Run (Debug)",
                Description = "Build and run image in Debug configuration",
                Category = "Docker & Containers",
                Execute = async (ctx) => 
                {
                    var buildResult = await ctx.DockerCli.BuildImageAsync("Debug", false);
                    if (!buildResult.Success)
                        return buildResult;
                    return await ctx.DockerCli.RunContainerAsync();
                }
            },
            new BdkTask
            {
                Key = "docker-image-build-run-release",
                Label = "Docker Image Build & Run (Release)",
                Description = "Build and run image in Release configuration",
                Category = "Docker & Containers",
                Execute = async (ctx) => 
                {
                    var buildResult = await ctx.DockerCli.BuildImageAsync("Release", false);
                    if (!buildResult.Success)
                        return buildResult;
                    return await ctx.DockerCli.RunContainerAsync();
                }
            },
            new BdkTask
            {
                Key = "docker-image-build-debug",
                Label = "Docker Image Build (Debug)",
                Description = "Build image in Debug configuration",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.BuildImageAsync("Debug", false)
            },
            new BdkTask
            {
                Key = "docker-image-build-release",
                Label = "Docker Image Build (Release)",
                Description = "Build image in Release configuration",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.BuildImageAsync("Release", false)
            },
            new BdkTask
            {
                Key = "docker-container-run",
                Label = "Docker Container Run",
                Description = "Run container (assumes image built)",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.RunContainerAsync()
            },
            new BdkTask
            {
                Key = "docker-container-logs",
                Label = "Docker Container Logs",
                Description = "View container logs",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ViewLogsAsync("", false)
            },
            new BdkTask
            {
                Key = "docker-container-logs-follow",
                Label = "Docker Container Logs (Follow)",
                Description = "Follow container logs in real-time",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ViewLogsAsync("", true)
            },
            new BdkTask
            {
                Key = "docker-container-ps",
                Label = "Docker Container PS",
                Description = "List running containers",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ListContainersAsync(false)
            },
            new BdkTask
            {
                Key = "docker-container-inspect",
                Label = "Docker Container Inspect",
                Description = "Show detailed container info",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.InspectContainerAsync("")
            },
            new BdkTask
            {
                Key = "docker-container-stop",
                Label = "Docker Container Stop",
                Description = "Stop container",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.StopContainerAsync()
            },
            new BdkTask
            {
                Key = "docker-container-remove",
                Label = "Docker Container Remove",
                Description = "Remove (stop & force delete) container",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.RemoveContainerAsync(false)
            },
            new BdkTask
            {
                Key = "docker-image-remove",
                Label = "Docker Image Remove",
                Description = "Remove container and image",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.RemoveImageAsync()
            },
            new BdkTask
            {
                Key = "docker-compose-up",
                Label = "Docker Compose Up",
                Description = "Start docker compose stack",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ComposeUpAsync()
            },
            new BdkTask
            {
                Key = "docker-compose-recreate",
                Label = "Docker Compose Recreate",
                Description = "Recreate all compose services",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ComposeRecreateAsync()
            },
            new BdkTask
            {
                Key = "docker-compose-down",
                Label = "Docker Compose Down",
                Description = "Stop docker compose stack (keep volumes)",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ComposeDownAsync()
            },
            new BdkTask
            {
                Key = "docker-compose-down-clean",
                Label = "Docker Compose Down Clean",
                Description = "Stop stack & remove volumes/images",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.ComposeDownCleanAsync()
            },
            new BdkTask
            {
                Key = "docker-cleanup-all",
                Label = "Docker Cleanup All",
                Description = "Clean up ALL Docker resources (containers, images, volumes, networks)",
                Category = "Docker & Containers",
                Execute = async (ctx) => await ctx.DockerCli.CleanupAllAsync()
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
