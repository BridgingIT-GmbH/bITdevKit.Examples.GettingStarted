# bITdevKit Reference Guide

This document provides a quick reference for key bITdevKit classes, interfaces, and extension methods used when implementing aggregates.

**Purpose**: Help developers understand and use bITdevKit abstractions effectively  
**Audience**: Developers implementing aggregates or extending the framework  
**Official Docs**: [bITdevKit Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs)

---

## Table of Contents

- [Domain Layer](#domain-layer)
- [Application Layer](#application-layer)
- [Infrastructure Layer](#infrastructure-layer)
- [Common Extensions](#common-extensions)
- [Result Pattern](#result-pattern)
- [Pipeline Behaviors](#pipeline-behaviors)
- [References](#references)

---

## Domain Layer

### AuditableAggregateRoot<TId>

**Namespace**: `BridgingIT.DevKit.Domain.Model`  
**Purpose**: Base class for aggregate roots with audit tracking

**Properties**:
```csharp
public class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditable
    where TId : IEntityId
{
    public AuditState AuditState { get; set; } // CreatedDate, CreatedBy, UpdatedDate, UpdatedBy
    public string ConcurrencyVersion { get; set; } // Optimistic concurrency
    public IReadOnlyCollection<DomainEventBase> DomainEvents { get; } // Domain events
}
```

**Usage**:
```csharp
[TypedEntityId<Guid>]
public partial class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
}
```

**Methods**:
- `Register(DomainEventBase domainEvent)`: Add domain event
- `ClearDomainEvents()`: Remove all domain events (called after publishing)

---

### ValueObject

**Namespace**: `BridgingIT.DevKit.Domain.Model`  
**Purpose**: Base class for value objects with structural equality

**Abstract Methods**:
```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetAtomicValues();
}
```

**Usage**:
```csharp
public class EmailAddress : ValueObject
{
    public string Value { get; private set; }
    
    private EmailAddress() { } // EF Core
    
    private EmailAddress(string value)
    {
        this.Value = value;
    }
    
    public static Result<EmailAddress> Create(string value)
    {
        return Result.Success()
            .Ensure(value.IsNotEmpty(), "Email is required")
            .Ensure(value.Contains("@"), "Email must contain @")
            .Tap(() => new EmailAddress(value));
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
    
    public static implicit operator string(EmailAddress email) => email?.Value;
}
```

**Equality**: Two value objects equal if all atomic values equal (structural equality)

---

### Enumeration

**Namespace**: `BridgingIT.DevKit.Domain.Model`  
**Purpose**: Base class for type-safe enumerations with behavior

**Base Properties/Methods**:
```csharp
public abstract class Enumeration : IComparable
{
    public int Value { get; }
    public string Name { get; }
    
    protected Enumeration(int value, string name)
    {
        this.Value = value;
        this.Name = name;
    }
    
    public static T FromValue<T>(int value) where T : Enumeration;
    public static T FromName<T>(string name) where T : Enumeration;
    public static IEnumerable<T> GetAll<T>() where T : Enumeration;
}
```

**Usage**:
```csharp
public class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Lead = new(1, nameof(Lead), enabled: true);
    public static readonly CustomerStatus Active = new(2, nameof(Active), enabled: true);
    public static readonly CustomerStatus Retired = new(3, nameof(Retired), enabled: false);
    
    public bool Enabled { get; private set; }
    
    private CustomerStatus(int value, string name, bool enabled) : base(value, name)
    {
        this.Enabled = enabled;
    }
}

// Retrieving enumerations
var active = CustomerStatus.FromName("Active");
var lead = CustomerStatus.FromValue(1);
var all = Enumeration.GetAll<CustomerStatus>();
```

---

### TypedEntityId<T>

**Namespace**: `BridgingIT.DevKit.Domain.Model`  
**Purpose**: Source generator attribute for creating strongly-typed entity IDs

**Usage (Attribute)**:
```csharp
[TypedEntityId<Guid>]
public partial class Customer : AuditableAggregateRoot<CustomerId>
{
    // Source generator creates CustomerId struct automatically
}

// Generated struct (not written by you):
public partial struct CustomerId : IEntityId<Guid>
{
    public Guid Value { get; }
    public static CustomerId Create(Guid value) => new(value);
}
```

**Usage (Manual)**:
```csharp
public partial struct CustomerId : IEntityId<Guid>
{
    private readonly Guid value;
    
    public Guid Value => this.value;
    
    private CustomerId(Guid value)
    {
        this.value = value;
    }
    
    public static CustomerId Create(Guid value) => new(value);
}
```

---

### DomainEventBase

**Namespace**: `BridgingIT.DevKit.Domain.EventSourcing`  
**Purpose**: Base class for domain events

**Properties**:
```csharp
public abstract class DomainEventBase : IDomainEvent
{
    public Guid EventId { get; } // Unique event ID
    public DateTime Timestamp { get; } // When event occurred
}
```

**Usage**:
```csharp
public partial class CustomerCreatedDomainEvent(Customer model) : DomainEventBase
{
    public Customer Model { get; private set; } = model;
}

// Registering event in aggregate
var customer = new Customer { /* ... */ };
customer.Register(new CustomerCreatedDomainEvent(customer));
```

---

### Domain Extensions (Result Pattern Helpers)

**Namespace**: `BridgingIT.DevKit.Domain`  
**Purpose**: Fluent helpers for Result<T> chaining in domain logic

**Create Pattern** (factory methods):
```csharp
public static Result<Customer> Create(string firstName, string lastName, string email, CustomerStatus status)
{
    return Result.Success()
        .Ensure(firstName.IsNotEmpty(), "First name is required")
        .Ensure(lastName.IsNotEmpty(), "Last name is required")
        .Bind(() => EmailAddress.Create(email))
        .Tap(emailAddress => new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = emailAddress,
            Status = status
        }.Register(new CustomerCreatedDomainEvent(...)));
}
```

**Change Pattern** (mutation methods):
```csharp
public Result<Customer> ChangeEmail(string email)
{
    return this.Change()
        .Ensure(() => email.IsNotEmpty(), "Email is required")
        .Set(() => EmailAddress.Create(email), v => this.Email = v)
        .Register(() => new CustomerUpdatedDomainEvent(this))
        .Apply();
}
```

**Methods**:
- `.Ensure(condition, errorMessage)`: Validate condition, return Failure if false
- `.Bind(func)`: Chain Result-returning functions (railway-oriented programming)
- `.Tap(func)`: Execute action on success, pass value through
- `.Change()`: Start change chain for aggregate mutation
- `.Set(valueFactory, setter)`: Set property value (handles Result unwrapping)
- `.Register(eventFactory)`: Register domain event
- `.Apply()`: Complete change chain, return Result<T>

---

## Application Layer

### RequestBase<TResponse>

**Namespace**: `BridgingIT.DevKit.Application.Commands` / `BridgingIT.DevKit.Application.Queries`  
**Purpose**: Base class for commands and queries

**Usage (Command)**:
```csharp
public class CustomerCreateCommand : RequestBase<Result<CustomerModel>>
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }
    public string Status { get; init; }
}
```

**Usage (Query)**:
```csharp
public class CustomerFindOneQuery : RequestBase<Result<CustomerModel>>
{
    public Guid Id { get; init; }
}

public class CustomerFindAllQuery : RequestBase<Result<IEnumerable<CustomerModel>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string OrderBy { get; init; }
}
```

---

### RequestHandlerBase<TRequest, TResponse>

**Namespace**: `BridgingIT.DevKit.Application.Commands` / `BridgingIT.DevKit.Application.Queries`  
**Purpose**: Base class for command/query handlers

**Usage**:
```csharp
public class CustomerCreateCommandHandler(
    IGenericRepository<Customer> repository,
    IMapper mapper)
    : RequestHandlerBase<CustomerCreateCommand, Result<CustomerModel>>
{
    private readonly IGenericRepository<Customer> repository = repository;
    private readonly IMapper mapper = mapper;
    
    public override async Task<Result<CustomerModel>> Handle(
        CustomerCreateCommand request,
        CancellationToken cancellationToken)
    {
        var customerResult = Customer.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            CustomerStatus.FromName(request.Status));
        
        if (customerResult.IsFailure)
            return Result.Failure<CustomerModel>(customerResult.Messages);
        
        await this.repository.InsertAsync(customerResult.Value, cancellationToken);
        
        return Result.Success(this.mapper.Map<CustomerModel>(customerResult.Value));
    }
}
```

---

### IRequester

**Namespace**: `BridgingIT.DevKit.Application.Messaging`  
**Purpose**: Mediator for sending commands/queries to handlers

**Methods**:
```csharp
public interface IRequester
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default);
}
```

**Usage (in Endpoints)**:
```csharp
public class CustomerEndpoints(IRequester requester) : EndpointsBase
{
    private readonly IRequester requester = requester;
    
    private async Task<IResult> Create(
        CustomerCreateCommand command,
        CancellationToken cancellationToken)
    {
        var result = await this.requester.SendAsync(command, cancellationToken);
        
        return result.Match(
            success => TypedResults.Created($"/api/core/customers/{success.Id}", success),
            failure => TypedResults.BadRequest(failure.ToString()));
    }
}
```

---

### FluentValidation (AbstractValidator<T>)

**Namespace**: `FluentValidation`  
**Purpose**: Declarative validation for commands/queries

**Usage**:
```csharp
public class CustomerCreateCommandValidator : AbstractValidator<CustomerCreateCommand>
{
    public CustomerCreateCommandValidator()
    {
        this.RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name too long");
        
        this.RuleFor(c => c.LastName)
            .NotEmpty()
            .MaximumLength(100);
        
        this.RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Invalid email format");
        
        this.RuleFor(c => c.Status)
            .Must(BeValidStatus).WithMessage("Invalid status");
    }
    
    private bool BeValidStatus(string status)
    {
        return CustomerStatus.FromName(status) is not null;
    }
}
```

**Common Rules**:
- `NotEmpty()`: Value not null/empty/whitespace
- `NotNull()`: Value not null
- `MaximumLength(n)`: String length ≤ n
- `EmailAddress()`: Valid email format
- `Must(predicate)`: Custom validation
- `WithMessage(message)`: Custom error message

---

## Infrastructure Layer

### IGenericRepository<T>

**Namespace**: `BridgingIT.DevKit.Domain.Repositories`  
**Purpose**: Generic repository abstraction for data access

**Methods**:
```csharp
public interface IGenericRepository<TEntity>
    where TEntity : class, IEntity
{
    Task<TEntity> FindOneAsync(
        object id,
        CancellationToken cancellationToken = default);
    
    Task<TEntity> FindOneAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> FindAllAsync(
        CancellationToken cancellationToken = default);
    
    Task<IEnumerable<TEntity>> FindAllAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);
    
    Task<TEntity> InsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
    
    Task<TEntity> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        object id,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
```

**Usage (in Handlers)**:
```csharp
public class CustomerCreateCommandHandler
{
    private readonly IGenericRepository<Customer> repository;
    
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        var customer = Customer.Create(...).Value;
        
        await this.repository.InsertAsync(customer, cancellationToken);
        
        return Result.Success(this.mapper.Map<CustomerModel>(customer));
    }
}
```

---

### Repository Registration

**Namespace**: `BridgingIT.DevKit.Infrastructure.EntityFramework`  
**Purpose**: Register repositories with DI container

**Usage (in Module startup)**:
```csharp
public static IServiceCollection AddCoreModule(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>()
        .WithBehavior<RepositoryLoggingBehavior<Customer>>()
        .WithBehavior<RepositoryAuditStateBehavior<Customer>>()
        .WithBehavior<RepositoryDomainEventBehavior<Customer>>();
    
    return services;
}
```

**Behaviors**:
- `RepositoryLoggingBehavior<T>`: Log repository operations
- `RepositoryAuditStateBehavior<T>`: Set CreatedDate, UpdatedDate, etc.
- `RepositoryDomainEventBehavior<T>`: Publish domain events after SaveChanges
- `RepositoryTrackerBehavior<T>`: Track entity changes (optional)

---

### DbContext Configuration

**Namespace**: `BridgingIT.DevKit.Infrastructure.EntityFramework`  
**Purpose**: Base class for module DbContext

**Usage**:
```csharp
public class CoreModuleDbContext : ModuleDbContextBase
{
    public DbSet<Customer> Customers { get; set; }
    
    public CoreModuleDbContext(DbContextOptions<CoreModuleDbContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerTypeConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
```

**Registration**:
```csharp
services.AddSqlServerDbContext<CoreModuleDbContext>(
    configuration.GetConnectionString("CoreModule"),
    provider => provider.UseSqlServer());
```

---

### Entity Type Configuration Extensions

**Namespace**: `BridgingIT.DevKit.Infrastructure.EntityFramework`  
**Purpose**: Extension methods for EF Core configuration

**OwnsOneAuditState()**:
```csharp
builder.OwnsOneAuditState(); // Configure AuditState as owned entity
```

**HasConversion (Enumeration)**:
```csharp
builder.Property(e => e.Status)
    .HasConversion(new EnumerationConverter<CustomerStatus>());
```

**HasConversion (TypedEntityId)**:
```csharp
builder.Property(e => e.Id)
    .HasConversion(
        id => id.Value,
        value => CustomerId.Create(value));
```

---

## Common Extensions

### String Extensions

**Namespace**: `BridgingIT.DevKit.Common`

**Methods**:
- `IsNotEmpty()`: Returns true if string not null/empty/whitespace
- `IsNullOrEmpty()`: Returns true if string is null, empty, or whitespace
- `TruncateLeft(length)`: Truncate from left, keep right portion
- `TruncateRight(length)`: Truncate from right, keep left portion

**Usage**:
```csharp
if (firstName.IsNotEmpty())
{
    // ...
}
```

---

### Enumerable Extensions

**Namespace**: `BridgingIT.DevKit.Common`

**Methods**:
- `SafeNull()`: Return empty collection if null
- `IsNullOrEmpty()`: Check if collection null or empty

**Usage**:
```csharp
foreach (var item in collection.SafeNull())
{
    // No NullReferenceException if collection is null
}
```

---

## Result Pattern

### Result<T>

**Namespace**: `BridgingIT.DevKit.Common`  
**Purpose**: Explicit success/failure handling without exceptions

**Properties**:
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public T Value { get; } // Only access if IsSuccess
    public IEnumerable<string> Messages { get; } // Error messages
}
```

**Static Factory Methods**:
```csharp
Result<T>.Success(T value);
Result<T>.Failure(string message);
Result<T>.Failure(IEnumerable<string> messages);
```

**Usage**:
```csharp
// Create Result
var result = Result<Customer>.Success(customer);
var failure = Result<Customer>.Failure("Email is required");

// Check Result
if (result.IsSuccess)
{
    var customer = result.Value;
}
else
{
    var errors = result.Messages;
}

// Match Pattern
return result.Match(
    success => TypedResults.Ok(success),
    failure => TypedResults.BadRequest(failure.ToString()));
```

**Railway-Oriented Programming**:
```csharp
return Result.Success()
    .Ensure(firstName.IsNotEmpty(), "First name required") // Failure stops chain
    .Ensure(lastName.IsNotEmpty(), "Last name required")
    .Bind(() => EmailAddress.Create(email)) // Chain Results
    .Tap(emailAddress => new Customer { Email = emailAddress }); // Execute on success
```

---

## Pipeline Behaviors

### ValidationPipelineBehavior

**Purpose**: Automatically validate commands/queries via FluentValidation

**Registration** (automatic when using `AddRequesterHandlers`):
```csharp
services.AddRequesterHandlers<CustomerCreateCommand>(); // Registers handlers and behaviors
```

**How It Works**:
1. Request sent via IRequester
2. ValidationPipelineBehavior runs first
3. If validation fails, returns Result.Failure with validation messages
4. If validation succeeds, calls next behavior/handler

---

### RetryPipelineBehavior

**Purpose**: Retry failed handler executions (transient failures)

**Usage (via attribute)**:
```csharp
[Retry(times: 2, delayMilliseconds: 100)]
public class CustomerCreateCommandHandler : RequestHandlerBase<...>
{
    // Handler implementation
}
```

---

### TimeoutPipelineBehavior

**Purpose**: Enforce timeout on handler execution

**Usage (via attribute)**:
```csharp
[Timeout(seconds: 30)]
public class CustomerCreateCommandHandler : RequestHandlerBase<...>
{
    // Handler implementation
}
```

---

### ModuleScopeBehavior

**Purpose**: Set module context for logging, telemetry, etc.

**Registration** (automatic):
```csharp
services.AddRequesterHandlers<CustomerCreateCommand>();
```

---

## Mapster (IMapper)

**Namespace**: `Mapster`  
**Purpose**: Object-to-object mapping (domain ↔ DTO)

**IMapper Interface**:
```csharp
public interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
}
```

**Usage (in Handlers)**:
```csharp
public class CustomerFindOneQueryHandler
{
    private readonly IMapper mapper;
    
    public async Task<Result<CustomerModel>> Handle(CustomerFindOneQuery request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        if (customer is null)
            return Result.Success<CustomerModel>(null);
        
        return Result.Success(this.mapper.Map<CustomerModel>(customer));
    }
}
```

**Configuration (IRegister)**:
```csharp
public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerModel>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.Status, src => src.Status.Name);
    }
}
```

**Registration**:
```csharp
services.AddMapping().WithMapster<CoreModuleMapperRegister>();
```

---

## Summary Table

| Component | Namespace | Purpose |
|-----------|-----------|---------|
| `AuditableAggregateRoot<TId>` | `BridgingIT.DevKit.Domain.Model` | Base class for aggregates with audit tracking |
| `ValueObject` | `BridgingIT.DevKit.Domain.Model` | Base class for value objects with structural equality |
| `Enumeration` | `BridgingIT.DevKit.Domain.Model` | Base class for type-safe enumerations |
| `TypedEntityId<T>` | `BridgingIT.DevKit.Domain.Model` | Source generator for strongly-typed entity IDs |
| `DomainEventBase` | `BridgingIT.DevKit.Domain.EventSourcing` | Base class for domain events |
| `RequestBase<TResponse>` | `BridgingIT.DevKit.Application.Commands/Queries` | Base class for commands/queries |
| `RequestHandlerBase<TRequest, TResponse>` | `BridgingIT.DevKit.Application.Commands/Queries` | Base class for command/query handlers |
| `IRequester` | `BridgingIT.DevKit.Application.Messaging` | Mediator for sending commands/queries |
| `AbstractValidator<T>` | `FluentValidation` | Declarative validation for commands/queries |
| `IGenericRepository<T>` | `BridgingIT.DevKit.Domain.Repositories` | Generic repository abstraction for data access |
| `ModuleDbContextBase` | `BridgingIT.DevKit.Infrastructure.EntityFramework` | Base class for module DbContext |
| `IMapper` | `Mapster` | Object-to-object mapping (domain ↔ DTO) |
| `Result<T>` | `BridgingIT.DevKit.Common` | Explicit success/failure handling |

---

## References

- **Official bITdevKit Documentation**: [GitHub Docs](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs)
- **Customer Aggregate Example**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/Customer.cs`
- **Result Pattern Examples**: `.github/skills/domain-add-aggregate/examples/result-chaining-patterns.md`
- **Architecture Overview**: `.github/skills/domain-add-aggregate/docs/architecture-overview.md`

---

**Note**: This is a quick reference. For comprehensive documentation, refer to the official bITdevKit docs and source code examples.
