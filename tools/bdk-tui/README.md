# BDK TUI - Terminal User Interface for bITdevKit

A cross-platform Terminal User Interface (TUI) for running bITdevKit development tasks.

## Status: ✅ Phase 2 Complete - Fancy OpenTUI Menus with Arrow Key Navigation!

**Working Features:**
- ✅ **Fancy OpenTUI Select menus** with arrow key navigation (↑/↓ or j/k)
- ✅ Direct CLI execution (for VS Code integration)
- ✅ Color-coded interactive UI (cyan highlights, descriptions)
- ✅ Real-time command output streaming
- ✅ Smart navigation (ESC=back, Q=quit, Enter=select)
- ✅ Task output inline (no screen clearing, menu renders below)
- ✅ Cross-platform (Windows/Linux/macOS)

## Features

- **Cross-platform**: Works identically on Windows, Linux, and macOS
- **Pure TypeScript**: No PowerShell dependency
- **Fast**: Built with Bun for optimal performance
- **Runtime config**: Editable `.env` file (no rebuild needed)
- **Two modes**: Interactive menus OR direct execution

## Usage

### Interactive Mode (Fancy Arrow Key Navigation)
```bash
# From repo root - launches fancy OpenTUI menu
./bdk-tui.sh

# Navigate with arrow keys:
# 1. Use ↑/↓ (or j/k) to navigate categories
# 2. Press Enter to select
# 3. Use ↑/↓ to navigate tasks
# 4. Press Enter to execute
# 5. Watch real-time output
# 6. Menu automatically appears below output
# 7. ESC to go back, Q to quit
```

**Keyboard Navigation:**
- **↑/↓ or j/k** - Navigate menu items
- **Enter** - Select / Execute
- **ESC** - Go back to previous menu
- **Q** - Quit application
- **Shift+↑/↓** - Fast scroll (5 items)

### Direct Execution (For VS Code Tasks)
```bash
# Execute tasks directly - perfect for automation!
./bdk-tui.sh --help     # Show all available tasks
./bdk-tui.sh version    # Quick command (~95ms)
./bdk-tui.sh restore    # Restore NuGet packages
./bdk-tui.sh build      # Build the solution
```

## Available Tasks

### Build & Maintenance
- `build` - Build the solution
- `clean` - Clean the solution  
- `restore` - Restore NuGet packages

### Utilities
- `version` - Show .NET SDK version

## VS Code Integration

Perfect for VS Code tasks! Add to `.vscode/tasks.json`:

```json
{
  "label": "BDK: Build",
  "type": "shell",
  "command": "./bdk-tui.sh",
  "args": ["build"],
  "problemMatcher": "$msCompile"
},
{
  "label": "BDK: Restore",
  "type": "shell",
  "command": "./bdk-tui.sh",
  "args": ["restore"]
}
```

## Configuration

Config is loaded from `tools/bdk-tui/config/bdk.env` at runtime.

**Edit anytime - no rebuild needed!**

```env
OUTPUT_DIRECTORY=.tmp
ARTIFACTS_DIRECTORY=.artifacts
DOCKER_FILE_PATH=src/Presentation.Web.Server/Dockerfile
# ... more settings
```

## Development

```bash
cd tools/bdk-tui

# Run in dev mode
bun run dev

# Run specific task
bun run dev build

# Show help
bun run dev --help
```

### Adding New Tasks

1. Open `src/tasks/registry.ts`
2. Add task definition to `TASK_REGISTRY`:

```typescript
{
  key: 'my-task',
  label: 'My Task',
  description: 'Does something awesome',
  category: 'Build & Maintenance',
  execute: async (ctx: TaskContext) => {
    const result = await dotnetCli.someCommand();
    return {
      success: result.success,
      exitCode: result.exitCode,
      duration: Date.now() - startTime
    };
  }
}
```

3. Test: `bun run dev my-task`

## Architecture

```
tools/bdk-tui/
├── config/
│   └── bdk.env              # Runtime configuration
├── src/
│   ├── core/
│   │   ├── config.ts        # Config loader (multi-path search)
│   │   └── executor.ts      # Cross-platform command executor
│   ├── lib/
│   │   └── dotnet.ts        # .NET CLI wrapper
│   ├── tasks/
│   │   └── registry.ts      # Task definitions
│   └── index.ts             # Entry point with routing
├── bin/                     # Compiled binaries (future)
└── package.json
```

### Cross-Platform Strategy

- **Commands**: Direct calls to `dotnet`, `docker`, `git` (all cross-platform)
- **Paths**: Forward slashes work everywhere
- **No PowerShell**: Pure TypeScript implementation

## Next Steps (Future Phases)

- [ ] Add more tasks (EF migrations, Docker, Testing - 70+ more)
- [ ] Advanced OpenTUI components (Select widgets, ScrollBox)
- [ ] Input dialogs for interactive prompts (migration names, etc.)
- [ ] Module/DbContext selection
- [ ] Compile to standalone binaries
- [ ] Process selection for diagnostics
- [ ] Task history and favorites

## License

Part of the bITdevKit GettingStarted project.
