// BDK CLI - Performance & Diagnostics Module
/// <summary>
/// Performance diagnostics and profiling utilities
/// </summary>

using System.Diagnostics;
using Spectre.Console;

public static class DiagnosticsUtils
{
    public static async Task<ExecutionResult> RunBenchmarksAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;

        try
        {
            var benchmarkProjects = Utils.FindFiles(ctx.RootDir, "*Benchmarks.csproj", recursive: true);

            if (benchmarkProjects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No benchmark project (*Benchmarks.csproj) found[/]");
                return new ExecutionResult { Success = true, ExitCode = 0, Duration = DateTime.Now - startTime };
            }

            var benchProject = benchmarkProjects.Count == 1 ? benchmarkProjects.First() : await Prompts.SelectBenchmarkProjectAsync(benchmarkProjects);

            if (string.IsNullOrEmpty(benchProject))
            {
                AnsiConsole.MarkupLine("[yellow]Benchmark selection cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }

            AnsiConsole.MarkupLine($"[cyan]Running benchmarks:[/] {Markup.Escape(benchProject)}");
            
            var args = $"run --project \"{benchProject}\" -c Release -- --filter \"*\" --anyCategories \"*\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[yellow]BenchmarkDotNet failed. Fallback to build + run.[/]");
                args = $"build \"{benchProject}\" -c Release";
                await ctx.Executor.ExecuteAsync("dotnet", args);
                args = $"run --project \"{benchProject}\" -c Release --no-build";
                result = await ctx.Executor.ExecuteAsync("dotnet", args);
            }
            
            ExportBenchmarkSummary(ctx, benchProject);
            
            return new ExecutionResult 
            { 
                Success = result.ExitCode == 0, 
                ExitCode = result.ExitCode, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error running benchmarks: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> RunSelectedBenchmarksAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var benchmarkProjects = Utils.FindFiles(ctx.RootDir, "*Benchmarks.csproj", recursive: true);
            
            if (benchmarkProjects.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No benchmark projects (*Benchmarks.csproj) found[/]");
                return new ExecutionResult { Success = true, ExitCode = 0, Duration = DateTime.Now - startTime };
            }
            
            var selected = await Prompts.SelectBenchmarkProjectAsync(benchmarkProjects);
            if (string.IsNullOrEmpty(selected))
            {
                AnsiConsole.MarkupLine("[yellow]Benchmark selection cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Running selected benchmarks:[/] {Markup.Escape(selected)}");
            
            var args = $"run --project \"{selected}\" -c Release -- --filter \"*\" --anyCategories \"*\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode == 0)
            {
                ExportBenchmarkSummary(ctx, selected);
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
            AnsiConsole.MarkupLine($"[red]Error running benchmarks: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CollectFlameTraceAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var durations = new Dictionary<string, string>
            {
                { "10 sec", "00:00:10" },
                { "30 sec", "00:00:30" },
                { "1 min", "00:01:00" },
                { "5 min", "00:05:00" }
            };
            
            var durationChoice = await Prompts.SelectDurationAsync(durations.Keys.ToList(), "10 sec");
            if (string.IsNullOrEmpty(durationChoice))
            {
                AnsiConsole.MarkupLine("[yellow]Flame trace cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var duration = durations[durationChoice];
            var processId = await Prompts.SelectProcessAsync("Select process to trace (flame)");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "diagnostics");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var traceFile = Path.Combine(outDir, $"trace_{processId}_{timestamp}.nettrace");
            var speedBase = Path.Combine(outDir, $"trace_{processId}_{timestamp}");
            
            AnsiConsole.MarkupLine($"[cyan]Collecting flame trace (SampleProfiler, {duration}) for PID {processId}...[/]");
            
            var args = $"trace collect --process-id {processId} --providers Microsoft-DotNETCore-SampleProfiler:1 --duration {duration} -o \"{traceFile}\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                AnsiConsole.MarkupLine("[yellow]SampleProfiler provider failed, retrying with default trace config...[/]");
                args = $"trace collect --process-id {processId} --duration {duration} -o \"{traceFile}\"";
                result = await ctx.Executor.ExecuteAsync("dotnet", args);
            }
            
            if (result.ExitCode != 0)
            {
                throw new Exception("Trace collection failed");
            }
            
            AnsiConsole.MarkupLine($"[cyan]Converting to speedscope...[/]");
            args = $"trace convert --format SpeedScope \"{traceFile}\" -o \"{speedBase}\"";
            result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("Trace conversion failed");
            }
            
            AnsiConsole.MarkupLine($"[green]Flame trace complete: {Markup.Escape(traceFile)}[/]");
            
            var speedFile = speedBase + ".speedscope.json";
            if (File.Exists(speedFile + ".speedscope.json"))
            {
                File.Move(speedFile + ".speedscope.json", speedFile, true);
            }
            
            AnsiConsole.MarkupLine($"[green]Speedscope file: {Markup.Escape(speedFile)}[/]");
            
            await OpenSpeedscopeAsync(ctx, speedFile);
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error collecting flame trace: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CollectCpuTraceAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var durations = new Dictionary<string, string>
            {
                { "10 sec", "00:00:10" },
                { "30 sec", "00:00:30" },
                { "1 min", "00:01:00" },
                { "5 min", "00:05:00" }
            };
            
            var durationChoice = await Prompts.SelectDurationAsync(durations.Keys.ToList(), "10 sec");
            if (string.IsNullOrEmpty(durationChoice))
            {
                AnsiConsole.MarkupLine("[yellow]CPU trace cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var duration = durations[durationChoice];
            var extended = await Prompts.SelectYesNoAsync("Include extended runtime providers?", false);
            if (extended == null)
            {
                AnsiConsole.MarkupLine("[yellow]CPU trace cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var processId = await Prompts.SelectProcessAsync("Select process for CPU trace");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "diagnostics");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var traceFile = Path.Combine(outDir, $"cpu_{processId}_{timestamp}.nettrace");
            var speedBase = Path.Combine(outDir, $"cpu_{processId}_{timestamp}");
            
            var providers = "Microsoft-DotNETCore-SampleProfiler:1,System.Runtime:4";
            if (extended.Value)
            {
                providers += ",Microsoft-DotNETCore-EventSource:5";
            }
            
            AnsiConsole.MarkupLine($"[cyan]Collecting CPU trace ({providers}, {duration}) for PID {processId}...[/]");
            
            var args = $"trace collect --process-id {processId} --providers \"{providers}\" --duration {duration} -o \"{traceFile}\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("CPU trace collection failed");
            }
            
            AnsiConsole.MarkupLine($"[cyan]Converting to speedscope...[/]");
            args = $"trace convert --format SpeedScope \"{traceFile}\" -o \"{speedBase}\"";
            result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("CPU trace conversion failed");
            }
            
            AnsiConsole.MarkupLine($"[green]CPU trace complete: {Markup.Escape(traceFile)}[/]");
            
            var speedFile = speedBase + ".speedscope.json";
            if (File.Exists(speedFile + ".speedscope.json"))
            {
                File.Move(speedFile + ".speedscope.json", speedFile, true);
            }
            
            AnsiConsole.MarkupLine($"[green]Speedscope file: {Markup.Escape(speedFile)}[/]");
            
            await OpenSpeedscopeAsync(ctx, speedFile);
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error collecting CPU trace: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CollectGcTraceAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var durations = new Dictionary<string, string>
            {
                { "10 sec", "00:00:10" },
                { "30 sec", "00:00:30" },
                { "1 min", "00:01:00" },
                { "5 min", "00:05:00" }
            };
            
            var durationChoice = await Prompts.SelectDurationAsync(durations.Keys.ToList(), "10 sec");
            if (string.IsNullOrEmpty(durationChoice))
            {
                AnsiConsole.MarkupLine("[yellow]GC trace cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var duration = durations[durationChoice];
            var processId = await Prompts.SelectProcessAsync("Select process for GC-focused trace");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "diagnostics");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var traceFile = Path.Combine(outDir, $"gc_{processId}_{timestamp}.nettrace");
            var speedBase = Path.Combine(outDir, $"gc_{processId}_{timestamp}");
            
            AnsiConsole.MarkupLine($"[cyan]Collecting GC-focused trace (SampleProfiler + System.Runtime, {duration}) for PID {processId}...[/]");
            
            var args = $"trace collect --process-id {processId} --providers \"Microsoft-DotNETCore-SampleProfiler:1,System.Runtime:4\" --duration {duration} -o \"{traceFile}\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("GC trace collection failed");
            }
            
            AnsiConsole.MarkupLine($"[cyan]Converting to speedscope...[/]");
            args = $"trace convert --format SpeedScope \"{traceFile}\" -o \"{speedBase}\"";
            result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("GC trace conversion failed");
            }
            
            AnsiConsole.MarkupLine($"[green]GC trace complete: {Markup.Escape(traceFile)}[/]");
            
            var speedFile = speedBase + ".speedscope.json";
            if (File.Exists(speedFile + ".speedscope.json"))
            {
                File.Move(speedFile + ".speedscope.json", speedFile, true);
            }
            
            AnsiConsole.MarkupLine($"[green]Speedscope file: {Markup.Escape(speedFile)}[/]");
            
            await OpenSpeedscopeAsync(ctx, speedFile);
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error collecting GC trace: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CreateHeapDumpAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var processId = await Prompts.SelectProcessAsync("Select process for heap dump");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "diagnostics");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var dumpFile = Path.Combine(outDir, $"heap_{processId}_{timestamp}.dmp");
            
            AnsiConsole.MarkupLine($"[cyan]Creating heap dump for PID {processId}...[/]");
            
            var args = $"dump collect --process-id {processId} --type full -o \"{dumpFile}\"";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("Heap dump failed");
            }
            
            AnsiConsole.MarkupLine($"[green]Heap dump created: {Markup.Escape(dumpFile)}[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error creating heap dump: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CollectGcStatsAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var processId = await Prompts.SelectProcessAsync("Select process for GC stats");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Sampling GC counters for PID {processId} (5s)...[/]");
            
            var args = $"counters monitor --process-id {processId} --counters System.Runtime --refresh-interval 1 --duration 5";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("GC stats collection failed");
            }
            
            AnsiConsole.MarkupLine("[green]GC sampling complete[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error collecting GC stats: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> CollectAspnetMetricsAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var processId = await Prompts.SelectProcessAsync("Select ASP.NET Core process for metrics");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Monitoring ASP.NET Core counters for PID {processId} (10s)...[/]");
            
            var args = $"counters monitor --process-id {processId} --counters Microsoft.AspNetCore.Hosting --refresh-interval 1 --duration 10";
            var result = await ctx.Executor.ExecuteAsync("dotnet", args);
            
            if (result.ExitCode != 0)
            {
                throw new Exception("ASP.NET metrics collection failed");
            }
            
            AnsiConsole.MarkupLine("[green]ASP.NET metrics sampling complete[/]");
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error collecting ASP.NET metrics: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> RunQuickDiagnosticsAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var processId = await Prompts.SelectProcessAsync("Select process for QUICK diagnostics");
            if (string.IsNullOrEmpty(processId))
            {
                AnsiConsole.MarkupLine("[yellow]No PID selected[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var outDir = Path.Combine(ctx.OutputDir, "diagnostics");
            Directory.CreateDirectory(outDir);
            
            var errors = new List<string>();
            
            // CPU trace (5s)
            try
            {
                var cpuFile = Path.Combine(outDir, $"cpuQuick_{processId}_{DateTime.Now:yyyyMMdd_HHmmss}.nettrace");
                AnsiConsole.MarkupLine($"[cyan][[Quick]] CPU trace (5s) for PID {processId}[/]");
                var args = $"trace collect --process-id {processId} --providers Microsoft-DotNETCore-SampleProfiler:1 --duration 00:00:05 -o \"{cpuFile}\"";
                var result = await ctx.Executor.ExecuteAsync("dotnet", args);
                
                if (result.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine($"[green][[Quick]] CPU trace saved: {Markup.Escape(cpuFile)}[/]");
                }
                else
                {
                    errors.Add("CPU trace failed");
                    AnsiConsole.MarkupLine($"[yellow][[Quick]] CPU trace error[/]");
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                AnsiConsole.MarkupLine($"[yellow][[Quick]] CPU trace error: {Markup.Escape(ex.Message)}[/]");
            }
            
            // GC trace (5s)
            try
            {
                var gcFile = Path.Combine(outDir, $"gcQuick_{processId}_{DateTime.Now:yyyyMMdd_HHmmss}.nettrace");
                AnsiConsole.MarkupLine($"[cyan][[Quick]] GC trace (5s) for PID {processId}[/]");
                var args = $"trace collect --process-id {processId} --providers \"Microsoft-DotNETCore-SampleProfiler:1,System.Runtime:4\" --duration 00:00:05 -o \"{gcFile}\"";
                var result = await ctx.Executor.ExecuteAsync("dotnet", args);
                
                if (result.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine($"[green][[Quick]] GC trace saved: {Markup.Escape(gcFile)}[/]");
                }
                else
                {
                    errors.Add("GC trace failed");
                    AnsiConsole.MarkupLine($"[yellow][[Quick]] GC trace error[/]");
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                AnsiConsole.MarkupLine($"[yellow][[Quick]] GC trace error: {Markup.Escape(ex.Message)}[/]");
            }
            
            // ASP.NET metrics (6s)
            try
            {
                AnsiConsole.MarkupLine($"[cyan][[Quick]] ASP.NET metrics (6s) for PID {processId}[/]");
                var args = $"counters monitor --process-id {processId} --counters Microsoft.AspNetCore.Hosting --refresh-interval 1 --duration 6";
                var result = await ctx.Executor.ExecuteAsync("dotnet", args);
                
                if (result.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine("[green][[Quick]] ASP.NET metrics sampling complete[/]");
                }
                else
                {
                    errors.Add("ASP.NET metrics failed");
                    AnsiConsole.MarkupLine($"[yellow][[Quick]] ASP.NET metrics error[/]");
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                AnsiConsole.MarkupLine($"[yellow][[Quick]] ASP.NET metrics error: {Markup.Escape(ex.Message)}[/]");
            }
            
            if (errors.Count > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Quick diagnostics finished with {errors.Count} error(s)[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"[dim] - {Markup.Escape(error)}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[green]Quick diagnostics completed successfully[/]");
            }
            
            return new ExecutionResult 
            { 
                Success = errors.Count == 0, 
                ExitCode = errors.Count > 0 ? 1 : 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error running quick diagnostics: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    public static async Task<ExecutionResult> ViewSpeedscopeAsync(TaskContext ctx)
    {
        var startTime = DateTime.Now;
        
        try
        {
            var diagDir = Path.Combine(ctx.OutputDir, "diagnostics");
            
            if (!Directory.Exists(diagDir))
            {
                AnsiConsole.MarkupLine($"[yellow]Diagnostics directory not found: {Markup.Escape(diagDir)}[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var profiles = Utils.FindFiles(diagDir, "*.speedscope.json", recursive: true);
            
            if (profiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No speedscope profiles found[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            var selected = await Prompts.SelectSpeedscopeProfileAsync(profiles);
            if (string.IsNullOrEmpty(selected))
            {
                AnsiConsole.MarkupLine("[yellow]Speedscope view cancelled[/]");
                return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
            }
            
            AnsiConsole.MarkupLine($"[cyan]Selected profile: {Markup.Escape(selected)}[/]");
            
            await OpenSpeedscopeAsync(ctx, selected);
            
            return new ExecutionResult 
            { 
                Success = true, 
                ExitCode = 0, 
                Duration = DateTime.Now - startTime 
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error viewing speedscope: {Markup.Escape(ex.Message)}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = DateTime.Now - startTime };
        }
    }
    
    private static void ExportBenchmarkSummary(TaskContext ctx, string benchmarkProjectPath)
    {
        try
        {
            if (string.IsNullOrEmpty(benchmarkProjectPath))
                return;
            
            var projectDir = Path.GetDirectoryName(benchmarkProjectPath) ?? "";
            var artifactsRoot = ctx.OutputDir;
            var artifactRoot = ResolveBenchmarkResultsDirectory(ctx, projectDir, artifactsRoot);

            if (string.IsNullOrEmpty(artifactRoot))
            {
                AnsiConsole.MarkupLine("[yellow]No BenchmarkDotNet results directory found[/]");
                return;
            }
            
            var outDir = Path.Combine(artifactsRoot, "benchmarks");
            Directory.CreateDirectory(outDir);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionDir = Path.Combine(outDir, timestamp);
            Directory.CreateDirectory(sessionDir);
            
            var copied = new List<string>();
            foreach (var file in Directory.GetFiles(artifactRoot))
            {
                var destFile = Path.Combine(sessionDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
                copied.Add(Path.GetFileName(file));
            }
            
            var csvFile = Directory.GetFiles(sessionDir, "*-report.csv").FirstOrDefault();
            var githubMdFile = Directory.GetFiles(sessionDir, "*-report-github.md").FirstOrDefault();
            
            var summary = new
            {
                project = benchmarkProjectPath,
                timestamp = timestamp,
                artifacts = copied,
                csvPath = csvFile ?? "",
                markdownGithubPath = githubMdFile ?? ""
            };
            
            var summaryFile = Path.Combine(sessionDir, "summary.json");
            File.WriteAllText(summaryFile, System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            
            AnsiConsole.MarkupLine($"[green]Benchmark summary exported: {Markup.Escape(summaryFile)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Failed exporting benchmark summary: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private static string ResolveBenchmarkResultsDirectory(TaskContext ctx, string projectDir, string artifactsRoot)
    {
        var candidates = new List<string>
        {
            Path.Combine(projectDir, "BenchmarkDotNet.Artifacts", "results"),
            Path.Combine(ctx.RootDir, "BenchmarkDotNet.Artifacts", "results")
        };

        // Benchmark host now writes to AppContext.BaseDirectory/BenchmarkDotNet.Artifacts.
        // This typically lands under <project>/bin/... and must be resolved dynamically.
        var binDir = Path.Combine(projectDir, "bin");
        if (Directory.Exists(binDir))
        {
            var internalCandidates = Directory.GetDirectories(binDir, "BenchmarkDotNet.Artifacts", SearchOption.AllDirectories)
                .Select(path => Path.Combine(path, "results"))
                .Where(Directory.Exists);
            candidates.AddRange(internalCandidates);
        }

        // Keep this as a backward-compatible fallback for older benchmark runs.
        candidates.Add(Path.Combine(artifactsRoot, "BenchmarkDotNet.Artifacts", "results"));

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .OrderByDescending(GetDirectoryFreshnessUtc)
            .FirstOrDefault() ?? "";
    }

    private static DateTime GetDirectoryFreshnessUtc(string directory)
    {
        try
        {
            var newestFile = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                .Select(File.GetLastWriteTimeUtc)
                .DefaultIfEmpty(Directory.GetLastWriteTimeUtc(directory))
                .Max();

            return newestFile;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
    
    private static async Task OpenSpeedscopeAsync(TaskContext ctx, string speedFile)
    {
        if (!string.IsNullOrEmpty(ctx.TraceNoView))
            return;
        
        try
        {
            if (!File.Exists(speedFile))
            {
                throw new Exception($"Speedscope file not found: {speedFile}");
            }
            
            var npxResult = await ctx.Executor.ExecuteAsync("which", "npx");
            
            if (npxResult.ExitCode == 0)
            {
                AnsiConsole.MarkupLine($"[cyan]Opening speedscope (npx) -> {Markup.Escape(speedFile)}[/]");
                await ctx.Executor.ExecuteAsync("npx", $"speedscope \"{speedFile}\"");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]npx not available; opening speedscope.app and folder[/]");
                Utils.OpenUrl("https://www.speedscope.app");
                Utils.OpenFolder(Path.GetDirectoryName(speedFile) ?? "");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Speedscope auto-open failed: {Markup.Escape(ex.Message)}[/]");
        }
    }
}
