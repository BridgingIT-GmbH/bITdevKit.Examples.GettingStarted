# BDK TUI - Changelog

## v0.2.0 - Phase 2 Complete (January 29, 2026)

### 🎨 Fancy OpenTUI Integration

**New Features:**
- ✅ **OpenTUI Select component** with arrow key navigation (↑/↓ or j/k)
- ✅ **Vim-style navigation** (j/k keys)
- ✅ **Fast scroll** with Shift+arrows (5 items at a time)
- ✅ **Color-coded UI** with cyan highlights and descriptions
- ✅ **No screen clearing** - task output stays visible, menu renders below
- ✅ **Smart navigation** - ESC goes back, Q quits, Enter selects
- ✅ **Visual feedback** - Selected items highlighted in cyan with black text

**UI Improvements:**
- Task descriptions visible in menu
- Help text at bottom of screen
- Smooth arrow key navigation
- Wrap-around selection
- No keypress wait after task execution

**Architecture:**
- SelectMenu component using OpenTUI SelectRenderable
- Proper event handling (ITEM_SELECTED)
- Global keyboard shortcuts (ESC, Q)
- Clean component lifecycle

### 🚀 Performance

- Startup: ~100ms
- Menu navigation: Instant (arrow keys)
- Task execution: Same as direct CLI

### 📝 Developer Experience

**Interactive Mode:**
```bash
./bdk-tui.sh
# → Fancy select menu with arrows
# → Select with Enter
# → Task runs, output shows
# → Menu appears below immediately
# → No waiting!
```

**Direct Mode (unchanged):**
```bash
./bdk-tui.sh build
# → Immediate execution
# → Perfect for VS Code
```

---

## v0.1.0 - Phase 1 Complete (January 29, 2026)

### ⚡ Initial Release

**Core Features:**
- ✅ Cross-platform command execution (Windows/Linux/macOS)
- ✅ Pure TypeScript (no PowerShell dependency)
- ✅ Runtime configuration loading (.env)
- ✅ Dual operation modes (interactive + direct)
- ✅ .NET CLI wrapper with solution file auto-detection
- ✅ Task registry system
- ✅ Real-time output streaming
- ✅ Color-coded output (ANSI escape codes)

**Available Tasks:**
- `build` - Build solution
- `clean` - Clean solution
- `restore` - Restore NuGet packages
- `version` - Show .NET SDK version

**Launchers:**
- `bdk-tui.ps1` - PowerShell launcher
- `bdk-tui.sh` - Bash launcher

**Architecture:**
- Config loader with multi-path search
- Cross-platform executor using Bun.spawn
- Task registry with categories
- Screen-based navigation

---

## Roadmap

### Phase 3 (Next)
- [ ] Add EF Core tasks (13 tasks)
- [ ] Add Docker tasks (10 tasks)
- [ ] Add Testing tasks (9 tasks)
- [ ] Module selection dialog
- [ ] Input dialog for migration names

### Phase 4 (Future)
- [ ] All 80+ tasks from PowerShell BDK
- [ ] Compiled binaries (win/linux/macos)
- [ ] Task history
- [ ] Search/filter
