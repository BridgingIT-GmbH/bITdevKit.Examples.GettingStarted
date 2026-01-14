# Result Chaining Patterns (Railway-Oriented Programming)

This document explains the Result pattern and railway-oriented programming used extensively in the bITdevKit GettingStarted example for error handling, validation, and functional composition.

## What is the Result Pattern?

The **Result pattern** represents the outcome of an operation that can succeed or fail, without using exceptions for control flow.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; } // Only valid if IsSuccess
    public IEnumerable<IResultError> Errors { get; } // Only valid if IsFailure
}
```

**Key Concept**: Instead of `throw new Exception()`, return `Result.Failure(error)`.

## Why Use Result Pattern?

### Problem: Exceptions for Control Flow

```csharp
// BAD: Using exceptions for expected failures
public Customer CreateCustomer(string firstName, string lastName, string email)
{
    if (string.IsNullOrEmpty(firstName))
        throw new ValidationException("First name required");
    
    if (string.IsNullOrEmpty(lastName))
        throw new ValidationException("Last name required");
    
    if (!email.Contains('@'))
        throw new ValidationException("Invalid email");
    
    return new Customer(firstName, lastName, email);
}

// Caller must use try-catch for expected errors
try
{
    var customer = CreateCustomer(firstName, lastName, email);
}
catch (ValidationException ex)
{
    // Handle validation errors
}
```

**Problems**:
- Exceptions are expensive (stack trace, unwinding)
- Hidden control flow (no indication in signature that method can fail)
- Forces caller to guess which exceptions might be thrown
- Exceptions should be for exceptional cases, not validation

### Solution: Result Pattern

```csharp
// GOOD: Using Result pattern
public static Result<Customer> Create(string firstName, string lastName, string email)
{
    return Result<Customer>
        .Ensure(() => !string.IsNullOrEmpty(firstName),
            new ValidationError("First name required", "FirstName"))
        .Ensure(() => !string.IsNullOrEmpty(lastName),
            new ValidationError("Last name required", "LastName"))
        .Bind(() => EmailAddress.Create(email))
        .Map(emailAddress => new Customer(firstName, lastName, emailAddress));
}

// Caller explicitly handles success/failure
var result = Customer.Create(firstName, lastName, email);
if (result.IsSuccess)
{
    var customer = result.Value;
    // Use customer
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error.Message);
    }
}
```

**Benefits**:
- Explicit: Method signature shows it can fail (`Result<T>`)
- Fast: No exceptions thrown for expected failures
- Composable: Can chain operations using `.Bind()`, `.Map()`, etc.
- Type-safe: Compiler enforces checking success/failure

## Railway-Oriented Programming

**Metaphor**: Operations are like railway tracks with two rails:
- **Success track** (top rail): Operations continue processing
- **Failure track** (bottom rail): Errors bypass remaining operations

```
Start
  ↓
  ┌─────────────┐
  │  Operation1 │ → Success → Continue on success track
  └─────────────┘
       ↓ Failure
       ↓
  [Switch to failure track, skip remaining operations]
       ↓
  ┌─────────────┐
  │  Operation2 │ → (Skipped if already failed)
  └─────────────┘
       ↓
Result<T> (Success or Failure)
```

## Core Result Methods

### 1. Result.Success / Result.Failure

**Create Result instances**:

```csharp
// Success
var success = Result<Customer>.Success(customer);

// Failure
var failure = Result<Customer>.Failure(
    new ValidationError("Email invalid", "Email"));

// Failure with multiple errors
var failure = Result<Customer>.Failure(
    new ValidationError("Email invalid", "Email"),
    new ValidationError("Name required", "Name"));
```

### 2. Ensure (Validation)

**Validates a condition, fails if false**:

```csharp
return Result<Customer>
    .Ensure(() => !string.IsNullOrEmpty(firstName),
        new ValidationError("First name required", "FirstName"))
    .Ensure(() => firstName.Length <= 50,
        new ValidationError("First name too long", "FirstName"))
    .Map(() => new Customer(firstName, ...));
```

**Flow**:
```
Input: firstName = ""
  ↓
Ensure: !string.IsNullOrEmpty(firstName) → FAIL
  ↓
Result<Customer>.Failure(ValidationError("First name required"))
```

### 3. Bind (Flat Map)

**Transforms value to Result<U>, flattens nested Result**:

```csharp
// EmailAddress.Create returns Result<EmailAddress>
return Result<Customer>
    .Bind(() => EmailAddress.Create(email))
    // Now have Result<EmailAddress>, not Result<Result<EmailAddress>>
    .Map(emailAddress => new Customer(..., emailAddress));
```

**Signature**:
```csharp
Result<T>.Bind<U>(Func<T, Result<U>> f) → Result<U>
```

**Without Bind (nested Result)**:
```csharp
// WRONG: Returns Result<Result<EmailAddress>>
Result<Customer>
    .Map(() => EmailAddress.Create(email)) // Returns Result<EmailAddress>
    // Now stuck with Result<Result<EmailAddress>>!
```

**With Bind (flattened)**:
```csharp
// CORRECT: Returns Result<EmailAddress>
Result<Customer>
    .Bind(() => EmailAddress.Create(email)) // Flattens to Result<EmailAddress>
    .Map(emailAddress => ...) // Works with EmailAddress, not Result<EmailAddress>
```

### 4. Map (Transform)

**Transforms value to different type**:

```csharp
return Result<Customer>
    .Bind(() => EmailAddress.Create(email))
    .Map(emailAddress => new Customer(..., emailAddress));
    // Transform EmailAddress → Customer
```

**Signature**:
```csharp
Result<T>.Map<U>(Func<T, U> f) → Result<U>
```

**Key Difference from Bind**:
- `Map`: Function returns `U` (plain value)
- `Bind`: Function returns `Result<U>` (Result-wrapped value)

### 5. Tap (Side Effect)

**Executes action without changing value**:

```csharp
return Result<Customer>
    .Bind(() => Customer.Create(...))
    .Tap(customer => customer.DomainEvents.Register(new CustomerCreatedDomainEvent(customer)))
    // Still returns Result<Customer>
    .Tap(customer => logger.LogInformation("Customer created: {Id}", customer.Id));
```

**Signature**:
```csharp
Result<T>.Tap(Action<T> action) → Result<T>
```

**Use Cases**:
- Logging
- Domain event registration
- Audit trail
- Notifications

### 6. BindAsync / MapAsync / TapAsync

**Async versions for operations that return Task**:

```csharp
return await Result<Customer>
    .Bind(() => Customer.Create(...))
    .BindAsync(async (customer, ct) =>
        await repository.InsertResultAsync(customer, ct), cancellationToken)
    .TapAsync(async (customer, ct) =>
        await eventPublisher.PublishAsync(customer.DomainEvents, ct), cancellationToken);
```

### 7. Unless (Conditional Failure)

**Fails if async predicate returns true**:

```csharp
return await Result<Customer>
    .UnlessAsync(async (ct) => await Rule
        .Add(new EmailShouldBeUniqueRule(email, repository))
        .CheckAsync(ct), cancellationToken: cancellationToken)
    .Map(() => new Customer(...));
```

**Flow**:
```
Check rule: Is email unique?
  ↓ Yes (unique) → Continue
  ↓ No (duplicate) → Fail with ValidationError
```

## Pattern: Chaining Value Object Creation

### Example from Customer.Create

```csharp
public static Result<Customer> Create(
    string firstName,
    string lastName,
    string email,
    string number)
{
    return Result<Customer>
        // Create EmailAddress value object
        .Bind(() => EmailAddress.Create(email))
        
        // Create CustomerNumber value object, carry EmailAddress
        .Bind(emailAddress => CustomerNumber.Create(number)
            .Map(customerNumber => (emailAddress, customerNumber)))
        
        // Validate firstName
        .Ensure((tuple) => RuleSet.IsNotEmpty(firstName),
            new ValidationError("First name required", "FirstName"))
        
        // Validate lastName
        .Ensure((tuple) => RuleSet.IsNotEmpty(lastName),
            new ValidationError("Last name required", "LastName"))
        
        // Create Customer with validated data
        .Map(tuple => new Customer(firstName, lastName, tuple.emailAddress, tuple.customerNumber))
        
        // Register domain event
        .Tap(customer => customer.DomainEvents.Register(new CustomerCreatedDomainEvent(customer)));
}
```

**Step-by-Step Flow**:

```
1. Input: firstName="John", lastName="Doe", email="john@example.com", number="CUS-001"
   ↓
2. Bind: EmailAddress.Create("john@example.com")
   → Result<EmailAddress>.Success(EmailAddress("john@example.com"))
   ↓
3. Bind: CustomerNumber.Create("CUS-001")
   → Result<CustomerNumber>.Success(CustomerNumber("CUS-001"))
   ↓
4. Map: (emailAddress, customerNumber) tuple
   → Result<(EmailAddress, CustomerNumber)>.Success((email, number))
   ↓
5. Ensure: firstName not empty → PASS
   ↓
6. Ensure: lastName not empty → PASS
   ↓
7. Map: new Customer(firstName, lastName, tuple.emailAddress, tuple.customerNumber)
   → Result<Customer>.Success(customer)
   ↓
8. Tap: Register CustomerCreatedDomainEvent
   → Side effect, still Result<Customer>.Success(customer)
   ↓
9. Return: Result<Customer>.Success(customer)
```

**If Email Invalid**:

```
1. Input: email="invalid"
   ↓
2. Bind: EmailAddress.Create("invalid")
   → Result<EmailAddress>.Failure(ValidationError("Email must contain @"))
   ↓
3-8. SKIPPED (already on failure track)
   ↓
9. Return: Result<Customer>.Failure(ValidationError("Email must contain @"))
```

## Pattern: Change Methods

### Example from Customer.ChangeName

```csharp
public Result<Customer> ChangeName(string firstName, string lastName)
{
    return this.Change()
        .Ensure(() => RuleSet.IsNotEmpty(firstName),
            new ValidationError("First name required", "FirstName"))
        .Ensure(() => RuleSet.IsNotEmpty(lastName),
            new ValidationError("Last name required", "LastName"))
        .Ensure(() => RuleSet.NotEqual(lastName, "notallowed"),
            new ValidationError("Last name forbidden", "LastName"))
        .Set(() =>
        {
            this.FirstName = firstName;
            this.LastName = lastName;
        })
        .Register(new CustomerUpdatedDomainEvent(this))
        .Apply();
}
```

**Change Pipeline Stages**:

```
1. this.Change() 
   → Start change tracking, return ChangeBuilder<Customer>
   ↓
2. Ensure: firstName not empty → Validate
   ↓
3. Ensure: lastName not empty → Validate
   ↓
4. Ensure: lastName not "notallowed" → Validate
   ↓
5. Set: () => { this.FirstName = firstName; this.LastName = lastName; }
   → Apply mutations to aggregate
   ↓
6. Register: new CustomerUpdatedDomainEvent(this)
   → Add domain event to aggregate.DomainEvents
   ↓
7. Apply()
   → Finalize change, return Result<Customer>
```

**Why This Pattern?**:
- **Atomicity**: All validations pass or no changes applied
- **Consistency**: Cannot have partial state changes
- **Auditability**: Domain events capture every change
- **Testability**: Easy to test validation without database

## Pattern: Handler Result Pipeline

### Query Handler Example

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(
    CustomerFindOneQuery request,
    SendOptions options,
    CancellationToken cancellationToken)
{
    return await repository
        // Load entity (returns Result<Customer>)
        .FindOneResultAsync(CustomerId.Create(request.Id), cancellationToken)
        
        // Log (side effect)
        .Log(logger, "Customer {Id} loaded", r => [r.Value.Id])
        
        // Audit (side effect)
        .Log(logger, "AUDIT - Customer {Id} retrieved", r => [r.Value.Id])
        
        // Map domain → DTO
        .MapResult<Customer, CustomerModel>(mapper)
        
        // Log (side effect)
        .Log(logger, "Customer mapped to {@Model}", r => [r.Value]);
}
```

**Flow**:
```
1. FindOneResultAsync → Result<Customer>
   ↓ (if success)
2. Log "Customer loaded"
   ↓
3. Log "AUDIT - Customer retrieved"
   ↓
4. MapResult<Customer, CustomerModel> → Result<CustomerModel>
   ↓
5. Log "Customer mapped"
   ↓
6. Return Result<CustomerModel>
```

**If Not Found**:
```
1. FindOneResultAsync → Result<Customer>.Failure(NotFoundError)
   ↓
2-5. SKIPPED (logs not executed on failure)
   ↓
6. Return Result<CustomerModel>.Failure(NotFoundError)
```

### Command Handler Example

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await Result<CustomerModel>
        // Validate business rules
        .UnlessAsync(async (ct) => await Rule
            .Add(new EmailShouldBeUniqueRule(request.Model.Email, repository))
            .CheckAsync(ct), cancellationToken)
        
        // Create aggregate
        .Bind(() => Customer.Create(
            request.Model.FirstName,
            request.Model.LastName,
            request.Model.Email,
            generatedNumber))
        
        // Persist
        .BindAsync(async (entity, ct) =>
            await repository.InsertResultAsync(entity, ct), cancellationToken)
        
        // Audit
        .Log(logger, "AUDIT - Customer {Id} created", r => [r.Value.Id])
        
        // Map to DTO
        .MapResult<Customer, CustomerModel>(mapper);
}
```

**Flow**:
```
1. Rule validation (email unique?)
   ↓ (if unique)
2. Customer.Create → Result<Customer>
   ↓ (if valid)
3. repository.InsertResultAsync → Result<Customer>
   ↓ (if successful)
4. Log audit trail
   ↓
5. MapResult → Result<CustomerModel>
   ↓
6. Return Result<CustomerModel>
```

## Pattern: Carrying Multiple Values Through Pipeline

### Using Tuples

```csharp
return Result<Customer>
    .Bind(() => EmailAddress.Create(email))
    .Bind(emailAddress => CustomerNumber.Create(number)
        .Map(customerNumber => (emailAddress, customerNumber)))
    // Now have Result<(EmailAddress, CustomerNumber)>
    .Ensure((tuple) => RuleSet.IsNotEmpty(firstName), ...)
    // Tuple still available
    .Map(tuple => new Customer(firstName, lastName, tuple.emailAddress, tuple.customerNumber));
```

**Why Tuples?**:
- Need both `emailAddress` and `customerNumber` for Customer constructor
- Cannot just return `customerNumber` (would lose `emailAddress`)
- Tuple carries both values through pipeline

### Using Context Object (Advanced)

```csharp
private class CustomerCreateContext
{
    public CustomerModel Model { get; init; }
    public CustomerNumber Number { get; set; }
    public Customer Entity { get; set; }
}

return await Result<CustomerModel>
    .Bind<CustomerCreateContext>(() => new(request.Model))
    .BindResultAsync(this.GenerateSequenceAsync, this.CaptureNumber, cancellationToken)
    .Bind(this.CreateEntity)
    .BindResultAsync(this.PersistEntityAsync, this.CapturePersistedEntity, cancellationToken)
    .Map(this.ToModel);
```

**Why Context Object?**:
- Carrier for multiple values through complex pipeline
- Methods can access and modify context
- More readable than nested tuples for complex scenarios
- Used in CustomerCreateCommandHandler (full implementation)

## Pattern: Result Extensions for HTTP Mapping

### MapHttpOk (200 OK)

```csharp
group.MapGet("/{id:guid}",
    async ([FromServices] IRequester requester, [FromRoute] string id, CancellationToken ct)
        => (await requester.SendAsync(new CustomerFindOneQuery(id), ct))
            .MapHttpOk(logger));
```

**Behavior**:
- Success → `Results.Ok(value)` (200 OK)
- Failure → `Results.Problem(...)` (400/404/500 depending on error type)

### MapHttpCreated (201 Created)

```csharp
group.MapPost("",
    async ([FromServices] IRequester requester, [FromBody] CustomerModel model, CancellationToken ct)
        => (await requester.SendAsync(new CustomerCreateCommand(model), ct))
            .MapHttpCreated(v => $"/api/coremodule/customers/{v.Id}"));
```

**Behavior**:
- Success → `Results.Created(location, value)` (201 Created with Location header)
- Failure → `Results.Problem(...)` (400/500)

### MapHttpNoContent (204 No Content)

```csharp
group.MapDelete("/{id:guid}",
    async ([FromServices] IRequester requester, [FromRoute] string id, CancellationToken ct)
        => (await requester.SendAsync(new CustomerDeleteCommand(id), ct))
            .MapHttpNoContent());
```

**Behavior**:
- Success → `Results.NoContent()` (204 No Content)
- Failure → `Results.Problem(...)` (400/404/500)

### Error Mapping

```csharp
Result error type         → HTTP Status
─────────────────────────────────────────
ValidationError           → 400 Bad Request
NotFoundError             → 404 Not Found
EntityNotFoundError       → 404 Not Found
ConflictError             → 409 Conflict
ConcurrencyError          → 409 Conflict
UnauthorizedError         → 401 Unauthorized
ForbiddenError            → 403 Forbidden
(Other errors)            → 500 Internal Server Error
```

## Testing Result Patterns

### Unit Tests for Result Chaining

```csharp
[Fact]
public void Create_WithValidData_ShouldReturnSuccess()
{
    // Act
    var result = Customer.Create("John", "Doe", "john@example.com", "CUS-001");
    
    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();
    result.Value.FirstName.ShouldBe("John");
}

[Fact]
public void Create_WithInvalidEmail_ShouldReturnFailure()
{
    // Act
    var result = Customer.Create("John", "Doe", "invalid", "CUS-001");
    
    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldContain(e => e.Message.Contains("Email"));
}

[Fact]
public void ChangeName_WithEmptyFirstName_ShouldReturnFailure()
{
    // Arrange
    var customer = Customer.Create("John", "Doe", "john@example.com", "CUS-001").Value;
    
    // Act
    var result = customer.ChangeName("", "Doe");
    
    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldContain(e => e.Message.Contains("First name"));
    customer.FirstName.ShouldBe("John"); // Unchanged due to failure
}
```

## Common Patterns Summary

### Pattern 1: Validate-Create-Return

```csharp
public static Result<T> Create(params)
{
    return Result<T>
        .Ensure(() => validation1, error1)
        .Ensure(() => validation2, error2)
        .Bind(() => ValueObject.Create(...))
        .Map(() => new T(...));
}
```

### Pattern 2: Load-Validate-Modify-Persist

```csharp
public async Task<Result<TModel>> HandleAsync(...)
{
    return await repository.FindOneResultAsync(id, ct)
        .UnlessAsync(async (e, ct) => await Rule.Add(...).CheckAsync(ct))
        .Bind(e => e.ChangeProperty(...))
        .BindAsync(async (e, ct) => await repository.UpdateResultAsync(e, ct))
        .MapResult<TEntity, TModel>(mapper);
}
```

### Pattern 3: Multiple-Operations-Then-Map

```csharp
return await Result<TModel>
    .Bind(() => Operation1(...))
    .BindAsync(async (r1, ct) => await Operation2(r1, ct))
    .Bind(r2 => Operation3(r2))
    .Map(r3 => ToDto(r3));
```

## Summary

**Result Pattern Benefits**:
- Explicit error handling (no hidden exceptions)
- Composable operations (railway-oriented programming)
- Type-safe (compiler enforces checking)
- Testable (easy to test success/failure paths)
- Performant (no exception overhead)

**Key Methods**:
- `Ensure`: Validate condition
- `Bind`: Flat map (Result<T> → Result<U>)
- `Map`: Transform (T → U)
- `Tap`: Side effect (logging, events)
- `Unless`: Conditional failure (business rules)

**Usage Guidelines**:
- Use Result for operations that can fail in expected ways
- Use exceptions for truly exceptional cases (programming errors)
- Chain operations using railway-oriented programming
- Test both success and failure paths
