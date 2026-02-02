# Publish Tasks Consolidation - Summary

**Date:** 2026-02-02  
**Status:** ✅ Completed  
**Changes:** Merged 6 publish tasks into 2 with sequential prompts

---

## Problem Solved

### Previous State (6 Separate Publish Tasks)
```
server-publish           # Debug, RID only
server-publish-release   # Release, RID only
server-publish-sc        # Release, RID, single-file only
publish-project         # Debug, RID only
publish-project-release # Release, RID only
publish-project-sc      # Release, RID, single-file only
```

**Issues:**
- ❌ Too many tasks for the same operation
- ❌ Users had to know which task to use for each scenario
- ❌ Couldn't easily switch between Debug/Release
- ❌ Couldn't easily choose single-file vs multi-file
- ❌ Confusing naming and options

---

## Solution Implemented

### Current State (2 Consolidated Publish Tasks)
```
server-publish   # Publish web server (config + RID + single-file prompts)
publish-project # Publish any project (config + RID + single-file prompts)
```

### Sequential Prompt Flow

Both tasks now use the same prompt sequence:

**1. Configuration Selection**
```
Select configuration:
> Debug
  Release
✕ Cancel
```

**2. RID Selection**
```
Select runtime identifier (RID):
> linux-x64 (default)
  linux-arm64
  win-x64
  win-arm64
  osx-x64
  osx-arm64
✕ Cancel
```

**3. Single-File Selection**
```
Create single-file executable?
> No (multi-file) (default)
  Yes (single-file)
```

**4. Publish Execution**
```
Publishing server: src/Presentation.Web.Server/Presentation.Web.Server.csproj
Configuration: Release | RID: linux-x64 | Single-file: Yes
```

---

## New Selection Methods Added

### 1. SelectConfigurationAsync()
```csharp
public static async Task<string> SelectConfigurationAsync(string defaultValue = "Debug")
```
- Options: Debug, Release
- Default: Debug (or custom)
- Cancel support
- Non-interactive fallback

### 2. SelectSingleFileAsync()
```csharp
public static async Task<bool?> SelectSingleFileAsync(bool defaultValue = false)
```
- Options: Yes (single-file), No (multi-file)
- Default: No (multi-file) or custom
- Cancel support (returns null)
- Returns `bool?` (nullable) to distinguish cancelled vs selected

---

## Task Comparison

### Before (6 Tasks)

| Task | Config | RID | Single-File | Project |
|-------|---------|-------|--------------|----------|
| server-publish | Debug (fixed) | Yes | No (fixed) | From .env |
| server-publish-release | Release (fixed) | Yes | No (fixed) | From .env |
| server-publish-sc | Release (fixed) | Yes | Yes (fixed) | From .env |
| publish-project | Debug (fixed) | Yes | No (fixed) | User selects |
| publish-project-release | Release (fixed) | Yes | No (fixed) | User selects |
| publish-project-sc | Release (fixed) | Yes | Yes (fixed) | User selects |

**Total:** 6 tasks, 18 fixed combinations

### After (2 Tasks)

| Task | Config | RID | Single-File | Project |
|-------|---------|-------|--------------|----------|
| server-publish | User selects | User selects | User selects | From .env |
| publish-project | User selects | User selects | User selects | User selects |

**Total:** 2 tasks, 8 flexible combinations (2 configs × 2 rids × 2 single-file = 8)

**Advantages:**
- ✅ 67% fewer tasks (6 → 2)
- ✅ All combinations possible (8 scenarios)
- ✅ Flexible - users choose each option at runtime
- ✅ Consistent workflow for both server and project publishes
- ✅ Can cancel any step of the process

---

## Example Workflows

### Scenario 1: Publish Server for Linux (Debug, Multi-File)
```bash
./bdk-cli.sh server-publish

# Prompts:
Select configuration: → Debug
Select runtime identifier (RID): → linux-x64
Create single-file executable? → No

# Output:
Publishing server: src/Presentation.Web.Server/Presentation.Web.Server.csproj
Configuration: Debug | RID: linux-x64 | Single-file: False
```

### Scenario 2: Publish Server for Windows (Release, Single-File)
```bash
./bdk-cli.sh server-publish

# Prompts:
Select configuration: → Release
Select runtime identifier (RID): → win-x64
Create single-file executable? → Yes

# Output:
Publishing server: src/Presentation.Web.Server/Presentation.Web.Server.csproj
Configuration: Release | RID: win-x64 | Single-file: True
```

### Scenario 3: Publish Module for macOS ARM64
```bash
./bdk-cli.sh publish-project

# Prompts:
Select a project to publish: → CoreModule.Application
Select configuration: → Release
Select runtime identifier (RID): → osx-arm64
Create single-file executable? → Yes

# Output:
Publishing project: src/Modules/CoreModule/CoreModule.Application/CoreModule.Application.csproj
Configuration: Release | RID: osx-arm64 | Single-file: True
```

---

## Code Changes

### Prompts.csx (+58 lines, total 305 lines)

**New Methods:**
```csharp
public static async Task<string> SelectConfigurationAsync(string defaultValue = "Debug")
public static async Task<bool?> SelectSingleFileAsync(bool defaultValue = false)
```

**Total Selection Methods:** 10
1. SelectProjectAsync
2. FindAllProjects
3. PromptTextAsync
4. ConfirmAsync
5. SelectFromListAsync
6. SelectSolutionAsync
7. SelectModuleAsync
8. SelectRidAsync
9. SelectConfigurationAsync ✨
10. SelectSingleFileAsync ✨

### TaskRegistry.csx (-4 tasks)

**Removed Tasks:**
- server-publish
- server-publish-release
- server-publish-sc
- publish-project
- publish-project-release
- publish-project-sc

**Added Tasks:**
- server-publish (consolidated with prompts)
- publish-project (consolidated with prompts)

**Net Change:** -4 tasks (29 → 25)

---

## Task Count Update

**Before Consolidation:**
- Total: 29 tasks
- Build & Maintenance: 24 tasks
- Testing: 3 tasks
- Utilities: 1 task

**After Consolidation:**
- Total: 25 tasks
- Build & Maintenance: 20 tasks
- Testing: 3 tasks
- Utilities: 1 task

**Net Change:** -4 tasks (-14% reduction in total task count)

---

## Testing

✅ **Compilation:** All files compile successfully  
✅ **Prompts Added:** SelectConfigurationAsync and SelectSingleFileAsync work  
✅ **Task Consolidation:** 6 publish tasks → 2 publish tasks  
✅ **Total Tasks:** 25 unique tasks (down from 29)  
✅ **Workflow:** Sequential prompts for config, RID, and single-file  

---

## All Publish Scenarios Now Supported

| Config | RID | Single-File | Task to Use |
|---------|-------|--------------|---------------|
| Debug | linux-x64 | No | server-publish |
| Debug | linux-x64 | Yes | server-publish |
| Debug | win-x64 | No | server-publish |
| Debug | win-x64 | Yes | server-publish |
| Release | linux-x64 | No | server-publish |
| Release | linux-x64 | Yes | server-publish |
| Release | win-x64 | No | server-publish |
| Release | win-x64 | Yes | server-publish |
| Debug | linux-x64 | No | publish-project |
| Debug | linux-x64 | Yes | publish-project |
| Debug | win-x64 | No | publish-project |
| Debug | win-x64 | Yes | publish-project |
| Release | linux-x64 | No | publish-project |
| Release | linux-x64 | Yes | publish-project |
| Release | win-x64 | No | publish-project |
| Release | win-x64 | Yes | publish-project |

**Total Scenarios:** 16 combinations (2 tasks × 2 configs × 4 rids × 2 single-file options)

---

## Files Modified

1. **lib/Prompts.csx** - Added SelectConfigurationAsync, SelectSingleFileAsync (+58 lines)
2. **lib/TaskRegistry.csx** - Consolidated 6 publish tasks into 2 (-4 tasks)

---

## Migration Status

**Progress:** 25/71 tasks (35%)

**Completed Categories:**
- ✅ Build & Maintenance (20/24 tasks - 83%)
- ✅ Testing (3/9 tasks - 33%)
- ✅ Utilities (1/13 tasks - 8%)

**Remaining Tasks in Build & Maintenance:**
- 📋 4 more tasks (need to check PowerShell CLI)

**Remaining Categories:**
- 📋 Testing (6 more tasks: test-unit-all, test-int-all, coverage, coverage-html, roslynator-*)
- 📋 EF & Persistence (13 tasks)
- 📋 Docker & Containers (11 tasks)
- 📋 Performance & Diagnostics (10 tasks)
- 📋 Security & Compliance (5 tasks)
- 📋 API & Spec (4 tasks)
- 📋 Utilities (12 more tasks: misc-*, doc-*)

**Total Remaining:** 46 tasks

---

## Benefits

**For Users:**
- ✅ Simpler CLI - fewer tasks to remember
- ✅ More flexible - choose any combination at runtime
- ✅ Consistent experience - same prompts for all publish scenarios
- ✅ Can cancel at any step - don't have to redo entire flow

**For Maintenance:**
- ✅ Less code - 2 task definitions instead of 6
- ✅ Reusable prompts - SelectConfigurationAsync and SelectSingleFileAsync for other tasks
- ✅ Easier to extend - can add more prompts without creating new tasks
