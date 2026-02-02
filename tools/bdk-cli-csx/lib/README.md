# BDK CLI Library Files

This directory contains the modular components of the BDK CLI, split from the original monolithic `bdk-cli.csx` file for better maintainability.

## File Structure

All files are loaded in **dependency order** by `bdk-cli.csx` using `#load` directives. The order is critical:

```
bdk-cli.csx (main entry point)
├── BdkConfig.csx          # Configuration loader (no dependencies)
├── CommandExecutor.csx      # Process execution (no dependencies)
├── TaskContext.csx          # Context definition (no dependencies)
├── DotnetCli.csx            # .NET CLI wrapper (depends on all above)
├── TaskRegistry.csx         # Task definitions (depends on DotnetCli)
└── BdkUI.csx                # Interactive UI (depends on all above)
```

## File Descriptions

### BdkConfig.csx (82 lines)
Loads configuration from `.env` file with default values. Handles parsing of key-value pairs.

### CommandExecutor.csx (92 lines)
Cross-platform command execution with real-time output streaming. Uses `Process` class and handles exit codes.

### TaskContext.csx (12 lines)
Simple data container that passes dependencies (Config, DotnetCli, Executor, SolutionFile) to task handlers.

### DotnetCli.csx (81 lines)
Wrapper for .NET CLI commands (build, test, restore, format, version). Includes solution file detection logic.

### TaskRegistry.csx (113 lines)
Defines all available tasks (9 tasks across 3 categories) organized by functionality.

### BdkUI.csx (190 lines)
Interactive UI components including:
- Compact FigletText banner with Grid layout
- Solution file selection prompt (multi-select support)
- Category menu with search
- Task menu with search
- Task execution display with duration tracking

## Important Notes

- **Using statements**: Each lib file imports its own `using Spectre.Console;` as needed
- **Implicit namespaces**: System.*, System.IO, System.Collections.Generic, System.Linq, System.Text are automatically available
- **Load order**: Changing the load order in `bdk-cli.csx` will break compilation
- **Main entry**: `bdk-cli.csx` is the only file with the shebang (`#!/usr/bin/env dotnet-script`)

## Adding New Components

1. Create new `.csx` file in this directory
2. Add necessary `using` statements
3. Add `#load "lib/YourFile.csx"` to `bdk-cli.csx` in correct dependency order
4. File can now use types defined in previously loaded files

## Refactoring Benefits

- **Smaller files**: Main entry reduced from 681 to 101 lines (85% reduction)
- **Easier navigation**: Each component in its own file
- **Logical grouping**: Related code stays together
- **Extensibility**: Easy to add new features in dedicated files
- **Maintainability**: Easier to find and fix bugs in specific areas
