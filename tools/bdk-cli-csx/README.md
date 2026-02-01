# BDK CLI - C# Script Version

A cross-platform Terminal CLI for running bITdevKit development tasks using C# scripts (dotnet-script).

## Features

- **Pure C#**: Native .NET implementation using dotnet-script
- **Cross-platform**: Works on Windows, Linux, and macOS
- **Interactive UI**: Beautiful Spectre.Console-powered menus with arrow key navigation
- **Logo Display**: Real bITdevKit logo image rendered in terminal (with ASCII fallback)
- **Smart Image Handling**: Automatically detects VS Code and shows ASCII art instead of image
- **Direct Execution**: Run tasks directly from command line (perfect for VS Code tasks)
- **Runtime Configuration**: Editable `.env` file (no recompilation needed)
- **Rich Output**: Color-coded output with progress indicators

## Prerequisites

### Quick Installation

Run the automated installer:

```bash
./tools/bdk-cli-csx/install.sh
```

This will:
1. Install `dotnet-script` globally
2. Configure your PATH automatically
3. Verify the installation

### Manual Installation

If you prefer manual setup:

1. **Install dotnet-script:**

```bash
dotnet tool install -g dotnet-script
```

2. **Add to PATH** (Linux/macOS):

```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:$HOME/.dotnet/tools"

# Apply immediately
source ~/.bashrc  # or source ~/.zshrc
```

3. **Verify installation:**

```bash
dotnet script --version
# Should show: 2.0.0 or higher
```

**Note:** The launcher script will automatically add `~/.dotnet/tools` to PATH for the current session if needed, but adding it to your shell profile makes it permanent.

## Usage

### Interactive Mode

Launch the interactive menu with arrow key navigation:

```bash
# PowerShell
./bdk-cli.ps1

# Bash/Linux/macOS
./bdk-cli.sh
```

**Navigation:**
- **↑/↓** - Navigate menu items
- **Enter** - Select / Execute
- **Type** - Search to filter options (type task name or description)
- **Wrap-around** - Navigation wraps from bottom to top and vice versa
- Choose category → Choose task → Watch execution
- Results displayed with color-coded success/failure

### Direct Execution Mode

Execute tasks directly from the command line:

```bash
# Show all available tasks
./bdk-cli.sh --help

# Run specific tasks
./bdk-cli.sh version        # Show .NET version
./bdk-cli.sh restore        # Restore NuGet packages
./bdk-cli.sh build          # Build solution
./bdk-cli.sh test           # Run all tests
./bdk-cli.sh test-unit      # Run unit tests only
./bdk-cli.sh format         # Format code
```

## Available Tasks

### Build & Maintenance
- `build` - Build the entire solution
- `clean` - Clean build artifacts
- `restore` - Restore NuGet packages
- `format` - Format code using dotnet format
- `format-check` - Verify code formatting

### Testing
- `test` - Run all unit and integration tests
- `test-unit` - Run unit tests only
- `test-integration` - Run integration tests only

### Utilities
- `version` - Display .NET SDK version

## VS Code Integration

Add to `.vscode/tasks.json`:

```json
{
  "label": "BDK: Build",
  "type": "shell",
  "command": "./bdk-cli.sh",
  "args": ["build"],
  "problemMatcher": "$msCompile"
},
{
  "label": "BDK: Test",
  "type": "shell",
  "command": "./bdk-cli.sh",
  "args": ["test"]
},
{
  "label": "BDK: Restore",
  "type": "shell",
  "command": "./bdk-cli.sh",
  "args": ["restore"]
}
```

## Logo Display

The C# Script edition features a beautiful bITdevKit logo display using Spectre.Console's official CanvasImage widget:

**In Regular Terminals**: The actual logo PNG image is rendered using the `CanvasImage` fluent API with bicubic resampling for high-quality scaling.

**In VS Code Terminal**: Automatically falls back to ASCII art instead of the image (detected via `VSCODE_PID` environment variable) for better compatibility.

**Logo Rendering Implementation**: 
```csharp
var image = new CanvasImage(logoPath)
    .MaxWidth(40)
    .BicubicResampler();

AnsiConsole.Write(image);
```

**Features**: 
- Automatically loads `bITDevKit_Logo_dark.png` from the repository root
- High-quality bicubic resampling for optimal scaling
- Gracefully falls back to ASCII art if the image cannot be loaded or rendered
- Smart terminal detection for VS Code compatibility
- Arranged in a grid layout alongside project information

## Configuration

Configuration is loaded from `tools/bdk-cli-csx/.env` at runtime.

**Edit anytime - no recompilation needed!**

```env
OUTPUT_DIRECTORY=.tmp
ARTIFACTS_DIRECTORY=.artifacts
SOURCES_DIRECTORY=src
MODULES_DIRECTORY=src/Modules
TESTS_DIRECTORY=tests
DOCKER_FILE_PATH=src/Presentation.Web.Server/Dockerfile
DOCKER_COMPOSE_PATH=docker-compose.yml
# ... more settings
```

## Development

### Running the Script Directly

```bash
cd tools/bdk-cli-csx

# Interactive mode
dotnet script bdk-cli.csx

# Direct execution
dotnet script bdk-cli.csx build
dotnet script bdk-cli.csx --help
```

### Adding New Tasks

1. Open `bdk-cli.csx`
2. Add a new task to the `TaskRegistry.GetAllTasks()` method:

```csharp
new BdkTask
{
    Key = "my-task",
    Label = "My Custom Task",
    Description = "Does something awesome",
    Category = "Build & Maintenance",
    Execute = async (ctx) =>
    {
        return await ctx.Executor.ExecuteAsync(
            "dotnet", 
            "my-command --arg value"
        );
    }
}
```

3. Test: `dotnet script bdk-cli.csx my-task`

## Architecture

```
tools/bdk-cli-csx/
├── .env                    # Runtime configuration
├── bdk-cli.csx            # Main C# script
└── README.md              # This file

Root launchers:
├── bdk-cli.ps1            # PowerShell launcher
└── bdk-cli.sh             # Bash launcher
```

### Key Components

- **BdkConfig**: Configuration loader from `.env` file
- **CommandExecutor**: Cross-platform process executor with real-time output
- **DotnetCli**: .NET CLI wrapper with solution file auto-detection
- **TaskRegistry**: Centralized task definitions organized by category
- **BdkUI**: Interactive Spectre.Console-based UI with menus

## Comparison with TypeScript Version

| Feature | C# Script (this) | TypeScript (bdk-tui) |
|---------|------------------|----------------------|
| Runtime | .NET + dotnet-script | Bun |
| Language | C# | TypeScript |
| UI Library | Spectre.Console | OpenTUI |
| Performance | Native .NET | Very fast (Bun) |
| Installation | `dotnet tool install` | `curl \| bash` |
| IDE Support | Excellent (VS/Rider) | Good (VS Code) |

**Choose C# Script if:**
- You prefer C# over TypeScript
- You want native .NET integration
- You have dotnet-script already installed
- You want rich Spectre.Console UI

**Choose TypeScript if:**
- You prefer TypeScript
- You want minimal dependencies (just Bun)
- You need ultra-fast startup times
- You prefer OpenTUI's rendering model

## Performance

| Operation | Typical Time | Notes |
|-----------|--------------|-------|
| Startup (interactive) | ~200-500ms | Includes Spectre.Console rendering |
| `dotnet --version` | ~100ms | Quick command |
| `dotnet restore` | ~1-2s | Package restore |
| `dotnet build` | ~2-5s | Full solution build |

## Troubleshooting

### "dotnet-script is not installed"

Install it globally:
```bash
dotnet tool install -g dotnet-script
```

### "Cannot find solution file"

Ensure you're running from the repository root or that a `.sln` or `.slnx` file exists.

### Script execution errors

Try running directly:
```bash
dotnet script tools/bdk-cli-csx/bdk-cli.csx --help
```

## Future Enhancements

- [ ] Add EF Core migration tasks
- [ ] Add Docker build/run tasks
- [ ] Add code coverage tasks
- [ ] Add OpenAPI generation tasks
- [ ] Module/DbContext selection dialogs
- [ ] Task history and favorites
- [ ] Custom task plugins

## License

Part of the bITdevKit GettingStarted project.
