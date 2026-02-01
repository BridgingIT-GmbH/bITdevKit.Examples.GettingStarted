#!/usr/bin/env dotnet-script
// BDK CLI - C# Script Version
// Cross-platform CLI for bITdevKit development tasks
// Usage: dotnet script bdk-cli.csx [command]
//        dotnet script bdk-cli.csx --help

#r "nuget: Spectre.Console, 0.54.0"
#r "nuget: Spectre.Console.ImageSharp, 0.54.0"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

// ============================================================================
// CONFIGURATION
// ============================================================================

public class BdkConfig
{
    public string OutputDirectory { get; set; } = ".tmp";
    public string ArtifactsDirectory { get; set; } = ".artifacts";
    public string SourcesDirectory { get; set; } = "src";
    public string ModulesDirectory { get; set; } = "src/Modules";
    public string TestsDirectory { get; set; } = "tests";
    public string DockerFilePath { get; set; } = "src/Presentation.Web.Server/Dockerfile";
    public string DockerComposePath { get; set; } = "docker-compose.yml";
    public string DotnetPublishProject { get; set; } = "src/Presentation.Web.Server/Presentation.Web.Server.csproj";
    public string EfStartupProject { get; set; } = "src/Presentation.Web.Server/Presentation.Web.Server.csproj";
    public string DockerDbConnectionString { get; set; } = "";

    public static BdkConfig LoadFromEnv(string envPath)
    {
        var config = new BdkConfig();
        
        if (!File.Exists(envPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Config file not found at {envPath}, using defaults[/]");
            return config;
        }

        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim().Trim('"');

            switch (key)
            {
                case "OUTPUT_DIRECTORY":
                    config.OutputDirectory = value;
                    break;
                case "ARTIFACTS_DIRECTORY":
                    config.ArtifactsDirectory = value;
                    break;
                case "SOURCES_DIRECTORY":
                    config.SourcesDirectory = value;
                    break;
                case "MODULES_DIRECTORY":
                    config.ModulesDirectory = value;
                    break;
                case "TESTS_DIRECTORY":
                    config.TestsDirectory = value;
                    break;
                case "DOCKER_FILE_PATH":
                    config.DockerFilePath = value;
                    break;
                case "DOCKER_COMPOSE_PATH":
                    config.DockerComposePath = value;
                    break;
                case "DOTNET_PUBLISH_PROJECT":
                    config.DotnetPublishProject = value;
                    break;
                case "EF_STARTUP_PROJECT":
                    config.EfStartupProject = value;
                    break;
                case "DOCKER_DB_CONNECTIONSTRING":
                    config.DockerDbConnectionString = value;
                    break;
            }
        }

        return config;
    }
}

// ============================================================================
// COMMAND EXECUTOR
// ============================================================================

public class CommandExecutor
{
    private readonly string _workingDirectory;

    public CommandExecutor(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public async Task<ExecutionResult> ExecuteAsync(string fileName, string arguments = "", bool captureOutput = false, bool showCommand = true)
    {
        // Show the command being executed
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

public class ExecutionResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}

// ============================================================================
// DOTNET CLI WRAPPER
// ============================================================================

public class DotnetCli
{
    private readonly CommandExecutor _executor;
    private readonly BdkConfig _config;
    private readonly string _solutionFile;

    public DotnetCli(CommandExecutor executor, BdkConfig config, string workingDirectory)
    {
        _executor = executor;
        _config = config;
        _solutionFile = FindSolutionFile(workingDirectory);
    }

    private string FindSolutionFile(string directory)
    {
        // Look for .sln or .slnx files
        var slnFiles = Directory.GetFiles(directory, "*.sln");
        if (slnFiles.Length > 0)
            return Path.GetFileName(slnFiles[0]);

        var slnxFiles = Directory.GetFiles(directory, "*.slnx");
        if (slnxFiles.Length > 0)
            return Path.GetFileName(slnxFiles[0]);

        return "";
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

// ============================================================================
// TASK REGISTRY
// ============================================================================

public class BdkTask
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public Func<TaskContext, Task<ExecutionResult>> Execute { get; set; } = null!;
}

public class TaskContext
{
    public BdkConfig Config { get; set; } = null!;
    public DotnetCli DotnetCli { get; set; } = null!;
    public CommandExecutor Executor { get; set; } = null!;
}

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

// ============================================================================
// UI SCREENS
// ============================================================================

public class BdkUI
{
    private readonly TaskContext _context;
    private readonly Dictionary<string, List<BdkTask>> _tasksByCategory;

    public BdkUI(TaskContext context)
    {
        _context = context;
        _tasksByCategory = TaskRegistry.GetTasksByCategory();
    }

    public async Task RunInteractiveAsync()
    {
        // Show header with ASCII art only once at startup
        Console.Clear();
        ShowStartupBanner();
        
        while (true)
        {
            var category = ShowCategoryMenu();
            if (category == null || category == "✕ Exit")
                break;

            // Keep showing task menu until user goes back or exits
            var shouldExit = await ShowTaskMenuLoopAsync(category);
            if (shouldExit)
                break; // Exit was selected from submenu
        }
    }

    private async Task<bool> ShowTaskMenuLoopAsync(string category)
    {
        while (true)
        {
            var task = ShowTaskMenu(category);
            if (task == null)
                return false; // Go back to category menu (don't clear screen)
            
            if (task.Key == "exit")
                return true; // Exit was selected - signal to quit app

            await ExecuteTaskAsync(task);
            // After task completes, loop back to show task menu again (without clearing)
        }
    }

    private void ShowStartupBanner()
    {
        var repoName = Path.GetFileName(Directory.GetCurrentDirectory());
        
        // Check if running in VS Code
        var isVSCode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_PID")) || 
                       Environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode";
        
        if (isVSCode)
        {
            // VS Code: Show ASCII art only
            ShowAsciiArtBanner(repoName);
        }
        else
        {
            // Terminal: Try to show image, fallback to ASCII art if needed
            try
            {
                ShowImageBanner(repoName);
            }
            catch (Exception ex)
            {
                // Fallback to ASCII art if image loading fails
                AnsiConsole.MarkupLine($"[dim]Note: Could not load image ({ex.GetType().Name}), showing ASCII art instead[/]");
                ShowAsciiArtBanner(repoName);
            }
        }
    }

    private void ShowAsciiArtBanner(string repoName)
    {
        var panel = new Panel(
            Align.Left(
                new Markup($@"[cyan]
       ██╗      ██████╗ ██╗  ██╗
       ╚██╗     ██╔══██╗██║ ██╔╝
        ╚██╗    ██████╔╝█████╔╝ 
        ██╔╝    ██╔══██╗██╔═██╗ 
       ██╔╝     ██████╔╝██║  ██╗
       ╚═╝      ╚═════╝ ╚═╝  ╚═╝[/]
       
       [white]{repoName}[/]
       [dim]C# Script Edition[/]
")))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.DeepSkyBlue3),
            Expand = true
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private void ShowImageBanner(string repoName)
    {
        // Look for logo file in repository root (current directory is set to repoRoot)
        var logoPath = "bITDevKit_Logo_dark.png";
        var absoluteLogoPath = Path.GetFullPath(logoPath);
        
        if (!File.Exists(absoluteLogoPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Note: Logo file not found at {absoluteLogoPath}, showing ASCII art instead[/]");
            ShowAsciiArtBanner(repoName);
            return;
        }

        try
        {
            // Create CanvasImage using the fluent API
            var image = new CanvasImage(absoluteLogoPath)
                .MaxWidth(40)
                .BicubicResampler();

            // Create layout with image on left and text on right
            var grid = new Grid()
                .AddColumn(new GridColumn().Width(45))
                .AddColumn(new GridColumn().Width(30));

            var info = new Panel(
                Align.Left(
                    new Markup($@"[white]{repoName}[/]
[dim]C# Script Edition[/]
[dim]bITdevKit Example Project[/]")))
            {
                Border = BoxBorder.None,
                Padding = new Padding(1, 0, 0, 0)
            };

            grid.AddRow(image, info);
            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            // If image rendering fails, fallback to ASCII art
            AnsiConsole.MarkupLine($"[dim]Image display failed ({ex.GetType().Name}), falling back to ASCII art[/]");
            ShowAsciiArtBanner(repoName);
        }
    }

    private string ShowCategoryMenu()
    {
        var categories = _tasksByCategory.Keys.ToList();
        categories.Add("✕ Exit");

        var prompt = new SelectionPrompt<string>()
            .Title("[cyan]Select a category:[/]")
            .PageSize(10)
            .AddChoices(categories);
        
        prompt.SearchEnabled = true;
        prompt.WrapAround = true;

        var selection = AnsiConsole.Prompt(prompt);

        return selection == "✕ Exit" ? null : selection;
    }

    private BdkTask ShowTaskMenu(string category)
    {
        var tasks = _tasksByCategory[category];
        var choices = tasks.Select(t => $"{t.Label} - {t.Description}").ToList();
        choices.Add("← Back");
        choices.Add("✕ Exit");

        var prompt = new SelectionPrompt<string>()
            .Title($"[cyan]{category}:[/]")
            .PageSize(15)
            .AddChoices(choices);
        
        prompt.SearchEnabled = true;
        prompt.WrapAround = true;

        var selection = AnsiConsole.Prompt(prompt);

        if (selection == "← Back")
            return null;
        
        if (selection == "✕ Exit")
            return new BdkTask { Key = "exit" };

        var selectedIndex = choices.IndexOf(selection);
        return tasks[selectedIndex];
    }

    private async Task ExecuteTaskAsync(BdkTask task)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════════[/]");
        AnsiConsole.MarkupLine($"[cyan bold]Task:[/] {task.Label}");
        AnsiConsole.MarkupLine($"[dim]{task.Description}[/]");
        AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════════[/]");
        AnsiConsole.WriteLine();

        var result = await task.Execute(_context);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════════[/]");
        
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓ Task completed successfully[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Task failed with exit code {result.ExitCode}[/]");
        }
        
        AnsiConsole.MarkupLine($"[dim]Duration: {result.Duration.TotalMilliseconds:F0}ms[/]");
        AnsiConsole.MarkupLine($"[cyan]═══════════════════════════════════════════[/]");
        AnsiConsole.WriteLine();
    }
}

// ============================================================================
// MAIN ENTRY POINT
// ============================================================================

// Get script location - dotnet-script provides this via __SCRIPT_FILE__ or we use the current directory
var scriptPath = Environment.GetEnvironmentVariable("__SCRIPT_FILE__") ?? typeof(object).Assembly.Location;
var scriptDirectory = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory();

// If scriptDirectory doesn't contain bdk-cli.csx, we're likely in the wrong location
// Fall back to assuming we're already at repo root
if (!File.Exists(Path.Combine(scriptDirectory, "bdk-cli.csx")))
{
    scriptDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tools", "bdk-cli-csx");
}

// Navigate to repository root (assuming script is in tools/bdk-cli-csx)
var repoRoot = Path.GetFullPath(Path.Combine(scriptDirectory, "../.."));
Directory.SetCurrentDirectory(repoRoot);

// Load configuration from the script directory
var envPath = Path.Combine(scriptDirectory, ".env");
var config = BdkConfig.LoadFromEnv(envPath);

// Initialize infrastructure
var executor = new CommandExecutor(repoRoot);
var dotnetCli = new DotnetCli(executor, config, repoRoot);
var context = new TaskContext
{
    Config = config,
    DotnetCli = dotnetCli,
    Executor = executor
};

// Parse arguments
var args = Args.ToList();

if (args.Count == 0)
{
    // Interactive mode
    var ui = new BdkUI(context);
    await ui.RunInteractiveAsync();
}
else if (args[0] == "--help" || args[0] == "-h")
{
    // Show help
    var table = new Table();
    table.Border(TableBorder.Rounded);
    table.AddColumn(new TableColumn("[cyan]Task[/]").Width(20));
    table.AddColumn(new TableColumn("[cyan]Description[/]").Width(50));
    table.AddColumn(new TableColumn("[cyan]Category[/]").Width(25));

    foreach (var task in TaskRegistry.GetAllTasks().OrderBy(t => t.Category).ThenBy(t => t.Key))
    {
        table.AddRow(task.Key, task.Description, task.Category);
    }

    AnsiConsole.Write(new Rule("[cyan]BDK CLI - Available Tasks[/]"));
    AnsiConsole.WriteLine();
    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
    Console.WriteLine("Usage: dotnet script bdk-cli.csx [task-name]");
    Console.WriteLine("       dotnet script bdk-cli.csx          (interactive mode)");
}
else
{
    // Direct execution mode
    var taskKey = args[0];
    var task = TaskRegistry.GetAllTasks().FirstOrDefault(t => t.Key == taskKey);

    if (task == null)
    {
        AnsiConsole.MarkupLine($"[red]Error: Unknown task '{taskKey}'[/]");
        AnsiConsole.MarkupLine($"[dim]Run 'dotnet script bdk-cli.csx --help' to see available tasks[/]");
        Environment.Exit(1);
    }

    AnsiConsole.MarkupLine($"[cyan]Executing:[/] {task.Label}");
    AnsiConsole.WriteLine();

    var result = await task.Execute(context);

    AnsiConsole.WriteLine();
    if (result.Success)
    {
        AnsiConsole.MarkupLine($"[green]✓ Completed in {result.Duration.TotalMilliseconds:F0}ms[/]");
        Environment.Exit(0);
    }
    else
    {
        AnsiConsole.MarkupLine($"[red]✗ Failed with exit code {result.ExitCode}[/]");
        Environment.Exit(result.ExitCode);
    }
}
