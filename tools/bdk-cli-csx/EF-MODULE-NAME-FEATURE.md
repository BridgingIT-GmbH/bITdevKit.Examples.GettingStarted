# EF Script & Bundle - Module Name Feature

**Date:** 2026-02-02  
**Status:** ✅ Completed

---

## Changes Made

### 1. Updated File Naming Pattern

**Before:**
- SQL script: `.tmp/ef/efscript.sql`
- Bundle: `.tmp/ef/efbundle.exe`

**After:**
- SQL script: `.tmp/ef/efscript_{moduleLower}.sql`
- Bundle: `.tmp/ef/efbundle_{moduleLower}.exe`

**Example with module "CoreModule":**
- SQL script: `.tmp/ef/efscript_coremodule.sql`
- Bundle: `.tmp/ef/efbundle_coremodule.exe`

### 2. Added Success Confirmation

**Files Modified:**
- `lib/DotnetCli.csx` - Updated EfScriptAsync() and EfBundleAsync() methods
- `lib/TaskRegistry.csx` - Updated default prompts for ef-script and ef-bundle tasks

**Changes:**

#### EfScriptAsync()
```csharp
// Before
var output = string.IsNullOrEmpty(outputPath) ? ".tmp/ef/efscript.sql" : outputPath;

// After
var moduleLower = moduleName.ToLower();
var output = string.IsNullOrEmpty(outputPath) ? $".tmp/ef/efscript_{moduleLower}.sql" : outputPath;
```

#### EfBundleAsync()
```csharp
// Before
var output = string.IsNullOrEmpty(outputPath) ? ".tmp/ef/efbundle.exe" : outputPath;

// After
var moduleLower = moduleName.ToLower();
var output = string.IsNullOrEmpty(outputPath) ? $".tmp/ef/efbundle_{moduleLower}.exe" : outputPath;
```

#### Success Messages
Added success confirmation after task completion:

**For ef-script:**
```
[green]✓ Script written:[/] [cyan]/full/path/to/.tmp/ef/efscript_coremodule.sql[/]
```

**For ef-bundle:**
```
[green]✓ Bundle written:[/] [cyan]/full/path/to/.tmp/ef/efbundle_coremodule.exe[/]
```

---

## Benefits

✅ **Unique filenames** - Prevents overwriting scripts from different modules  
✅ **Identifiable outputs** - Easy to see which module generated which file  
✅ **Lowercase consistency** - Module name normalized to lowercase  
✅ **Full path display** - Shows exactly where file was written  
✅ **Backward compatible** - Custom paths still supported via prompt  

---

## Usage Examples

### Default Output (with module name)
```bash
# Export SQL script for CoreModule
dotnet script tools/bdk-cli-csx/bdk-cli.csx ef-script

# Output: .tmp/ef/efscript_coremodule.sql
# Displayed: ✓ Script written: /home/user/project/.tmp/ef/efscript_coremodule.sql
```

### Custom Output Path
```bash
# Export SQL script to custom location
dotnet script tools/bdk-cli-csx/bdk-cli.csx ef-script

# Prompt: Output path:
# Default: .tmp/ef/efscript_coremodule.sql
# User input: /custom/path/myscript.sql
# Output: /custom/path/myscript.sql (module name not included when custom path specified)
```

### Multiple Modules
```bash
# CoreModule: .tmp/ef/efscript_coremodule.sql
# InventoryModule: .tmp/ef/efscript_inventorymodule.sql
# OrderModule: .tmp/ef/efscript_ordermodule.sql
```

---

## Files Modified

1. **lib/DotnetCli.csx**
   - EfScriptAsync(): Added module name in lowercase, success message
   - EfBundleAsync(): Added module name in lowercase, success message
   - Lines: 375 (+16)

2. **lib/TaskRegistry.csx**
   - ef-script task: Updated default prompt with module name
   - ef-bundle task: Updated default prompt with module name
   - Lines: 630 (+2)

3. **EF-TASKS-COMPLETE.md**
   - Updated file operations section with new naming pattern

---

## Summary

✅ **Module name included** in default script/bundle filenames  
✅ **Lowercase normalization** for consistent naming  
✅ **Success messages** display full output paths  
✅ **Custom paths** still supported (module name omitted)  
✅ **Backward compatible** with existing workflows  

**Total changes:** 18 lines modified across 2 files
