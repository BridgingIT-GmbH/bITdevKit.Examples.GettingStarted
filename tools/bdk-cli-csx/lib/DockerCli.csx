// BDK CLI - Docker CLI Wrapper Module
/// <summary>
/// Wraps Docker commands for container and compose operations
/// </summary>

using System.Text;
using System.Text.Json;
using Spectre.Console;

/// <summary>
/// Docker environment variables from configuration
/// </summary>
public class DockerConfig
{
    public string RegistryHost { get; set; } = "";
    public string ContainerPrefix { get; set; } = "";
    public string NetworkName { get; set; } = "";
    public string DockerfilePath { get; set; } = "";
    public string ComposeFilePath { get; set; } = "";
    public string DockerContext { get; set; } = ".";
    public int HostPort { get; set; } = 8080;
    public int ContainerPort { get; set; } = 8080;

    public string GetContainerName()
    {
        return string.IsNullOrEmpty(ContainerPrefix) ? "web" : $"{ContainerPrefix}-web";
    }

    public string GetImageTag()
    {
        var containerName = GetContainerName();
        var registry = string.IsNullOrEmpty(RegistryHost) ? "" : $"{RegistryHost}/";
        return $"{registry}{containerName}:latest";
    }

    public string GetDockerConfigPath()
    {
        var containerName = GetContainerName();
        return $".docker/{containerName}.json";
    }
}

/// <summary>
/// Container information
/// </summary>
public class ContainerInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Status { get; set; } = "";
    public string Ports { get; set; } = "";
    public string ServiceName { get; set; } = "";
}

/// <summary>
/// Wraps Docker commands for container and compose operations
/// </summary>
public class DockerCli
{
    private readonly BdkConfig _config;
    private readonly CommandExecutor _executor;
    private DockerConfig _dockerConfig = null!;

    public DockerCli(BdkConfig config, CommandExecutor executor)
    {
        _config = config;
        _executor = executor;
        _dockerConfig = new DockerConfig
        {
            RegistryHost = _config.DockerRegistryHost ?? "",
            ContainerPrefix = _config.ContainerPrefix ?? "",
            NetworkName = _config.NetworkName ?? "",
            DockerfilePath = _config.DockerFilePath ?? "",
            ComposeFilePath = _config.DockerComposePath ?? "",
            HostPort = _config.DockerHostPort,
            ContainerPort = _config.DockerContainerPort
        };
    }

    private async Task<ExecutionResult> ExecuteDockerCommandAsync(string args)
    {
        return await _executor.ExecuteAsync("docker", args, showCommand: true);
    }

    private async Task EnsureNetworkAsync()
    {
        var networkName = _dockerConfig.NetworkName;
        if (string.IsNullOrEmpty(networkName))
        {
            AnsiConsole.MarkupLine("[yellow]Warning: NETWORK_NAME not configured, skipping network check[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Ensuring docker network '{networkName}' exists[/]");
        var result = await _executor.ExecuteAsync("docker", $"network ls --format '{{{{.Name}}}}'", showCommand: true);
        
        if (result.Success && result.Output.Contains(networkName))
        {
            AnsiConsole.MarkupLine($"[dim]Network '{networkName}' already exists[/]");
            return;
        }

        var createResult = await _executor.ExecuteAsync("docker", $"network create {networkName}", showCommand: true);
        if (createResult.Success)
        {
            AnsiConsole.MarkupLine($"[green]Created network {networkName}[/]");
        }
    }

    private List<string> GetEnvironmentVariables()
    {
        var envVars = new List<string>();
        var configPath = _dockerConfig.GetDockerConfigPath();
        
        if (File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[cyan]Reading settings from {configPath}[/]");
            try
            {
                var jsonContent = File.ReadAllText(configPath);
                var jsonDoc = JsonDocument.Parse(jsonContent);
                if (jsonDoc.RootElement.TryGetProperty("Environment", out var envArray))
                {
                    foreach (var env in envArray.EnumerateArray())
                    {
                        envVars.Add(env.GetString() ?? "");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Failed to read Docker config: {ex.Message}[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error: No settings file found at {configPath}[/]");
        }

        return envVars;
    }

    public async Task<ExecutionResult> BuildImageAsync(string config, bool noCache)
    {
        await EnsureNetworkAsync();
        
        var imageTag = _dockerConfig.GetImageTag();
        var dockerfile = _dockerConfig.DockerfilePath;
        var context = _dockerConfig.DockerContext;

        if (string.IsNullOrEmpty(dockerfile))
        {
            AnsiConsole.MarkupLine("[red]Error: DOCKER_FILE_PATH not configured in .env[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
        }

        AnsiConsole.MarkupLine($"[cyan]Building Docker image:[/] [green]{imageTag}[/]");
        AnsiConsole.MarkupLine($"[dim]Dockerfile:[/] [cyan]{dockerfile}[/]");
        AnsiConsole.MarkupLine($"[dim]Config:[/] [cyan]{config}[/]");

        var args = new StringBuilder();
        args.Append($"build -t {imageTag} -f {dockerfile} --build-arg CONFIG={config}");
        if (noCache)
            args.Append(" --no-cache");
        args.Append($" {context}");

        return await ExecuteDockerCommandAsync(args.ToString());
    }

    public async Task<ExecutionResult> BuildAndRunAsync(string config, bool noCache)
    {
        var buildResult = await BuildImageAsync(config, noCache);
        if (!buildResult.Success)
            return buildResult;

        return await RunContainerAsync();
    }

    public async Task<ExecutionResult> RunContainerAsync()
    {
        await EnsureNetworkAsync();

        var imageTag = _dockerConfig.GetImageTag();
        var containerName = _dockerConfig.GetContainerName();
        var networkName = _dockerConfig.NetworkName;
        var hostPort = _dockerConfig.HostPort;
        var containerPort = _dockerConfig.ContainerPort;

        AnsiConsole.MarkupLine($"[cyan]Running container:[/] [green]{containerName}[/]");

        await _executor.ExecuteAsync("docker", $"stop {containerName}", showCommand: true);
        await _executor.ExecuteAsync("docker", $"rm {containerName}", showCommand: true);

        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        if (!Directory.Exists(logsDir))
            Directory.CreateDirectory(logsDir);

        var envVars = GetEnvironmentVariables();
        
        var args = new StringBuilder();
        args.Append($"run -d --name {containerName}");
        args.Append($" -p {hostPort}:{containerPort}");
        if (!string.IsNullOrEmpty(networkName))
            args.Append($" --network {networkName}");
        foreach (var env in envVars)
            args.Append($" -e \"{env}\"");
        args.Append($" -v \"{Path.GetFullPath(logsDir)}:/.logs\"");
        args.Append($" {imageTag}");

        var runResult = await ExecuteDockerCommandAsync(args.ToString());
        if (runResult.Success)
        {
            AnsiConsole.MarkupLine($"[green]Container running: http://localhost:{hostPort}[/]");
            await ShowContainerDetailsAsync(containerName);
        }

        return runResult;
    }

    private async Task ShowContainerDetailsAsync(string containerName)
    {
        try
        {
            var format = "{{.ID}};{{.Names}};{{.Status}};{{.Ports}}";
            var result = await _executor.ExecuteAsync("docker", $"ps --filter name={containerName} --format \"{format}\"", showCommand: true);
            
            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                var cols = result.Output.Split(';');
                if (cols.Length >= 4)
                {
                    AnsiConsole.MarkupLine("[cyan]Container details:[/]");
                    AnsiConsole.MarkupLine($"  [dim]ID:[/] [green]{cols[0]}[/]");
                    AnsiConsole.MarkupLine($"  [dim]Name:[/] [green]{cols[1]}[/]");
                    AnsiConsole.MarkupLine($"  [dim]Status:[/] [green]{cols[2]}[/]");
                    AnsiConsole.MarkupLine($"  [dim]Ports:[/] [green]{string.Join(" ", cols[3].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))}[/]");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Failed to list container details: {ex.Message}[/]");
        }
    }

    public async Task<ExecutionResult> StopContainerAsync()
    {
        var containerName = _dockerConfig.GetContainerName();
        AnsiConsole.MarkupLine($"[cyan]Stopping container:[/] [green]{containerName}[/]");
        return await ExecuteDockerCommandAsync($"stop {containerName}");
    }

    public async Task<ExecutionResult> RemoveContainerAsync(bool removeNetwork = false)
    {
        var containerName = _dockerConfig.GetContainerName();
        var networkName = _dockerConfig.NetworkName;

        AnsiConsole.MarkupLine($"[cyan]Removing container:[/] [green]{containerName}[/]");
        
        await _executor.ExecuteAsync("docker", $"stop {containerName}", showCommand: true);
        var removeResult = await ExecuteDockerCommandAsync($"rm -f {containerName}");

        if (removeNetwork && !string.IsNullOrEmpty(networkName))
        {
            AnsiConsole.MarkupLine($"[cyan]Attempting to remove network:[/] [green]{networkName}[/]");
            await _executor.ExecuteAsync("docker", $"network rm {networkName}", showCommand: true);
        }

        return removeResult;
    }

    public async Task<ExecutionResult> RemoveImageAsync()
    {
        await StopContainerAsync();
        await RemoveContainerAsync(false);

        var imageTag = _dockerConfig.GetImageTag();
        AnsiConsole.MarkupLine($"[cyan]Removing image:[/] [green]{imageTag}[/]");
        return await ExecuteDockerCommandAsync($"rmi -f {imageTag}");
    }

    private async Task<List<ContainerInfo>> GetRunningContainersAsync()
    {
        var containers = new List<ContainerInfo>();
        
        var format = "{{.Names}}|{{.Image}}|{{.Status}}|{{.ID}}";
        var result = await _executor.ExecuteAsync("docker", $"ps --format \"{format}\"", showCommand: true);
        
        if (result.Success && !string.IsNullOrEmpty(result.Output))
        {
            foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    var container = new ContainerInfo
                    {
                        Name = parts[0],
                        Image = parts[1],
                        Status = parts[2],
                        Id = parts[3]
                    };

                    var serviceName = await GetComposeServiceNameAsync(container.Name);
                    container.ServiceName = string.IsNullOrEmpty(serviceName) ? container.Name : serviceName;
                    
                    containers.Add(container);
                }
            }
        }

        return containers;
    }

    private async Task<string> GetComposeServiceNameAsync(string containerName)
    {
        try
        {
            var format = "{{ index .Config.Labels \"com.docker.compose.service\" }}";
            var result = await _executor.ExecuteAsync("docker", $"inspect --format \"{format}\" {containerName}", showCommand: true);
            
            if (result.Success && !string.IsNullOrEmpty(result.Output) && !result.Output.Contains("<no value>"))
            {
                return result.Output.Trim();
            }
        }
        catch
        {
        }

        var prefix = _dockerConfig.ContainerPrefix;
        if (!string.IsNullOrEmpty(prefix) && containerName.StartsWith($"{prefix}_"))
        {
            return containerName.Substring(prefix.Length + 1);
        }

        if (containerName.Contains('_'))
        {
            var parts = containerName.Split('_');
            return parts[^1];
        }

        return containerName;
    }

    public async Task<ExecutionResult> ComposeUpAsync()
    {
        var composeFile = _dockerConfig.ComposeFilePath;
        if (string.IsNullOrEmpty(composeFile) || !File.Exists(composeFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Compose file not found: {composeFile}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
        }

        AnsiConsole.MarkupLine($"[cyan]Starting compose stack:[/] [green]{composeFile}[/]");
        return await ExecuteDockerCommandAsync($"compose -f {composeFile} up -d");
    }

    public async Task<ExecutionResult> ComposeDownAsync()
    {
        var composeFile = _dockerConfig.ComposeFilePath;
        if (string.IsNullOrEmpty(composeFile) || !File.Exists(composeFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Compose file not found: {composeFile}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
        }

        AnsiConsole.MarkupLine($"[cyan]Stopping compose stack:[/] [green]{composeFile}[/]");
        return await ExecuteDockerCommandAsync($"compose -f {composeFile} down");
    }

    public async Task<ExecutionResult> ComposeDownCleanAsync()
    {
        var composeFile = _dockerConfig.ComposeFilePath;
        if (string.IsNullOrEmpty(composeFile) || !File.Exists(composeFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Compose file not found: {composeFile}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
        }

        AnsiConsole.MarkupLine($"[cyan]Stopping and cleaning compose stack:[/] [green]{composeFile}[/]");
        
        var result = await ExecuteDockerCommandAsync($"compose -f {composeFile} down --remove-orphans --volumes");

        if (result.Success && File.Exists(composeFile))
        {
            var images = new List<string>();
            foreach (var line in File.ReadAllLines(composeFile))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"image:\s*(\S+)");
                if (match.Success)
                {
                    var image = match.Groups[1].Value;
                    if (!images.Contains(image))
                        images.Add(image);
                }
            }

            foreach (var img in images)
            {
                AnsiConsole.MarkupLine($"[cyan]Removing image:[/] [green]{img}[/]");
                await _executor.ExecuteAsync("docker", $"rmi {img}", showCommand: true);
            }
        }

        return result;
    }

    public async Task<ExecutionResult> ComposeRecreateAsync()
    {
        var composeFile = _dockerConfig.ComposeFilePath;
        if (string.IsNullOrEmpty(composeFile) || !File.Exists(composeFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Compose file not found: {composeFile}[/]");
            return new ExecutionResult { Success = false, ExitCode = 1, Duration = TimeSpan.Zero };
        }

        AnsiConsole.MarkupLine($"[cyan]Recreating ALL services:[/] [green]{composeFile}[/]");
        return await ExecuteDockerCommandAsync($"compose -f {composeFile} up -d --force-recreate");
    }

    public async Task<ExecutionResult> ListContainersAsync(bool all = false)
    {
        var args = all ? "ps -a" : "ps";
        AnsiConsole.MarkupLine("[cyan]Listing containers...[/]");
        return await ExecuteDockerCommandAsync(args);
    }

    public async Task<ExecutionResult> ListImagesAsync()
    {
        AnsiConsole.MarkupLine("[cyan]Listing images...[/]");
        return await ExecuteDockerCommandAsync("images");
    }

    public async Task<ExecutionResult> InspectContainerAsync(string containerName)
    {
        if (string.IsNullOrEmpty(containerName))
            containerName = _dockerConfig.GetContainerName();

        AnsiConsole.MarkupLine($"[cyan]Inspecting container:[/] [green]{containerName}[/]");
        return await ExecuteDockerCommandAsync($"inspect {containerName}");
    }

    public async Task<ExecutionResult> ViewLogsAsync(string containerName, bool follow)
    {
        if (string.IsNullOrEmpty(containerName))
            containerName = _dockerConfig.GetContainerName();

        var args = follow ? $"logs -f {containerName}" : $"logs {containerName}";
        AnsiConsole.MarkupLine($"[cyan]Viewing logs for:[/] [green]{containerName}[/]");
        return await ExecuteDockerCommandAsync(args);
    }
}
