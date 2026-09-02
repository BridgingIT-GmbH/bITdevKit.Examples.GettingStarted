# ADR-0015: DevKit-Native Durable Jobs

## Status

Accepted

## Context

The GettingStarted application performs recurring work that does not belong in an HTTP request. The current example is `CustomerExportJob`, which periodically retrieves customers and demonstrates where an application would export them to an external system or file. This work must execute reliably, expose its outcome to operators, and remain owned by the module that owns the customer domain and persistence model.

A timer alone is insufficient for this responsibility. Scheduled work needs stable identities, trigger definitions, concurrency control, retry behavior, durable occurrences, execution history, and coordination between application instances. Operators also need a consistent way to inspect definitions, runs, failures, and scheduler state without application-specific diagnostic infrastructure.

The application uses bITdevKit as its application foundation. Its Jobs subsystem provides code-first job definitions and triggers, `Result`-based execution contracts, durable Entity Framework persistence, operational endpoints, console commands, dashboard pages, metrics, and MCP handlers. These capabilities fit the modular-monolith architecture and use the same observability and error-handling conventions as the rest of the application.

### Technical Requirements

- Register jobs and triggers in source code with stable public names.
- Run `CustomerExportJob` every minute after a 30-second scheduler startup delay.
- Limit the job to one concurrent execution.
- Retry failed executions up to three attempts with a fixed one-second delay.
- Persist runtime state, trigger state, occurrences, dependencies, batches, executions, history, accepted events, and leases.
- Return repository failures as failed `Result` values so retries and execution history are accurate.
- Resolve the job and its repository with scoped dependency-injection lifetimes.
- Expose job definitions and execution state through the standard DevKit operational surfaces.
- Keep scheduler registration and persistence ownership inside `CoreModule`.

### Business Requirements

- Scheduled operations must remain observable and diagnosable after process restarts.
- Failures must be visible and actionable instead of being represented as successful runs.
- The sample must demonstrate a production-oriented Jobs setup without adding infrastructure unrelated to its domain.
- Operational identifiers must remain predictable for dashboards, automation, diagnostics, and support tooling.

### Design Challenges

Job definitions are application behavior and therefore belong in source control, while execution state is operational data and must be durable. The design must keep source registration authoritative without losing occurrences, attempts, history, or coordination state when the host restarts.

The export job depends on a scoped repository. Scheduler activation must respect that lifetime and avoid manually-created nested scopes. The execution contract must also preserve the repository's `Result` failure information so scheduler retry and history decisions reflect the real outcome.

The application is modular. Although the web host starts the scheduler, `CoreModule` owns the job, repository, database context, and module identity. Configuration must preserve that ownership boundary while still enabling host-wide dashboard, API, console, metrics, and MCP access.

## Decision

Use `BridgingIT.DevKit.Application.Jobs` for background job definition and execution, with `BridgingIT.DevKit.Presentation.Web.Jobs` providing the operational web integration.

`CoreModuleModule` owns the complete registration of `CustomerExportJob`. The job derives from `JobBase`, returns `Task<Result>`, receives its scoped repository and typed logger through constructor injection, and reports a non-sensitive execution summary through the job context and result.

Use the Entity Framework Jobs provider with `CoreModuleDbContext` as the durable store. The context implements `IJobsContext` and exposes every required Jobs entity set. Jobs schema changes are managed through the module's normal EF Core migration workflow.

### How It Works

1. `CoreModuleModule` calls `AddJobScheduler(configuration)` during module registration.
2. `CustomerExportJob` is registered under the stable name `CoreModule_CustomerExportJob` with the stable trigger name `cron`.
3. The trigger uses `CronExpressions.EveryMinute`; the scheduler starts after 30 seconds.
4. The job uses a scoped lifetime, a concurrency limit of one, and three attempts with a fixed one-second delay.
5. `WithEntityFramework<CoreModuleDbContext>()` stores scheduler and execution data in the CoreModule database.
6. `ModuleScopeBehavior` establishes the module context for job execution.
7. Jobs endpoints and console commands are enabled by the module registration. The application dashboard includes the Jobs dashboard plugin, and the DevKit integration supplies metrics and MCP handlers.
8. A successful execution reports the number of exported customers. A repository failure is returned as a failed `Result` and is eligible for the configured retry policy.

The canonical scheduler registration is:

```csharp
services.AddJobScheduler(configuration)
    .StartupDelay(TimeSpan.FromSeconds(30))
    .WithJob<CustomerExportJob>(CustomerExportJob.JobName, job => job
        .Description("Exports all customers from the repository.")
        .Module(this.Name)
        .UseLifetime(ServiceLifetime.Scoped)
        .WithConcurrency(1)
        .WithRetry(retry => retry
            .MaxAttempts(3)
            .FixedDelay(TimeSpan.FromSeconds(1)))
        .AddTrigger(CustomerExportJob.TriggerName, trigger => trigger
            .Cron(CronExpressions.EveryMinute)))
    .WithEntityFramework<CoreModuleDbContext>()
    .WithBehavior<ModuleScopeBehavior>()
    .AddEndpoints()
    .AddConsoleCommands();
```

## Rationale

1. **Consistent application contracts**: Jobs return `Result`, matching the explicit success and failure semantics used by commands, queries, repositories, and other application services.
2. **Durable execution model**: Occurrences, attempts, history, batches, accepted events, and leases survive process restarts and support coordinated execution across hosts.
3. **Code-first ownership**: Job and trigger definitions remain reviewable source code while persisted runtime state augments, but does not replace, those definitions.
4. **Module cohesion**: Scheduler registration is colocated with the module's job, repository, persistence context, and endpoint registration.
5. **Correct dependency lifetimes**: Scoped job activation allows direct injection of the scoped repository and keeps scope ownership within the scheduler runtime.
6. **Operational visibility**: Standard APIs, console commands, dashboard pages, metrics, and MCP tools expose the same scheduler model without custom adapters.
7. **Deterministic testing**: `JobSchedulerTestHarness` supports job activation, controlled time, trigger materialization, retries, and retained-history assertions without starting the complete web host.

## Consequences

### Positive

- Background work follows the same `Result`-based failure model as the rest of the application.
- Retry decisions and durable history reflect repository failures accurately.
- Stable job and trigger names provide predictable identities for operations and automation.
- The CoreModule composition root contains the complete job definition and persistence configuration.
- Execution state survives host restarts and can be coordinated safely through leases.
- Operators can inspect definitions, triggers, occurrences, attempts, and history through standard DevKit surfaces.
- Tests can verify scheduler-visible outcomes with a purpose-built harness.

### Negative

- The CoreModule database contains additional Jobs tables and operational data that require retention and maintenance consideration.
- `CoreModuleDbContext` depends on the Jobs persistence contract and must stay aligned with the installed DevKit package version.
- In-process execution shares CPU, memory, and availability with the web host.
- Each registered job requires deliberate retry, concurrency, trigger, and lifetime configuration.
- Durable multi-host operation depends on database availability and correct lease behavior.

### Neutral

- CRON is the scheduling notation for the recurring customer export.
- The web application hosts both HTTP traffic and scheduled background execution.
- The Jobs schema is managed through the same EF Core migration process as module-owned business data.
- Job definitions remain source-controlled even though operators can manage supported runtime state such as enablement and pausing.

## Alternatives Considered

### 1. ASP.NET Core `BackgroundService` with `PeriodicTimer`

A hosted service could execute the export operation on a fixed interval with little initial setup.

Rejected because the application would need custom persistence, retry coordination, concurrency control, history, lease management, operational endpoints, dashboard integration, metrics, and MCP diagnostics. A timer also does not provide stable named trigger definitions.

### 2. Direct integration with a third-party scheduler

The application could configure a scheduler library and expose its concepts directly in the module and host.

Rejected because this would couple application code and operational tooling to provider-specific contracts. The DevKit Jobs subsystem already supplies the required scheduling semantics while preserving the application's standard `Result`, module, persistence, and observability conventions.

### 3. External scheduler or serverless worker

The recurring export could run in a separate deployment unit triggered by a cloud scheduler.

Rejected because the reference application would require additional deployment, identity, networking, configuration, and operational coordination. The demonstrated workload does not require process isolation or independent scaling.

### 4. In-memory Jobs persistence

The DevKit Jobs runtime can use its lightweight in-memory provider.

Rejected for this application because occurrences, execution history, runtime state, and leases must survive restarts. The in-memory provider remains appropriate for isolated tests and transient prototypes.

## Related Decisions

- [ADR-0002](0002-result-pattern-error-handling.md): Jobs use `Result` for explicit execution outcomes.
- [ADR-0003](0003-modular-monolith-architecture.md): CoreModule owns its job registration and persistence integration.
- [ADR-0007](0007-entity-framework-core-code-first-migrations.md): Jobs persistence participates in the module's EF Core migration workflow.
- [ADR-0016](0016-logging-observability-strategy.md): Job execution uses structured logging and shared telemetry.
- [ADR-0018](0018-dependency-injection-service-lifetimes.md): Scoped jobs receive scoped dependencies directly.
- [ADR-0021](0021-bitdevkit-as-the-application-foundation.md): The application uses the DevKit Jobs subsystem and its operational integrations.

## References

- [bITdevKit Jobs guide](../../.bdk/docs/features-jobs.md)
- [bITdevKit Dashboard guide](../../.bdk/docs/features-presentation-dashboard.md)
- [bITdevKit Metrics guide](../../.bdk/docs/features-metrics.md)
- [bITdevKit MCP guide](../../.bdk/docs/features-cli-mcp.md)
- [CoreModule documentation](../../src/Modules/CoreModule/CoreModule-README.md)

## Notes

### Job Contract

`CustomerExportJob` receives its dependencies through constructor injection and preserves repository failures:

```csharp
public class CustomerExportJob(
    ILogger<CustomerExportJob> logger,
    IGenericRepository<Customer> repository) : JobBase
{
    public override async Task<Result> ExecuteAsync(
        IJobExecutionContext<Unit> context,
        CancellationToken cancellationToken = default)
    {
        var customersResult = await repository.FindAllResultAsync(
            cancellationToken: cancellationToken);

        if (customersResult.IsFailure)
        {
            return Result.Failure(customersResult.Messages, customersResult.Errors);
        }

        var customerCount = customersResult.Value.Count();
        var message = $"Customer export completed. Customers={customerCount}";
        context.Messages.Add(message);

        return Result.Success(message);
    }
}
```

The execution summary contains only the customer count. Logs identify customers by their entity ID and do not include email addresses or other customer contact data.

### Durable Context

The module context implements the Jobs persistence capability and exposes the required entity sets:

```csharp
public class CoreModuleDbContext(DbContextOptions<CoreModuleDbContext> options)
    : ModuleDbContextBase(options), IOutboxDomainEventContext, IJobsContext
{
    public DbSet<JobRuntimeStateEntity> JobRuntimeStates { get; set; }
    public DbSet<JobTriggerRuntimeStateEntity> JobTriggerRuntimeStates { get; set; }
    public DbSet<JobOccurrenceEntity> JobOccurrences { get; set; }
    public DbSet<JobOccurrenceDependencyEntity> JobOccurrenceDependencies { get; set; }
    public DbSet<JobBatchEntity> JobBatches { get; set; }
    public DbSet<JobBatchOccurrenceEntity> JobBatchOccurrences { get; set; }
    public DbSet<JobExecutionEntity> JobExecutions { get; set; }
    public DbSet<JobExecutionHistoryEntity> JobExecutionHistory { get; set; }
    public DbSet<JobBatchHistoryEntity> JobBatchHistory { get; set; }
    public DbSet<JobAcceptedEventEntity> JobAcceptedEvents { get; set; }
    public DbSet<JobLeaseEntity> JobLeases { get; set; }
}
```

### Test Contract

Scheduler-facing tests use `JobSchedulerTestHarness` to verify successful counts, the zero-customer result, and repository failures. Tests should assert the returned `Result` and reported execution messages rather than relying only on log output.

### Implementation Files

- `src/Modules/CoreModule/CoreModule.Application/Jobs/CustomerExportJob.cs`
- `src/Modules/CoreModule/CoreModule.Presentation/CoreModuleModule.cs`
- `src/Modules/CoreModule/CoreModule.Infrastructure/EntityFramework/CoreModuleDbContext.cs`
- `src/Modules/CoreModule/CoreModule.Infrastructure/EntityFramework/Migrations/*_AddJobs.cs`
- `src/Presentation.Web.Server/ProgramExtensions.Authentication.cs`
- `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Jobs/CustomerExportJobTests.cs`
