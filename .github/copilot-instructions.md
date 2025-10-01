# Copilot / LLM Project Instructions


## Code Style and Structure
- Write concise, idiomatic C# code.
- Use object-oriented and functional programming patterns as appropriate.
- Prefer LINQ and lambda expressions for collections.
- Use descriptive variable and method names.
- Adhere a modular development approach to separate concerns between layers.
- Use file scoped namespaces.

## Naming Conventions
- PascalCase for class names, method names, and public members.
- camelCase for local variables and private fields.
- UPPERCASE for constants.
- Prefix interface names with "I" (e.g., IUserService).
- Fields should be used with the "this." prefix for clarity.

## C# and .NET Usage
- Use C# 10+ features (record types, pattern matching, null-coalescing assignment).
- Leverage ASP.NET Core and bITdevKit features (https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs).
- Use Entity Framework Core with a DbContext and the bITdevKit repository abstractions.

## Syntax and Formatting
- Follow C# Coding Conventions.
- Use expressive syntax (null-conditional, string interpolation).
- Use `var` for implicit typing when the type is obvious.
- Keep code clean and consistent.
- nullability annotations disabled.

## Error Handling and Validation
- Use exceptions for exceptional cases only. Prefer Result<T> for expected failures.
- Implement error logging with Serilog throug the .NET ILogger system.
- Use Fluent Validation for model validation.
- Return appropriate HTTP status codes and consistent error responses in Http endpoints.

## API Design
- Follow RESTful API design principles.
- Use minimal API and Endpoint routing.

## Performance Optimization
- Use async/await for I/O-bound operations.
- Write efficient LINQ queries and avoid N+1 problems.

---
## 1. Project Identity
- **Name**: BridgingIT DevKit GettingStarted Example
- **Purpose**: Demonstrates a modular, Domain-Driven Design (DDD) application using the BridgingIT DevKit (bITdevKit) on .NET 9.
- **Primary Domain Concept**: Customer management lifecycle (create, update, list, delete, export job).
- **Architecture Style**: Onion / Clean Architecture + Modular (Module = vertical slice: Domain, Application, Infrastructure, Presentation, Tests).

## 2. High-Level Architecture
Layers (outer depends inward; inner knows nothing about outer):
1. **Domain**: Aggregates, Value Objects, Enumerations, Domain Events, Business Rules.
2. **Application**: Commands & Queries (Request/Response), Request/Notification handlers, pipeline behaviors (validation, retry, timeout, module scoping), DTO models, Specifications.
3. **Infrastructure**: EF Core DbContext, configurations, repositories + repository behaviors (logging, audit, domain event publishing), migrations, schedulers, startup tasks.
4. **Presentation**: Minimal API endpoints, module registration, mapping profiles, system endpoints.
5. **Host (Web Server)**: Composition root (`Program.cs`), DI wiring, middleware (Serilog, correlation, problem details, swagger).

Modules live in `src/Modules/<ModuleName>` (e.g., `CoreModule`) and must be self-contained; cross-module coupling should be minimized (communicate via abstractions / events where needed).

## 3. Key Frameworks & Packages
- .NET 9 SDK (`global.json` pinned to 9.0.305).
- bITdevKit packages (version 9.0.18) providing requester/notifier, repositories, startup tasks, job scheduling, module infrastructure.
- EF Core (SQL Server), Serilog, Mapster, Quartz, FluentValidation, xUnit/NSubstitute/Shouldly.

## 4. Naming & Folder Conventions
Within each module:
- `<Module>.Domain` -> domain model (`Model/`, `Events/`, `Rules/`, `Specifications/`).
- `<Module>.Application` -> `Commands/`, `Queries/`, `Models/`, `Handlers/` (co-located with command/query), `Behaviors/` (rare), `Validators/` (if not nested).
- `<Module>.Infrastructure` -> `EntityFramework/` (DbContext, Configurations, Migrations), `Repositories/`, `Jobs/`, `StartupTasks/`.
- `<Module>.Presentation` -> `Web/Endpoints/`, `MapperRegister`, `Module` class.
- Tests mirror structure: `<Module>.UnitTests`, `<Module>.IntegrationTests`.

Command names: `<Entity><Action>Command` (e.g., `CustomerCreateCommand`).
Query names: `<Entity><Action>Query` (e.g., `CustomerFindAllQuery`).
Handlers: `<Command|Query>Handler` in same folder.
Domain Events: `<Entity><PastTenseEvent>DomainEvent`.
Value Objects: Singular descriptive (e.g., `EmailAddress`).
Enumerations: PascalCase static instances defined inside class `Enumeration` derivative.

## 5. Dependency & Layering Rules (IMPORTANT)
- Domain: no references to Application, Infrastructure, Presentation, Host.
- Application: may reference Domain; must NOT reference Infrastructure or Presentation directly.
- Infrastructure: may reference Domain & Application (for DTO or specification needs) but should expose interfaces / repository abstractions consumed by Application.
- Presentation: references Application (for requester/notifier invocation) and Domain types only when needed for mapping (avoid leaking infrastructure concerns).
- Tests can reference any layer needed for verification but architecture tests enforce boundaries.

## 6. Dependency Injection & Pipelines
Core DI happens in `Program.cs` and each module's `Module` class (e.g., `CoreModule : WebModuleBase`). Typical pipeline behaviors added:
- `ModuleScopeBehavior`
- `ValidationPipelineBehavior`
- `RetryPipelineBehavior`
- `TimeoutPipelineBehavior`

Add new behaviors only if cross-cutting concerns justify it—avoid duplicating existing functionality provided by DevKit.

## 7. Mapping
Mapster is used via `services.AddMapping().WithMapster<CoreModuleMapperRegister>()`.
- Add map configurations inside module-specific `MapperRegister` class.
- Favor attribute / fluent config over manual inline mapping in handlers.

## 8. Validation
- Prefer nested `Validator` classes inside commands/queries (inherits `AbstractValidator<T>`).
- Business invariants belong in Domain (Value Object creation, Entity methods, Rules pattern).
- Combine FluentValidation (input) + domain guard rules (invariants) rather than mixing concerns.

## 9. Persistence & Repositories
- Use `AddSqlServerDbContext<T>` extension with connection string from module configuration.
- Repositories: `AddEntityFrameworkRepository<TEntity, TDbContext>()` with chainable behaviors.
- Common behaviors: logging, audit state (sets audit fields), domain event publishing.
- Do NOT access DbContext directly in Application handlers—use repository abstraction.

## 10. Domain Modeling Guidelines
- Aggregates inherit from `AuditableAggregateRoot<TId>` & register events via `DomainEvents.Register(...)`.
- Use static factory methods (e.g., `Customer.Create`) instead of public constructors.
- Value objects inherit from `ValueObject` and override `GetAtomicValues()`.
- Use enumerations pattern for bounded sets (`CustomerStatus`).
- Concurrency: implement `IConcurrency` with a `Guid ConcurrencyVersion` property when needed.

## 11. Jobs & Startup Tasks
- Jobs (Quartz) registered in module's `Register` with `AddJobScheduling` -> `.WithJob<YourJob>().Cron(...)`.
- Startup tasks via `services.AddStartupTasks(...).WithTask<YourTask>(...)` for environment-scoped seeding/migrations.

## 12. Logging & Observability
- Serilog enrichers included (Environment, Thread, ShortTypeName).
- Use structured logging (`logger.LogInformation("Customer {CustomerId} created", customer.Id);`).
- Avoid logging sensitive PII beyond what's necessary.
- Correlation set by `app.UseRequestCorrelation()`; include `CorrelationId` in outbound logs if manual logging.

## 14. Testing Strategy
- Unit Tests: focus on handlers, domain logic, rules, mapping.
- Integration Tests: use WebApplicationFactory, exercise endpoints & persistence.
- Architecture Tests: ensure layering boundaries (e.g., Domain doesn't expose public constructors incorrectly, etc.).
- Use `Result` assertions and repository test doubles/mocks (NSubstitute) for application layer unit tests.

## 15. Adding a New Feature (Template Workflow)
1. Create Domain model additions (Aggregate/ValueObject/Enumeration/Events/Rules).
2. Add Db mappings (type configuration + migration) if persistence needed.
3. Add Application layer command/query + validator + handler.
4. Add Mapster mapping definitions.
5. Add endpoints in `<Module>.Presentation/Web/Endpoints` using requester pattern.
6. Register endpoints via `services.AddEndpoints<TEndpoints>();` in module.
7. Extend module tests (unit + integration + architecture if new rules introduced).
8. Update documentation if externally visible.

## 16. LLM Prompt Guidance (For Users)
When asking the AI to implement something:
- ALWAYS specify the module. (Example: "Add a Product aggregate to a new InventoryModule.")
- Clarify layer scope (Domain vs Application vs Presentation).
- Indicate if persistence & migrations are required.
- Request mapping + validation explicitly when needed.
- Provide expected endpoint shape (HTTP verbs, route pattern, request/response DTO).

Good prompt example:
> Add a new command/query pair to CoreModule to deactivate a Customer (sets Status=Retired). Include validator, handler retry/timeout attributes, endpoint (PUT /api/core/customers/{id:guid}/deactivate), and unit tests.

Bad prompt example:
> Make a thing that handles customers better.

## 17. Do / Don't for AI Generated Changes
Do:
- Keep domain purity (no external service calls from entities/value objects).
- Use existing extension methods and DevKit pipeline behaviors.
- Co-locate validators and handlers with their commands/queries.
- Prefer Result<T> over throwing for recoverable errors.
- Follow naming conventions & folder structure.

Don't:
- Introduce circular project references.
- Bypass repositories to talk directly to DbContext in handlers.
- Put UI/presentation concerns in Application layer.
- Add static mutable state.
- Use reflection-heavy hacks when a straightforward pattern exists.

## 18. Security & Data Concerns
- Assume standard customer PII; avoid logging full emails where not needed.
- Validate all external input via validators.
- Future enhancements may include authn/authz—design endpoints to allow attribute-based constraints later.

## 19. Performance Considerations
- Use specifications for filtering queries rather than in-memory LINQ after materialization.
- Batch repository operations if adding future bulk features.
- Offload long-running workflows to jobs instead of synchronous request handlers.

## 20. Common Extension Points
- Add new repository behaviors (cross-cutting) in Infrastructure.
- Add new pipeline behaviors (cross-cutting) but ensure ordering is intentional (Validation -> Retry -> Timeout is current pattern after ModuleScope).
- Add new modules under `src/Modules/`—mirror CoreModule layout.

## 21. Glossary
- Requester: Mediator-like abstraction for commands/queries with pipeline behaviors.
- Notifier: Publishes notifications (domain or integration events) via handlers.
- Specification: Encapsulated query predicate chaining logic for repositories.
- Result<T>: Functional-style success/failure wrapper carrying value or errors.

## 22. How to Ask for Refactors
Provide: current filename(s), goal (e.g., split large handler), constraints (no public API break), desired outcome (e.g., extract mapping profile). The AI should propose a diff then apply minimal, isolated changes.

## 23. Repository Layout Snapshot
```
/ (root)
  ./github/copilot-instructions.md
  README.md
  src/
    Modules/CoreModule/
      CoreModule.Domain/
      CoreModule.Application/
      CoreModule.Infrastructure/
      CoreModule.Presentation/
    Presentation.Web.Server/
  tests/
    UnitTests/
    IntegrationTests/
```