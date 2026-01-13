# OpenCode Session Management

This directory contains exported OpenCode sessions that can be shared across your team and continued on different machines.

## Quick Start

### Save Your Current Session

When you reach a good stopping point (feature complete, end of day, etc.):

```bash
# In OpenCode TUI
/session-save
```

This exports your current session to this directory with a timestamped filename.

### Load a Saved Session

To continue from a saved session (yours or a teammate's):

```bash
# In OpenCode TUI
/session-load
```

Select from the list of available sessions and continue where you left off.

---

## Workflow for Team Collaboration

### Scenario 1: Continue Work on Another Machine

**On Machine A:**
```bash
# At end of work session
You: /session-save
OpenCode: Session saved to .opencode/sessions/2026-01-13-14-35-customer-feature.json

# Commit and push
git add .opencode/sessions/
git commit -m "Session: Customer validation feature - completed domain logic"
git push
```

**On Machine B:**
```bash
# Pull latest changes
git pull

# Start OpenCode and load the session
opencode
You: /session-load
OpenCode: [Lists available sessions]
You: 1  # Select the session
OpenCode: Session imported successfully!

# Continue working from where you left off
```

### Scenario 2: Share Context with Team Member

**Developer A (working on a complex feature):**
```bash
# Save session at milestone
/session-save
git add .opencode/sessions/2026-01-13-refactoring-work.json
git commit -m "Session: Refactoring repository pattern - need review"
git push
```

**Developer B (reviewing/continuing work):**
```bash
git pull
opencode
/session-load  # Load the refactoring session
# Now has full context of what was done and discussed with OpenCode
```

### Scenario 3: Preserving Important Conversations

Some sessions are valuable as documentation:
- Complex problem-solving sessions
- Architecture decisions made with OpenCode
- Debugging sessions that solved tricky issues
- Feature implementation patterns

Save these as reference material:
```bash
/session-save
# Commit with descriptive message
git commit -m "Session: How we solved the N+1 query issue in CustomerRepository"
```

Team members can later review these sessions to understand decisions and approaches.

---

## Best Practices

### When to Save Sessions

**DO save sessions when:**
- Completing a significant feature or milestone
- Solving a complex problem that took multiple iterations
- Making architectural decisions
- Need to continue on another machine
- Want to share context with team members
- End of day with unfinished work

**DON'T save sessions for:**
- Every small change or quick question
- Trivial conversations
- Exploratory/experimental work you won't continue

### Naming Conventions

Sessions are automatically named: `YYYY-MM-DD-HH-MM-<title>.json`

For special sessions, you can rename files to be more descriptive:
```bash
# Auto-generated name
2026-01-13-14-35-customer-feature.json

# More descriptive (optional)
2026-01-13-customer-validation-architecture-decisions.json
```

### Commit Message Guidelines

Use descriptive commit messages so team members understand the session context:

```bash
# Good commit messages
git commit -m "Session: Customer validation feature - completed and tested"
git commit -m "Session: Debugging EF Core N+1 queries - solution found"
git commit -m "Session: Refactoring CoreModule structure - WIP"

# Less helpful
git commit -m "Saved session"
git commit -m "Update sessions"
```

### Managing Session Size

Sessions can grow large (100KB+). Consider:

1. **Save milestone sessions only** - Don't save every session
2. **Use .gitignore patterns** - Exclude WIP sessions (see `.gitignore` in this directory)
3. **Clean up old sessions** - Remove outdated sessions that are no longer relevant
4. **Git LFS for large sessions** - If sessions get very large (rare), consider Git LFS

---

## File Structure

```
.opencode/sessions/
├── README.md                                          # This file
├── .gitignore                                         # Control what gets committed
├── 2026-01-13-14-35-customer-feature.json            # Saved session
├── 2026-01-12-10-20-refactoring-work.json            # Saved session
└── 2026-01-11-api-endpoints.json                     # Saved session
```

Each JSON file contains:
- Full conversation history (prompts and responses)
- File changes and diffs
- Tool executions and results
- Session metadata (model used, timestamps, etc.)

---

## Technical Details

### Session Storage Location

OpenCode stores active sessions in: `~/.local/share/opencode/storage/session/`

These are personal/local sessions. The `/session-save` command exports them to this repository directory for sharing.

### Commands Available

| Command | Description |
|---------|-------------|
| `/session-save` | Export current session to `.opencode/sessions/` |
| `/session-load` | Import and continue from a saved session |
| `opencode session list` | List all local sessions (CLI) |
| `opencode export <id>` | Export specific session (CLI) |
| `opencode import <file>` | Import session from file (CLI) |

### Manual Export/Import (Advanced)

If you prefer using CLI commands directly:

```bash
# List sessions to find the ID
opencode session list

# Export manually
opencode export <session-id> > .opencode/sessions/my-session.json

# Import manually
opencode import .opencode/sessions/my-session.json
```

---

## Troubleshooting

### Session Won't Load

**Problem:** `/session-load` fails to import a session

**Solutions:**
1. Check if the JSON file is valid: `cat .opencode/sessions/<file>.json | jq`
2. Verify file permissions: `ls -la .opencode/sessions/`
3. Try manual import: `opencode import .opencode/sessions/<file>.json`
4. Check OpenCode logs: Look for error messages

### No Sessions Available

**Problem:** `/session-load` shows no sessions

**Solutions:**
1. Verify sessions directory exists: `ls .opencode/sessions/`
2. Check if you pulled latest from git: `git pull`
3. Verify .gitignore isn't excluding all sessions
4. Save your first session: `/session-save`

### Session Too Large

**Problem:** Git complains about large session files

**Solutions:**
1. Consider if you need to commit this session
2. Add pattern to `.gitignore` to exclude it
3. Use Git LFS for large files (optional)
4. Clean up old/unnecessary sessions

### Session Contains Sensitive Data

**Problem:** Worried about committing sensitive information

**Solutions:**
1. Review the JSON file before committing
2. Use naming convention `*-sensitive-*.json` (excluded by default in .gitignore)
3. Edit the JSON to remove sensitive portions
4. Don't save sessions that interact with production data/secrets

---

## FAQ

**Q: Can I delete old sessions?**
A: Yes, just delete the JSON files and commit the change. Sessions are just files.

**Q: What happens if two people modify the same session?**
A: Git will handle it like any file conflict. Each import creates a new session ID, so there's no actual conflict in OpenCode itself.

**Q: Can I load a session from a different project?**
A: Yes, but it's not recommended. Sessions are tied to the project context (file paths, etc.).

**Q: How many sessions should we keep?**
A: Keep milestone sessions and important reference sessions. Delete outdated ones periodically.

**Q: Can I edit a session JSON before loading?**
A: Technically yes, but not recommended unless you know the structure. Better to import and continue with new messages.

**Q: Does loading a session overwrite my current session?**
A: No, importing creates a new session. Your current session remains in your local history.

---

## Integration with Development Workflow

### With Feature Branches

```bash
# Create feature branch
git checkout -b feature/customer-validation

# Work with OpenCode, save milestone sessions
/session-save
git add .opencode/sessions/
git commit -m "Session: Customer validation - phase 1 complete"

# Continue on feature branch
# When feature is done, sessions go with the PR
```

### With Pull Requests

Sessions provide valuable context in PRs:
- Reviewers can see the thought process
- Complex decisions are documented
- Can replay the development process if needed

Consider including session info in PR descriptions:
```markdown
## Development Session

See `.opencode/sessions/2026-01-13-customer-validation.json` for full context
on implementation decisions and alternatives considered.
```

---

## Support

For issues or questions:
1. Check OpenCode docs: https://opencode.ai/docs
2. Review this README
3. Ask team members who have used this workflow
4. File an issue in the project repository

---

**Last Updated:** January 13, 2026
**OpenCode Version:** Compatible with v1.0+
