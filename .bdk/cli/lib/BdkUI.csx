// BDK CLI - User Interface Module
/// <summary>
/// Interactive UI screens for task selection and execution
/// </summary>

using Spectre.Console;

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
        Console.Clear();
        
        // Discover modules and auto-select if only one exists
        _context.AvailableModules = Prompts.DiscoverModules();
        _context.SelectedModule = Prompts.AutoSelectModule();
        
        ShowStartupBanner();
        
        await SelectSolutionFileAsync();
        
        // If no solution selected, exit
        if (string.IsNullOrEmpty(_context.SolutionFile))
        {
            AnsiConsole.MarkupLine("[yellow]No solution selected. Exiting.[/]");
            return;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[dim]Use [cyan]↑↓[/] to navigate, [cyan]type[/] to search, [cyan]Enter[/] to select[/]");
        
        while (true)
        {
            var category = ShowCategoryMenu();
            if (category == null || category == "✕ Exit")
                break;

            var shouldExit = await ShowTaskMenuLoopAsync(category);
            if (shouldExit)
                break;
        }
    }

    private async Task SelectSolutionFileAsync()
    {
        var selected = await Prompts.SelectSolutionAsync(_context);
        
        if (string.IsNullOrEmpty(selected))
        {
            AnsiConsole.MarkupLine("[yellow]No solution selected or cancelled[/]");
            return;
        }
        
        _context.SolutionFile = selected;
    }

    private async Task<bool> ShowTaskMenuLoopAsync(string category)
    {
        while (true)
        {
            var task = ShowTaskMenu(category);
            if (task == null)
                return false;
            
            if (task.Key == "exit")
                return true;

            await ExecuteTaskAsync(task);
        }
    }

    private void ShowStartupBanner()
    {
        var repoName = Path.GetFileName(Directory.GetCurrentDirectory());
        
        var figlet = new FigletText("BDK")
        {
            Color = Color.Cyan,
            Justification = Justify.Center
        };

//   █▀▀▄ █▀▀▄ █ █▀
//   █▀▀▄ █  █ █▀▄ 
//   ▀▀▀  ▀▀▀  ▀ ▀
        
        var grid = new Grid().AddColumn().AddColumn().AddColumn();
        grid
            .AddRow(figlet, 
                new Markup("[bold cyan]bITdevKit[/]\n[dim]C# Script Edition[/]"));
        
        var panel = new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan),
            Padding = new Padding(1, 0, 1, 0)
        };
        
        AnsiConsole.Write(panel);
        
        // Display available modules below banner
        if (_context.AvailableModules.Count > 0)
        {
            AnsiConsole.Markup("[dim]Modules:[/]");
            
            foreach (var module in _context.AvailableModules)
            {
                if (module == _context.SelectedModule)
                {
                    AnsiConsole.Markup($"  [green]✓[/] [cyan]{module}[/]");
                }
                else
                {
                    AnsiConsole.Markup($"    [dim]{module}[/]");
                }
            }
        }
        
        //AnsiConsole.MarkupLine("[dim]Use [cyan]↑↓[/] to navigate, [cyan]type[/] to search, [cyan]Enter[/] to select[/]");
        AnsiConsole.WriteLine();
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
        var tasks = _tasksByCategory[category]
            .OrderBy(t => t.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var labelWidth = tasks.Count == 0 ? 0 : tasks.Max(t => t.Label.Length);
        var choices = tasks
            .Select(t => $"{t.Label.PadRight(labelWidth)} - {t.Description}")
            .ToList();
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
