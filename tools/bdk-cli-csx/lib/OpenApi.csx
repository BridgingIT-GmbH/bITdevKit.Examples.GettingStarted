// BDK CLI - API & Spec Module
/// <summary>
/// OpenAPI specification tasks (linting, client generation, HTTP requests)
/// </summary>

using Spectre.Console;

public static class OpenApiUtils
{
    public static async Task<ExecutionResult> LintOpenApiAsync(TaskContext ctx, string specPath = "src/Presentation.Web.Server/wwwroot/openapi.json", string rulesetPath = ".spectral.yaml", string failSeverity = "error", string format = "stylish")
    {
        var startTime = DateTime.Now;
        
        try
        {
            var fullSpecPath = Path.Combine(ctx.RootDir, specPath);
            var fullRulesetPath = string.IsNullOrEmpty(rulesetPath) ? "" : Path.Combine(ctx.RootDir, rulesetPath);
            
            if (!File.Exists(fullSpecPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: OpenAPI spec not found: {Markup.Escape(specPath)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var validSeverities = new[] { "error", "warn", "info", "hint", "off" };
            if (!validSeverities.Contains(failSeverity.ToLower()))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid -FailSeverity '{failSeverity}'. Valid: {string.Join(", ", validSeverities)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 3, Duration = DateTime.Now - startTime };
            }
            
            var validFormats = new[] { "stylish", "json", "text" };
            if (!validFormats.Contains(format.ToLower()))
            {
                AnsiConsole.MarkupLine($"[red]Error: Invalid -Format '{format}'. Valid: {string.Join(", ", validFormats)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 4, Duration = DateTime.Now - startTime };
            }
            
            var dockerImage = "stoplight/spectral:latest";
            
            AnsiConsole.MarkupLine($"[cyan]Spec path:[/] {Markup.Escape(fullSpecPath)}[/]");
            AnsiConsole.MarkupLine($"[cyan]Fail severity:[/] {failSeverity}[/]");
            AnsiConsole.MarkupLine($"[cyan]Output format:[/] {format}[/]");
            
            var args = $"run --rm -v \"{ctx.RootDir}:/work\" {dockerImage} lint \"/work/{specPath}\" --format {format} --fail-severity {failSeverity}";
            
            if (!string.IsNullOrEmpty(fullRulesetPath) && File.Exists(fullRulesetPath))
            {
                args += $" -r \"/work/{rulesetPath}\"";
            }
            
            var result = await ctx.Executor.ExecuteAsync("docker", args);
            
            if (result.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]Spectral lint failed (exit code {result.ExitCode})[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]OpenAPI lint succeeded with no violations above threshold[/]");
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
            AnsiConsole.MarkupLine($"[red]Error linting OpenAPI spec: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> GenerateDotNetClientAsync(TaskContext ctx, string specPath = "src/Presentation.Web.Server/wwwroot/openapi.json", string clientClassName = "ApiClient", string ns = "OpenApi.Client")
    {
        var startTime = DateTime.Now;
        
        try
        {
            var fullSpecPath = Path.Combine(ctx.RootDir, specPath);
            
            if (!File.Exists(fullSpecPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: OpenAPI spec not found: {Markup.Escape(specPath)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "openapi", "dotnet");
            Directory.CreateDirectory(outDir);
            
            var args = $"kiota generate -d \"{fullSpecPath}\" -l CSharp -o \"{outDir}\" --log-level Error -c {clientClassName} -n {ns}";
            
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("Kiota generation failed");
            }
            
            var fileCount = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).Length;
            AnsiConsole.MarkupLine($"[green]Kiota C# client generated ({fileCount} files)[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating C# client: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> GenerateTypeScriptClientAsync(TaskContext ctx, string specPath = "src/Presentation.Web.Server/wwwroot/openapi.json", string clientClassName = "ApiClient")
    {
        var startTime = DateTime.Now;
        
        try
        {
            var fullSpecPath = Path.Combine(ctx.RootDir, specPath);
            
            if (!File.Exists(fullSpecPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: OpenAPI spec not found: {Markup.Escape(specPath)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "openapi", "typescript");
            Directory.CreateDirectory(outDir);
            
            var args = $"kiota generate -d \"{fullSpecPath}\" -l TypeScript -o \"{outDir}\" --log-level Error -c {clientClassName}";
            
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("Kiota generation failed");
            }
            
            var fileCount = Directory.GetFiles(outDir, "*", SearchOption.AllDirectories).Length;
            AnsiConsole.MarkupLine($"[green]Kiota TypeScript client generated ({fileCount} files)[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating TypeScript client: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> GenerateHttpRequestFilesAsync(TaskContext ctx, string specPath = "src/Presentation.Web.Server/wwwroot/openapi.json", string baseUrl = "https://localhost:5001", string outputType = "OneFilePerTag")
    {
        var startTime = DateTime.Now;
        
        try
        {
            var fullSpecPath = Path.Combine(ctx.RootDir, specPath);
            
            if (!File.Exists(fullSpecPath))
            {
                AnsiConsole.MarkupLine($"[red]Error: OpenAPI spec not found: {Markup.Escape(specPath)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "openapi", "http");
            Directory.CreateDirectory(outDir);
            
            AnsiConsole.MarkupLine($"[cyan]Spec:[/] {Markup.Escape(fullSpecPath)}[/]");
            AnsiConsole.MarkupLine($"[cyan]Output:[/] {Markup.Escape(outDir)}[/]");
            AnsiConsole.MarkupLine($"[cyan]BaseUrl:[/] {baseUrl}[/]");
            AnsiConsole.MarkupLine($"[cyan]OutputType:[/] {outputType}[/]");
            
            var args = $"httpgenerator \"{fullSpecPath}\" --base-url {baseUrl} --output \"{outDir}\" --authorization-header \"Bearer TOKEN\" --output-type {outputType}";
            
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("httpgenerator failed");
            }
            
            var httpFiles = Directory.GetFiles(outDir, "*.http", SearchOption.AllDirectories);
            var count = httpFiles.Length;
            AnsiConsole.MarkupLine($"[green]Generated {count} .http file(s) in {Markup.Escape(outDir)}[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating HTTP request files: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
}
