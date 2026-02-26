// BDK CLI - Configuration Module
// Handles loading and parsing of .env configuration files

using Spectre.Console;

/// <summary>
/// Configuration settings for BDK CLI loaded from .env file
/// </summary>
public class BdkConfig
{
    public string OutputDirectory { get; set; } = ".artifacts";
    public string SourcesDirectory { get; set; } = "src";
    public string ModulesDirectory { get; set; } = "src/Modules";
    public string TestsDirectory { get; set; } = "tests";
    public string DockerFilePath { get; set; } = "src/Presentation.Web.Server/Dockerfile";
    public string DockerComposePath { get; set; } = "docker-compose.yml";
    public string DotnetPublishProject { get; set; } = "src/Presentation.Web.Server/Presentation.Web.Server.csproj";
    public string EfStartupProject { get; set; } = "src/Presentation.Web.Server/Presentation.Web.Server.csproj";
    public string DockerDbConnectionString { get; set; } = "";
    public string DockerRegistryHost { get; set; } = "localhost:5500";
    public string ContainerPrefix { get; set; } = "bit_devkit_gettingstarted";
    public string NetworkName { get; set; } = "bit_devkit_gettingstarted";
    public int DockerHostPort { get; set; } = 8080;
    public int DockerContainerPort { get; set; } = 8080;

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
                case "REGISTRY_HOST":
                    config.DockerRegistryHost = value;
                    break;
                case "CONTAINER_PREFIX":
                    config.ContainerPrefix = value;
                    break;
                case "NETWORK_NAME":
                    config.NetworkName = value;
                    break;
                case "DOCKER_HOST_PORT":
                    if (int.TryParse(value, out var hostPort))
                        config.DockerHostPort = hostPort;
                    break;
                case "DOCKER_CONTAINER_PORT":
                    if (int.TryParse(value, out var containerPort))
                        config.DockerContainerPort = containerPort;
                    break;
            }
        }

        return config;
    }
}
