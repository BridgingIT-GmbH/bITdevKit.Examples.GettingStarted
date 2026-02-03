// BDK CLI - Utility & Maintenance Module
/// <summary>
/// Utility and maintenance tasks (clean, docs, repl, browser, etc.)
/// </summary>

using System.Diagnostics;
using Spectre.Console;

public static class MiscUtils
{
    public static async Task<ExecutionResult> GenerateDocsAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Generating consolidated markdown documentation...[/]");
            
            var outputDir = Path.Combine(ctx.OutputDir, "docs");
            Directory.CreateDirectory(outputDir);
            
            var projects = Utils.FindFiles(ctx.RootDir, "*.csproj");
            
            if (projects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No projects found for documentation generation[/]");
                return new ExecutionResult { Success = true, ExitCode = 0, Duration = DateTime.Now - startTime };
            }
            
            foreach (var project in projects)
            {
                var projectName = Path.GetFileNameWithoutExtension(project);
                var projectDir = Path.GetDirectoryName(Path.Combine(ctx.RootDir, project));
                
                if (string.IsNullOrEmpty(projectDir))
                    continue;
                
                var mdFile = Path.Combine(outputDir, $"{projectName}.g.md");
                
                try
                {
                    var sourceFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".g.cs") && !f.EndsWith(".designer.cs") && !f.EndsWith(".generated.cs"));
                    
                    var mdContent = new List<string>();
                    mdContent.Add($"# {projectName}");
                    mdContent.Add("");
                    
                    foreach (var file in sourceFiles)
                    {
                        var relativePath = Path.GetRelativePath(projectDir, file).Replace("\\", "/");
                        mdContent.Add($"## {relativePath}");
                        mdContent.Add("```csharp");
                        var content = File.ReadAllText(file);
                        mdContent.Add(content);
                        mdContent.Add("```");
                        mdContent.Add("");
                    }
                    
                    File.WriteAllLines(mdFile, mdContent);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Failed to generate docs for {projectName}: {Markup.Escape(ex.Message)}[/]");
                }
            }
            
            AnsiConsole.MarkupLine($"[green]Documentation generated in:[/] {Markup.Escape(outputDir)}[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating docs: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CleanWorkspaceAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Cleaning workspace build artifacts...[/]");
            
            var patterns = new[] 
            { 
                "bin", "obj", "bld", "Backup", "_UpgradeReport_Files", 
                "Debug", "Release", "ipch", "node_modules", 
                ctx.OutputDir, ".tmp"
            };
            
            var deletedCount = 0;
            var repoRoot = ctx.RootDir;
            
            foreach (var pattern in patterns)
            {
                var dirs = Directory.GetDirectories(repoRoot, $"*{pattern}", SearchOption.AllDirectories);
                
                foreach (var dir in dirs)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        deletedCount++;
                    }
                    catch
                    {
                        // Skip directories that can't be deleted
                    }
                }
            }
            
            AnsiConsole.MarkupLine($"[green]Workspace cleaned. {deletedCount} directories removed[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error cleaning workspace: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> RemoveFileHeadersAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Removing MIT license headers from C# files...[/]");
            
            var srcPath = Path.Combine(ctx.RootDir, "src");
            var testsPath = Path.Combine(ctx.RootDir, "tests");
            var paths = new List<string>();
            
            if (Directory.Exists(srcPath))
                paths.AddRange(Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories));
            
            if (Directory.Exists(testsPath))
                paths.AddRange(Directory.GetFiles(testsPath, "*.cs", SearchOption.AllDirectories));
            
            var modifiedCount = 0;
            var headerPattern = @"^[\s]*//\s*MIT-License\s*$|//\s*Copyright\s+BridgingIT";
            
            foreach (var file in paths)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var lines = content.Split('\n');
                    var headerEndIndex = -1;
                    
                    for (var i = 0; i < lines.Length && i < 10; i++)
                    {
                        if (System.Text.RegularExpressions.Regex.IsMatch(lines[i], headerPattern))
                        {
                            headerEndIndex = i;
                            if (i + 1 < lines.Length && string.IsNullOrWhiteSpace(lines[i + 1]))
                                headerEndIndex = i + 1;
                            break;
                        }
                    }
                    
                    if (headerEndIndex >= 0)
                    {
                        var newContent = string.Join('\n', lines[(headerEndIndex + 1)..]);
                        File.WriteAllText(file, newContent);
                        modifiedCount++;
                    }
                }
                catch
                {
                    // Skip files that can't be processed
                }
            }
            
            AnsiConsole.MarkupLine($"[green]File header removal complete. {modifiedCount} files modified[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error removing file headers: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> RunCSharpReplAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Launching C# REPL (Ctrl+C to exit)...[/]");
            
            var args = $"tool run csharprepl";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error running REPL: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> KillDotnetProcessAsync(TaskContext ctx, int? processId = null, bool force = false)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var selectedPid = processId?.ToString();
            
            if (string.IsNullOrEmpty(selectedPid))
            {
                var processes = Utils.GetDotnetProcesses();
                
                if (processes.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No dotnet processes found[/]");
                    return new ExecutionResult { Success = true, ExitCode = 0, Duration = DateTime.Now - startTime };
                }
                
                var choices = processes.Select(p => p.DisplayName).ToList();
                choices.Add("✕ Cancel");
                
                var prompt = new SelectionPrompt<string>()
                    .Title("[cyan]Select .NET process to KILL:[/]")
                    .PageSize(15)
                    .AddChoices(choices);
                
                prompt.SearchEnabled = true;
                prompt.WrapAround = true;
                
                var selected = AnsiConsole.Prompt(prompt);
                
                if (selected == "✕ Cancel")
                {
                    AnsiConsole.MarkupLine("[yellow]Kill operation cancelled[/]");
                    return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
                }
                
                selectedPid = processes.First(p => p.DisplayName == selected).Id.ToString();
            }
            
            var process = Process.GetProcessById(int.Parse(selectedPid));
            
            if (process == null)
            {
                AnsiConsole.MarkupLine($"[yellow]Process with PID {selectedPid} no longer exists[/]");
                return new ExecutionResult { Success = true, ExitCode = 0, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Target:[/] {process.ProcessName} (PID: {selectedPid})[/]");
            
            if (!force)
            {
                var confirm = AnsiConsole.Confirm($"[yellow]Terminate process {selectedPid}?[/]", false);
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Kill operation cancelled[/]");
                    return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
                }
            }
            
            process.Kill();
            AnsiConsole.MarkupLine($"[green]Process {selectedPid} terminated[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error killing process: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> ShowMinVerAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Displaying MinVer semantic version...[/]");
            
            var args = $"minver -v d -p preview.0";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error running MinVer: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> OpenBrowserUrlAsync(TaskContext ctx, string url, string title)
    {
        var startTime = DateTime.Now;
        
        try
        {
            AnsiConsole.MarkupLine($"[cyan]Opening {title}:[/] {url}[/]");
            
            Utils.OpenUrl(url);
            
            AnsiConsole.MarkupLine($"[green]{title} opened.[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error opening browser: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
}

