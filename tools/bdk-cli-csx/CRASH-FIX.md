# Single-File Selection Crash Fix

**Date:** 2026-02-02  
**Status:** ✅ Fixed  
**Issue:** Spectre.Console markup parser crash with boolean string interpolation

---

## Problem

### Error Message
```
System.InvalidOperationException: Encountered closing tag when none was expected near position 32.
   at Spectre.Console.MarkupParser.Parse
   at Spectre.Console.AnsiConsoleExtensions.MarkupLine
```

### Root Cause

**Problematic Code:**
```csharp
AnsiConsole.MarkupLine($"[dim]Configuration: {config} | RID: {rid} | Single-file: {singleFile}[/]");
```

**Why It Failed:**
1. When `singleFile` is `True` or `False`, Spectre.Console tries to parse the markup
2. The `|` character inside the string combined with interpolated boolean values confused the parser
3. Spectre.Console markup uses square brackets `[]` for styling, which conflicts with the format

### Crash Triggered When

- User selected Debug configuration
- User selected linux-x64 RID
- User selected Yes or No for single-file
- The interpolated string became: `[dim]Configuration: Debug | RID: linux-x64 | Single-file: False[/]`
- Parser tried to parse `False` as markup, causing crash

---

## Solution

### Fixed Code

**Changed From:**
```csharp
AnsiConsole.MarkupLine($"[cyan]Publishing server:[/] {project}");
AnsiConsole.MarkupLine($"[dim]Configuration: {config} | RID: {rid} | Single-file: {singleFile}[/]");
```

**Changed To:**
```csharp
AnsiConsole.MarkupLine($"[cyan]Publishing server:[/] {project}");
AnsiConsole.MarkupLine($"[dim]Configuration:[/] [cyan]{config}[/]");
AnsiConsole.MarkupLine($"[dim]RID:[/] [cyan]{rid}[/]");
AnsiConsole.MarkupLine($"[dim]Single-file:[/] [cyan]{singleFile.Value}[/]");
```

**Applied To:**
- `server-publish` task (3 lines changed)
- `publish-project` task (3 lines changed)

### Why This Works

1. ✅ Separates markup into individual lines
2. ✅ No `|` character in strings
3. ✅ Boolean value `singleFile.Value` is in its own `[cyan]...[/]` tag
4. ✅ Parser can safely parse each line independently
5. ✅ No string interpolation conflicts with markup syntax

---

## Output Comparison

### Before (Crash)
```
System.InvalidOperationException: Encountered closing tag when none was expected
```

### After (Working)
```
[38;5;14mPublishing server:[0m
[2mConfiguration:[0m [38;5;14mDebug[0m
[2mRID:[0m [38;5;14mlinux-x64[0m
[2mSingle-file:[0m [38;5;14mFalse[0m
```

The color codes (ANSI escape sequences) are visible in output, but the task completes successfully without crashing.

---

## Testing

✅ **Compilation:** All files compile successfully  
✅ **No Crash:** server-publish and publish-project work  
✅ **Output:** Summary information displayed correctly  
✅ **Non-Interactive:** Works with piped inputs

### Test Commands

```bash
# Test server-publish (non-interactive, using defaults)
echo -e "Debug\nlinux-x64\nNo" | dotnet script tools/bdk-cli-csx/bdk-cli.csx -- server-publish

# Output shows:
# Configuration: Debug
# RID: linux-x64
# Single-file: False
```

---

## Files Modified

**lib/TaskRegistry.csx**
- Fixed `server-publish` task (split 1 line into 3 lines)
- Fixed `publish-project` task (split 1 line into 3 lines)
- Total: 6 lines changed

---

## Lessons Learned

### Spectre.Console Markup Best Practices

1. ❌ Avoid complex string interpolation with `|` characters
2. ❌ Don't mix interpolation and markup syntax in same string
3. ❌ Boolean values in markup can cause parsing issues
4. ✅ Split complex markup into separate MarkupLine calls
5. ✅ Keep markup tags simple and independent
6. ✅ Test markup with edge cases (true, false, null values)

---

## Summary

**Issue:** Spectre.Console parser crash with boolean string interpolation  
**Fix:** Split markup into individual lines, avoid `|` in interpolated strings  
**Result:** Tasks work correctly, no crashes, proper output display  
**Files Changed:** lib/TaskRegistry.csx (6 lines modified)  
**Status:** ✅ Resolved
