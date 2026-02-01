# BDK CLI Tools - Comparison Guide

This project now offers **three different CLI tool implementations** for running bITdevKit development tasks. This guide helps you choose the right one for your needs.

## Quick Comparison Table

| Feature | PowerShell Scripts | TypeScript/Bun (TUI) | C# Script |
|---------|-------------------|----------------------|-----------|
| **Location** | `.bdk/` | `tools/bdk-tui/` | `tools/bdk-cli-csx/` |
| **Launcher** | `./bdk.ps1` | `./bdk-tui.sh` | `./bdk-cli.sh` |
| **Language** | PowerShell | TypeScript | C# (.csx) |
| **Runtime** | PowerShell 7+ | Bun | .NET + dotnet-script |
| **UI Style** | Command-line prompts | OpenTUI menus | Spectre.Console menus |
| **Startup Time** | ~200-500ms | ~100ms | ~200-300ms (cached) |
| **Installation** | Built-in (Windows) | `curl \| bash` (Bun) | `dotnet tool install` |
| **Cross-Platform** | ✅ (requires PS7) | ✅ | ✅ |
| **Windows Native** | ✅✅ | ✅ | ✅ |
| **Linux Native** | ✅ (with PS7) | ✅✅ | ✅✅ |
| **Task Count** | 70+ tasks | 9 tasks (Phase 2) | 9 tasks (Phase 1) |
| **Interactive Mode** | ✅ | ✅ (arrow keys) | ✅ (arrow keys) |
| **Direct Execution** | ✅ | ✅ | ✅ |
| **VS Code Tasks** | ✅ | ✅ | ✅ |
| **Configuration** | `.bdk/.env` | `tools/bdk-tui/config/bdk.env` | `tools/bdk-cli-csx/.env` |
| **Package Manager** | N/A | npm (Bun) | NuGet |

## Detailed Comparison

### 1. PowerShell Scripts (Original)

**Location**: `.bdk/` directory  
**Launcher**: `./bdk.ps1 -Task <task-name>`

#### Strengths
- ✅ **Most feature-complete** - 70+ tasks covering all aspects of development
- ✅ **Mature & battle-tested** - Original implementation with all edge cases handled
- ✅ **Windows-optimized** - Native Windows experience
- ✅ **No additional installation** (on Windows with PowerShell 7+)
- ✅ **Rich diagnostics** - Extensive error handling and logging
- ✅ **VS Code integration** - Full task.json support

#### Considerations
- Requires PowerShell 7+ on Linux/macOS (`sudo snap install powershell` or `brew install powershell`)
- Not as modern UI compared to TUI alternatives
- Larger codebase to maintain

#### Best For
- ✅ Windows developers (PowerShell native)
- ✅ Production use (most stable and complete)
- ✅ Complex workflows requiring all 70+ tasks
- ✅ Teams already using PowerShell tooling

#### Usage
```bash
# Interactive mode
./bdk.ps1

# Direct execution
./bdk.ps1 -Task build
./bdk.ps1 -Task test-unit-all
./bdk.ps1 -Task ef-migration-add -MigrationName "AddCustomerEmail"
```

---

### 2. TypeScript/Bun TUI (bdk-tui)

**Location**: `tools/bdk-tui/`  
**Launcher**: `./bdk-tui.sh [task]`

#### Strengths
- ✅ **Fastest startup** - ~100ms cold start
- ✅ **Modern UI** - OpenTUI with fancy arrow key navigation
- ✅ **Minimal dependencies** - Just Bun (single runtime)
- ✅ **Cross-platform parity** - Identical experience on Windows/Linux/macOS
- ✅ **TypeScript ecosystem** - npm packages, modern JS tooling
- ✅ **Real-time output** - No screen clearing, output streams inline
- ✅ **Small footprint** - Lightweight and fast

#### Considerations
- Requires Bun installation (`curl -fsSL https://bun.sh/install | bash`)
- Currently 9 tasks (Phase 2 in progress, will expand to 70+)
- Less mature than PowerShell version

#### Best For
- ✅ TypeScript/JavaScript developers
- ✅ Cross-platform teams (Linux/macOS primary)
- ✅ Performance-sensitive workflows (fast startup critical)
- ✅ Modern UI/UX preferences
- ✅ Development/prototyping phase

#### Usage
```bash
# Interactive mode (fancy arrow key menus)
./bdk-tui.sh

# Direct execution
./bdk-tui.sh build
./bdk-tui.sh version
./bdk-tui.sh restore

# Help
./bdk-tui.sh --help
```

---

### 3. C# Script (bdk-cli)

**Location**: `tools/bdk-cli-csx/`  
**Launcher**: `./bdk-cli.sh [task]`

#### Strengths
- ✅ **Native .NET** - Pure C# implementation, native .NET SDK integration
- ✅ **Spectre.Console UI** - Rich, beautiful terminal UI (tables, prompts, spinners)
- ✅ **Type-safe** - Full C# compiler support
- ✅ **IDE-friendly** - Excellent support in Visual Studio, Rider, VS Code
- ✅ **NuGet ecosystem** - Access to all .NET packages
- ✅ **Debuggable** - Full C# debugging support
- ✅ **Cross-platform** - Runs anywhere .NET runs

#### Considerations
- Requires dotnet-script installation (`dotnet tool install -g dotnet-script`)
- Currently 9 tasks (Phase 1 complete, will expand)
- First run slower (~500ms due to compilation, then cached to ~200-300ms)

#### Best For
- ✅ C#/.NET developers
- ✅ Teams using .NET tooling exclusively
- ✅ Rich UI requirements (Spectre.Console components)
- ✅ Scenarios requiring .NET SDK integration
- ✅ Developers comfortable with C# scripting

#### Usage
```bash
# Interactive mode (Spectre.Console menus)
./bdk-cli.sh

# Direct execution
./bdk-cli.sh build
./bdk-cli.sh test-unit
./bdk-cli.sh format

# Help (formatted table)
./bdk-cli.sh --help
```

---

## Decision Matrix

### Choose **PowerShell Scripts** if:
1. You're on Windows primarily
2. You need all 70+ tasks (production-ready)
3. You want the most stable, battle-tested solution
4. PowerShell 7+ is already installed
5. You prefer mature, feature-complete tooling

### Choose **TypeScript/Bun TUI** if:
1. You prefer TypeScript over PowerShell/C#
2. Startup speed is critical (~100ms vs ~300ms)
3. You want modern OpenTUI arrow key navigation
4. You're comfortable with Bun runtime
5. Cross-platform parity is important
6. You're okay with Phase 2 task set (expanding)

### Choose **C# Script** if:
1. You prefer C# and .NET ecosystem
2. You want native .NET SDK integration
3. You love Spectre.Console's rich UI components
4. You need full C# debugging capabilities
5. You want type-safe scripting
6. You're working in Visual Studio/Rider
7. You're okay with Phase 1 task set (expanding)

---

## Installation Guide

### PowerShell Scripts (Built-in)
No additional installation on Windows with PowerShell 7+.

**Linux/macOS:**
```bash
# Ubuntu/Debian
sudo snap install powershell --classic

# macOS
brew install powershell

# Verify
pwsh --version
```

### TypeScript/Bun TUI
```bash
# Install Bun
curl -fsSL https://bun.sh/install | bash

# Verify
bun --version

# Test
./bdk-tui.sh version
```

### C# Script
```bash
# Install dotnet-script
dotnet tool install -g dotnet-script

# Add to PATH (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"

# Verify
dotnet script --version

# Test
./bdk-cli.sh version
```

---

## VS Code Integration

All three tools support VS Code tasks! Example `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "BDK: Build (PowerShell)",
      "type": "shell",
      "command": "./bdk.ps1",
      "args": ["-Task", "build"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "BDK: Build (TypeScript TUI)",
      "type": "shell",
      "command": "./bdk-tui.sh",
      "args": ["build"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "BDK: Build (C# Script)",
      "type": "shell",
      "command": "./bdk-cli.sh",
      "args": ["build"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

---

## Roadmap

### PowerShell Scripts
- ✅ 70+ tasks complete
- ✅ Production-ready
- 🔄 Ongoing maintenance

### TypeScript/Bun TUI
- ✅ Phase 1-2 complete (9 tasks)
- 🔄 Phase 3: Add EF, Docker, Testing tasks (targeting 70+ tasks)
- 🔄 Advanced OpenTUI components

### C# Script
- ✅ Phase 1 complete (9 tasks)
- 🔄 Phase 2: Add EF, Docker, Testing tasks (targeting parity with TypeScript)
- 🔄 Standalone binary compilation
- 🔄 Plugin system for custom tasks

---

## Can They Coexist?

**Yes!** All three tools can coexist in the same repository:
- They use separate configuration files
- They don't conflict with each other
- You can use different tools for different tasks
- Teams can choose their preferred tool

This flexibility allows gradual migration or parallel usage based on team preferences.

---

## Recommendations by Team Profile

### .NET/C# Team (Microsoft Stack)
**Primary:** C# Script  
**Fallback:** PowerShell Scripts

### Full-Stack/Node.js Team
**Primary:** TypeScript/Bun TUI  
**Fallback:** PowerShell Scripts

### Mixed Team (Multiple Languages)
**Primary:** TypeScript/Bun TUI (most universal)  
**Alternative:** C# Script (if .NET is primary language)

### Windows-Heavy Team
**Primary:** PowerShell Scripts  
**Alternative:** C# Script

### Linux/macOS-Heavy Team
**Primary:** TypeScript/Bun TUI  
**Alternative:** C# Script

---

## Performance Summary

| Tool | Cold Start | Warm Start | Build (12s task) | Notes |
|------|-----------|-----------|------------------|-------|
| PowerShell | 200-500ms | 200-500ms | 12s | Consistent |
| TypeScript/Bun | ~100ms | ~100ms | 12s | Fastest |
| C# Script | ~500ms | 200-300ms | 12s | Cached compilation |

*Actual task execution time (e.g., build) is identical - the difference is in startup overhead.*

---

## Summary

🎯 **For Production**: PowerShell Scripts (most complete)  
⚡ **For Speed**: TypeScript/Bun TUI (fastest startup)  
🔷 **For .NET Devs**: C# Script (native integration)  

**All three are valid choices!** Choose based on your team's preferences, existing tooling, and requirements.
