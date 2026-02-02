# Project Selection Improvements - Summary

**Date:** 2026-02-02  
**Status:** ✅ Completed  
**Changes:** Fixed server tasks to use DOTNET_PUBLISH_PROJECT, added build-project and run-project with selection

---

## Changes Made

### 1. Fixed Server Tasks (No Selection Required)

**Changed Tasks:**
- `server-build` - Now directly uses `DOTNET_PUBLISH_PROJECT` from `.env`
- `server-publish` - Now directly uses `DOTNET_PUBLISH_PROJECT` from `.env`
- `server-publish-release` - Now directly uses `DOTNET_PUBLISH_PROJECT` from `.env`
- `server-publish-sc` - Now directly uses `DOTNET_PUBLISH_PROJECT` from `.env`
- `server-run-dev` - Now directly uses `DOTNET_PUBLISH_PROJECT` from `.env`

**Behavior:**
- These tasks NO LONGER prompt for project selection
- They use the web server project defined in `.env`: `DOTNET_PUBLISH_PROJECT=src/Presentation.Web.Server/Presentation.Web.Server.csproj`
- If `.env` is not configured, they show an error

### 2. Added New Project Selection Tasks

**New Tasks:**
- `build-project` - Build a specific project (with selection prompt)
- `run-project` - Run a specific project (with selection prompt)

**Behavior:**
- Both tasks prompt the user to select any `.csproj` from the solution
- Interactive mode: Searchable list of all projects with cancel option
- Non-interactive mode: Uses first available project
- User can cancel selection and return to menu

### 3. Enhanced Prompts Module

**File:** `lib/Prompts.csx` (247 lines, +139 from previous)

**New Methods:**
- `SelectFromListAsync(title, options, defaultValue)` - Generic selection with cancel support
- `SelectSolutionAsync(context)` - Solution file selection with cancel support
- `SelectModuleAsync(context)` - Module selection with cancel support
- `SelectRidAsync(defaultValue)` - Runtime identifier (RID) selection with cancel support

**Updated Methods:**
- `SelectProjectAsync()` - Now supports cancel option ("✕ Cancel")

**Key Features:**
- All selection prompts have a cancel option ("✕ Cancel")
- Canceling returns empty string, allowing tasks to handle gracefully
- Works in both interactive and non-interactive modes
- Search-enabled for large lists
- Consistent UX across all selection prompts

### 4. Updated BdkUI for Cancel Support

**File:** `lib/BdkUI.csx`

**Changes:**
- `SelectSolutionFileAsync()` now uses `Prompts.SelectSolutionAsync()`
- Handles cancellation gracefully - exits if no solution selected
- Simplified implementation by delegating to Prompts module

### 5. Added Missing Using Statement

**File:** `lib/TaskRegistry.csx`

**Changes:**
- Added `using Spectre.Console;` for AnsiConsole access
- Required for error messages in server tasks

---

## Pack vs Pack-Projects - What's the Difference?

### `pack` - Pack Solution
**Description:** Creates NuGet packages for the entire solution
**Command:** `dotnet pack bITdevKit.Examples.GettingStarted.slnx`
**Output:** Packages all packable projects in the solution
**Use Case:** When you want to package all projects at once
**Default Configuration:** Release builds

### `pack-projects` - Pack Module Projects
**Description:** Packs only module projects (Domain/Application/Infrastructure/Presentation)
**Command:** Packs individual module projects in `src/Modules/`
**Output:** Packages for each module layer separately
**Use Case:** When you want to package module layers for distribution
**Configuration:** Release builds, output to `.tmp/packages/`
**Projects Packaged:** 
- `CoreModule.Domain.csproj`
- `CoreModule.Application.csproj`
- `CoreModule.Infrastructure.csproj`
- `CoreModule.Presentation.csproj`

**Key Differences:**
| Feature | pack | pack-projects |
|----------|-------|---------------|
| Scope | Entire solution | Only module projects |
| Projects | All packable projects | Module layers only |
| Output | Standard dotnet pack output | `.tmp/packages/` directory |
| Use Case | General packaging | Module distribution |

---

## Task Count Update

**Previous:** 26 tasks  
**Current:** 28 tasks (+2 new tasks)

**All Build & Maintenance Tasks (23 total):**
```
clean, restore, build, build-release, build-nr, pack, pack-projects,
tool-restore, server-build, server-publish, server-publish-release,
server-publish-sc, server-run-dev, server-watch, build-project, run-project,
update-packages, update-packages-devkit, format-apply, format-check,
analyzers, analyzers-export
```

**New This Session:**
- build-project (project selection with cancel)
- run-project (project selection with cancel)

**Fixed This Session:**
- server-build (no selection, uses DOTNET_PUBLISH_PROJECT)
- server-publish (no selection, uses DOTNET_PUBLISH_PROJECT)
- server-publish-release (no selection, uses DOTNET_PUBLISH_PROJECT)
- server-publish-sc (no selection, uses DOTNET_PUBLISH_PROJECT)
- server-run-dev (no selection, uses DOTNET_PUBLISH_PROJECT)

---

## Usage Examples

### Server Tasks (No Selection)
```bash
# Build the web server project (uses DOTNET_PUBLISH_PROJECT)
./bdk-cli.sh server-build

# Publish web server (Debug)
./bdk-cli.sh server-publish

# Publish web server (Release)
./bdk-cli.sh server-publish-release

# Publish as single-file
./bdk-cli.sh server-publish-sc

# Run web server in dev mode
./bdk-cli.sh server-run-dev
```

### Project Tasks (With Selection)
```bash
# Build any project (will prompt for selection)
./bdk-cli.sh build-project

# Run any project (will prompt for selection)
./bdk-cli.sh run-project
```

### Non-Interactive Mode
```bash
# Build project (uses first available)
export NON_INTERACTIVE=1
./bdk-cli.sh build-project

# Server tasks still require DOTNET_PUBLISH_PROJECT in .env
./bdk-cli.sh server-build  # Will use configured web server project
```

---

## Testing

✅ **Compilation:** All files compile successfully  
✅ **Server Tasks:** Use DOTNET_PUBLISH_PROJECT correctly  
✅ **Build-Project:** Prompts for project selection with cancel  
✅ **Run-Project:** Prompts for project selection with cancel  
✅ **Solution Selection:** Now supports cancel in BdkUI  
✅ **Total Tasks:** 28 tasks (from 26)

---

## Future Selection Methods Available

The `Prompts.csx` module now provides ready-to-use selection methods for future task categories:

```csharp
// Project selection (already used by build-project, run-project)
await Prompts.SelectProjectAsync(context, "Select a project:");

// Solution selection (used by BdkUI)
await Prompts.SelectSolutionAsync(context);

// Module selection (for EF tasks, testing tasks)
await Prompts.SelectModuleAsync(context);

// Runtime identifier selection (for publish tasks)
await Prompts.SelectRidAsync("linux-x64");

// Generic list selection (for any custom selection)
await Prompts.SelectFromListAsync("Select option:", options, defaultValue);

// Text input with default
await Prompts.PromptTextAsync("Enter value:", "default");

// Yes/No confirmation
Prompts.ConfirmAsync("Continue?", true);
```

---

## Files Modified

1. **lib/Prompts.csx** - Rewritten with cancel support, 247 lines
2. **lib/TaskRegistry.csx** - Added build-project, run-project, fixed server tasks
3. **lib/BdkUI.csx** - Updated to use Prompts.SelectSolutionAsync with cancel handling
