# BDK CLI Menu Structure

## Visual Menu Layout

### Startup Banner (shown once)
```
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║      ██╗      ██████╗ ██╗  ██╗                               ║
║      ╚██╗     ██╔══██╗██║ ██╔╝                               ║
║       ╚██╗    ██████╔╝█████╔╝                                ║
║       ██╔╝    ██╔══██╗██╔═██╗                                ║
║      ██╔╝     ██████╔╝██║  ██╗                               ║
║      ╚═╝      ╚═════╝ ╚═╝  ╚═╝                               ║
║                                                               ║
║      bIT.bITdevKit.Examples.GettingStarted                    ║
║      C# Script Edition                                        ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```

### Category Menu
```
Select a category:
  > Build & Maintenance
    Testing
    Utilities
    ✕ Exit
```

**Navigation:**
- `↑/↓` or `j/k` - Move selection
- `Enter` - Select category
- Select "✕ Exit" - Quit application

---

### Task Menu (example: Build & Maintenance)
```
Build & Maintenance:
  > Build Solution - Build the entire solution
    Clean Solution - Clean build artifacts
    Restore Packages - Restore NuGet packages
    Format Code - Format code using dotnet format
    Format Check - Verify code formatting
    ← Back
    ✕ Exit
```

**Navigation:**
- `↑/↓` or `j/k` - Move selection
- `Enter` - Execute task or navigate
- Select "← Back" - Return to Category Menu
- Select "✕ Exit" - Quit application

---

### Task Execution
```
═══════════════════════════════════════════
Task: Build Solution
Build the entire solution
═══════════════════════════════════════════

[exec] dotnet build bITdevKit.Examples.GettingStarted.slnx
  Determining projects to restore...
  All projects are up-to-date for restore.
  CoreModule.Domain -> /path/to/CoreModule.Domain.dll
  ...
  Build succeeded.
    0 Warning(s)
    0 Error(s)

═══════════════════════════════════════════
✓ Task completed successfully
Duration: 12259ms
═══════════════════════════════════════════

[Task menu reappears here, output remains visible above]
```

**After Execution:**
- Task menu reappears automatically
- No screen clearing
- Previous output scrolled up
- Can run another task immediately

---

## Menu Icons

| Icon | Meaning | Usage |
|------|---------|-------|
| `←` | Back | Return to previous menu level |
| `✕` | Exit | Quit the application |

## Complete Flow Example

```
┌─────────────────────────────────────────────────────────┐
│ [1] Startup - ASCII Banner                              │
│     ╔═══════════════════════════════════════╗           │
│     ║  ██╗  ██████╗ ██╗  ██╗                ║           │
│     ║  Repository Name                      ║           │
│     ║  C# Script Edition                    ║           │
│     ╚═══════════════════════════════════════╝           │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│ [2] Category Menu                                       │
│     Select a category:                                  │
│       Build & Maintenance                               │
│       Testing                                          │
│       Utilities                                        │
│       ✕ Exit                                           │
└─────────────────────────────────────────────────────────┘
                        ↓ Select "Build & Maintenance"
┌─────────────────────────────────────────────────────────┐
│ [3] Task Menu                                           │
│     Build & Maintenance:                                │
│       Build Solution - Build the entire solution        │
│       Clean Solution - Clean build artifacts            │
│       Restore Packages - Restore NuGet packages         │
│       Format Code - Format code using dotnet format     │
│       Format Check - Verify code formatting             │
│       ← Back                                            │
│       ✕ Exit                                            │
└─────────────────────────────────────────────────────────┘
                        ↓ Select "Build Solution"
┌─────────────────────────────────────────────────────────┐
│ [4] Task Execution                                      │
│     ═══════════════════════════════════════════         │
│     Task: Build Solution                                │
│     Build the entire solution                           │
│     ═══════════════════════════════════════════         │
│                                                         │
│     [exec] dotnet build solution.slnx                   │
│     [build output streams...]                           │
│     Build succeeded.                                    │
│                                                         │
│     ═══════════════════════════════════════════         │
│     ✓ Task completed successfully                       │
│     Duration: 12259ms                                   │
│     ═══════════════════════════════════════════         │
└─────────────────────────────────────────────────────────┘
                        ↓ Auto-return (no clear)
┌─────────────────────────────────────────────────────────┐
│ [Previous output remains visible, scrolled up]          │
│                                                         │
│ [5] Task Menu (again)                                   │
│     Build & Maintenance:                                │
│       Build Solution - Build the entire solution        │
│       Clean Solution - Clean build artifacts            │
│       Restore Packages - Restore NuGet packages         │
│       Format Code - Format code using dotnet format     │
│       Format Check - Verify code formatting             │
│       ← Back                                            │
│       ✕ Exit                                            │
└─────────────────────────────────────────────────────────┘
         ↓ Select "Restore"        ↓ Select "← Back"      ↓ Select "✕ Exit"
   [Run another task]         [Category Menu]         [Quit app]
```

## Key Features

### 1. No Screen Clearing
- Banner shown once at startup
- All subsequent navigation preserves output
- Natural terminal scrolling

### 2. Flexible Navigation
- **← Back**: Return to previous menu (preserves context)
- **✕ Exit**: Quit from anywhere (no need to navigate to top)

### 3. Efficient Workflow
- Stay in task menu after execution
- Run multiple related tasks quickly
- Review all outputs in scroll buffer

### 4. Professional Appearance
- Unicode icons (←, ✕)
- Color-coded output (cyan headers, green success, red errors)
- Formatted tables and panels
- ASCII art branding

## Direct Execution Mode

For automation and VS Code tasks:

```bash
./bdk-cli.sh version      # Quick commands
./bdk-cli.sh build        # CI/CD pipelines
./bdk-cli.sh test-unit    # Task automation

# Help
./bdk-cli.sh --help       # Shows formatted table of all tasks
```

No menu navigation, direct execution with full output streaming.
