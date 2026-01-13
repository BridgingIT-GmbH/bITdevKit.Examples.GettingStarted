---
name: adr-writer
description: 'Write high-quality Architectural Decision Records (ADRs) following MADR format. Use when documenting important architectural decisions, technology choices, or cross-cutting concerns. Never use emoji or special characters - use plain V for correct and X for wrong.'
---

# ADR Writer

## Overview

This skill helps you write comprehensive Architectural Decision Records (ADRs) following the MADR (Markdown Architectural Decision Records) format. It enforces consistent structure, thoroughness, and adherence to project conventions.

## Prerequisites

- Understanding of the problem/decision to document
- Research completed on alternatives and tradeoffs
- Team discussion completed (if applicable)
- Clear recommendation ready

## Core Rules

1. **NEVER** use emoji or special characters in ADRs. Use plain ASCII:
   - Use `V` for correct examples
   - Use `X` for wrong examples
   - Use `WARNING:` for warnings (not emoji)
2. **ALWAYS** include all required MADR sections (Status, Context, Decision, Rationale, Consequences, Alternatives, References)
3. **NEVER** reference `AGENTS.md` or `.github/copilot-instructions.md` in ADR References section
4. **ALWAYS** use 4-digit numbering (0001, 0002, etc.)
5. **ALWAYS** update Quick Reference table in `docs/ADR/README.md` after creating ADR

## Workflows

### Creating a New ADR

#### Step 1: Determine ADR Number

Check existing ADRs and use next sequential number:

```bash
ls docs/ADR/*.md
# If last is 0012, use 0013
```

#### Step 2: Create File

Filename format: `docs/ADR/XXXX-decision-title-lowercase-hyphenated.md`

Example: `docs/ADR/0013-graphql-api-strategy.md`

#### Step 3: Follow MADR Structure

**Required Sections (in order):**

1. **Status**: Proposed | Accepted | Deprecated | Superseded
2. **Context**: Problem description, forces, constraints, requirements
3. **Decision**: Clear statement of what is being decided
4. **Rationale**: 5-7 reasons why this is the best choice
5. **Consequences**: Positive, Negative, Neutral impacts
6. **Alternatives Considered**: 2-4 alternatives with rejection reasons
7. **Related Decisions**: Links to other ADRs
8. **References**: Links to docs (NOT AGENTS.md)
9. **Notes**: Code examples, implementation files, migration paths

#### Step 4: Write Context Section

Include:

- **Problem Statement**: What challenge exists?
- **Technical Requirements**: What must the solution provide?
- **Business Requirements**: What business value is needed?
- **Design Challenges**: What forces are in tension?
- **Related Decisions**: Link to ADRs this builds upon

Example:

```markdown
## Context

The application requires object-to-object mapping between domain entities and DTOs.

### Technical Requirements
- Domain to DTO mapping for API responses
- DTO to Domain reconstruction for requests
- Value object conversions (EmailAddress -> string)
- Performance: minimal overhead

### Business Requirements
- API stability separate from domain changes
- Developer productivity (reduce boilerplate)

### Design Challenges
- Where to define mappings (Application vs Presentation)
- Convention vs explicit configuration
- Value object complexity
```

#### Step 5: State Decision Clearly

Be specific and actionable:

```markdown
## Decision

Use **Mapster** as the object mapping library with **explicit configurations**
defined in module-specific `MapperRegister` classes in the Presentation layer.

### How It Works
1. Each module defines `IRegister` implementation
2. Configure mappings using Fluent API
3. Register via `services.AddMapping().WithMapster()`
```

#### Step 6: Provide Strong Rationale

List 5-7 concrete reasons:

```markdown
## Rationale

1. **Performance**: Mapster generates IL code (10x faster than reflection-based mappers)
2. **Simplicity**: Cleaner API than AutoMapper with less boilerplate
3. **Modern Design**: Built for .NET Core+ with async support
4. **Explicit Control**: MapWith() for value objects is clearer than converters
5. **Predictable**: Less magic, more explicit behavior
```

#### Step 7: Document Consequences Honestly

```markdown
## Consequences

### Positive
- Fast IL-generated mappings (minimal overhead)
- 100+ lines of manual code eliminated per aggregate
- Type-safe compile-time mapping validation
- Clear separation between domain and DTOs

### Negative
- Learning curve for Mapster API (ForType, MapWith, ConstructUsing)
- Explicit configs require more code than pure conventions
- Runtime errors if misconfigured (forgot value object conversion)

### Neutral
- Requires Mapster NuGet package (stable, maintained)
- Configs held in memory (negligible impact)
```

#### Step 8: List Alternatives with Rejection Reasons

```markdown
## Alternatives Considered

### 1. AutoMapper
**Description**: Popular object mapper with extensive features.

**Pros**:
- Mature with extensive documentation
- Convention-based auto-mapping

**Cons**:
- Slower (reflection-based vs Mapster's IL generation)
- More complex API with steeper learning curve
- Convention magic can be unpredictable

**Rejected Because**: Mapster provides better performance with simpler API.

### 2. Manual Mapping (Extension Methods)
**Description**: Write ToModel() extension methods manually.

**Cons**:
- 50-100 lines of boilerplate per entity
- Must update manually when properties change
- Error-prone (easy to forget new properties)

**Rejected Because**: Not maintainable at scale.
```

#### Step 9: Link Related Decisions

```markdown
## Related Decisions

- **ADR-0001**: Clean/Onion Architecture - Mapping at Presentation layer boundary
- **ADR-0008**: Typed Entity IDs - Mappers convert typed IDs to primitives
- **ADR-0011**: Application Logic in Commands/Queries - Handlers use IMapper
```

#### Step 10: Add References

```markdown
## References

- [Mapster Documentation](https://github.com/MapsterMapper/Mapster)
- [bITdevKit Mapping Extensions](https://github.com/BridgingIT-GmbH/bITdevKit/docs)
- Project Documentation: `README.md` (Presentation Layer section)
- Module Documentation: `src/Modules/CoreModule/CoreModule-README.md`
```

**IMPORTANT**: Never reference:

- `AGENTS.md`
- `.github/copilot-instructions.md`

#### Step 11: Add Implementation Notes

```markdown
## Notes

### Key Implementation Files
\`\`\`
src/Modules/CoreModule/CoreModule.Presentation/
└── CoreModuleMapperRegister.cs    # Mapping configurations

src/Presentation.Web.Server/
└── Program.cs                     # Registration
\`\`\`

### Usage Pattern
\`\`\`csharp
// In handler
public class CustomerCreateCommandHandler(IMapper mapper, ...)
{
    // Map domain to DTO
    return mapper.Map<Customer, CustomerModel>(customer);
}
\`\`\`

### Common Patterns

**Value Object Conversion**:
\`\`\`csharp
config.NewConfig<EmailAddress, string>()
    .MapWith(src => src.Value);
\`\`\`

**Factory Method Construction**:
\`\`\`csharp
config.ForType<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(...).Value);
\`\`\`
```

#### Step 12: Use Plain ASCII Characters

**CORRECT Usage:**

```markdown
V Correct: Use typed IDs for type safety
X Wrong: Use raw Guid everywhere
```

**WRONG Usage (DO NOT DO THIS):**

```markdown
Correct: Use typed IDs
❌ Wrong: Use raw Guid
⚠️ Warning: This is dangerous
```

#### Step 13: Update Quick Reference

After creating ADR, edit `docs/ADR/README.md` and add entry:

```markdown
| [ADR-0013](0013-graphql-api-strategy.md) | GraphQL API Strategy | Accepted |
```

## Examples

### User: "Document the decision to use Entity Framework Core"

**Actions**:

1. Determine next number: check `ls docs/ADR/*.md` (assume 0007)
2. Create file: `docs/ADR/0007-entity-framework-core-code-first-migrations.md`
3. Write comprehensive ADR covering:
   - Context: ORM requirements, code-first vs database-first
   - Decision: EF Core with code-first migrations, DbContext per module
   - Rationale: Type safety, migration system, .NET integration, etc.
   - Consequences: Productivity gains, learning curve, etc.
   - Alternatives: Dapper, NHibernate, manual ADO.NET
   - References: EF Core docs, project README
   - Notes: Code examples, migration commands, file paths
4. Use plain ASCII (V/X, not emoji)
5. Update `docs/ADR/README.md` Quick Reference table

### User: "Create ADR for FluentValidation strategy"

**Actions**:

1. Check numbering: `ls docs/ADR/*.md` (assume 0009)
2. Create: `docs/ADR/0009-fluentvalidation-strategy.md`
3. Write sections:
   - **Context**: Input validation needs, pipeline integration
   - **Decision**: FluentValidation in Application layer with nested validators
   - **Rationale**: Expressiveness, testability, complex rules, separation
   - **Consequences**: Positive (productivity, fail-fast) and Negative (learning curve, duplication risk)
   - **Alternatives**: DataAnnotations, manual validation, domain-only
   - **Related**: Link to ADR-0005 (ValidationPipelineBehavior), ADR-0011/0012 (validation layers)
   - **Notes**: Validator examples, pipeline order, testing patterns
4. **CRITICAL**: Use `V` and `X` (NOT emoji)
5. Update README Quick Reference

## Quality Checklist

Before finalizing, verify:

- [ ] Status is "Proposed" or "Accepted"
- [ ] Context explains problem clearly (2-4 paragraphs)
- [ ] Decision is specific and actionable
- [ ] Rationale has 5+ concrete reasons
- [ ] Consequences include Positive AND Negative
- [ ] At least 2 alternatives shown with rejection reasons
- [ ] Related ADRs linked (if applicable)
- [ ] References included (NOT AGENTS.md)
- [ ] Code examples included in Notes
- [ ] Implementation file paths listed
- [ ] Only plain ASCII used (V/X, no emoji)
- [ ] Quick Reference table updated in README.md

## Common Pitfalls

**X DO NOT:**

- Use emoji or special characters (✅❌⚠️) - use V/X instead
- Reference AGENTS.md or .github/copilot-instructions.md
- Write vague decisions ("use better error handling")
- Skip Alternatives Considered section
- Hide negative consequences
- Make ADRs too short (aim for 7,000-15,000 bytes for technical ADRs)
- Write implementation docs (belongs in code comments)

**V DO:**

- Use plain ASCII: V for correct, X for wrong
- Write comprehensive context (problem, forces, constraints)
- Be specific in decision statement
- List 5-7 rationale points
- Be honest about tradeoffs (positive AND negative)
- Show alternatives considered
- Include code examples in Notes
- Link to other ADRs and project docs
- Update README Quick Reference table

## Decision Categories

### High Priority (Write ADRs)

- Architectural patterns (Clean Architecture, Modular Monolith)
- Framework/library choices (EF Core, FluentValidation, Mapster)
- Cross-cutting concerns (Result Pattern, Validation Strategy)
- Infrastructure decisions (ORM, messaging, caching)
- Design patterns (Repository, Outbox, Mediator)

### Medium Priority

- API design decisions (REST conventions, versioning)
- Security approaches (authentication, authorization)
- Performance strategies (caching, query optimization)
- Testing strategies (unit vs integration)

### Low Priority (No ADR Needed)

- Implementation details (specific method logic)
- Coding style (variable naming, formatting)
- Trivial/reversible decisions

## File Structure

```text
docs/ADR/
├── README.md                                    # Template + Quick Reference
├── 0001-clean-onion-architecture.md
├── 0002-result-pattern-error-handling.md
├── 0007-entity-framework-core-code-first-migrations.md
├── 0009-fluentvalidation-strategy.md
└── 0010-mapster-object-mapping.md
```

## Template Summary

```markdown
# ADR-XXXX: Title

## Status
Accepted

## Context
[Problem, challenges, requirements]

## Decision
[Clear statement + how it works]

## Rationale
[5-7 reasons]

## Consequences
### Positive / Negative / Neutral

## Alternatives Considered
[2-4 alternatives with rejection reasons]

## Related Decisions
[Links to other ADRs]

## References
[Links to docs - NOT AGENTS.md]

## Notes
[Code examples, file paths, migration notes]
```

## Validation Rules

### Context Section

- Minimum 2 paragraphs
- Must include: problem statement, requirements, challenges
- Should reference related decisions (if building on previous ADRs)

### Decision Section

- Must be specific and actionable
- Include "How It Works" subsection with implementation approach
- Use code examples or diagrams where helpful

### Rationale Section

- Minimum 5 reasons
- Each reason should be a distinct point (not variations)
- Focus on *why* not *what*

### Consequences Section

- Must include both Positive AND Negative
- Negative consequences cannot be empty (be honest about tradeoffs)
- Neutral consequences optional

### Alternatives Section

- Minimum 2 alternatives
- Each alternative must have rejection reason
- Rejection reasons should be specific (not vague)

### Notes Section

- Must include implementation file paths
- Should include code examples (2-3 minimum)
- May include migration path if changing existing code

## ASCII Character Usage

**ALWAYS use plain ASCII:**

```markdown
V Correct example
X Wrong example
WARNING: Be careful here
NOTE: Important detail
```

**NEVER use emoji or special Unicode:**

```markdown
Correct    <- DO NOT USE
❌ Wrong      <- DO NOT USE
⚠️ Warning   <- DO NOT USE
```

## References

- **ADR Template**: `docs/ADR/README.md`
- **Existing ADRs**: Study `docs/ADR/0001-*.md` through `0012-*.md` as examples
- **Project Documentation**: `README.md`
- **Module Documentation**: `src/Modules/CoreModule/CoreModule-README.md`
- **bITdevKit Documentation**: `.bdk/docs/`
