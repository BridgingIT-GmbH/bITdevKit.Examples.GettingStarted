# BDK CLI

BDK CLI is a cross-platform development tool for bITdevKit projects, built with C# script and powered by `dotnet-script`. It provides a unified interface for common .NET development tasks with an interactive TUI and direct command execution.

## Prerequisites

Before using BDK CLI, ensure you have the following installed:

- **.NET SDK 10** - Required for development

  ```bash
  # Check version
  dotnet --version
  ```

- **dotnet-script** - Global tool for executing C# scripts

  ```bash
  # Install dotnet-script
  dotnet tool install -g dotnet-script

  # Verify installation
  dotnet script --version
  ```

## Quick Start

### Interactive Mode (TUI)

Launch the interactive terminal user interface:

```bash
# Windows
./bdk-cli.ps1

# Linux/macOS
./bdk-cli.sh
```

The TUI provides:

- Category-based task navigation
- Search functionality
- Module selection
- Real-time task execution with status display

### Direct Command Execution

Run tasks directly from command line:

```bash
# Windows
./bdk-cli.ps1 build
./bdk-cli.ps1 test-unit
./bdk-cli.ps1 ef-apply

# Linux/macOS
./bdk-cli.sh build
./bdk-cli.sh test-unit
./bdk-cli.sh ef-apply
```

### Help Command

List all available tasks:

```bash
./bdk-cli.ps1 help
./bdk-cli.ps1 --help
```

Displays tasks in a formatted table with:

- Task key (for direct execution)
- Task label
- Description
- Category

## Architecture Overview

### CLI Structure

```text
.bdk/cli/
├── bdk-cli.csx          # Main entry point (C# script)
├── bdk-cli.ps1          # Windows launcher
├── bdk-cli.sh           # Linux/macOS launcher
├── install.ps1           # Windows installation helper
├── install.sh            # Linux/macOS installation helper
├── .env                 # Configuration file
└── lib/                 # Library modules
    ├── TaskRegistry.csx
    ├── TaskContext.csx
    ├── BdkConfig.csx
    ├── CommandExecutor.csx
    ├── Prompts.csx
    ├── BdkUI.csx
    ├── DotnetCli.csx
    ├── DockerCli.csx
    ├── Diagnostics.csx
    ├── Security.csx
    ├── OpenApi.csx
    ├── MiscUtils.csx
    └── Utils.csx
```

### Execution Flow

```text
Launcher (.ps1/.sh)
  ↓
bdk-cli.csx (dotnet-script)
  ↓
Load lib/ modules
  ↓
Initialize TaskContext (config, modules, CLI wrappers)
  ↓
Execute task (interactive or direct)
  ↓
CommandExecutor → Shell command
  ↓
Result with Success/ExitCode/Duration
```

## Core Components

### TaskRegistry.csx

Central registry of all available tasks organized by category.

**BdkTask Definition:**

```csharp
public class BdkTask
{
    public string Key { get; set; }          // Task key for direct execution
    public string Label { get; set; }        // Display name
    public string Description { get; set; }    // Brief description
    public string Category { get; set; }      // Task category
    public Func<TaskContext, Task<ExecutionResult>> Execute { get; set; }
}
```

Contains 90+ tasks across these categories:

- Build & Maintenance
- Testing
- Utilities
- Performance & Diagnostics
- Security & Compliance
- API & Spec
- EF & Persistence
- Docker & Containers

### TaskContext.csx

Context passed to task execution handlers containing all dependencies:

```csharp
public class TaskContext
{
    public BdkConfig Config { get; set; }
    public DotnetCli DotnetCli { get; set; }
    public DockerCli DockerCli { get; set; }
    public CommandExecutor Executor { get; set; }
    public string SolutionFile { get; set; }
    public string RootDir { get; set; }
    public string OutputDir { get; set; }
    public string SelectedModule { get; set; }
    public List<string> AvailableModules { get; set; }
    public List<string> AvailableDbContexts { get; set; }
}
```

### CommandExecutor.csx

Cross-platform shell command execution with real-time output streaming.

```csharp
public class CommandExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(
        string fileName,
        string arguments = "",
        bool captureOutput = false,
        bool showCommand = true
    );
}

public class ExecutionResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}
```

### BdkConfig.csx

Configuration loader for `.env` file.

**Supported Settings:**

```bash
OUTPUT_DIRECTORY=.artifacts
SOURCES_DIRECTORY=src
MODULES_DIRECTORY=src/Modules
TESTS_DIRECTORY=tests
DOCKER_FILE_PATH=src/Presentation.Web.Server/Dockerfile
DOCKER_COMPOSE_PATH=docker-compose.yml
DOTNET_PUBLISH_PROJECT=src/Presentation.Web.Server/Presentation.Web.Server.csproj
EF_STARTUP_PROJECT=src/Presentation.Web.Server/Presentation.Web.Server.csproj
DOCKER_DB_CONNECTIONSTRING="..."
REGISTRY_HOST=localhost:5500
CONTAINER_PREFIX=bit_devkit_gettingstarted
NETWORK_NAME=bit_devkit_gettingstarted
DOCKER_HOST_PORT=8080
DOCKER_CONTAINER_PORT=8080
```

### Prompts.csx

Interactive user selection utilities using Spectre.Console.

**Functions:**

- `SelectProjectAsync()` - Select .csproj file
- `SelectModuleForTaskAsync()` - Select module
- `SelectDbContextForTaskAsync()` - Select DbContext
- `SelectSolutionAsync()` - Select solution file
- `SelectConfigurationAsync()` - Select Debug/Release
- `SelectRidAsync()` - Select runtime identifier
- `SelectSingleFileAsync()` - Select single-file option
- `PromptTextAsync()` - Text input prompt

### BdkUI.csx

Interactive terminal user interface (TUI) powered by Spectre.Console.

**Features:**

- ASCII art banner
- Category-based navigation
- Search-enabled selection
- Task execution display
- Success/failure feedback with duration

## Specialized Modules

### DotnetCli.csx

Wrapper for .NET CLI commands.

**Capabilities:**

- Build, clean, restore, pack
- Test execution with filters
- Format checking/applying
- Roslyn analyzers
- EF Core migrations (info, list, add, remove, apply, undo, recreate, reset, script, bundle)
- Project-specific operations (build, publish, run, watch)

### DockerCli.csx

Wrapper for Docker commands.

**Capabilities:**

- Image operations (build, remove)
- Container operations (run, logs, ps, inspect, stop, remove)
- Docker Compose operations (up, recreate, down, down-clean)
- Network management
- Registry operations

### Diagnostics.csx

Performance and diagnostics tools.

**Capabilities:**

- BenchmarkDotNet benchmark execution
- Performance traces (flame, CPU, GC)
- Heap dumps
- GC statistics monitoring
- ASP.NET Core metrics collection
- Speedscope profile viewing

### Security.csx

Security and compliance tools.

**Capabilities:**

- Vulnerability scanning (direct and deep/transitive)
- Outdated package listing (with JSON export)
- License report generation (Markdown + JSON)

### OpenApi.csx

OpenAPI specification tooling.

**Capabilities:**

- Lint OpenAPI specs with Spectral
- Generate C# client with Kiota
- Generate TypeScript client with Kiota
- Generate .http request files

### MiscUtils.csx

Utility functions.

**Capabilities:**

- Digest source code for LLM consumption
- Clean workspace (remove bin/obj/node_modules)
- Remove file headers
- C# REPL (csharprepl)
- Kill .NET processes
- MinVer semantic version display
- Update DevKit documentation
- Browser shortcuts (SEQ, AdminNeo, Server Kestrel, Server Docker)

## Task Categories

### Build & Maintenance

Solution-level build operations, package management, code formatting, and analyzers.

### Testing

Unit, integration, architecture, and system test execution with coverage reporting.

### Utilities

Developer utilities for workspace management, REPL, process management, and browser shortcuts.

### Performance & Diagnostics

Benchmarking, performance tracing (flame, CPU, GC), heap dumps, and metrics collection.

### Security & Compliance

Vulnerability scanning, outdated package detection, and license reporting.

### API & Spec

OpenAPI specification validation and client code generation.

### EF & Persistence

Entity Framework Core migration management (add, remove, apply, undo, recreate, reset, script, bundle).

### Docker & Containers

Docker image and container management, Docker Compose operations.

## Adding a New Task

### Step 1: Define Task in TaskRegistry.csx

Add a new task definition to the `GetAllTasks()` method:

```csharp
new() {
    Key = "your-task-key",
    Label = "Your Task Label",
    Description = "Brief description of what the task does",
    Category = "Your Category",
    Execute = async (ctx) => await YourImplementationAsync(ctx)
}
```

**Parameters:**

- `Key` - Unique identifier for direct command execution
- `Label` - Display name in TUI
- `Description` - Brief explanation in help output
- `Category` - One of the 8 categories
- `Execute` - Async function accepting `TaskContext`, returning `ExecutionResult`

### Step 2: Implement Task Logic

Add implementation to the appropriate module:

**Example: Adding to DotnetCli.csx**

```csharp
public Task<ExecutionResult> YourImplementationAsync(string projectPath)
{
    var args = $"build \"{projectPath}\" -c Release";
    return await _executor.ExecuteAsync("dotnet", args);
}
```

**Useful Context Properties:**

- `ctx.Executor` - Execute shell commands
- `ctx.DotnetCli` - Access .NET CLI wrapper
- `ctx.DockerCli` - Access Docker CLI wrapper
- `ctx.Config` - Access configuration
- `ctx.SelectedModule` - Selected module (for module-specific tasks)
- `ctx.SolutionFile` - Solution file path
- `ctx.OutputDir` - Output directory

**Interactive Prompts:**

```csharp
var project = await Prompts.SelectProjectAsync(ctx, "Select a project:");
var config = await Prompts.SelectConfigurationAsync("Debug");
```

### Step 3: Test the Task

Test from command line:

```bash
./bdk-cli.ps1 your-task-key
```

Task is automatically available in:

- Interactive TUI
- Help command output
- VS Code tasks.json (after update)

## Configuration

The `.bdk/cli/.env` file contains local development settings.

**Common Customizations:**

- Change output directory: `OUTPUT_DIRECTORY=build-output`
- Modify Docker ports: `DOCKER_HOST_PORT=9090`, `DOCKER_CONTAINER_PORT=8080`
- Update registry host: `REGISTRY_HOST=my-registry.azurecr.io`
- Custom container prefix: `CONTAINER_PREFIX=myapp`

## Task Discovery

### Help Command

List all tasks with descriptions and categories:

```bash
./bdk-cli.ps1 help
```

Output format:

```text
┌─────────────────┬──────────────────────────────────────────┬─────────────────────────┐
│ Task            │ Description                      │ Category                        │
├─────────────────┼──────────────────────────────────────────┼─────────────────────────┤
│ build           │ Build solution in Debug config    │ Build & Maintenance  │
│ test-unit       │ Run unit tests only             │ Testing                │
│ ef-add          │ Add new migration              │ EF & Persistence       │
│ ...
└─────────────────┴──────────────────────────────────────────┴─────────────────────────┘
```

### Interactive Mode

Browse tasks by category with search:

```bash
./bdk-cli.ps1
```

Use arrow keys to navigate, type to search, Enter to execute.

## Cross-Platform Support

BDK CLI is designed to work on Windows, Linux, and macOS:

- **Windows**: Uses `bdk-cli.ps1` (PowerShell launcher)
- **Linux/macOS**: Uses `bdk-cli.sh` (Bash launcher)
- **Command execution**: Cross-platform via `CommandExecutor.csx`

Both launchers delegate to the same `bdk-cli.csx` script, ensuring consistent behavior across platforms.
