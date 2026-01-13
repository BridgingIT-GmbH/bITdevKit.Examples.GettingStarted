---
description: Export current OpenCode session to repository for team collaboration
agent: build
---

# Session Save Command

This command exports the current OpenCode session to the `.opencode/sessions/` directory so it can be committed to git and shared with your team or continued on another machine.

## Instructions

Follow these steps to save the current session:

1. **Get the current session ID**:
   - Run: `opencode session list -n 1 --format json`
   - Parse the JSON output to get the most recent session ID for this project
   - The session list is sorted by recency, so the first entry is the current/most recent session

2. **Generate a descriptive filename**:
   - Format: `YYYY-MM-DD-HH-MM-<title>.json`
   - Use current date/time for the timestamp portion
   - Extract a clean title from the session (use the session's title if available, or create one from the first message)
   - Remove special characters, limit to 50 chars, convert spaces to hyphens
   - Example: `2026-01-13-14-35-customer-validation-feature.json`

3. **Export the session**:
   - Create the `.opencode/sessions/` directory if it doesn't exist
   - Run: `opencode export <session-id> > .opencode/sessions/<filename>.json`
   - Verify the export was successful by checking the file exists and has content

4. **Provide feedback to user**:
   - Display success message with the filename
   - Show the file size and number of messages in the session
   - Remind the user they can commit this to git when ready:
     ```
     Session saved to: .opencode/sessions/<filename>.json
     
     To share with your team:
       git add .opencode/sessions/
       git commit -m "Session: <brief description>"
       git push
     ```

5. **Error handling**:
   - If no active session exists, inform the user
   - If export fails, show the error and suggest troubleshooting steps
   - If the sessions directory cannot be created, check permissions

## Example Output

```
✓ Session exported successfully!

File: .opencode/sessions/2026-01-13-14-35-customer-feature.json
Size: 127 KB
Messages: 45

To share this session with your team or continue on another machine:
  git add .opencode/sessions/
  git commit -m "Session: Customer validation feature implementation"
  git push

On another machine, use /session-load to import this session.
```

## Notes

- Only the most recent session for the current project is saved
- Sessions can be large (100KB+) - consider saving only milestone sessions
- Review the exported JSON before committing if it might contain sensitive information
- Use descriptive commit messages to help team members understand the session context
