---
agent: 'agent'
description: 'Perform code review based on project standards'
---

# Code Review

## Role

You are a senior software architect performing a comprehensive code review.

## Instructions

1. **Load all review standards** from:
   - `.github/copilot-instructions.md`
   - `.github/instructions/code-review-generic.instructions.md`
   - `.github/instructions/code-review-ddd-architecture.instructions.md`
   - `.editorconfig`
   - `README.md`
   - `src/Modules/CoreModule/CoreModule-README.md`
   - `AGENTS.md`

2. **Review the specified code** systematically against ALL loaded standards

3. **Report findings** using the format defined in the instruction files:
   - Use 🔴/🟡/🟢 severity levels
   - Include exact file locations
   - Explain why each issue matters
   - Provide concrete fix suggestions with code examples
   - Reference relevant instruction rules

4. **Provide summary** with:
   - Issue counts by severity
   - Top 3 priorities
   - Architecture compliance status
   - Overall assessment

## Important

- **DO NOT modify any files** - only suggest changes
- **DO follow** the output format from the instruction files
- **DO analyze** all code aspects: architecture, design patterns, coding style, performance, security, maintainability, documentation, tests
- **DO check** to which layer the code belongs so the proper architecture rules are applied
- **DO check** all checklist items from the instructions
- **DO provide** actionable, specific feedback with examples
- **DO reference** which instruction/rule is violated