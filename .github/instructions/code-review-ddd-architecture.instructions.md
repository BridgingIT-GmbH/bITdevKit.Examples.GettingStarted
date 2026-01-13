---
description: 'DDD Clean Architecture code review for modular monolith'
applyTo: '**'
excludeAgent: ["coding-agent"]
---

# DDD Clean Architecture Code Review

Specialized code review instructions for a modular monolith using DDD, Clean Architecture and bITdevKit patterns. These instructions extend the generic C# and .NET code review guidelines with project-specific architecture rules ().

## When to Apply

Use this instruction when:
- Reviewing C# code in a DDD/Clean Architecture project
- Checking architecture boundary violations
- Verifying domain model purity
- Validating CQRS command/query patterns
- Ensuring proper use of Result<T> error handling

## Architecture Layers

The project follows Clean Architecture with these layers:

```
Domain (innermost) → Application → Infrastructure → Presentation (outermost)
```

### Layer Dependency Rules

**🔴 CRITICAL**: Dependencies must only flow inward, never outward.

- **Domain**: ZERO external dependencies (no EF, no Application, no Infrastructure)
- **Application**: May reference Domain only
- **Infrastructure**: May reference Application and Domain
- **Presentation**: May reference all layers

### Module Structure

Each module under `src/Modules/<ModuleName>` follows this structure:

```
<ModuleName>.Domain/
<ModuleName>.Application/
  Commands/
  Queries/
<ModuleName>.Infrastructure/
<ModuleName>.Presentation/
  Web/
    Endpoints/
```

**🔴 CRITICAL**: Check for cross-module dependencies - modules should be self-contained.

## DDD Pattern Review

### Aggregates

**Requirements**:
- Properly encapsulated with business logic in aggregate root
- No direct access to child entities from outside aggregate
- Aggregate root enforces all invariants and business rules
- Methods return `Result<T>` for operations that can fail
- Prefer `Result<T>` or `Result` over throwing Exceptions for business rule violations or programming errors when the method returns a result `Result<T>` or `Result`
- No public setters; use methods to modify state
- Collection properties typed as `IReadOnlyCollection<T>`
- No lazy loading; all required data loaded eagerly

**Example**:
```csharp
namespace MyApp.Domain.CustomerAggregate;

/// <summary>
/// Aggregate root for customer management.
/// </summary>
public class Customer : AggregateRoot<CustomerId>
{
    private readonly List<Order> orders = [];

    /// <summary>
    /// Gets the customer orders.
    /// </summary>
    public IReadOnlyCollection<Order> Orders => this.orders.AsReadOnly();

    /// <summary>
    /// Adds an order to the customer.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result AddOrder(Order order)
    {
        if (order == null)
        {
            return Result.Failure("Order cannot be null");
        }

        if (this.orders.Count >= MaxOrdersPerCustomer)
        {
            return Result.Failure("Customer has reached maximum order limit");
        }

        this.orders.Add(order);
        this.RaiseDomainEvent(new OrderAddedDomainEvent(this.Id, order.Id));

        return Result.Success();
    }
}
```

### Entities

**Requirements**:
- Have identity properties (e.g., `Id`)
- Business logic encapsulated in methods
- No public setters; use methods to modify state
- Collection properties as `IReadOnlyCollection<T>`
- No lazy loading
- Enforce invariants
- Methods return `Result<T>` for operations that can fail

### Value Objects

**Requirements**:
- Immutable (no public setters)
- Validated via constructor or `Create()` factory method
- Descriptive names (e.g., `EmailAddress`, not `Email`)
- Return `Result<T>` from creation methods
- Equality based on properties, not identity
- Small and focused on single concept
- No external dependencies

**Example**:
```csharp
namespace MyApp.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address.
/// </summary>
public sealed class EmailAddress : ValueObject
{
    private EmailAddress(string value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the email address value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new email address.
    /// </summary>
    /// <param name="value">The email address string.</param>
    /// <returns>Result containing the email address or validation error.</returns>
    public static Result<EmailAddress> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<EmailAddress>.Failure("Email address cannot be empty");
        }

        if (!value.Contains('@'))
        {
            return Result<EmailAddress>.Failure("Email address must contain @");
        }

        return Result<EmailAddress>.Success(new EmailAddress(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return this.Value;
    }
}
```

### Domain Events

**Requirements**:
- Named in past tense with `DomainEvent` suffix (e.g., `CustomerCreatedDomainEvent`)
- Published via aggregate root methods
- Handlers implement `INotificationHandler<T>`
- Used for domain layer communication only (can be handled in Application layer)
- Contain only relevant data for the event
- Raised only for significant state changes

**Example**:
```csharp
namespace MyApp.Domain.CustomerAggregate.Events;

/// <summary>
/// Domain event raised when a customer is created.
/// </summary>
public sealed record CustomerCreatedDomainEvent(
    CustomerId CustomerId,
    string CustomerName) : DomainEvent;
```

### Smart Enumerations

**Requirements**:
- Derive from `Enumeration` base class
- PascalCase static instances
- Used instead of primitive enums for domain concepts

**Example**:
```csharp
namespace MyApp.Domain.Enumerations;

/// <summary>
/// Enumeration of customer status values.
/// </summary>
public sealed class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Active = new(1, nameof(Active));
    public static readonly CustomerStatus Inactive = new(2, nameof(Inactive));
    public static readonly CustomerStatus Suspended = new(3, nameof(Suspended));

    private CustomerStatus(int value, string name) : base(value, name)
    {
    }
}
```

### Strongly-Typed Entity IDs

**Requirements**:
- Use `TypedEntityId<T>` attribute for code generation
- IDs are immutable
- Used consistently across application
- Used in repository methods, DTOs, API models
- Used in logging and error messages

**Example**:
```csharp
namespace MyApp.Domain.CustomerAggregate;

/// <summary>
/// Strongly-typed identifier for Customer aggregate.
/// </summary>
[TypedEntityId<Guid>]
public readonly partial struct CustomerId;
```

## CQRS Pattern Review

### Commands

**Naming**: `[Entity][Action]Command` (e.g., `CustomerCreateCommand`)

**Location**: `<Module>.Application/Commands/`

**Handler Naming**: `[Entity][Command]Handler` or `[Entity][CommandName]CommandHandler`

**Handler Location**: Same folder as command, either in same file or separate file

**Example**:
```csharp
namespace MyApp.Application.Commands;

using MediatR;

/// <summary>
/// Command to create a new customer.
/// </summary>
public sealed record CustomerCreateCommand(
    string Name,
    string Email) : IRequest<Result<CustomerId>>;

/// <summary>
/// Handler for customer creation command.
/// </summary>
internal sealed class CustomerCreateCommandHandler : IRequestHandler<CustomerCreateCommand, Result<CustomerId>>
{
    private readonly ICustomerRepository repository;

    public CustomerCreateCommandHandler(ICustomerRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<CustomerId>> Handle(
        CustomerCreateCommand request,
        CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result<CustomerId>.Failure(emailResult.Error);
        }

        var customer = Customer.Create(request.Name, emailResult.Value);
        await this.repository.AddAsync(customer, cancellationToken);

        return Result<CustomerId>.Success(customer.Id);
    }
}
```

### Queries

**Naming**: `[Entity][Action]Query` (e.g., `CustomerGetByIdQuery`)

**Location**: `<Module>.Application/Queries/`

**Handler Naming**: `[Entity][Query]Handler` or `[Entity][QueryName]QueryHandler`

**Handler Location**: Same folder as query, either in same file or separate file

### Validation

**Requirements**:
- FluentValidation validators are optional
- Typically implemented as nested `Validator` class when present
- Pipeline behavior attributes (retry, timeout) are optional

**Example**:
```csharp
namespace MyApp.Application.Commands;

using FluentValidation;

public sealed record CustomerCreateCommand(
    string Name,
    string Email) : IRequest<Result<CustomerId>>
{
    internal sealed class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            this.RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}
```

## Repository & Data Access Review

### Application Layer

**🔴 CRITICAL**: Application layer must:
- Only reference Domain layer
- Use repository abstractions (interfaces)
- NEVER reference DbContext directly
- Use specification pattern for complex queries
- Use Commands via `IRequester.SendAsync()` pattern
- Use Queries via `IRequester.SendAsync()` pattern

### Infrastructure Layer

**Requirements**:
- Contains EF Core DbContext
- May contain repository implementations (optional)
- References Application and Domain
- All data access code belongs here

### Performance Checks

**🟡 IMPORTANT**: Watch for:
- N+1 query problems (missing `.Include()`)
- Lazy loading issues
- Missing indexes on frequently queried fields
- Inefficient LINQ queries

**Example of N+1 problem**:
```csharp
// WRONG: N+1 query problem
var customers = await this.context.Customers.ToListAsync();
foreach (var customer in customers)
{
    // This executes a separate query for each customer!
    var orders = customer.Orders.ToList();
}

// ✅ GOOD: Eager loading
var customers = await this.context.Customers
    .Include(c => c.Orders)
    .ToListAsync();
```

## Presentation Layer Review

### Endpoint Organization

**Requirements**:
- Endpoints in `<Module>.Presentation/Web/Endpoints/`
- Classes derive from `EndpointsBase`
- Endpoints are thin adapters only
- No business logic in endpoints
- All business logic via `IRequester.SendAsync()`

### Endpoint Structure

**Requirements**:
- Use `.MapHttpOk()`, `.MapHttpCreated()`, `.MapHttpNoContent()`, `.MapHttpOkAll()` for Result<T> responses
- Use `MapGroup()` for common route prefixes
- Apply `.RequireAuthorization()` where needed
- Include `.WithName()`, `.WithSummary()`, `.WithDescription()` for API docs
- Include OpenAPI metadata: `.Produces<T>()`, `.Accepts<T>()`, `.ProducesProblem()`, `.ProducesResultProblem()`
- Use parameter binding attributes explicitly: `[FromServices]`, `[FromRoute]`, `[FromQuery]`, `[FromBody]`
- Use route constraints: `{id:guid}`
- Follow REST conventions
- Always include `CancellationToken ct` parameter

**Example**:
```csharp
namespace MyApp.Presentation.Web.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Customer management endpoints.
/// </summary>
public class CustomerEndpoints : EndpointsBase
{
    /// <inheritdoc/>
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/customers")
            .WithTags("Customers");

        group.MapPost("", this.CreateCustomerAsync)
            .WithName("CreateCustomer")
            .WithSummary("Creates a new customer")
            .WithDescription("Creates a new customer with the provided details")
            .Produces<CustomerId>(StatusCodes.Status201Created)
            .ProducesResultProblem()
            .Accepts<CustomerCreateCommand>("application/json");

        group.MapGet("{id:guid}", this.GetCustomerByIdAsync)
            .WithName("GetCustomerById")
            .WithSummary("Retrieves a customer by ID")
            .Produces<CustomerResponse>(StatusCodes.Status200OK)
            .ProducesResultProblem();
    }

    private async Task<IResult> CreateCustomerAsync(
        [FromBody] CustomerCreateCommand command,
        [FromServices] IRequester requester,
        CancellationToken ct)
    {
        var result = await requester.SendAsync(command, ct);
        return result.MapHttpCreated(r => $"/api/customers/{r}");
    }

    private async Task<IResult> GetCustomerByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IRequester requester,
        CancellationToken ct)
    {
        var query = new CustomerGetByIdQuery(new CustomerId(id));
        var result = await requester.SendAsync(query, ct);
        return result.MapHttpOk();
    }
}
```

## Error Handling Review

### Result<T> Pattern

**🔴 CRITICAL**: Use `Result<T>` for recoverable failures, not exceptions.

**Requirements**:
- Domain methods return `Result` or `Result<T>`
- Application handlers return `Result<T>`
- Exceptions only for truly exceptional cases
- Validation failures return `Result.Failure()`

**Example**:
```csharp
// WRONG: Using exceptions for flow control
public Customer CreateCustomer(string name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        throw new ArgumentException("Name is required");
    }

    return new Customer(name);
}

// ✅ GOOD: Using Result<T>
public static Result<Customer> Create(string name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Result<Customer>.Failure("Name is required");
    }

    return Result<Customer>.Success(new Customer(name));
}
```

## Mapping Review

### Mapster Configuration

**Requirements**:
- All mapping via Mapster
- Configured in module `MapperRegister` class
- No ad-hoc inline mapping in handlers
- Use registered configurations

**Example**:
```csharp
namespace MyApp.Application;

using Mapster;

/// <summary>
/// Mapster configuration for Customer module.
/// </summary>
internal sealed class MapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerResponse>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Email, src => src.Email.Value);
    }
}
```

## bITdevKit Pattern Review

**Check for**:
- Proper repository behavior chaining (logging, audit, domain events)
- Correct use of `IRequester` and `INotifier` patterns
- Module registration follows `services.AddModule<T>()` pattern
- Startup tasks use devkit infrastructure
- Quartz jobs registered via devkit extensions

## Review Checklist

When reviewing code, systematically check:

### 🔴 Critical Issues
- [ ] No cross-layer dependencies (Domain → Application, etc.)
- [ ] No circular module references
- [ ] Domain layer has zero infrastructure dependencies
- [ ] No DbContext usage in Application or Domain layers
- [ ] Result<T> used for recoverable failures
- [ ] All aggregates properly encapsulated
- [ ] No public setters on aggregates/entities
- [ ] IDisposable objects properly disposed

### 🟡 Important Issues
- [ ] Command/Query naming follows conventions
- [ ] Handlers co-located with commands/queries
- [ ] Value objects are immutable
- [ ] Domain events named in past tense
- [ ] Strongly-typed IDs used consistently
- [ ] Repository abstractions used in Application layer
- [ ] Endpoints are thin adapters only
- [ ] All business logic via IRequester
- [ ] No N+1 query problems
- [ ] Proper async/await usage

### 🟢 Suggestions
- [ ] XML documentation on public APIs
- [ ] Mapster used for all mapping
- [ ] Modern C# features used appropriately
- [ ] Code follows .editorconfig rules
- [ ] Proper OpenAPI documentation on endpoints
- [ ] CancellationToken passed through call chains

## Reference Files

Review against:
- `.editorconfig` - Formatting and style rules
- `AGENTS.md` - Architecture patterns and conventions
- `.github/copilot-instructions.md` - Detailed coding guidelines

## Output Format

Provide feedback using this format:

### [🔴/🟡/🟢] Category

**Location**: `file_path:line_number`

**Issue**: Description of the problem

**Why**: Explanation of impact (architecture, performance, maintainability)

**Fix**: Specific recommendation with code example

**Reference**: Link to documentation or .editorconfig rule

---

## Summary Template

End review with:

## Review Summary

**Issues Found**:
- 🔴 Critical: X
- 🟡 Important: Y
- 🟢 Suggestions: Z

**Top 3 Priorities**:
1. [Most critical issue]
2. [Second priority]
3. [Third priority]

**Overall Assessment**: [Brief quality assessment]

**Architecture Compliance**: ✅/⚠️/❌

## Success Criteria

Code passes review when:
- ✅ No architecture boundary violations
- ✅ DDD patterns correctly applied
- ✅ Domain layer is pure (no external dependencies)
- ✅ Proper Result<T> error handling
- ✅ Repository abstractions used correctly
- ✅ Endpoints follow thin adapter pattern
- ✅ No N+1 query problems
- ✅ Ready for testing
- ✅ .editorconfig rules followed