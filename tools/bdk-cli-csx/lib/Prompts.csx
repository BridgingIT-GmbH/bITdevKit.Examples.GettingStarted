// BDK CLI - Prompts Module
/// <summary>
/// Reusable prompt utilities for user selection (modules, projects, solutions, RIDs, etc.)
/// </summary>

using Spectre.Console;

public static class Prompts
{
    /// <summary>
    /// Prompts the user to select a project file (.csproj) from the solution
    /// </summary>
    /// <param name="context">Task context containing solution info</param>
    /// <param name="promptTitle">Custom title for the prompt</param>
    /// <param name="defaultProject">Default project to use in non-interactive mode</param>
    /// <returns>Selected project path relative to repo root, or empty string if cancelled</returns>
    public static async Task<string> SelectProjectAsync(TaskContext context, string promptTitle = "Select a project:", string defaultProject = "")
    {
        // Check for non-interactive mode
        if (!Console.IsInputRedirected && Environment.GetEnvironmentVariable("NON_INTERACTIVE") != "1")
        {
            // Interactive mode - find all .csproj files
            var projects = FindAllProjects(context);
            
            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No .csproj files found in solution[/]");
                return "";
            }
            
            if (projects.Count == 1)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] [dim]Project:[/] {projects[0]}");
                return projects[0];
            }
            
            var choices = new List<string>(projects)
            {
                "✕ Cancel"
            };
            
            var prompt = new SelectionPrompt<string>()
                .Title($"[cyan]{promptTitle}[/]")
                .PageSize(15)
                .AddChoices(choices);
            
            prompt.SearchEnabled = true;
            prompt.WrapAround = true;
            
            var selected = AnsiConsole.Prompt(prompt);
            
            if (selected == "✕ Cancel")
            {
                AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
                return "";
            }
            
            AnsiConsole.MarkupLine($"[green]✓ Selected:[/] {selected}");
            return selected;
        }
        else
        {
            // Non-interactive mode
            if (!string.IsNullOrEmpty(defaultProject))
            {
                AnsiConsole.MarkupLine($"[dim]Using default project: {defaultProject}[/]");
                return defaultProject;
            }
            
            var projects = FindAllProjects(context);
            if (projects.Count > 0)
            {
                AnsiConsole.MarkupLine($"[dim]Using first project: {projects[0]}[/]");
                return projects[0];
            }
            
            AnsiConsole.MarkupLine("[yellow]Warning: No project specified and none found[/]");
            return "";
        }
    }
    
    /// <summary>
    /// Finds all .csproj files in the solution
    /// </summary>
    private static List<string> FindAllProjects(TaskContext context)
    {
        var projects = new List<string>();
        var repoRoot = Directory.GetCurrentDirectory();
        
        // Search in src/ and tests/ directories
        var searchDirs = new[] { "src", "tests" };
        
        foreach (var dir in searchDirs)
        {
            var dirPath = Path.Combine(repoRoot, dir);
            if (!Directory.Exists(dirPath))
                continue;
                
            projects.AddRange(
                Directory.GetFiles(dirPath, "*.csproj", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(repoRoot, f)
                        .Replace("\\", "/"))
            );
        }
        
        return projects.OrderBy(p => p).ToList();
    }
    
    /// <summary>
    /// Prompts the user for text input with optional default value
    /// </summary>
    public static async Task<string> PromptTextAsync(string title, string defaultValue = "")
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            var prompt = new TextPrompt<string>($"[cyan]{title}[/]")
                .DefaultValue(defaultValue);
            return AnsiConsole.Prompt(prompt);
        }
        else
        {
            var prompt = new TextPrompt<string>($"[cyan]{title}[/]")
                .AllowEmpty();
            return AnsiConsole.Prompt(prompt);
        }
    }
    
    /// <summary>
    /// Prompts the user for confirmation
    /// </summary>
    public static bool ConfirmAsync(string message, bool defaultValue = false)
    {
        return AnsiConsole.Confirm($"[cyan]{message}[/]", defaultValue);
    }
    
    /// <summary>
    /// Generic selection from a list of options with cancel support
    /// </summary>
    /// <param name="title">Prompt title</param>
    /// <param name="options">List of options to choose from</param>
    /// <param name="defaultValue">Default value for non-interactive mode</param>
    /// <returns>Selected option, or empty string if cancelled</returns>
    public static async Task<string> SelectFromListAsync(string title, List<string> options, string defaultValue = "")
    {
        if (!Console.IsInputRedirected && Environment.GetEnvironmentVariable("NON_INTERACTIVE") != "1")
        {
            if (options.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No options available[/]");
                return "";
            }
            
            if (options.Count == 1)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] [dim]Selection:[/] {options[0]}");
                return options[0];
            }
            
            var choices = new List<string>(options)
            {
                "✕ Cancel"
            };
            
            var prompt = new SelectionPrompt<string>()
                .Title($"[cyan]{title}[/]")
                .PageSize(15)
                .AddChoices(choices);
            
            prompt.SearchEnabled = true;
            prompt.WrapAround = true;
            
            var selected = AnsiConsole.Prompt(prompt);
            
            if (selected == "✕ Cancel")
            {
                AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
                return "";
            }
            
            AnsiConsole.MarkupLine($"[green]✓ Selected:[/] {selected}");
            return selected;
        }
        else
        {
            if (!string.IsNullOrEmpty(defaultValue))
            {
                AnsiConsole.MarkupLine($"[dim]Using default: {defaultValue}[/]");
                return defaultValue;
            }
            
            if (options.Count > 0)
            {
                AnsiConsole.MarkupLine($"[dim]Using first option: {options[0]}[/]");
                return options[0];
            }
            
            return "";
        }
    }
    
    /// <summary>
    /// Selects a solution file from the repository
    /// </summary>
    /// <param name="context">Task context</param>
    /// <returns>Selected solution file, or empty string if cancelled</returns>
    public static async Task<string> SelectSolutionAsync(TaskContext context)
    {
        if (!Console.IsInputRedirected && Environment.GetEnvironmentVariable("NON_INTERACTIVE") != "1")
        {
            var solutions = DotnetCli.FindAllSolutionFiles(Directory.GetCurrentDirectory());
            
            if (solutions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: No solution files (.sln or .slnx) found[/]");
                return "";
            }
            
            if (solutions.Count == 1)
            {
                AnsiConsole.MarkupLine($"[dim]Solution:[/] [green]✓[/] {solutions[0]}");
                return solutions[0];
            }
            
            var choices = new List<string>(solutions)
            {
                "✕ Cancel"
            };
            
            var prompt = new SelectionPrompt<string>()
                .Title("[cyan]Select a solution file:[/]")
                .PageSize(10)
                .AddChoices(choices);
            
            prompt.SearchEnabled = true;
            prompt.WrapAround = true;
            
            var selected = AnsiConsole.Prompt(prompt);
            
            if (selected == "✕ Cancel")
            {
                AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
                return "";
            }
            
            AnsiConsole.MarkupLine($"[green]✓ Selected:[/] {selected}");
            return selected;
        }
        else
        {
            var solutions = DotnetCli.FindAllSolutionFiles(Directory.GetCurrentDirectory());
            if (solutions.Count > 0)
            {
                AnsiConsole.MarkupLine($"[dim]Using first solution: {solutions[0]}[/]");
                return solutions[0];
            }
            
            return "";
        }
    }
    
    /// <summary>
    /// Selects a module from the solution
    /// </summary>
    /// <param name="context">Task context</param>
    /// <returns>Selected module name, or empty string if cancelled</returns>
    public static async Task<string> SelectModuleAsync(TaskContext context)
    {
        var modulesDir = Path.Combine(Directory.GetCurrentDirectory(), "src", "Modules");
        
        if (!Directory.Exists(modulesDir))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: Modules directory not found[/]");
            return "";
        }
        
        var modules = Directory.GetDirectories(modulesDir)
            .Select(Path.GetFileName)
            .OrderBy(m => m)
            .ToList();
        
        if (modules.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: No modules found[/]");
            return "";
        }
        
        var choices = new List<string>(modules)
        {
            "✕ Cancel"
        };
        
        var prompt = new SelectionPrompt<string>()
            .Title("[cyan]Select a module:[/]")
            .PageSize(10)
            .AddChoices(choices);
        
        prompt.SearchEnabled = true;
        prompt.WrapAround = true;
        
        var selected = AnsiConsole.Prompt(prompt);
        
        if (selected == "✕ Cancel")
        {
            AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
            return "";
        }
        
        AnsiConsole.MarkupLine($"[green]✓ Selected:[/] {selected}");
        return selected;
    }
    
    /// <summary>
    /// Selects a runtime identifier (RID) for cross-platform publishing
    /// </summary>
    /// <param name="defaultValue">Default RID for non-interactive mode</param>
    /// <returns>Selected RID, or empty string if cancelled</returns>
    public static async Task<string> SelectRidAsync(string defaultValue = "linux-x64")
    {
        var rids = new List<string>
        {
            "linux-x64",
            "linux-arm64",
            "win-x64",
            "win-arm64",
            "osx-x64",
            "osx-arm64"
        };
        
        return await SelectFromListAsync("Select runtime identifier (RID):", rids, defaultValue);
    }
    
    /// <summary>
    /// Selects build configuration (Debug or Release)
    /// </summary>
    /// <param name="defaultValue">Default configuration</param>
    /// <returns>Selected configuration, or empty string if cancelled</returns>
    public static async Task<string> SelectConfigurationAsync(string defaultValue = "Debug")
    {
        return await SelectFromListAsync("Select configuration:", ["Debug", "Release"], defaultValue);
    }
    
    /// <summary>
    /// Selects whether to create single-file executable
    /// </summary>
    /// <param name="defaultValue">Default value</param>
    /// <returns>True for single-file, False for multi-file, or null if cancelled</returns>
    public static async Task<bool?> SelectSingleFileAsync(bool defaultValue = false)
    {
        if (!Console.IsInputRedirected && Environment.GetEnvironmentVariable("NON_INTERACTIVE") != "1")
        {
            var choices = new List<string>
            {
                "No (multi-file)",
                "Yes (single-file)",
                "✕ Cancel"
            };
            
            var prompt = new SelectionPrompt<string>()
                .Title("[cyan]Create single-file executable?[/]")
                .PageSize(5)
                .AddChoices(choices);
            
            prompt.SearchEnabled = false;
            prompt.WrapAround = true;
            
            var selected = AnsiConsole.Prompt(prompt);
            
            if (selected == "✕ Cancel")
            {
                AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
                return null;
            }
            
            var isSingleFile = selected.StartsWith("Yes");
            // AnsiConsole.MarkupLine($"[green]✓ Selected:[/] {(isSingleFile ? "Single-file" : "Multi-file")}[/]");
            return isSingleFile;
        }
        else
        {
            var isSingleFile = defaultValue;
            // AnsiConsole.MarkupLine($"[dim]Using default: {(isSingleFile ? "Single-file" : "Multi-file")}[/]");
            return isSingleFile;
        }
    }
    
    /// <summary>
    /// Discovers all modules in the solution
    /// </summary>
    /// <returns>List of module names</returns>
    public static List<string> DiscoverModules()
    {
        var modules = new List<string>();
        var repoRoot = Directory.GetCurrentDirectory();
        var modulesDir = Path.Combine(repoRoot, "src", "Modules");
        
        if (!Directory.Exists(modulesDir))
            return modules;
        
        modules.AddRange(
            Directory.GetDirectories(modulesDir)
                .Select(Path.GetFileName)
                .Where(m => !string.IsNullOrEmpty(m) && m != "Common" && m != "Shared")
                .OrderBy(m => m)
        );
        
        return modules;
    }
    
    /// <summary>
    /// Auto-selects a module if only one exists
    /// </summary>
    /// <returns>Selected module name, or empty string if none or multiple</returns>
    public static string AutoSelectModule()
    {
        var modules = DiscoverModules();
        
        if (modules.Count == 1)
        {
            return modules[0];
        }
        
        return "";
    }
    
    /// <summary>
    /// Selects a module for task execution
    /// If a module is already selected in context and valid, returns it without prompting
    /// If multiple modules exist and none selected, prompts for selection
    /// </summary>
    /// <param name="context">Task context with available modules and selected module</param>
    /// <param name="promptTitle">Title for selection prompt</param>
    /// <returns>Selected module name, or empty string if cancelled</returns>
    public static async Task<string> SelectModuleForTaskAsync(TaskContext context, string promptTitle = "Select a module:")
    {
        var modules = context.AvailableModules;
        
        if (modules.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: No modules found[/]");
            return "";
        }
        
        // Auto-select if only one module exists
        if (modules.Count == 1)
        {
            var selectedModule = modules[0];
            context.SelectedModule = selectedModule;
            return selectedModule;
        }
        
        // If module already selected in context and valid, use it
        if (!string.IsNullOrEmpty(context.SelectedModule) && modules.Contains(context.SelectedModule))
        {
            return context.SelectedModule;
        }
        
        // Multiple modules exist, prompt for selection
        if (!Console.IsInputRedirected && Environment.GetEnvironmentVariable("NON_INTERACTIVE") != "1")
        {
            var choices = new List<string>(modules)
            {
                "✕ Cancel"
            };
            
            var prompt = new SelectionPrompt<string>()
                .Title($"[cyan]{promptTitle}[/]")
                .PageSize(10)
                .AddChoices(choices);
            
            prompt.SearchEnabled = true;
            prompt.WrapAround = true;
            
            var selected = AnsiConsole.Prompt(prompt);
            
            if (selected == "✕ Cancel")
            {
                AnsiConsole.MarkupLine("[yellow]Selection cancelled[/]");
                return "";
            }
            
            context.SelectedModule = selected;
            return selected;
        }
        else
        {
            // Non-interactive mode: use first module
            var firstModule = modules[0];
            context.SelectedModule = firstModule;
            return firstModule;
        }
    }
}
