# AGENTS.md

This document provides AI agents with concise, high-signal context about this repository to produce high-quality, maintainable code and helpful developer assistance. It complements `.github/copilot-instructions.md` with a broader perspective on architecture, patterns, workflows, and conventions. The project heavily uses the bITdevKit — refer to the official docs: [bITdevKit Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs).

## Project Overview

- **Name**: bITdevKit GettingStarted Example
- **Purpose**: Demonstrates a modular, Domain-Driven Design (DDD) application using the bITdevKit on .NET 10.
- **Primary Domain Concept**: Customer management lifecycle (create, update, list, delete, export job).
- **Architecture**: Onion / Clean Architecture + Modular vertical slices (Domain, Application, Infrastructure, Presentation, Tests).
- **Runtime**: ASP.NET Core minimal APIs, EF Core (SQL Server), Serilog.
- **Modules**: Located under `src/Modules/<ModuleName>` (e.g., `CoreModule`). Each module is self-contained.
- **Host**: Composition root in `src/Presentation.Web.Server` (`Program.cs`, DI wiring, middleware).

## Goals for Copilot

- Generate concise, idiomatic C# 10+ (.NET 10) code following DDD and clean architecture.
- Respect layering boundaries and module isolation; avoid cross-layer leakage.
- Prefer repository abstractions and specifications over direct DbContext access in Application code.
- Use existing devkit features (requester, notifier, pipeline behaviors) instead of re-inventing infrastructure.
- Produce testable changes with unit/integration tests where meaningful.

## Coding Standards

- Please follow the rules in [.editorconfig](./.editorconfig).
- **Language**: C# 10+; file-scoped namespaces.
- **Style**: Follow C# Coding Conventions; descriptive names; expressive syntax (null-conditional, string interpolation).
- **Types**: Use `var` when type is obvious; prefer records, pattern matching, null-coalescing assignment.
- **Naming**:
  - PascalCase for classes, methods, public members.
  - camelCase for locals/private fields; prefix interfaces with `I` (e.g., `IUserService`).
  - Constants in UPPERCASE.
  - Use `this.` for fields.
- **Validation & Errors**: Prefer `Result<T>` for recoverable failures; exceptions only for exceptional cases. Use FluentValidation for inputs.
- **Async**: Use `async/await` for I/O-bound operations.
- **LINQ**: Prefer efficient LINQ; avoid N+1 queries.
- **Nullability**: Project uses disabled nullability annotations; maintain consistency.

## Tech Stack

- **Frameworks**: ASP.NET Core minimal API, EF Core (SQL Server), Mapster, Serilog, Quartz, FluentValidation.
- **bITdevKit**: Requester/Notifier, repositories, startup tasks, job scheduling, module infrastructure.
- **Testing**: xUnit, NSubstitute, Shouldly; WebApplicationFactory for integration.

## Architecture & Layering

- **Domain**: Aggregates, Value Objects, Enumerations, Domain Events, Business Rules. No references to outer layers.
- **Application**: Commands/Queries, Handlers, DTO models, Specifications. References Domain only; do not reference Infrastructure/Presentation.
- **Infrastructure**: EF Core DbContext/configurations, repositories, jobs, startup tasks. May reference Domain & Application; expose abstractions.
- **Presentation**: Minimal API endpoints, module registration, mapping profiles; references Application (and Domain types as needed for mapping).
- **Host**: Server project wiring, middleware (Serilog, correlation, problem details, swagger).

### Command/Query Naming & Placement

- Commands: `[Entity][Action]Command` (e.g., `CustomerCreateCommand`).
- Queries: `[Entity][Action]Query` (e.g., `CustomerFindAllQuery`).
- Handlers: `[Entity][Command|Query]Handler` co-located with commands/queries.
- Domain Events: `[Entity]<PastTenseEvent>DomainEvent`.
- Value Objects: Singular descriptive (e.g., `EmailAddress`).
- Enumerations: `Enumeration` derivative with PascalCase static instances.

### Mapping

- Use Mapster via `services.AddMapping().WithMapster<CoreModuleMapperRegister>()`.
- Define mappings in the module-specific `MapperRegister` class; avoid ad-hoc inline mapping in handlers.

### Persistence & Repositories

- Use `AddSqlServerDbContext<T>` with connection string from module config.
- Register repositories via `AddEntityFrameworkRepository<TEntity, TDbContext>()` and chain behaviors (logging, audit, domain events).
- Application handlers must depend on repository abstractions, not the DbContext.

### Pipeline Behaviors

- Typical pipeline: `ModuleScopeBehavior` -> `ValidationPipelineBehavior` -> `RetryPipelineBehavior` -> `TimeoutPipelineBehavior`.
- Add new behaviors only for justified cross-cutting concerns.

## Development Workflows

- Use tasks defined in the workspace to build, test, and manage EF:
  - Build: `Solution [build]`
  - Format: `Solution [format apply]`
  - Tests: `Tests [unit all]`, `Tests [integration all]`
  - Coverage: `Coverage [all -> html]`
  - EF Migrations: `EF [migration add]`, `EF [apply migrations]`, `EF [update database]`
  - Docker: `Docker [build & run]`, `Docker [compose up]`
- Prefer these tasks over custom scripts to maintain consistency.

## Cloud & Deployment

- Containers are supported via Docker. Compose files and scripts exist in the repo.
- When adding cloud-related code (e.g., Azure), follow the Azure best practices guidance in `azure.instructions.md` (extension resource). Do not assume AKS/Terraform unless explicitly requested.
- Publishing: use Server publish tasks (`Server [publish]`, `Server [publish release]`) when needed.
- For DevKit patterns and module infrastructure, consult the DevKit docs: [bITdevKit Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs).

## Observability & Logging

- Use Serilog with structured logging.
- Include correlation via `app.UseRequestCorrelation()`; propagate `CorrelationId` in logs and context.
- Avoid logging sensitive PII; use structured templates (e.g., `logger.LogInformation("Customer {CustomerId} created", customer.Id);`).

## Security

- Validate all external input via FluentValidation + Domain guard rules.
- Keep domain pure; avoid external calls in value objects/entities.
- Plan for future authn/authz; design endpoints to allow attribute-based constraints.

## Documentation

- Use Markdown for docs located under `/docs/`.
- Keep `README.md` updated for setup steps.
- Update module `README` files (e.g., `CoreModule-README.md`) when adding features.

## Internal APIs & Shared Code

- Common functionality is organized per module; avoid cross-module duplication.
- Use repository abstractions and specifications rather than duplicating query logic.
- Mapping configurations live in each module’s `MapperRegister`.

## Testing Strategy

- Unit tests: focus on handlers, domain logic, rules, mapping.
- Integration tests: use WebApplicationFactory; exercise endpoints and persistence.
- Architecture tests: enforce layering boundaries.
- Prefer `Result<T>` assertions and repository test doubles/mocks (NSubstitute) for application layer tests.

## Git & PR Process

- Branch naming: `feature/<area>-<short-description>`, `fix/<issue>`, `chore/<task>`.
- Small, focused PRs; follow existing folder structure and naming conventions.
- Include tests and docs updates where applicable.
- Avoid unrelated formatting changes; use `Solution [format apply]` for targeted formatting.

## Guidance for Copilot Prompts

When asking Copilot to implement something, include:

- Target module (e.g., `CoreModule`).
- Layer scope (Domain vs Application vs Presentation).
- Persistence & migrations requirements.
- Mapping + validation needs.
- Endpoint shape (HTTP verb, route, request/response DTO).

Example prompt:
> Add a new command/query pair to CoreModule to deactivate a Customer (sets Status=Retired). Include validator, handler retry/timeout attributes, endpoint (PUT /api/core/customers/{id:guid}/deactivate), and unit tests.

## Do / Don't for AI-Generated Changes

- Do: Keep domain purity; use existing extension methods and pipeline behaviors; co-locate validators/handlers; prefer `Result<T>`; follow naming + folder conventions.
- Don’t: Introduce circular references; access DbContext directly in Application; leak infrastructure to Presentation; add static mutable state; use reflection-heavy hacks.

## Repository Layout Snapshot

```text
/ (root)
  .github/copilot-instructions.md
  .editorconfig
  README.md
  src/
    Modules/CoreModule/
      CoreModule.Domain/
      CoreModule.Application/
      CoreModule.Infrastructure/
      CoreModule.Presentation/
      CoreModule-README.md
    Presentation.Web.Server/
  tests/
    UnitTests/
    IntegrationTests/
```

## Quick Commands (PowerShell)

Use workspace tasks when possible or dotnet tools; for ad-hoc commands:

```powershell
# Build
pwsh -NoProfile -File .\bdk.ps1 -Task build

# Unit tests
pwsh -NoProfile -File .\bdk.ps1 -Task test-unit-all

# Apply EF migrations
pwsh -NoProfile -File .\bdk.ps1 -Task ef-apply
```

## Alignment with `.github/copilot-instructions.md`

This AGENTS.md reinforces and summarizes the rules found in `.github/copilot-instructions.md`. Copilot should treat that file as authoritative for architectural boundaries, naming, and module practices.

---

By following this guidance, Agents can generate precise, maintainable code that fits the project’s modular DDD architecture and established patterns and workflows.
