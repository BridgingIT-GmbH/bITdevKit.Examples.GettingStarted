---
description: Review code against project standards
agent: plan
subtask: false
---

Review $ARGUMENTS against the coding standards and architecture patterns.

## Review Scope

Perform a comprehensive code review checking for:

### Architecture & Layering Boundaries

- **No cross-layer violations**: Domain → Application → Infrastructure → Presentation
- **Module isolation**: Each module under `src/Modules/<ModuleName>` is self-contained
- **Domain purity**: Domain layer has zero references to outer layers (no EF, no Infrastructure or Application dependencies)
- **Application layer**: Only references Domain; uses repository abstractions, not DbContext, proper usage of Commands and Queries with the Requester pattern.
- **Infrastructure layer**: Only references Application and Domain; contains EF DbContext, optional repository implementations
- **Circular dependencies**: Check for any circular references between layers

### DDD Patterns & Conventions

- **Aggregates**: Properly encapsulated with business logic in the aggregate root.
  - No direct access to child entities from outside the aggregate
  - Aggregate root enforces invariants and business rules
  - Aggregate methods return Result<T> for operations that can fail
  - No public setters on aggregate properties; use methods to modify state
  - Aggregate roots have collection navigation properties as `IReadOnlyCollection<T>`; modification via methods only
  - No lazy loading in aggregates; all required data loaded eagerly
- **Entities**: Mutable, identity-based with meaningful methods.
  - Entities have identity properties (e.g., `Id`)
  - Business logic encapsulated in methods
  - No public setters on entity properties; use methods to modify state
  - Entities have collection navigation properties as `IReadOnlyCollection<T>`; modification via methods only
  - No lazy loading in entities; all required data loaded eagerly
  - Entities enforce invariants and business rules
  - Entity methods return Result<T> for operations that can fail
- **Value Objects**: Immutable, validated, descriptive names (e.g., `EmailAddress`)
  - No public setters; all properties set via constructor or via Create factory method
  - Value object methods return Result<T> for operations that can fail
  - Value objects do not have identity; equality based on properties
  - Value objects are small and focused on a single concept
  - Value objects are used to encapsulate domain concepts and ensure data integrity
  - Value objects do not depend on external services or infrastructure
- **Domain Events**: Named in past tense with `DomainEvent` suffix (e.g., `CustomerCreatedDomainEvent`)
  - Published via aggregate root methods
  - Handlers implement `INotificationHandler<T>`
  - Domain events used to communicate state changes within the domain layer only
  - Domain events can be handled in the Application layer if needed
  - Domain events contain only relevant data for the event
  - Domain events are raised only for significant state changes
- **Smart Enumerations**: Derive from `Enumeration` base class with PascalCase static instances
- **Entity IDs**: Use strongly-typed identifiers (e.g., `CustomerId`)
  - Code genereted by using the `TypedEntityId<T>` attribute
  - IDs are immutable and set only at creation
  - IDs are used consistently across the application for entity identification
  - IDs are used in repository methods and queries
  - IDs are used in DTOs and API models where appropriate
  - IDs are used in logging and error messages for clarity

### Command/Query Patterns

- **Naming**: Commands as `[Entity][Action]Command`, Queries as `[Entity][Action]Query`
- **Handlers**: Named `[Entity][Command|Query]Handler`, co-located in same folder as their command/query
  - Commands/handlers in `Commands/` folder, queries/handlers in `Queries/` folder
  - Handler can be in same file as separate class, or in separate file in same directory
  - Examples: `CustomerDeleteCommand.cs` + `CustomerDeleteCommandHandler.cs` in `Commands/`, or both classes in `CustomerDeleteCommand.cs`
- **Validation**: FluentValidation validators are optional (typically nested `Validator` class when present)
- **Pipeline behaviors**: Pipeline behavior attributes (retry, timeout, etc.) are optional

### Error Handling & Results

- **Result<T> usage**: Use `Result<T>` for recoverable failures, not exceptions
- **Exception usage**: Only throw exceptions for truly exceptional cases
- **Validation**: All external input validated via FluentValidation + domain guard rules
- **Null handling**: Proper null-coalescing and null-conditional operators

### Performance & Data Access

- **LINQ efficiency**: Check for N+1 queries and inefficient query patterns
- **Async/await**: All I/O-bound operations use async/await properly
- **No blocking calls**: No `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- **Repository patterns**: Application uses repository abstractions with specifications
- **DbContext**: Direct DbContext access only in Infrastructure layer

### Naming Conventions

- **Classes/Methods**: PascalCase
- **Interfaces**: Prefixed with `I` (e.g., `ICustomerRepository`)
- **Private fields**: camelCase with `this.` prefix for field access
- **Locals/parameters**: camelCase
- **Constants**: UPPERCASE or PascalCase
- **File-scoped namespaces**: Use C# 10+ file-scoped namespace syntax

### Mapping & DTOs

- **Mapster usage**: All mapping via Mapster configured in module `MapperRegister` class
- **No ad-hoc mapping**: Avoid inline mapping in handlers; use registered configurations
- **DTO placement**: Request/response models in Application layer

### Code Style & Formatting

- **.editorconfig compliance**: Follow all rules defined in `.editorconfig`
- **var usage**: Use `var` when type is obvious from right-hand side
- **Modern C# syntax**: Records, pattern matching, string interpolation, null-coalescing assignment
- **Expressive code**: Prefer LINQ and expressive syntax over verbose loops

### Logging & Observability

- **Structured logging**: Use Serilog with structured templates
- **No PII**: Avoid logging personally identifiable information
- **Correlation**: Ensure CorrelationId propagation in logs where applicable
- **Log levels**: Appropriate use of Information, Warning, Error, Debug

### Presentation Layer Endpoints

- **Endpoint organization**: Endpoints in `<Module>.Presentation/Web/Endpoints/` classes deriving from `EndpointsBase`
- **No business logic**: Endpoints are thin adapters - only route mapping, parameter binding, and response mapping
- **IRequester usage**: All business logic invoked via `IRequester.SendAsync()` to send commands/queries
- **Response mapping**: Use `.MapHttpOk()`, `.MapHttpCreated()`, `.MapHttpNoContent()`, `.MapHttpOkAll()` for Result<T> responses
- **Route groups**: Use `MapGroup()` for common prefixes (e.g., `api/coremodule/customers`)
- **Authorization**: Apply `.RequireAuthorization()` on groups or individual endpoints as needed
- **API documentation**: All endpoints must have `.WithName()`, `.WithSummary()`, `.WithDescription()`
- **OpenAPI metadata**: Include `.Produces<T>()`, `.Accepts<T>()`, `.ProducesProblem()`, `.ProducesResultProblem()` for proper Swagger docs
- **Parameter binding**: Use `[FromServices]`, `[FromRoute]`, `[FromQuery]`, `[FromBody]` attributes explicitly
- **Route constraints**: Use constraints like `{id:guid}` for type safety
- **HTTP verbs**: Follow REST conventions (GET for queries, POST for create/search, PUT for update, DELETE for delete)
- **CancellationToken**: Always include `CancellationToken ct` parameter and pass to requester
- **No inline mapping**: Don't map between models in endpoints; that's the handler's responsibility

### bITdevKit Patterns

- **Repository behaviors**: Check for proper chaining (logging, audit, domain events)
- **Requester/Notifier**: Proper use of devkit communication patterns
- **Module registration**: Follows `services.AddModule<T>()` pattern
- **Startup tasks**: Background initialization uses devkit startup task infrastructure
- **Job scheduling**: Quartz jobs properly registered via devkit extensions

### Testing Readiness

- **Testable design**: Code structured to support unit and integration testing
- **Dependency injection**: All dependencies injected via constructor
- **Abstractions**: Proper use of interfaces for external dependencies
- **Pure domain logic**: Domain rules testable without infrastructure

### Security Considerations

- **Input validation**: All external inputs validated at boundary
- **Guard clauses**: Domain entities protect invariants
- **Sensitive data**: No secrets or credentials in code
- **SQL injection**: Use parameterized queries (EF Core handles this)

## Reference Files

Review against these authoritative sources:

- @.editorconfig - Formatting and style rules
- @AGENTS.md - Architecture patterns and conventions
- @.github/copilot-instructions.md - Detailed coding guidelines
- @.github/instructions/code-review-generic.instructions.md - Detailed generic dotnet coding guidelines

## Output Format

Provide specific, actionable feedback using this format:

```
### [Severity Level] Issue Category

**Location**: `file_path:line_number`

**Issue**: Clear description of the problem

**Why**: Explanation of why this matters (architecture, performance, maintainability)

**Fix**: Specific recommendation with code example if applicable

---
```

**Severity Levels**:

- 🔴 **Critical**: Violates architecture boundaries or causes bugs
- 🟡 **Important**: Violates conventions or impacts maintainability
- 🟢 **Suggestion**: Minor improvements or style preferences

## Focus Areas

Pay special attention to:

1. **Architecture violations** - These break the clean architecture model
2. **Domain purity** - Domain should have zero infrastructure dependencies
3. **Result<T> usage** - Proper error handling patterns
4. **Repository abstractions** - Application and Domain shouldn't touch DbContext
5. **N+1 queries** - Performance issues in data access

## Success Criteria

Code passes review when:

- ✅ No architecture boundary violations
- ✅ DDD patterns correctly applied
- ✅ Proper error handling with Result<T>
- ✅ Efficient data access patterns
- ✅ Ready for testing (testable design)
- ✅ .editorconfig rules followed

Provide a summary at the end with:

- Total issues found by severity
- Top 3 priorities to address
- Overall code quality assessment
