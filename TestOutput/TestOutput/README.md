# BridgingIT DevKit GettingStarted Example

![bITDevKit](https://raw.githubusercontent.com/bridgingIT/bITdevKit.Examples.GettingStarted/main/bITDevKit_Logo.png)

An application built using .NET 9 and following a Domain-Driven Design (DDD) approach by using the BridgingIT DevKit (bIT DevKit).

## Features
- Modular architecture with TestCore as an example. [Modules](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-modules.md)
- Application layer with Commands (e.g., CustomerCreateCommand) and Queries (e.g., CustomerFindAllQuery, CustomerFindOneQuery) using IRequester. [Requester](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-requester-notifier.md), [Commands and Queries](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-application-commands-queries.md)
- Domain layer with Aggregates (Customer), Value Objects (EmailAddress, CustomerId), Enumerations (CustomerStatus), Domain Events (CustomerCreatedDomainEvent, CustomerUpdatedDomainEvent), and Business Rules (e.g., EmailShouldBeUniqueRule). [Domain](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-domain-models.md), [DomainEvents](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-domain-events.md), [Rules](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-rules.md)
- Infrastructure layer with Entity Framework Core (CoreDbContext, migrations, configurations) and Generic Repositories with behaviors (logging, audit, domain event publishing). [Repositories](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-domain-repositories.md)
- Presentation layer with Web API Endpoints for CRUD operations on Customers, using minimal API-style routing. [Endpoints](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-presentation-endpoints.md)
- Startup tasks for seeding domain data (CoreDomainSeederTask). [StartupTasks](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-startuptasks.md)
- Job scheduling with Quartz (e.g., CustomerExportJob). [JobScheduling](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-jobscheduling.md)
- Comprehensive testing: Unit tests (e.g., for command/query handlers, architecture rules), Integration tests (e.g., for endpoints).
- Architecture validation tests to enforce Onion Architecture dependencies and domain rules (e.g., no public constructors on entities/value objects).

## Frameworks and Libaries
- [.NET 9](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
- [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) for data access
- [Serilog](https://serilog.net/) for structured logging
- [Mapster](https://github.com/MapsterMapper/Mapster) for object mapping
- [FluentValidation](https://fluentvalidation.net/) for validation
- [Quartz.NET](https://www.quartz-scheduler.net/) for job scheduling
- [xUnit.net](https://xunit.net/), [NSubstitute](https://nsubstitute.github.io/), [Shouldly](https://docs.shouldly.org/) for testing
---
## Getting Started

### Running the Application

1. Ensure you have .NET 9/10 SDK installed.
2. Configure the database connection string in `appsettings.json` under "TestCore:ConnectionStrings:Default" (e.g., SQL Server LocalDB).
3. Optionally, start supporting containers with `docker-compose up` or `docker-compose up -d` for SQL Server and Seq logging.
4. Set `Presentation.Web.Server` as the startup project.
5. Run with `CTRL+F5` to start the host at [https://localhost:7144](https://localhost:7144).

- **SQL Server** details: Use the connection string from `appsettings.json` (e.g., `Server=(localdb)\\MSSQLLocalDB;Database=bit_devkit_gettingstarted;Trusted_Connection=True`).
- **Swagger UI** is available [here](https://localhost:7144/swagger/index.html).
- **Seq** Dashboard (if using containers) is available [here](http://localhost:15349).

The application will automatically migrate the database on startup (via DatabaseMigratorService in TestCore) and seed initial data (via CoreDomainSeederTask) in development mode.

### Architecture Overview

The GettingStarted project follows Clean/Onion Architecture principles, powered by bIT DevKit for modular DDD:

![](assets/Onion.drawio.png)

- **Core (Domain)**: Business logic, aggregates, value objects, events.
- **Application**: Commands, queries, handlers with behaviors (retry, timeout).
- **Infrastructure**: Persistence (EF Core), repositories with behaviors (logging, audit).
- **Presentation**: Web API endpoints, module registration.

### Solution Structure

```
TestOutput.sln
├── src
│   ├── Modules
│   │   └── TestCore
│   │       ├── TestCore.Application
│   │       ├── TestCore.Domain
│   │       ├── TestCore.Infrastructure
│   │       ├── TestCore.Presentation
│   │       ├── TestCore.UnitTests
│   │       └── TestCore.IntegrationTests
│   └── Presentation.Web.Server
└── docker-compose.yml
```

### Application

Contains commands, queries, and handlers for business operations.

#### Commands

([CustomerCreateCommand.cs](./src/Modules/TestCore/TestCore.Application/Commands/CustomerCreateCommand.cs))

```csharp
public class CustomerCreateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    public CustomerModel Model { get; set; } = model;

    public class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();
            this.RuleFor(c => c.Model.FirstName).NotNull().NotEmpty().WithMessage("Must not be empty.");
            this.RuleFor(c => c.Model.LastName).NotNull().NotEmpty().WithMessage("Must not be empty.");
            this.RuleFor(c => c.Model.Email).NotNull().NotEmpty().WithMessage("Must not be empty.");
        }
    }
}
```

([CustomerCreateCommandHandler.cs](./src/Modules/TestCore/TestCore.Application/Commands/CustomerCreateCommandHandler.cs))

```csharp
[HandlerRetry(2, 100)]   // retry twice, wait 100ms between retries
[HandlerTimeout(500)]    // timeout after 500ms execution
public class CustomerCreateCommandHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerCreateCommand, CustomerModel>(loggerFactory)
{
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerCreateCommand request,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        var customer = mapper.Map<CustomerModel, Customer>(request.Model);
        return await repository.InsertResultAsync(customer, cancellationToken: cancellationToken)
            .Tap(_ => Console.WriteLine("AUDIT"))
            .Map(mapper.Map<Customer, CustomerModel>);
    }
}
```

#### Queries

([CustomerFindAllQuery.cs](./src/Modules/TestCore/TestCore.Application/Queries/CustomerFindAllQuery.cs))

```csharp
public class CustomerFindAllQuery : RequestBase<IEnumerable<CustomerModel>>
{
    public Specification<Customer>? Filter { get; set; } = null;
}
```

([CustomerFindAllQueryHandler.cs](./src/Modules/TestCore/TestCore.Application/Queries/CustomerFindAllQueryHandler.cs))

```csharp
[HandlerRetry(2, 100)]   // retry twice, wait 100ms between retries
[HandlerTimeout(500)]    // timeout after 500ms execution
public class CustomerFindAllQueryHandler(
    ILoggerFactory loggerFactory,
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerFindAllQuery, IEnumerable<CustomerModel>>(loggerFactory)
{
    protected override async Task<Result<IEnumerable<CustomerModel>>> HandleAsync(
        CustomerFindAllQuery request,
        SendOptions options,
        CancellationToken cancellationToken) =>
        await repository.FindAllResultAsync(request.Filter, cancellationToken: cancellationToken)
            .Tap(_ => Console.WriteLine("AUDIT"))
            .Map(mapper.Map<Customer, CustomerModel>);
}
```

### Domain

Core business logic with domain models and aggregates.

#### Aggregates

([Customer.cs](./src/Modules/TestCore/TestCore.Domain/Model/Customer.cs))

```csharp
[DebuggerDisplay("Id={Id}, Name={FirstName} {LastName}, Status={Status}")]
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>, IConcurrency
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public EmailAddress Email { get; private set; }
    public CustomerStatus Status { get; private set; } = CustomerStatus.Lead;
    public Guid ConcurrencyVersion { get; set; }

    public static Customer Create(string firstName, string lastName, string email)
    {
        var customer = new Customer(firstName, lastName, EmailAddress.Create(email));
        customer.DomainEvents.Register(new CustomerCreatedDomainEvent(customer));
        return customer;
    }

    // Additional methods for changing name, email, status with domain event registration
}
```

#### Value Objects

([EmailAddress.cs](./src/Modules/TestCore/TestCore.Domain/Model/EmailAddress.cs))

```csharp
[DebuggerDisplay("Value={Value}")]
public class EmailAddress : ValueObject
{
    public string Value { get; private set; }

    public static EmailAddress Create(string value)
    {
        value = value?.Trim()?.ToLowerInvariant();
        Rule.Add(RuleSet.IsValidEmail(value)).Throw();
        return new EmailAddress(value);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

#### Enumerations

([CustomerStatus.cs](./src/Modules/TestCore/TestCore.Domain/Model/CustomerStatus.cs))

```csharp
[DebuggerDisplay("Id={Id}, Value={Value}")]
public class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Lead = new(1, nameof(Lead), "Lead customer");
    public static readonly CustomerStatus Active = new(2, nameof(Active), "Active customer");
    public static readonly CustomerStatus Retired = new(3, nameof(Retired), "Retired customer");

    // Additional properties and methods
}
```

#### Domain Events

([CustomerCreatedDomainEvent.cs](./src/Modules/TestCore/TestCore.Domain/Events/CustomerCreatedDomainEvent.cs))

```csharp
public class CustomerCreatedDomainEvent(Customer model) : DomainEventBase
{
    public Customer Model { get; } = model;
}
```

### Infrastructure

Persistence setup with EF Core.

#### DbContext

([CoreDbContext.cs](./src/Modules/TestCore/TestCore.Infrastructure/EntityFramework/CoreDbContext.cs))

```csharp
public class CoreDbContext(DbContextOptions<CoreDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
    }
}
```

#### Configurations

([CustomerTypeConfiguration.cs](./src/Modules/TestCore/TestCore.Infrastructure/EntityFramework/Configurations/CustomerTypeConfiguration.cs))

```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers").HasKey(x => x.Id).IsClustered(false);
        // Additional property configurations for Id, Names, Email, Status, AuditState
    }
}
```

#### Migrations

Initial migration creates Customers table with audit fields.

### Presentation

Web API endpoints and module registration.

#### Module Registration

([TestCore.cs](./src/Modules/TestCore/TestCore.Presentation/TestCore.cs))

```csharp
public class TestCore : WebModuleBase
{
    public override IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration = null,
        IWebHostEnvironment environment = null)
    {
        var moduleConfiguration = this.Configure<TestCoreConfiguration, TestCoreConfiguration.Validator>(services, configuration);

        services.AddStartupTasks(o => o.WithTask<CoreDomainSeederTask>(o => o.Enabled(environment.IsLocalDevelopment())));
        services.AddJobScheduling(o => o.StartupDelay(configuration["JobScheduling:StartupDelay"]), configuration)
            .WithSqlServerStore(configuration["JobScheduling:Quartz:quartz.dataSource.default.connectionString"])
            .WithJob<CustomerExportJob>().Cron(CronExpressions.EveryHour).Named(nameof(CustomerExportJob)).RegisterScoped();

        services.AddSqlServerDbContext<CoreDbContext>(o => o.UseConnectionString(moduleConfiguration.ConnectionStrings["Default"]).UseLogger())
            .WithDatabaseMigratorService(o => o.Enabled(environment.IsLocalDevelopment()).DeleteOnStartup(environment.IsLocalDevelopment()));

        services.AddEntityFrameworkRepository<Customer, CoreDbContext>()
            .WithBehavior<RepositoryLoggingBehavior<Customer>>()
            .WithBehavior<RepositoryAuditStateBehavior<Customer>>()
            .WithBehavior<RepositoryDomainEventPublisherBehavior<Customer>>();

        services.AddEndpoints<CoreCustomerEndpoints>();

        return services;
    }
}
```

#### Endpoints

([CoreCustomerEndpoints.cs](./src/Modules/TestCore/TestCore.Presentation/Web/Endpoints/CoreCustomerEndpoints.cs))

```csharp
public class CoreCustomerEndpoints : EndpointsBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/core/customers").WithTags("Core.Customers");

        group.MapGet("/{id:guid}", CustomerFindOne).WithName("Core.Customers.GetById");
        group.MapGet("", CustomerFindAll).WithName("Core.Customers.GetAll");
        group.MapPost("", CustomerCreate).WithName("Core.Customers.Create");
        group.MapPut("/{id:guid}", CustomerUpdate).WithName("Core.Customers.Update");
        group.MapDelete("/{id:guid}", CustomerDelete).WithName("Core.Customers.Delete");
    }

    // Handler methods for each endpoint using IRequester
}
```

#### Mapper Registration

([TestCoreMapperRegister.cs](./src/Modules/TestCore/TestCore.Presentation/TestCoreMapperRegister.cs))

```csharp
public class TestCoreMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Configurations for value objects, enumerations, and aggregate-DTO mappings
    }
}
```

Perfect 🚀 — let’s make it **short and developer‑oriented**, but also **informative** about how the OpenAPI part is setup (MSBuild, package references, and post‑build step).  

Here’s a **drop‑in section** for your README that matches the style of what you already have:

---

### OpenAPI Specification and Swagger UI

The project uses **build‑time OpenAPI** documentation creation.

- On **build**, the OpenAPI specification is generated to `wwwroot/openapi.json`.  
- ASP.NET Core serves this as a **static file** at [https://localhost:7144/openapi.json](https://localhost:7144/openapi.json).
- **Swagger UI** ([https://localhost:7144/swagger/index.html](https://localhost:7144/swagger/index.html)) is configured to use the generated specification.

This ensures the specification is **consistent across environments** and available as a build artifact.

#### Setup

- **Packages used**:  
  - `Microsoft.AspNetCore.OpenApi`  
  - `Microsoft.Extensions.ApiDescription.Server`  
  - `Swashbuckle.AspNetCore.SwaggerUI` (for the UI only)

- **Project file configuration** (`Presentation.Web.Server.csproj`):
  ```xml
  <PropertyGroup>
    <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
    <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/wwwroot</OpenApiDocumentsDirectory>
    <OpenApiGenerateDocumentsOptions>--file-name openapi</OpenApiGenerateDocumentsOptions>
  </PropertyGroup>
  <Target Name="GenerateOpenApiAfterBuild" AfterTargets="Build" DependsOnTargets="GenerateOpenApiDocuments" />
  ```

- **OpenApi services and static files** (`Program.cs`):
  ```csharp
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    ...
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi.json", "v1");
        app.MapOpenApi();
    });
    app.UseStaticFiles();
  ```


This ensures `openapi.json` is automatically created **after each build**. More information can be found in the [Microsoft Docs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time).

#### Client generation

The generated specification can be used to build strongly‑typed clients:

```bash
# TypeScript client
nswag openapi2tsclient /input:src/Presentation.Web.Server/wwwroot/openapi.json /output:Client.ts

# C# client
nswag openapi2csclient /input:src/Presentation.Web.Server/wwwroot/openapi.json /output:Client.cs
```

Since Swagger UI serves the exact same specification as a static asset, it can be targeted at the following endpoint: `https://localhost:7144/openapi.json`

### Testing

#### Unit Tests

Run tests in `TestCore.UnitTests` (e.g., CustomerCreateCommandHandlerTests, ArchitectureTests).

#### Integration Tests

Run tests in `TestCore.IntegrationTests` (e.g., CustomerEndpointTests for HTTP responses).

#### HTTP Tests

Use tools like Bruno/Postman or VS HTTP file to test endpoints:
- GET /api/core/customers
- POST /api/core/customers with body { "firstName": "John", "lastName": "Doe", "email": "john.doe@example.com" }

A few example requests are in [Core-API.http](./src/Modules/TestCore/Core-API.http).

--- 
## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.