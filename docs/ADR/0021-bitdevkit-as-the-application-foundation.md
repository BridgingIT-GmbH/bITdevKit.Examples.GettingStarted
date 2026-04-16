# ADR-0021: bITdevKit as the Application Foundation

## Status

Accepted

## Context

This repository is a modular, Domain-Driven Design application on .NET 10. The architecture is intentionally opinionated so the codebase structures modules, commands and queries, repositories, endpoints, background jobs, and observability in a way that stays consistent across layers.

Without a shared application foundation, we would need to assemble and document many separate building blocks ourselves: mediator-style request dispatching, module registration, repository abstractions, endpoint discovery, mapping abstractions, job scheduling integration, result handling, and supporting conventions for testing and operations. That would add boilerplate and shift attention away from business capabilities and architectural consistency toward custom infrastructure glue code.

The repository therefore needs a toolkit that:

1. Supports Clean/Onion Architecture and DDD building blocks without pushing infrastructure concerns into the domain model
2. Works well for a modular monolith hosted in a single ASP.NET Core application
3. Provides consistent abstractions for commands, queries, notifications, repositories, endpoints, mapping, and jobs
4. Reduces repetitive plumbing so the application can focus on business flow and architectural patterns
5. Is consumable as NuGet packages so the application stays close to how the toolkit is adopted in production codebases
6. Keeps cross-cutting concerns such as validation, retries, timeouts, module scoping, and observability integrated instead of fragmented across unrelated libraries

This requirement is especially important in this repository because the chosen foundation should make the intended architecture obvious in the source tree, the host composition root, and the supporting ADRs.

## Decision

Use **bITdevKit** as the primary application foundation for this repository, consumed through the `BridgingIT.DevKit.*` NuGet packages.

### How It Works

The repository standardizes on bITdevKit for the architectural building blocks that appear throughout the solution:

1. **Domain modeling** through bITdevKit domain primitives and source generators
2. **Module composition** through `AddModules(...)` and module classes derived from `WebModuleBase`
3. **Application request handling** through `IRequester`, `INotifier`, handlers, and shared pipeline behaviors
4. **Persistence integration** through repository abstractions and Entity Framework Core integration packages
5. **Presentation composition** through endpoint registration and mapping helpers
6. **Boundary mapping** through the devkit mapping abstraction with Mapster integration
7. **Background processing** through the job scheduling packages and behaviors
8. **Operational consistency** through shared logging, module context, correlation, and related helpers

We do not treat bITdevKit as an optional convenience library in this repository. It is the architectural backbone that ties the application's modules, application flow, persistence, and presentation model together.

## Rationale

1. **Architectural consistency**: bITdevKit provides a cohesive set of abstractions that match the project's existing Clean Architecture and modular monolith decisions, which avoids mixing incompatible patterns across layers.
2. **Integrated cross-cutting behavior**: module scoping, validation, retry, timeout, tracing, and related behaviors work together through shared pipelines instead of custom middleware or ad hoc decorators per feature.
3. **Lower infrastructure boilerplate**: the repository can focus on domain and use-case code rather than re-implementing mediator wiring, repository registration, endpoint discovery, job hosting, and mapping registration.
4. **Cohesive platform choice**: using the toolkit end-to-end avoids fragmented architectural patterns and keeps core application building blocks aligned.
5. **Strong alignment between layers**: the same toolkit supports the domain, application, infrastructure, and presentation boundaries, which reduces friction when composing features like customer creation, export jobs, or module-specific endpoints.
6. **NuGet-based consumption model**: adopting the devkit through packages mirrors real project usage and keeps package references, versioning, and host-level composition explicit.
7. **Extensibility without abandoning conventions**: the repository can still add custom handlers, validators, mappings, repository behaviors, and module registrations while staying inside a well-defined architectural model.

## Consequences

### Positive

- The repository expresses a consistent architectural style from `Program.cs` through module registration and into handlers, repositories, and endpoints.
- New features can reuse established patterns instead of introducing one-off infrastructure decisions.
- The application stays focused on domain behavior and application flow rather than framework assembly code.
- Cross-cutting concerns are configured once and applied broadly through the same abstractions.
- Documentation, ADRs, tests, and code all point at the same conceptual model, which improves onboarding.
- The package inventory clearly communicates which devkit capabilities are in use in this solution.

### Negative

- The codebase is intentionally coupled to bITdevKit conventions, abstractions, and package release cadence.
- Developers need to learn bITdevKit patterns in addition to standard ASP.NET Core and EF Core concepts.
- Upgrades may require coordinated changes across several `BridgingIT.DevKit.*` packages.
- Some implementation choices are constrained by devkit extension points and established behaviors rather than being fully bespoke.

### Neutral

- The repository still uses mainstream .NET libraries such as ASP.NET Core, EF Core, Quartz.NET, FluentValidation, and Mapster, but bITdevKit is the integration layer that standardizes how they are used here.
- The solution remains modular and testable because the devkit is consumed via packages and abstractions rather than copied source code.
- The decision does not prevent custom code; it sets the default foundation that custom code is expected to build upon.

## Alternatives Considered

- **Alternative 1: Hand-rolled application framework**
  - Build module registration, mediator dispatching, repository wiring, endpoint discovery, result handling, and job integration directly in this repository.
  - Rejected because it would create a large amount of maintenance-heavy plumbing and would dilute the repository's architectural consistency.

- **Alternative 2: Compose multiple independent libraries without bITdevKit**
  - Use separate libraries such as MediatR, Scrutor, custom endpoint conventions, direct EF Core repositories, and bespoke pipeline wiring.
  - Rejected because the integration burden would move into this repository, resulting in more glue code, less consistency, and weaker alignment with the documented project architecture.

## Related Decisions

- [ADR-0001](0001-clean-onion-architecture.md): Defines the layer boundaries that bITdevKit helps preserve
- [ADR-0003](0003-modular-monolith-architecture.md): Establishes the modular host shape implemented through devkit modules
- [ADR-0004](0004-repository-decorator-behaviors.md): Uses repository abstractions and behaviors provided by the devkit stack
- [ADR-0005](0005-requester-notifier-mediator-pattern.md): Uses bITdevKit request and notification dispatching as the application interaction model
- [ADR-0007](0007-entity-framework-core-code-first-migrations.md): Uses the devkit Entity Framework integration in module infrastructure
- [ADR-0010](0010-mapster-object-mapping.md): Uses the devkit mapping abstraction with Mapster
- [ADR-0011](0011-application-logic-in-commands-queries.md): Places use-case orchestration in command and query handlers executed through the devkit requester
- [ADR-0014](0014-minimal-api-endpoints-dto-exposure.md): Uses the devkit endpoint model for presentation
- [ADR-0015](0015-background-jobs-quartz-scheduling.md): Uses the devkit job scheduling integration
- [ADR-0016](0016-logging-observability-strategy.md): Benefits from shared tracing, correlation, and structured logging patterns

## References

- [bITdevKit Documentation Index](../../.bdk/docs/INDEX.md)
- [bITdevKit Modules](../../.bdk/docs/features-modules.md)
- [bITdevKit Requester and Notifier](../../.bdk/docs/features-requester-notifier.md)
- [bITdevKit Domain](../../.bdk/docs/features-domain.md)
- [bITdevKit Domain Repositories](../../.bdk/docs/features-domain-repositories.md)
- [bITdevKit Common Mapping](../../.bdk/docs/common-mapping.md)
- [bITdevKit Presentation Endpoints](../../.bdk/docs/features-presentation-endpoints.md)
- [bITdevKit JobScheduling](../../.bdk/docs/features-jobscheduling.md)

## Notes

### Current Adoption Footprint

The solution consumes the devkit through centrally managed package versions:

```xml
<PackageVersion Include="BridgingIT.DevKit.Application.JobScheduling" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Common.Mapping" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Domain" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Domain.CodeGen" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Infrastructure.EntityFramework" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Infrastructure.EntityFramework.SqlServer" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Presentation.Web" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Presentation.Web.JobScheduling" Version="10.0.106-preview.0.20" />
<PackageVersion Include="BridgingIT.DevKit.Presentation.Serilog" Version="10.0.106-preview.0.20" />
```

### Composition Root Example

The host composes the application through devkit registration points instead of custom startup infrastructure:

```csharp
builder.Services.AddModules(builder.Configuration, builder.Environment)
    .WithModule<CoreModuleModule>();

builder.Services.AddRequester()
    .AddHandlers().WithDefaultBehaviors();
builder.Services.AddNotifier()
    .AddHandlers().WithDefaultBehaviors();

builder.Services.AddJobScheduling(o => o
    .StartupDelay(builder.Configuration["JobScheduling:StartupDelay"]), builder.Configuration)
    .WithSqlServerStore(builder.Configuration["JobScheduling:Quartz:quartz.dataSource.default.connectionString"])
    .WithBehavior<ModuleScopeJobSchedulingBehavior>()
    .AddEndpoints()
    .AddConsoleCommands();

builder.Services.AddMapping().WithMapster();
builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized());
```

### Module Registration Example

The core module uses devkit extensions for startup tasks, jobs, database setup, repositories, and endpoint registration:

```csharp
services.AddStartupTasks(o => o
    .StartupDelay(moduleConfiguration.SeederTaskStartupDelay))
    .WithTask<CoreModuleDomainSeederTask>(o => o
        .Enabled(environment.IsLocalDevelopment() || environment.IsContainerized()));

services.AddJobScheduling(o => o
   .StartupDelay(configuration["JobScheduling:StartupDelay"]), configuration)
   .WithJob<CustomerExportJob>()
       .Cron(CronExpressions.EveryMinute)
       .Named($"{this.Name}_{nameof(CustomerExportJob)}").RegisterScoped();

services.AddSqlServerDbContext<CoreModuleDbContext>(o => o
        .UseConnectionString(moduleConfiguration.ConnectionStrings["Default"]))
    .WithSequenceNumberGenerator()
    .WithDatabaseMigratorService(o => o
        .Enabled(environment.IsLocalDevelopment() || environment.IsContainerized()))
    .WithOutboxDomainEventService(o => o
        .ProcessingInterval("00:00:30")
        .ProcessingModeImmediate()
        .StartupDelay("00:00:15")
        .PurgeOnStartup());

services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>()
    .WithBehavior<RepositoryTracingBehavior<Customer>>()
    .WithBehavior<RepositoryLoggingBehavior<Customer>>()
    .WithBehavior<RepositoryAuditStateBehavior<Customer>>()
    .WithBehavior<RepositoryOutboxDomainEventBehavior<Customer, CoreModuleDbContext>>();

services.AddEndpoints<CustomerEndpoints>();```
