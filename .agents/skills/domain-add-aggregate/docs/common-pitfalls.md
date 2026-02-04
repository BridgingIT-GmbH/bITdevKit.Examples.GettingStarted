# Common Pitfalls and Anti-Patterns

This document catalogs common mistakes when implementing aggregates and provides corrective guidance with WRONG/CORRECT examples.

**Purpose**: Help developers avoid common errors that violate DDD principles, architecture boundaries, or project conventions  
**Audience**: Developers implementing new aggregates or reviewing existing code

---

## Table of Contents

- [Domain Layer Pitfalls](#domain-layer-pitfalls)
- [Application Layer Pitfalls](#application-layer-pitfalls)
- [Infrastructure Layer Pitfalls](#infrastructure-layer-pitfalls)
- [Presentation Layer Pitfalls](#presentation-layer-pitfalls)
- [Cross-Cutting Pitfalls](#cross-cutting-pitfalls)
- [References](#references)

---

## Domain Layer Pitfalls

### Pitfall 1: Anemic Domain Model

**Problem**: Domain entities are just data containers with no behavior, all logic in handlers.

**WRONG**:
```csharp
// Domain: Just properties, no behavior
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public EmailAddress Email { get; set; }
    public CustomerStatus Status { get; set; }
}

// Application: All business logic in handler
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        // Business rules in handler (WRONG!)
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure("Email is required");
        
        if (!IsValidEmail(request.Email))
            return Result.Failure("Email format invalid");
        
        customer.FirstName = request.FirstName;
        customer.Email = EmailAddress.Create(request.Email).Value;
        
        await repository.UpdateAsync(customer);
        return Result.Success(mapper.Map<CustomerModel>(customer));
    }
}
```

**CORRECT**:
```csharp
// Domain: Rich model with behavior and business rules
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; private set; } // Private setters
    public string LastName { get; private set; }
    public EmailAddress Email { get; private set; }
    public CustomerStatus Status { get; private set; }
    
    // Factory method with validation
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
            });
    }
    
    // Change method with business rules
    public Result<Customer> ChangeEmail(string email)
    {
        return this.Change()
            .Ensure(() => email.IsNotEmpty(), "Email is required")
            .Set(() => EmailAddress.Create(email), v => this.Email = v)
            .Register(() => new CustomerUpdatedDomainEvent(this))
            .Apply();
    }
}

// Application: Handler orchestrates only, no business logic
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        // Use domain change method (business rules enforced)
        var result = customer.ChangeEmail(request.Email);
        if (result.IsFailure)
            return Result.Failure<CustomerModel>(result.Messages);
        
        await repository.UpdateAsync(customer);
        return Result.Success(mapper.Map<CustomerModel>(customer));
    }
}
```

**Why This Matters**: Domain model should encapsulate business rules. Scattering rules across handlers makes them hard to test, maintain, and reuse.

---

### Pitfall 2: Primitive Obsession

**Problem**: Using primitive types (string, int) instead of domain-specific value objects.

**WRONG**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string Email { get; set; } // Primitive string, no validation
    public string PhoneNumber { get; set; }
    public decimal CreditLimit { get; set; }
}

// Validation scattered across handlers
public class CustomerCreateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        if (!IsValidEmail(request.Email)) // Duplicate validation logic
            return Result.Failure("Invalid email");
        
        var customer = new Customer { Email = request.Email };
        // ...
    }
}
```

**CORRECT**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public EmailAddress Email { get; private set; } // Value object with validation
    public PhoneNumber PhoneNumber { get; private set; }
    public Money CreditLimit { get; private set; }
}

public class EmailAddress : ValueObject
{
    public string Value { get; private set; }
    
    public static Result<EmailAddress> Create(string value)
    {
        return Result.Success()
            .Ensure(value.IsNotEmpty(), "Email is required")
            .Ensure(value.Contains("@"), "Email must contain @")
            .Tap(() => new EmailAddress { Value = value });
    }
}

// Handler uses value object (validation happens once in value object)
public class CustomerCreateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        return Customer.Create(
            request.FirstName,
            request.LastName,
            request.Email, // Factory converts string -> EmailAddress
            CustomerStatus.Active);
    }
}
```

**Why This Matters**: Value objects enforce validation in one place, prevent invalid states, and make intent explicit.

---

### Pitfall 3: Public Setters on Aggregates

**Problem**: Allowing external code to modify aggregate state directly, bypassing business rules.

**WRONG**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; set; } // Public setter
    public CustomerStatus Status { get; set; }
}

// Handler bypasses encapsulation
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        customer.FirstName = request.FirstName; // Direct state mutation
        customer.Status = CustomerStatus.Retired; // No validation
        
        await repository.UpdateAsync(customer);
    }
}
```

**CORRECT**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; private set; } // Private setter
    public CustomerStatus Status { get; private set; }
    
    public Result<Customer> ChangeName(string firstName, string lastName)
    {
        return this.Change()
            .Ensure(() => firstName.IsNotEmpty(), "First name required")
            .Set(() => firstName, v => this.FirstName = v)
            .Set(() => lastName, v => this.LastName = v)
            .Register(() => new CustomerUpdatedDomainEvent(this))
            .Apply();
    }
    
    public Result<Customer> Retire()
    {
        return this.Change()
            .Ensure(() => this.Status != CustomerStatus.Retired, "Already retired")
            .Set(() => CustomerStatus.Retired, v => this.Status = v)
            .Register(() => new CustomerRetiredDomainEvent(this))
            .Apply();
    }
}

// Handler uses change methods
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        var result = customer.ChangeName(request.FirstName, request.LastName);
        if (result.IsFailure)
            return Result.Failure<CustomerModel>(result.Messages);
        
        await repository.UpdateAsync(customer);
    }
}
```

**Why This Matters**: Encapsulation ensures all state changes go through validated change methods, maintaining invariants.

---

### Pitfall 4: Not Using Result Pattern

**Problem**: Throwing exceptions for business rule violations instead of returning Result<T>.

**WRONG**:
```csharp
public static Customer Create(string firstName, string lastName, string email)
{
    if (string.IsNullOrWhiteSpace(firstName))
        throw new ArgumentException("First name is required"); // Exception for validation
    
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required");
    
    return new Customer
    {
        FirstName = firstName,
        LastName = lastName,
        Email = EmailAddress.Create(email) // May throw
    };
}
```

**CORRECT**:
```csharp
public static Result<Customer> Create(string firstName, string lastName, string email, CustomerStatus status)
{
    return Result.Success()
        .Ensure(firstName.IsNotEmpty(), "First name is required") // Result, not exception
        .Ensure(lastName.IsNotEmpty(), "Last name is required")
        .Bind(() => EmailAddress.Create(email)) // Chain Results
        .Tap(emailAddress => new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = emailAddress,
            Status = status
        });
}
```

**Why This Matters**: Exceptions are for exceptional cases (infrastructure failures). Business rule violations are expected and should return Failure Result.

---

### Pitfall 5: Leaking Domain Events

**Problem**: Exposing DomainEvents collection publicly or not clearing after publishing.

**WRONG**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public List<DomainEventBase> DomainEvents { get; set; } = new(); // Public, mutable
    
    public static Result<Customer> Create(...)
    {
        var customer = new Customer { /* ... */ };
        customer.DomainEvents.Add(new CustomerCreatedDomainEvent(customer)); // Direct mutation
        return Result.Success(customer);
    }
}
```

**CORRECT**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    // DomainEvents collection managed by base class, not exposed
    
    public static Result<Customer> Create(string firstName, string lastName, string email, CustomerStatus status)
    {
        return Result.Success()
            .Ensure(firstName.IsNotEmpty(), "First name required")
            .Ensure(lastName.IsNotEmpty(), "Last name required")
            .Bind(() => EmailAddress.Create(email))
            .Tap(emailAddress =>
            {
                var customer = new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = emailAddress,
                    Status = status
                };
                customer.Register(new CustomerCreatedDomainEvent(customer)); // Use Register method
                return customer;
            });
    }
}
```

**Why This Matters**: Domain events are internal implementation detail. Use base class methods (`.Register()`) to add events safely.

---

## Application Layer Pitfalls

### Pitfall 6: Business Logic in Handlers

**Problem**: Implementing business rules in handlers instead of delegating to domain.

**WRONG**:
```csharp
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        // Business logic in handler (WRONG!)
        if (request.FirstName.Length < 2)
            return Result.Failure("First name too short");
        
        if (customer.Status == CustomerStatus.Retired && request.Status == "Active")
            return Result.Failure("Cannot reactivate retired customer");
        
        customer.FirstName = request.FirstName;
        customer.Status = CustomerStatus.FromName(request.Status);
        
        await repository.UpdateAsync(customer);
        return Result.Success(mapper.Map<CustomerModel>(customer));
    }
}
```

**CORRECT**:
```csharp
public class CustomerUpdateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerUpdateCommand request)
    {
        var customer = await repository.FindOneAsync(request.Id);
        
        // Delegate to domain change methods (business rules enforced there)
        var result = customer
            .ChangeName(request.FirstName, request.LastName)
            .Bind(() => customer.ChangeStatus(CustomerStatus.FromName(request.Status)));
        
        if (result.IsFailure)
            return Result.Failure<CustomerModel>(result.Messages);
        
        await repository.UpdateAsync(customer);
        return Result.Success(mapper.Map<CustomerModel>(customer));
    }
}
```

**Why This Matters**: Handlers orchestrate, domain enforces. Keeps rules testable and reusable.

---

### Pitfall 7: Direct DbContext Access

**Problem**: Injecting DbContext into handlers instead of using repository abstraction.

**WRONG**:
```csharp
public class CustomerCreateCommandHandler
{
    private readonly CoreModuleDbContext dbContext; // Direct DbContext
    
    public CustomerCreateCommandHandler(CoreModuleDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        var customer = Customer.Create(...).Value;
        
        dbContext.Customers.Add(customer); // Direct DbContext usage
        await dbContext.SaveChangesAsync();
        
        return Result.Success(mapper.Map<CustomerModel>(customer));
    }
}
```

**CORRECT**:
```csharp
public class CustomerCreateCommandHandler
{
    private readonly IGenericRepository<Customer> repository; // Repository abstraction
    private readonly IMapper mapper;
    
    public CustomerCreateCommandHandler(
        IGenericRepository<Customer> repository,
        IMapper mapper)
    {
        this.repository = repository;
        this.mapper = mapper;
    }
    
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        var customerResult = Customer.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            CustomerStatus.FromName(request.Status));
        
        if (customerResult.IsFailure)
            return Result.Failure<CustomerModel>(customerResult.Messages);
        
        await repository.InsertAsync(customerResult.Value, cancellationToken);
        
        return Result.Success(mapper.Map<CustomerModel>(customerResult.Value));
    }
}
```

**Why This Matters**: Repository abstraction allows mocking in tests, enables behaviors (logging, audit), and respects layer boundaries.

---

### Pitfall 8: Not Handling Result<T> Failures

**Problem**: Accessing `.Value` without checking `.IsSuccess`, causing runtime exceptions.

**WRONG**:
```csharp
public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
{
    var customer = Customer.Create(
        request.FirstName,
        request.LastName,
        request.Email,
        CustomerStatus.Active).Value; // May be null if validation failed!
    
    await repository.InsertAsync(customer); // NullReferenceException if validation failed
    
    return Result.Success(mapper.Map<CustomerModel>(customer));
}
```

**CORRECT**:
```csharp
public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
{
    var customerResult = Customer.Create(
        request.FirstName,
        request.LastName,
        request.Email,
        CustomerStatus.Active);
    
    if (customerResult.IsFailure) // Check before accessing .Value
        return Result.Failure<CustomerModel>(customerResult.Messages);
    
    await repository.InsertAsync(customerResult.Value, cancellationToken);
    
    return Result.Success(mapper.Map<CustomerModel>(customerResult.Value));
}

// Or use Match
public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
{
    return await Customer.Create(
            request.FirstName,
            request.LastName,
            request.Email,
            CustomerStatus.Active)
        .Bind(async customer =>
        {
            await repository.InsertAsync(customer, cancellationToken);
            return Result.Success(mapper.Map<CustomerModel>(customer));
        });
}
```

**Why This Matters**: Result<T> forces explicit error handling, preventing silent failures.

---

### Pitfall 9: Mapping in Domain Layer

**Problem**: Adding mapping logic to domain entities.

**WRONG**:
```csharp
// Domain layer
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public CustomerModel ToModel() // Mapping in domain (WRONG!)
    {
        return new CustomerModel
        {
            Id = this.Id.Value,
            FirstName = this.FirstName,
            Email = this.Email.Value
        };
    }
}
```

**CORRECT**:
```csharp
// Domain layer: Pure domain logic, no mapping
public class Customer : AuditableAggregateRoot<CustomerId>
{
    public string FirstName { get; private set; }
    public EmailAddress Email { get; private set; }
    // No ToModel() method
}

// Presentation layer: Mapping in MapperRegister
public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerModel>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Email, src => src.Email.Value);
    }
}
```

**Why This Matters**: Domain should not know about DTOs or presentation concerns. Mapping is a presentation responsibility.

---

## Infrastructure Layer Pitfalls

### Pitfall 10: Not Converting TypedEntityId

**Problem**: Not configuring EF Core to convert TypedEntityId to Guid, causing persistence errors.

**WRONG**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(e => e.Id); // CustomerId not converted to Guid
        // EF Core tries to persist CustomerId as complex type (error!)
    }
}
```

**CORRECT**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value, // CustomerId -> Guid
                value => CustomerId.Create(value)); // Guid -> CustomerId
    }
}
```

**Why This Matters**: Database stores primitives (Guid), not custom types. Conversion is essential.

---

### Pitfall 11: Not Ignoring Domain Events

**Problem**: EF Core tries to persist DomainEvents collection, causing errors.

**WRONG**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", CoreModuleConstants.Schema);
        builder.HasKey(e => e.Id);
        // DomainEvents not ignored, EF Core tries to map it
    }
}
```

**CORRECT**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", CoreModuleConstants.Schema);
        builder.HasKey(e => e.Id);
        
        builder.Ignore(e => e.DomainEvents); // Ignore domain events
    }
}
```

**Why This Matters**: Domain events are transient, not persisted. Must be ignored in EF Core mapping.

---

### Pitfall 12: Missing Concurrency Token

**Problem**: Not configuring ConcurrencyVersion for optimistic concurrency, risking lost updates.

**WRONG**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(e => e.ConcurrencyVersion); // Not marked as concurrency token
    }
}
```

**CORRECT**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(e => e.ConcurrencyVersion)
            .IsConcurrencyToken(); // Mark as concurrency token
    }
}
```

**Why This Matters**: Optimistic concurrency prevents lost updates when multiple users edit same entity.

---

## Presentation Layer Pitfalls

### Pitfall 13: Business Logic in Endpoints

**Problem**: Implementing validation or business rules in endpoint methods.

**WRONG**:
```csharp
private async Task<IResult> Create(CustomerCreateCommand command)
{
    // Validation in endpoint (WRONG!)
    if (string.IsNullOrWhiteSpace(command.FirstName))
        return TypedResults.BadRequest("First name is required");
    
    if (command.Email?.Contains("@") != true)
        return TypedResults.BadRequest("Invalid email format");
    
    var result = await requester.SendAsync(command);
    
    return result.Match(
        success => TypedResults.Created($"/api/core/customers/{success.Id}", success),
        failure => TypedResults.BadRequest(failure.ToString()));
}
```

**CORRECT**:
```csharp
private async Task<IResult> Create(
    CustomerCreateCommand command,
    CancellationToken cancellationToken)
{
    // No validation here, delegate to Application layer
    var result = await requester.SendAsync(command, cancellationToken);
    
    // Only handle Result mapping to HTTP responses
    return result.Match(
        success => TypedResults.Created($"/api/core/customers/{success.Id}", success),
        failure => TypedResults.BadRequest(failure.ToString()));
}
```

**Why This Matters**: Endpoints translate HTTP ↔ commands/queries, no business logic. Validation happens in validators (Application layer).

---

### Pitfall 14: Exposing Result<T> in HTTP Response

**Problem**: Returning Result<T> object directly instead of unwrapping via Match.

**WRONG**:
```csharp
private async Task<IResult> GetById(Guid id)
{
    var query = new CustomerFindOneQuery { Id = id };
    var result = await requester.SendAsync(query);
    
    return TypedResults.Ok(result); // Result<CustomerModel> exposed (WRONG!)
}
```

**CORRECT**:
```csharp
private async Task<IResult> GetById(Guid id, CancellationToken cancellationToken)
{
    var query = new CustomerFindOneQuery { Id = id };
    var result = await requester.SendAsync(query, cancellationToken);
    
    return result.Match(
        success => success is not null
            ? TypedResults.Ok(success) // CustomerModel exposed, not Result<CustomerModel>
            : TypedResults.NotFound(),
        failure => TypedResults.Problem(failure.ToString()));
}
```

**Why This Matters**: API consumers shouldn't see internal Result<T> structure. Return DTO directly.

---

### Pitfall 15: Wrong HTTP Status Codes

**Problem**: Returning incorrect status codes for operations.

**WRONG**:
```csharp
private async Task<IResult> Create(CustomerCreateCommand command)
{
    var result = await requester.SendAsync(command);
    
    return result.Match(
        success => TypedResults.Ok(success), // Should be 201 Created, not 200 OK
        failure => TypedResults.Ok(failure)); // Should be 400 Bad Request, not 200 OK
}

private async Task<IResult> Delete(Guid id)
{
    var command = new CustomerDeleteCommand { Id = id };
    var result = await requester.SendAsync(command);
    
    return result.Match(
        success => TypedResults.Ok(), // Should be 204 No Content
        failure => TypedResults.NotFound());
}
```

**CORRECT**:
```csharp
private async Task<IResult> Create(CustomerCreateCommand command)
{
    var result = await requester.SendAsync(command);
    
    return result.Match(
        success => TypedResults.Created($"/api/core/customers/{success.Id}", success), // 201 Created
        failure => TypedResults.BadRequest(failure.ToString())); // 400 Bad Request
}

private async Task<IResult> Delete(Guid id)
{
    var command = new CustomerDeleteCommand { Id = id };
    var result = await requester.SendAsync(command);
    
    return result.Match(
        success => TypedResults.NoContent(), // 204 No Content
        failure => TypedResults.NotFound()); // 404 Not Found
}
```

**Why This Matters**: RESTful conventions: 200 OK (GET/PUT success), 201 Created (POST success), 204 No Content (DELETE success), 400 Bad Request (validation error), 404 Not Found (entity missing).

---

## Cross-Cutting Pitfalls

### Pitfall 16: Breaking Layer Dependencies

**Problem**: Inner layers referencing outer layers (e.g., Domain referencing Application).

**WRONG**:
```csharp
// Domain layer
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain
{
    using BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Application; // WRONG!
    
    public class Customer : AuditableAggregateRoot<CustomerId>
    {
        public CustomerModel ToModel() // Domain knows about Application DTO
        {
            return new CustomerModel { /* ... */ };
        }
    }
}
```

**CORRECT**:
```csharp
// Domain layer: No references to outer layers
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain
{
    // Only references: System, bITdevKit.Domain
    
    public class Customer : AuditableAggregateRoot<CustomerId>
    {
        // Pure domain logic, no knowledge of Application/Infrastructure/Presentation
    }
}
```

**Why This Matters**: Dependency Inversion Principle. Dependencies flow inward toward domain.

---

### Pitfall 17: Not Using Factory Methods

**Problem**: Using `new` keyword to instantiate aggregates or value objects, bypassing validation.

**WRONG**:
```csharp
public class CustomerCreateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        var customer = new Customer // Using constructor directly (WRONG!)
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = new EmailAddress { Value = request.Email } // Bypassing validation
        };
        
        await repository.InsertAsync(customer);
    }
}
```

**CORRECT**:
```csharp
public class CustomerCreateCommandHandler
{
    public async Task<Result<CustomerModel>> Handle(CustomerCreateCommand request)
    {
        var customerResult = Customer.Create( // Using factory method (CORRECT!)
            request.FirstName,
            request.LastName,
            request.Email,
            CustomerStatus.FromName(request.Status));
        
        if (customerResult.IsFailure)
            return Result.Failure<CustomerModel>(customerResult.Messages);
        
        await repository.InsertAsync(customerResult.Value, cancellationToken);
        return Result.Success(mapper.Map<CustomerModel>(customerResult.Value));
    }
}
```

**Why This Matters**: Factory methods enforce validation, maintain invariants, and return Result<T> for error handling.

---

### Pitfall 18: Ignoring Cancellation Tokens

**Problem**: Not passing CancellationToken to async methods.

**WRONG**:
```csharp
public async Task<Result<CustomerModel>> Handle(
    CustomerFindOneQuery request,
    CancellationToken cancellationToken)
{
    var customer = await repository.FindOneAsync(request.Id); // CancellationToken not passed
    
    return Result.Success(mapper.Map<CustomerModel>(customer));
}
```

**CORRECT**:
```csharp
public async Task<Result<CustomerModel>> Handle(
    CustomerFindOneQuery request,
    CancellationToken cancellationToken)
{
    var customer = await repository.FindOneAsync(
        CustomerId.Create(request.Id),
        cancellationToken); // Pass CancellationToken
    
    if (customer is null)
        return Result.Success<CustomerModel>(null);
    
    return Result.Success(mapper.Map<CustomerModel>(customer));
}
```

**Why This Matters**: Cancellation tokens allow graceful shutdown, improve responsiveness, prevent resource waste.

---

### Pitfall 19: Not Testing Result Pattern

**Problem**: Tests not checking Result<T> success/failure.

**WRONG**:
```csharp
[Fact]
public void Create_ValidInputs_ReturnsCustomer()
{
    var result = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active);
    
    result.Value.ShouldNotBeNull(); // May throw if result.IsFailure
    result.Value.FirstName.ShouldBe("John");
}
```

**CORRECT**:
```csharp
[Fact]
public void Create_ValidInputs_ReturnsSuccessResult()
{
    var result = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active);
    
    result.ShouldBeSuccess(); // Check success first
    result.Value.ShouldNotBeNull();
    result.Value.FirstName.ShouldBe("John");
}

[Fact]
public void Create_EmptyEmail_ReturnsFailureResult()
{
    var result = Customer.Create("John", "Doe", string.Empty, CustomerStatus.Active);
    
    result.ShouldBeFailure(); // Check failure
    result.Messages.ShouldContain(m => m.Contains("email", StringComparison.OrdinalIgnoreCase));
}
```

**Why This Matters**: Result pattern requires explicit success/failure checks. Always test both paths.

---

### Pitfall 20: Inconsistent Naming

**Problem**: Not following project naming conventions.

**WRONG**:
```csharp
// Command name inconsistent
public class CreateCustomerCommand : RequestBase<Result<CustomerModel>> { }

// Handler name inconsistent
public class CreateCustomerHandler : RequestHandlerBase<CreateCustomerCommand, Result<CustomerModel>> { }

// Validator name inconsistent
public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand> { }
```

**CORRECT**:
```csharp
// Follow pattern: [Entity][Action]Command
public class CustomerCreateCommand : RequestBase<Result<CustomerModel>> { }

// Follow pattern: [Entity][Action]CommandHandler
public class CustomerCreateCommandHandler : RequestHandlerBase<CustomerCreateCommand, Result<CustomerModel>> { }

// Follow pattern: [Entity][Action]CommandValidator
public class CustomerCreateCommandValidator : AbstractValidator<CustomerCreateCommand> { }
```

**Why This Matters**: Consistent naming improves discoverability, maintainability, and code navigation.

---

## Summary of Anti-Patterns

| Anti-Pattern | Impact | Solution |
|-------------|--------|----------|
| Anemic Domain Model | Business logic scattered, hard to test | Rich domain model with behavior |
| Primitive Obsession | Duplicate validation, no type safety | Use value objects |
| Public Setters | Broken encapsulation, invalid states | Private setters + change methods |
| Throwing Exceptions for Business Errors | Poor error handling, stack traces | Use Result<T> pattern |
| Business Logic in Handlers | Tight coupling, hard to reuse | Delegate to domain |
| Direct DbContext Access | Can't mock, no behaviors | Use IGenericRepository<T> |
| Not Handling Result Failures | Runtime exceptions (NullReferenceException) | Check `.IsSuccess` or use `.Match()` |
| Mapping in Domain | Violates layer boundaries | Mapping in Presentation layer |
| Not Converting TypedEntityId | EF Core persistence errors | Use `.HasConversion()` |
| Not Ignoring Domain Events | EF Core tries to persist events | Use `.Ignore(e => e.DomainEvents)` |
| Missing Concurrency Token | Lost updates | Use `.IsConcurrencyToken()` |
| Business Logic in Endpoints | Violates SRP, hard to test | Endpoints orchestrate only |
| Exposing Result<T> in API | Leaks internal structure | Unwrap via `.Match()` |
| Wrong HTTP Status Codes | Violates REST conventions | 200 OK, 201 Created, 204 No Content, 400 Bad Request, 404 Not Found |
| Breaking Layer Dependencies | Circular dependencies, coupling | Follow dependency rules (inward only) |
| Not Using Factory Methods | Bypasses validation | Always use `.Create()` |
| Ignoring Cancellation Tokens | Resource waste, poor responsiveness | Pass CancellationToken everywhere |
| Not Testing Result Pattern | False positives in tests | Test `.IsSuccess` and `.IsFailure` |
| Inconsistent Naming | Hard to find code, confusion | Follow naming conventions |

---

## References

- **Customer Aggregate Example**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/Customer.cs`
- **ADR-0002**: [Result Pattern for Error Handling](../../docs/ADR/0002-result-pattern-error-handling.md)
- **ADR-0012**: [Domain Logic Encapsulation in Domain Layer](../../docs/ADR/0012-domain-logic-in-domain-layer.md)
- **Architecture Overview**: `.github/skills/domain-add-aggregate/docs/architecture-overview.md`
- **Naming Conventions**: `.github/skills/domain-add-aggregate/docs/naming-conventions.md`

---

**Next Document**: devkit-references.md
