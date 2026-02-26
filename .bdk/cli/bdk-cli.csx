#!/usr/bin/env dotnet-script
// BDK CLI - C# Script Version
// Cross-platform CLI for bITdevKit development tasks
// Usage: dotnet script bdk-cli.csx [command]
//        dotnet script bdk-cli.csx --help

#r "nuget: Spectre.Console, 0.54.0"

#load "lib/BdkConfig.csx"
#load "lib/CommandExecutor.csx"
#load "lib/TaskContext.csx"
#load "lib/DotnetCli.csx"
#load "lib/DockerCli.csx"
#load "lib/Prompts.csx"
#load "lib/TaskRegistry.csx"
#load "lib/BdkUI.csx"
#load "lib/Utils.csx"
#load "lib/Diagnostics.csx"
#load "lib/Security.csx"
#load "lib/OpenApi.csx"
#load "lib/MiscUtils.csx"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spectre.Console;

var scriptPath = Environment.GetEnvironmentVariable("__SCRIPT_FILE__") ?? typeof(object).Assembly.Location;
var scriptDirectory = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory();

if (!File.Exists(Path.Combine(scriptDirectory, "bdk-cli.csx")))
{
    scriptDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".bdk", "cli");
}

var repoRoot = Path.GetFullPath(Path.Combine(scriptDirectory, "../.."));
Directory.SetCurrentDirectory(repoRoot);

var envPath = Path.Combine(scriptDirectory, ".env");
var config = BdkConfig.LoadFromEnv(envPath);

var executor = new CommandExecutor(repoRoot);
var dotnetCli = new DotnetCli(executor, config, repoRoot);
var dockerCli = new DockerCli(config, executor);

// Initialize module information
var availableModules = Prompts.DiscoverModules();
var selectedModule = Prompts.AutoSelectModule();

var context = new TaskContext
{
    Config = config,
    DotnetCli = dotnetCli,
    DockerCli = dockerCli,
    Executor = executor,
    SolutionFile = dotnetCli.SolutionFile,
    SolutionPath = dotnetCli.SolutionFile,
    RootDir = repoRoot,
    OutputDir = config.OutputDirectory ?? ".artifacts",
    TraceNoView = Environment.GetEnvironmentVariable("TRACE_NO_VIEW") ?? "",
    AvailableModules = availableModules,
    SelectedModule = selectedModule
};

var args = Args.ToList();

if (args.Count == 0)
{
    var ui = new BdkUI(context);
    await ui.RunInteractiveAsync();
}
else if (args[0] == "help" || args[0] == "-h")
    {
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
        Console.WriteLine("       dotnet script bdk-cli.csx help     (show this help)");
    }
else
{
    var taskKey = args[0];
    var task = TaskRegistry.GetAllTasks().FirstOrDefault(t => t.Key == taskKey);

    if (task == null)
    {
        AnsiConsole.MarkupLine($"[red]Error: Unknown task '{taskKey}'[/]");
        AnsiConsole.MarkupLine($"[dim]Run 'dotnet script bdk-cli.csx --help' to see available tasks[/]");
        Environment.Exit(1);
    }

    // Display selected module
    if (!string.IsNullOrEmpty(context.SelectedModule))
    {
        AnsiConsole.MarkupLine($"[green]✓ Module:[/] [cyan]{context.SelectedModule}[/]");
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
