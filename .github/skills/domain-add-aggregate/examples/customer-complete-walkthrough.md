# Customer Aggregate: Complete Walkthrough

This document provides a comprehensive analysis of the `Customer` aggregate implementation, demonstrating all patterns, design decisions, and implementation details used in the bITdevKit GettingStarted example.

## Overview

**Location**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/Customer.cs`

**Purpose**: Demonstrates a complete DDD aggregate root implementation following Clean Architecture, bITdevKit patterns, and domain-driven design principles.

**Key Characteristics**:
- Aggregate root with typed ID (`CustomerId`)
- Value objects (`EmailAddress`, `CustomerNumber`)
- Enumeration (`CustomerStatus`)
- Owned child entity collection (`Address`)
- Domain events (Created, Updated, Deleted)
- Business rules and invariants
- Factory pattern for creation
- Change methods for updates
- Result pattern for error handling

## File Structure

```
Customer.cs (276 lines)
├── Class Definition (lines 17-23)
├── Properties (lines 25-46)
├── Constructors (lines 48-67)
├── Factory Method: Create (lines 69-86)
├── Change Methods (lines 88-174)
│   ├── ChangeName (lines 88-99)
│   ├── ChangeEmail (lines 101-112)
│   ├── ChangeBirthDate (lines 114-125)
│   ├── ChangeStatus (lines 127-138)
│   └── ChangeNumber (lines 140-151)
└── Address Management Methods (lines 175-275)
    ├── AddAddress (lines 175-210)
    ├── RemoveAddress (lines 212-233)
    ├── ChangeAddress (lines 235-269)
    └── SetPrimaryAddress (lines 271-275)
```

## 1. Class Definition and Inheritance

```csharp
// Lines 17-23
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>
{
    // Implementation...
}
```

**Patterns Demonstrated**:
1. **TypedEntityId Attribute**: Source generator creates strongly-typed `CustomerId` wrapper around `Guid`
2. **AuditableAggregateRoot<TId>**: bITdevKit base class provides:
   - `Id` property (typed as `CustomerId`)
   - `DomainEvents` collection
   - `ConcurrencyVersion` (optimistic concurrency)
   - `AuditState` (CreatedDate, UpdatedDate, CreatedBy, UpdatedBy)

**Why This Matters**:
- Type safety: Cannot accidentally pass `ProductId` where `CustomerId` expected
- Audit trail: Automatic tracking of creation/modification
- Concurrency: Prevents lost updates in concurrent scenarios
- Domain events: Enables reactive side effects

## 2. Properties

```csharp
// Lines 25-46
public string FirstName { get; private set; }
public string LastName { get; private set; }
public CustomerNumber Number { get; private set; }
public DateOnly? DateOfBirth { get; private set; }
public EmailAddress Email { get; private set; }
public CustomerStatus Status { get; private set; }
private readonly List<Address> addresses = [];
public IReadOnlyCollection<Address> Addresses => this.addresses.AsReadOnly();
```

**Patterns Demonstrated**:
1. **Private Setters**: Properties can only be modified via change methods (encapsulation)
2. **Value Objects**: `EmailAddress`, `CustomerNumber` (not primitive strings)
3. **Enumeration**: `CustomerStatus` (not enum or magic strings)
4. **Backing Field + ReadOnly Property**: Prevents external modification of collection
5. **Nullable DateOnly**: Optional birth date (C# 10+)

**Why This Matters**:
- Encapsulation: Cannot bypass validation by setting properties directly
- Domain invariants: Value objects enforce rules at creation
- Immutability: External code cannot add/remove addresses directly
- Type safety: Compiler prevents invalid status values

## 3. Constructors

```csharp
// Lines 48-51: EF Core constructor (private, parameterless)
private Customer()
{
}

// Lines 53-67: Domain constructor (private, for factory)
private Customer(string firstName, string lastName, EmailAddress email, CustomerNumber number)
{
    this.FirstName = firstName;
    this.LastName = lastName;
    this.Email = email;
    this.Number = number;
    this.Status = CustomerStatus.Lead; // Default status
}
```

**Patterns Demonstrated**:
1. **Private Constructors**: Forces use of factory method (`Create`)
2. **Parameterless Constructor**: Required by EF Core for entity reconstruction
3. **Parameterized Constructor**: Used by factory, enforces required properties
4. **Default Values**: Status defaults to `Lead` for new customers

**Why This Matters**:
- Factory pattern: Creation logic centralized in one place
- ORM compatibility: EF Core needs parameterless constructor
- Required properties: Compiler enforces passing firstName, lastName, email, number
- Business rules: Default status ensures valid initial state

## 4. Factory Method: Create

```csharp
// Lines 69-86
public static Result<Customer> Create(
    string firstName,
    string lastName,
    string email,
    string number)
{
    return Result<Customer>
        .Bind(() => EmailAddress.Create(email))
        .Bind(emailAddress => CustomerNumber.Create(number)
            .Map(customerNumber => (emailAddress, customerNumber)))
        .Ensure(
            (tuple) => RuleSet.IsNotEmpty(firstName),
            new ValidationError("First name cannot be empty", "FirstName"))
        .Ensure(
            (tuple) => RuleSet.IsNotEmpty(lastName),
            new ValidationError("Last name cannot be empty", "LastName"))
        .Map(tuple => new Customer(firstName, lastName, tuple.emailAddress, tuple.customerNumber))
        .Tap(customer => customer.DomainEvents.Register(new CustomerCreatedDomainEvent(customer)));
}
```

**Patterns Demonstrated**:
1. **Result Pattern**: Returns `Result<Customer>` instead of throwing exceptions
2. **Railway-Oriented Programming**: `.Bind()`, `.Ensure()`, `.Map()`, `.Tap()` chaining
3. **Value Object Creation**: `EmailAddress.Create()`, `CustomerNumber.Create()` with validation
4. **Tuple Unpacking**: Carrying multiple values through pipeline
5. **Domain Event Registration**: `CustomerCreatedDomainEvent` registered before returning
6. **Guard Clauses**: `RuleSet.IsNotEmpty()` validates required fields

**Why This Matters**:
- Fail fast: Invalid email/number detected immediately
- No exceptions: Errors are values (Result pattern)
- Composition: Value object validation + aggregate validation combined
- Side effects: Domain event triggers reactive behaviors

**Flow Diagram**:
```
email string → EmailAddress.Create()
                ↓ (Result<EmailAddress>)
              Bind
                ↓
number string → CustomerNumber.Create()
                ↓ (Result<CustomerNumber>)
              Map to tuple
                ↓
              Ensure firstName not empty
                ↓
              Ensure lastName not empty
                ↓
              Map to new Customer(...)
                ↓
              Tap to register CustomerCreatedDomainEvent
                ↓
              Result<Customer>
```

## 5. Change Methods Pattern

All change methods follow the same pattern. Let's analyze `ChangeName`:

```csharp
// Lines 88-99
public Result<Customer> ChangeName(string firstName, string lastName)
{
    return this.Change()
        .Ensure(() => RuleSet.IsNotEmpty(firstName),
            new ValidationError("First name cannot be empty", "FirstName"))
        .Ensure(() => RuleSet.IsNotEmpty(lastName),
            new ValidationError("Last name cannot be empty", "LastName"))
        .Ensure(() => RuleSet.NotEqual(lastName, "notallowed"),
            new ValidationError("Last name contains forbidden value", "LastName"))
        .Set(() =>
        {
            this.FirstName = firstName;
            this.LastName = lastName;
        })
        .Register(new CustomerUpdatedDomainEvent(this))
        .Apply();
}
```

**Patterns Demonstrated**:
1. **Change Builder Pattern**: `this.Change()` starts a fluent change pipeline
2. **Validation Before Mutation**: `.Ensure()` checks rules before changing state
3. **Mutation**: `.Set()` applies changes to properties
4. **Domain Event**: `.Register()` records update event
5. **Finalization**: `.Apply()` completes the change

**Why This Matters**:
- Atomicity: All validations pass or no changes made
- Auditability: Domain events capture every change
- Consistency: Cannot have partial state changes
- Testability: Easy to test validation without database

**Change Pipeline Stages**:
```
1. this.Change()          → Start change tracking
2. .Ensure(rule)          → Validate invariants (can chain multiple)
3. .Set(() => {...})      → Apply state changes
4. .Register(event)       → Record domain event
5. .Apply()               → Finalize (returns Result<Customer>)
```

## 6. Address Management (Child Collection)

The Customer aggregate manages a collection of `Address` entities. Let's analyze `AddAddress`:

```csharp
// Lines 175-210
public Result<Customer> AddAddress(
    string name,
    string line1,
    string line2,
    string postalCode,
    string city,
    string country,
    bool isPrimary = false)
{
    return this.Change()
        // Validate: Only one primary address allowed
        .Ensure(() => !isPrimary || !this.addresses.Any(a => a.IsPrimary),
            new ValidationError("Only one primary address is allowed", "Address"))
        // Create Address entity (returns Result<Address>)
        .Bind(() => Address.Create(name, line1, line2, postalCode, city, country, isPrimary))
        // Add to collection and register event
        .Tap(address =>
        {
            this.addresses.Add(address);
            this.DomainEvents.Register(new CustomerUpdatedDomainEvent(this));
        })
        // Return Customer (not Address)
        .Map(_ => this)
        .Apply();
}
```

**Patterns Demonstrated**:
1. **Aggregate Boundary**: Customer controls Address lifecycle
2. **Child Entity Factory**: `Address.Create()` validates child entity
3. **Business Rule**: Only one primary address allowed
4. **Collection Encapsulation**: External code cannot access `addresses` list directly
5. **Bind for Child Creation**: `.Bind()` propagates child creation errors
6. **Map to Parent**: Returns `Result<Customer>`, not `Result<Address>`

**Why This Matters**:
- Consistency: Customer aggregate ensures collection invariants
- Encapsulation: Cannot add invalid addresses
- Atomicity: Address creation + adding to collection happens together
- Event sourcing: Update event captures collection changes

## 7. Value Objects

### EmailAddress (src/Modules/CoreModule/CoreModule.Domain/Model/EmailAddress.cs)

```csharp
public class EmailAddress : ValueObject
{
    public string Value { get; private set; }

    private EmailAddress(string value) => this.Value = value;

    public static Result<EmailAddress> Create(string value)
    {
        return Result<EmailAddress>
            .Ensure(() => !string.IsNullOrWhiteSpace(value),
                new ValidationError("Email address cannot be empty", "Email"))
            .Ensure(() => value?.Contains('@') == true,
                new ValidationError("Email address must contain @", "Email"))
            .Map(() => new EmailAddress(value.ToLowerInvariant()));
    }

    public static implicit operator string(EmailAddress email) => email?.Value;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

**Patterns Demonstrated**:
1. **ValueObject Base Class**: Equality based on `GetAtomicValues()`
2. **Private Constructor + Factory**: Forces validation at creation
3. **Result Pattern**: Returns validation errors, not exceptions
4. **Immutability**: No setters, value set once in constructor
5. **Implicit Operator**: Can use as string in many contexts
6. **Normalization**: `ToLowerInvariant()` for consistency

**Why This Matters**:
- Type safety: Cannot pass any string as email
- Validation: Invalid emails cannot exist
- Equality: Two EmailAddress with same value are equal
- Ubiquitous language: "EmailAddress" is a domain concept

## 8. Enumerations

### CustomerStatus (src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/CustomerStatus.cs)

```csharp
public class CustomerStatus : Enumeration
{
    public static readonly CustomerStatus Lead = new(1, "Lead", true, "Potential customer");
    public static readonly CustomerStatus Active = new(2, "Active", true, "Active customer");
    public static readonly CustomerStatus Retired = new(3, "Retired", false, "Inactive customer");

    private CustomerStatus(int id, string value, bool enabled, string description)
        : base(id, value)
    {
        this.Enabled = enabled;
        this.Description = description;
    }

    public bool Enabled { get; }
    public string Description { get; }
}
```

**Patterns Demonstrated**:
1. **Enumeration Class**: Rich enum with properties and methods
2. **Static Readonly Instances**: Compile-time constants like enum
3. **Additional Properties**: `Enabled`, `Description` beyond name/id
4. **Private Constructor**: Prevents external instantiation
5. **Base Class**: `Enumeration` provides `GetAll()`, `FromId()`, equality

**Why This Matters**:
- Rich behavior: Can add methods to enumeration
- Database storage: Stores as int (1, 2, 3) for performance
- Type safety: Cannot pass arbitrary int as status
- Discoverability: `CustomerStatus.GetAll()` returns all valid values

## 9. Domain Events

### CustomerCreatedDomainEvent

```csharp
public partial class CustomerCreatedDomainEvent(Customer model) : DomainEventBase
{
    public Customer Model { get; private set; } = model;
}
```

**Patterns Demonstrated**:
1. **Primary Constructor Syntax**: C# 12+ concise parameter declaration
2. **DomainEventBase Inheritance**: Provides EventId, Timestamp, AggregateId
3. **Partial Class**: Allows source generator extensions
4. **Immutable Model**: Private setter prevents modification

**Why This Matters**:
- Reactive architecture: Other modules can react to customer creation
- Audit trail: Every event is timestamped and logged
- Loose coupling: Event handlers don't know about Customer aggregate
- Testability: Can test event handlers independently

## 10. Key Patterns Summary

### Pattern: Result Chaining (Railway-Oriented Programming)

```csharp
return Result<Customer>
    .Bind(() => EmailAddress.Create(email))           // Create value object
    .Ensure(() => !string.IsNullOrEmpty(firstName))   // Validate invariant
    .Map(email => new Customer(...))                  // Transform to aggregate
    .Tap(customer => customer.DomainEvents.Register(...)) // Side effect
    // Returns Result<Customer>
```

**Methods**:
- `.Bind()`: Transform value, propagate errors (flatMap)
- `.Ensure()`: Validate condition, fail if false
- `.Map()`: Transform value to different type
- `.Tap()`: Side effect without changing value
- `.Apply()`: Finalize change pipeline

### Pattern: Change Method Structure

```csharp
public Result<Customer> Change[Property]([params])
{
    return this.Change()              // 1. Start change tracking
        .Ensure(() => [rule])         // 2. Validate business rules
        .Set(() => { [mutation] })    // 3. Apply state changes
        .Register([DomainEvent])      // 4. Record event
        .Apply();                     // 5. Finalize
}
```

### Pattern: Aggregate Boundaries

```
Customer Aggregate Root
├── Properties (value objects, primitives)
├── Address Collection (owned entities)
│   └── Managed via AddAddress, RemoveAddress, ChangeAddress
└── Domain Events (registered on changes)

Rules:
- External code accesses Customer only
- Address lifecycle controlled by Customer
- Changes tracked via domain events
```

### Pattern: Typed IDs

```csharp
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>

// Source generator creates:
public readonly struct CustomerId
{
    public Guid Value { get; }
    public static CustomerId Create(Guid value) => new(value);
    // Equality, ToString, GetHashCode, etc.
}
```

## 11. Simplifications for Templates

The templates in this skill use simplified versions of these patterns:

**Full Customer.cs** (276 lines):
- Address collection with add/remove/change methods
- Sequence number generator integration
- CustomerCreateContext pattern in handler
- DebuggerDisplay attribute
- Extensive validation rules

**Template Simplifications**:
- No child collections (simpler aggregate)
- No sequence number generator
- Direct Result chaining in handlers (no context)
- No DebuggerDisplay (optional)
- Moderate validation (null checks + basic rules)

**Rationale**: Templates should be easy to understand and adapt. Full Customer.cs demonstrates advanced patterns; templates provide a solid foundation that developers can extend.

## 12. Usage Example: Creating a Customer

### Step 1: From Presentation Layer (Endpoint)

```csharp
group.MapPost("",
    async ([FromServices] IRequester requester,
           [FromBody] CustomerModel model, CancellationToken ct)
           => (await requester
                .SendAsync(new CustomerCreateCommand(model), cancellationToken: ct))
                .MapHttpCreated(v => $"/api/coremodule/customers/{v.Id}"));
```

### Step 2: Command Validation (FluentValidation)

```csharp
public class Validator : AbstractValidator<CustomerCreateCommand>
{
    this.RuleFor(c => c.Model).NotNull();
    this.RuleFor(c => c.Model.Id).MustBeDefaultOrEmptyGuid();
    this.RuleFor(c => c.Model.FirstName).NotNull().NotEmpty();
    // ... more structural validation
}
```

### Step 3: Handler Processing

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await Result<CustomerModel>
        // Business rule validation (Rule pattern)
        .UnlessAsync(async (ct) => await Rule
            .Add(new EmailShouldBeUniqueRule(request.Model.Email, repository))
            .CheckAsync(ct))
        // Create aggregate (factory pattern)
        .Bind(() => Customer.Create(
            request.Model.FirstName,
            request.Model.LastName,
            request.Model.Email,
            generatedNumber))
        // Persist (repository pattern)
        .BindAsync(async (entity, ct) => 
            await repository.InsertResultAsync(entity, ct))
        // Map to DTO (Mapster)
        .MapResult<Customer, CustomerModel>(mapper);
}
```

### Step 4: Domain Event Handling

```csharp
public class CustomerCreatedDomainEventHandler 
    : DomainEventHandlerBase<CustomerCreatedDomainEvent>
{
    protected override async Task HandleAsync(
        CustomerCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Send welcome email
        // Create audit log entry
        // Update read model
    }
}
```

## 13. Testing Examples

### Unit Test: Factory Method

```csharp
[Fact]
public void Create_WithValidData_ShouldSucceed()
{
    // Arrange
    var firstName = "John";
    var lastName = "Doe";
    var email = "john.doe@example.com";
    var number = "CUS-2024-000001";

    // Act
    var result = Customer.Create(firstName, lastName, email, number);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.FirstName.ShouldBe(firstName);
    result.Value.Email.Value.ShouldBe(email.ToLowerInvariant());
    result.Value.Status.ShouldBe(CustomerStatus.Lead);
}

[Fact]
public void Create_WithInvalidEmail_ShouldFail()
{
    // Act
    var result = Customer.Create("John", "Doe", "invalid", "CUS-2024-000001");

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldContain(e => e.Message.Contains("Email"));
}
```

### Unit Test: Change Method

```csharp
[Fact]
public void ChangeName_WithValidNames_ShouldSucceedAndRegisterEvent()
{
    // Arrange
    var customer = Customer.Create("John", "Doe", "john@example.com", "CUS-001").Value;
    customer.DomainEvents.Clear(); // Clear creation event

    // Act
    var result = customer.ChangeName("Jane", "Smith");

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.FirstName.ShouldBe("Jane");
    result.Value.LastName.ShouldBe("Smith");
    result.Value.DomainEvents.ShouldContain<CustomerUpdatedDomainEvent>();
}
```

## 14. References

- **Customer.cs**: Lines 1-276 (full implementation)
- **EmailAddress.cs**: Lines 1-96 (value object pattern)
- **CustomerStatus.cs**: Lines 1-28 (enumeration pattern)
- **CustomerCreatedDomainEvent.cs**: Lines 1-23 (domain event pattern)
- **CustomerCreateCommandHandler.cs**: Lines 1-143 (CQRS handler pattern)
- **CustomerTypeConfiguration.cs**: Lines 1-120 (EF Core mapping)
- **CoreModuleMapperRegister.cs**: Lines 1-108 (Mapster configuration)

## Conclusion

The Customer aggregate demonstrates all key DDD, Clean Architecture, and bITdevKit patterns in a production-ready implementation. Understanding these patterns enables you to create robust, maintainable aggregates for any domain.

**Key Takeaways**:
1. Factory pattern enforces validation at creation
2. Change methods ensure consistency during updates
3. Value objects prevent invalid data from existing
4. Domain events enable reactive behaviors
5. Result pattern eliminates exception-based control flow
6. Typed IDs provide compile-time safety
7. Aggregate boundaries maintain consistency
