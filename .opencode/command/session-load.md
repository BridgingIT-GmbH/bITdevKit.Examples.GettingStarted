---
description: Import and continue from a saved OpenCode session
agent: build
---

# Session Load Command

This command lists available sessions from `.opencode/sessions/` and imports the selected one so you can continue working from where you or a team member left off.

## Instructions

Follow these steps to load a saved session:

1. **Check for available sessions**:
   - List all JSON files in `.opencode/sessions/` directory
   - If the directory doesn't exist or is empty, inform the user:
     ```
     No saved sessions found in .opencode/sessions/
     
     To save a session, use the /session-save command.
     If you just pulled from git, make sure the sessions directory was included.
     ```

2. **Parse and display session information**:
   - For each JSON file, read and parse it to extract metadata:
     - Filename (use as identifier)
     - Session title (from the session data)
     - Number of messages
     - File size (for reference)
     - Date/time from filename or session data
   - Sort by date (newest first)
   - Display in a numbered list format:
     ```
     Available sessions in .opencode/sessions/:
     
     1. 2026-01-13-14-35-customer-feature.json
        Title: Customer validation feature implementation
        Messages: 45 | Size: 127 KB | Date: Jan 13, 2026 2:35 PM
     
     2. 2026-01-12-10-20-refactoring-work.json
        Title: Repository pattern refactoring
        Messages: 23 | Size: 89 KB | Date: Jan 12, 2026 10:20 AM
     ```

3. **Prompt user to select a session**:
   - Ask which session to load (by number or filename)
   - Handle invalid selections gracefully
   - Allow cancellation

4. **Import the selected session**:
   - Run: `opencode import .opencode/sessions/<selected-filename>`
   - Wait for the import to complete
   - Capture any output or errors

5. **Provide feedback**:
   - On success:
     ```
     ✓ Session imported successfully!
     
     Loaded: customer-feature.json (45 messages)
     
     You can now continue the conversation where it left off.
     Note: This creates a new session ID but preserves all the conversation history.
     ```
   - On failure:
     ```
     ✗ Failed to import session: <error message>
     
     Troubleshooting:
     - Verify the JSON file is valid
     - Check file permissions
     - Try: opencode import .opencode/sessions/<filename> manually
     ```

6. **Additional options**:
   - If user provides an argument (filename), skip the selection prompt and load that directly
   - Example: `/session-load 2026-01-13-14-35-customer-feature.json`

## Example Interaction

```
User: /session-load

OpenCode: 
Available sessions in .opencode/sessions/:

1. 2026-01-13-14-35-customer-feature.json
   Title: Customer validation feature implementation
   Messages: 45 | Size: 127 KB | Date: Jan 13, 2026 2:35 PM

2. 2026-01-12-10-20-refactoring-work.json
   Title: Repository pattern refactoring
   Messages: 23 | Size: 89 KB | Date: Jan 12, 2026 10:20 AM

3. 2026-01-11-15-10-api-endpoint-work.json
   Title: New API endpoints for customer module
   Messages: 31 | Size: 95 KB | Date: Jan 11, 2026 3:10 PM

Which session would you like to load? (Enter number or filename, or 'cancel')

User: 1

OpenCode: 
✓ Session imported successfully!

Loaded: customer-feature.json (45 messages)

You can now continue the conversation where it left off.
The session history has been restored and you can pick up from the last message.
```

## Notes

- Importing a session creates a new session ID but preserves the conversation history
- The original session file remains in `.opencode/sessions/` - importing is non-destructive
- You can load the same session multiple times (useful for trying different approaches)
- After importing, you can immediately start sending messages to continue the conversation
- The imported session will appear in your local session list (`opencode session list`)

## Error Handling

- **No sessions found**: Guide user to use `/session-save` first or check if they pulled latest from git
- **Corrupt JSON**: Suggest checking the file or re-exporting from the original machine
- **Permission errors**: Check file/directory permissions
- **Import command fails**: Show the full error and suggest manual import for debugging
