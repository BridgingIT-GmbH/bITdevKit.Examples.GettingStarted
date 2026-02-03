// BDK CLI - Security & Compliance Module
/// <summary>
/// Security and compliance utilities (vulnerabilities, outdated packages, licenses)
/// </summary>

using Spectre.Console;

public static class SecurityUtils
{
    public static async Task<ExecutionResult> ListVulnerablePackagesAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var solution = ctx.SolutionPath;
            if (string.IsNullOrEmpty(solution))
            {
                AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Checking for vulnerable packages in:[/] {Markup.Escape(solution)}");
            
            var args = $"list \"{solution}\" package --vulnerable";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]No vulnerable packages found[/]");
            }
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error checking vulnerabilities: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> ListVulnerablePackagesDeepAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var solution = ctx.SolutionPath;
            if (string.IsNullOrEmpty(solution))
            {
                AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Checking for vulnerable packages (including transitive) in:[/] {Markup.Escape(solution)}");
            
            var args = $"list \"{solution}\" package --vulnerable --include-transitive";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]No vulnerable packages found[/]");
            }
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error checking vulnerabilities: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> ListOutdatedPackagesAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var solution = ctx.SolutionPath;
            if (string.IsNullOrEmpty(solution))
            {
                AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Checking for outdated packages in:[/] {Markup.Escape(solution)}");
            
            var args = $"list \"{solution}\" package --outdated";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode == 0)
            {
                AnsiConsole.MarkupLine("[green]Outdated packages check complete[/]");
            }
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error checking outdated packages: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> ListOutdatedPackagesJsonAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var solution = ctx.SolutionPath;
            if (string.IsNullOrEmpty(solution))
            {
                AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "compliance");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outFile = Path.Combine(outDir, $"outdated_{timestamp}.json");
            
            AnsiConsole.MarkupLine($"[cyan]Collecting outdated packages (JSON) -> {Markup.Escape(outFile)}[/]");
            
            var args = $"list \"{solution}\" package --outdated";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args, captureOutput: true);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("dotnet list outdated failed");
            }
            
            var lines = result.Output.Split('\n');
            var pkgs = new List<object>();
            
            foreach (var line in lines)
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @">\s*(?<name>[^\s]+)\s+(?<current>\S+)\s+(?<wanted>\S+)\s+(?<latest>\S+)");
                if (match.Success)
                {
                    pkgs.Add(new
                    {
                        name = match.Groups["name"].Value,
                        current = match.Groups["current"].Value,
                        wanted = match.Groups["wanted"].Value,
                        latest = match.Groups["latest"].Value
                    });
                }
            }
            
            var json = System.Text.Json.JsonSerializer.Serialize(pkgs, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outFile, json);
            
            AnsiConsole.MarkupLine($"[green]Outdated packages captured: {pkgs.Count}[/]");
            AnsiConsole.MarkupLine($"[green]Output written to: {Markup.Escape(outFile)}[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error checking outdated packages (JSON): {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> GenerateLicenseReportAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var solution = ctx.SolutionPath;
            if (string.IsNullOrEmpty(solution))
            {
                AnsiConsole.MarkupLine("[red]Error: No solution file found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "compliance");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var mdFile = Path.Combine(outDir, $"licenses_{timestamp}.md");
            var jsonFile = Path.Combine(outDir, $"licenses_{timestamp}.json");
            
            AnsiConsole.MarkupLine("[cyan]Generating license report[/]");
            
            var args = $"tool run nuget-license -i \"{solution}\" -t -o JsonPretty";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args, captureOutput: true);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("nuget-license failed");
            }
            
            var parseSource = result.Output.TrimStart();
            if (!parseSource.StartsWith("[") && !parseSource.StartsWith("{"))
            {
                throw new Exception($"nuget-license did not return JSON output. Raw: {parseSource}");
            }
            
            var data = System.Text.Json.JsonSerializer.Deserialize<List<LicensePackage>>(parseSource);
            if (data == null)
            {
                throw new Exception("Failed to parse nuget-license JSON output");
            }
            
            var rows = new List<string>
            {
                "| Package | Version | License | LicenseUrl |",
                "|---------|---------|---------|-----------|"
            };
            
            var licenseStats = new Dictionary<string, int>();
            var jsonList = new List<object>();
            
            foreach (var pkg in data)
            {
                var name = pkg.PackageId;
                var ver = pkg.PackageVersion;
                var licRaw = pkg.License ?? "(unknown)";
                var licUrl = pkg.LicenseUrl ?? "(none)";
                var lic = licRaw.Length > 120 || licRaw.Contains("\n") ? "(Embedded License Text)" : licRaw;
                
                rows.Add($"| {name} | {ver} | {lic} | {licUrl} |");
                
                if (licenseStats.ContainsKey(lic))
                    licenseStats[lic]++;
                else
                    licenseStats[lic] = 1;
                
                jsonList.Add(new { package = name, version = ver, license = lic, licenseUrl = licUrl });
            }
            
            var total = jsonList.Count;
            var unknownCount = jsonList.Count(j => ((dynamic)j).license == "(unknown)");
            
            var summaryLines = new List<string>
            {
                "",
                "## License Summary",
                $"Total packages: {total}",
                $"Unknown licenses: {unknownCount}",
                "Top licenses:"
            };
            
            foreach (var key in licenseStats.Keys.OrderBy(k => k))
            {
                summaryLines.Add($"  - {key}: {licenseStats[key]}");
            }
            
            File.WriteAllLines(mdFile, rows.Concat(summaryLines));
            
            var jsonObj = new
            {
                generated = DateTime.Now.ToString("o"),
                total = total,
                unknown = unknownCount,
                licenses = licenseStats,
                packages = jsonList
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(jsonObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonFile, json);
            
            AnsiConsole.MarkupLine("[green]License reports created with nuget-license:[/]");
            AnsiConsole.MarkupLine($"[dim]  Markdown:   {Markup.Escape(mdFile)}[/]");
            AnsiConsole.MarkupLine($"[dim]  JSON    : {Markup.Escape(jsonFile)}[/]");
            
            Utils.OpenFile(mdFile);
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating license report: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    private class LicensePackage
    {
        public string PackageId { get; set; } = "";
        public string PackageVersion { get; set; } = "";
        public string License { get; set; } = "";
        public string LicenseUrl { get; set; } = "";
    }
}
