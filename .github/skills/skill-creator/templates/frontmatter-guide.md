# YAML Frontmatter Reference for Agent Skills

This document provides a comprehensive guide to the YAML frontmatter structure used in Agent Skills `SKILL.md` files.

## Required Structure

Every `SKILL.md` file MUST begin with YAML frontmatter delimited by `---`:

```yaml
---
skill: skill-name
description: Brief one-line description of what the skill does
globs: []
alwaysApply: false
---
```

## Field Definitions

### `skill` (Required)
- **Type**: String
- **Format**: Lowercase with hyphens (kebab-case)
- **Purpose**: Unique identifier for the skill
- **Rules**:
  - Must match the directory name
  - Use descriptive, action-oriented names
  - Avoid abbreviations unless universally understood
  
**CORRECT Examples:**
```yaml
skill: adr-writer
skill: nuget-manager
skill: domain-add-aggregate
skill: skill-creator
```

**WRONG Examples:**
```yaml
skill: ADRWriter          # WRONG: Not kebab-case
skill: write_adr          # WRONG: Use hyphens, not underscores
skill: adr                # WRONG: Too vague
skill: aggregate          # WRONG: Missing action verb
```

### `description` (Required)
- **Type**: String (single line, no line breaks)
- **Length**: 60-120 characters recommended
- **Purpose**: Shown in skill selection UI; helps AI decide when to invoke
- **Rules**:
  - Start with action verb (e.g., "Creates", "Manages", "Analyzes")
  - Be specific about WHAT the skill does
  - Include WHY it's beneficial (optional but recommended)
  - Avoid generic phrases like "helps with" or "assists in"
  - No trailing period

**CORRECT Examples:**
```yaml
description: Creates Architectural Decision Records following project conventions and best practices
description: Manages NuGet package dependencies across the solution with version conflict detection
description: Adds new Domain Aggregates using Clean Architecture and DDD principles with full CRUD scaffolding
description: Guides creation of high-quality Agent Skills following agentskills.io open standard
```

**WRONG Examples:**
```yaml
description: Helps with ADRs                                    # WRONG: Too vague
description: A tool for writing documentation                   # WRONG: Not specific
description: Creates ADRs.                                      # WRONG: Has trailing period
description: This skill will help you create Architectural 
Decision Records for your project                               # WRONG: Multi-line
description: NuGet stuff                                        # WRONG: Unprofessional, vague
```

### `globs` (Required)
- **Type**: Array of strings
- **Purpose**: File patterns that trigger automatic skill relevance
- **Format**: Standard glob patterns (e.g., `*.md`, `src/**/*.cs`)
- **Rules**:
  - Use empty array `[]` if skill should NOT auto-trigger based on files
  - Use specific patterns to avoid over-triggering
  - Consider both read and write operations
  - Path separators should use forward slashes `/`

**CORRECT Examples:**
```yaml
# ADR writer - triggers when working with ADR files
globs:
  - docs/ADR/*.md
  - docs/adr/*.md

# NuGet manager - triggers when working with project files
globs:
  - "*.csproj"
  - "*.sln"
  - Directory.Build.props
  - Directory.Packages.props

# Domain aggregate - triggers when working with domain layer
globs:
  - src/Modules/*/Domain/**/*.cs
  - src/**/Domain/Model/**/*.cs

# Skill creator - empty because it's meta-level
globs: []
```

**WRONG Examples:**
```yaml
globs:
  - "**/*"                          # WRONG: Too broad, triggers on everything
  
globs:
  - docs\ADR\*.md                   # WRONG: Use forward slashes, not backslashes
  
globs:
  - *.cs                            # WRONG: Too broad for domain-specific skill
  
# Missing entirely                  # WRONG: globs is required field
```

### `alwaysApply` (Required)
- **Type**: Boolean
- **Default**: `false`
- **Purpose**: Controls whether skill is always considered relevant
- **Rules**:
  - Set to `true` ONLY if skill should be globally available
  - Most skills should be `false` (context-triggered)
  - Use `true` sparingly to avoid cognitive overload

**When to Use `true`:**
- Meta-skills (like skill-creator)
- Cross-cutting concerns (logging, error handling)
- General-purpose utilities

**When to Use `false`:**
- Domain-specific operations (most skills)
- Context-dependent workflows
- File-type-specific operations

**CORRECT Examples:**
```yaml
# Meta-skill that could be needed anytime
skill: skill-creator
alwaysApply: true

# Domain-specific workflow (only relevant when working with domain code)
skill: domain-add-aggregate
alwaysApply: false

# File-specific operation
skill: adr-writer
alwaysApply: false
```

## Complete Example Templates

### Minimal Skill (Simple Tool)
```yaml
---
skill: code-formatter
description: Formats code files according to .editorconfig rules with validation
globs:
  - "*.cs"
  - "*.js"
  - "*.ts"
alwaysApply: false
---
```

### Workflow Skill (Multi-Step Process)
```yaml
---
skill: feature-scaffolder
description: Scaffolds complete feature implementation with tests, docs, and migrations using vertical slice architecture
globs:
  - src/Modules/**/*.cs
  - src/**/Application/**/*.cs
alwaysApply: false
---
```

### Meta-Skill (Always Available)
```yaml
---
skill: architecture-advisor
description: Analyzes code changes for architectural violations and suggests improvements based on project ADRs
globs: []
alwaysApply: true
---
```

### Documentation Skill (Specific File Type)
```yaml
---
skill: api-doc-generator
description: Generates API documentation from OpenAPI spec with examples and authentication flows
globs:
  - openapi.yaml
  - openapi.json
  - swagger.json
alwaysApply: false
---
```

## Common Patterns

### Pattern 1: Single File Type
When skill operates on one type of file:
```yaml
globs:
  - "*.csproj"
alwaysApply: false
```

### Pattern 2: Multiple Related Files
When skill operates on related file types:
```yaml
globs:
  - "*.csproj"
  - "*.sln"
  - Directory.*.props
alwaysApply: false
```

### Pattern 3: Directory-Scoped
When skill operates on files in specific directories:
```yaml
globs:
  - docs/ADR/*.md
  - src/Modules/*/Domain/**/*.cs
alwaysApply: false
```

### Pattern 4: Always Available
When skill is globally relevant:
```yaml
globs: []
alwaysApply: true
```

## Validation Checklist

Before finalizing frontmatter, verify:

- [ ] `skill` matches directory name exactly
- [ ] `skill` uses kebab-case (lowercase with hyphens)
- [ ] `description` is a single line (no line breaks)
- [ ] `description` starts with action verb
- [ ] `description` is 60-120 characters (recommended)
- [ ] `description` has no trailing period
- [ ] `globs` is present (even if empty array)
- [ ] `globs` patterns use forward slashes `/`
- [ ] `globs` are specific enough to avoid over-triggering
- [ ] `alwaysApply` is explicitly set to `true` or `false`
- [ ] `alwaysApply: true` is justified (not overused)
- [ ] YAML syntax is valid (proper indentation, quotes for special chars)

## Testing Your Frontmatter

After creating frontmatter, test it by:

1. **Syntax Validation**: Ensure YAML parses correctly
   ```bash
   # Use any YAML validator or parser
   cat SKILL.md | head -n 10 | yaml-validator
   ```

2. **Context Triggering**: Verify skill activates in correct contexts
   - Open files matching `globs` patterns
   - Confirm skill appears in suggestions
   - Check skill doesn't appear when irrelevant

3. **Description Clarity**: Show description to someone unfamiliar
   - Can they understand what it does?
   - Would they know when to use it?

## References

- [agentskills.io Open Standard](https://agentskills.io)
- [VS Code Copilot Agent Skills Documentation](https://code.visualstudio.com/docs/copilot/customization/agent-skills)
- [YAML Specification](https://yaml.org/spec/)
- Project example: `.github/skills/adr-writer/SKILL.md`
- Project example: `.github/skills/nuget-manager/SKILL.md`
