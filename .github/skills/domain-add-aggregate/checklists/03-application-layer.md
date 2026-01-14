# Application Layer Checklist

This checklist ensures your application layer implements CQRS correctly, follows bITdevKit patterns, and maintains Clean Architecture boundaries.

## Commands

### Command Structure
- [ ] Inherits from `RequestBase<TResponse>`
- [ ] Uses primary constructor for required properties
- [ ] Properties are init-only or have private setters (immutability)
- [ ] Located in `Application/Commands/`
- [ ] File name: `[Entity][Action]Command.cs`

### Command Properties
- [ ] Contains DTO model or primitive parameters
- [ ] No domain types exposed (use primitives/DTOs)
- [ ] For Create: ID should be empty/null (database generates it)
- [ ] For Update: ID and ConcurrencyVersion required
- [ ] For Delete: Only ID required

### Command Validator
- [ ] Nested `Validator` class inherits from `AbstractValidator<TCommand>`
- [ ] Validates structural properties (not null, format, length)
- [ ] Uses FluentValidation rules: `RuleFor()`, `NotNull()`, `NotEmpty()`, etc.
- [ ] Custom validators: `MustBeDefaultOrEmptyGuid()`, `MustNotBeDefaultOrEmptyGuid()`
- [ ] Meaningful error messages with `WithMessage()`

### Example Command
```csharp
public class CustomerCreateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    public CustomerModel Model { get; set; } = model;
    
    public class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();
            this.RuleFor(c => c.Model.Id).MustBeDefaultOrEmptyGuid();
            this.RuleFor(c => c.Model.FirstName).NotNull().NotEmpty();
        }
    }
}
```

## Command Handlers

### Handler Structure
- [ ] Inherits from `RequestHandlerBase<TRequest, TResponse>`
- [ ] Located in `Application/Commands/`
- [ ] File name: `[Entity][Action]CommandHandler.cs`
- [ ] Inject dependencies via primary constructor
- [ ] Override `HandleAsync()` method

### Handler Dependencies
- [ ] `ILogger<THandler>` for logging
- [ ] `IMapper` for domain ↔ DTO mapping
- [ ] `IGenericRepository<TEntity>` for persistence
- [ ] Other dependencies as needed (INotifier, custom services)

### Handler Attributes (Optional)
- [ ] `[Retry(2)]` for transient failure retry (recommended)
- [ ] `[Timeout(30)]` for timeout enforcement (recommended)
- [ ] Attributes commented with explanation

### Create Handler Pattern
- [ ] Validate business rules using `Rule` pattern
- [ ] Create aggregate via factory method: `[Entity].Create()`
- [ ] Persist using repository: `repository.InsertResultAsync()`
- [ ] Log audit trail
- [ ] Map domain → DTO: `.MapResult<[Entity], [Entity]Model>(mapper)`

### Update Handler Pattern
- [ ] Load entity: `repository.FindOneResultAsync([Entity]Id.Create(id))`
- [ ] Validate business rules (excluding current entity from uniqueness checks)
- [ ] Apply changes via change methods: `.Bind(e => e.Change[Property]())`
- [ ] Set concurrency version: `entity.ConcurrencyVersion = Guid.Parse(...)`
- [ ] Persist using repository: `repository.UpdateResultAsync()`
- [ ] Map domain → DTO

### Delete Handler Pattern
- [ ] Load entity: `repository.FindOneResultAsync([Entity]Id.Create(id))`
- [ ] Validate business rules (can entity be deleted?)
- [ ] Register domain event: `entity.DomainEvents.Register(new [Entity]DeletedDomainEvent(entity))`
- [ ] Delete: `repository.DeleteResultAsync(entity)`
- [ ] Publish events: `entity.DomainEvents.PublishAsync(notifier)`
- [ ] Return Unit: `.Unwrap()` to convert Result<(entity, deleted)> → Result<Unit>

### Result Chaining
- [ ] Use `.Bind()` for operations returning Result<T>
- [ ] Use `.BindAsync()` for async operations returning Result<T>
- [ ] Use `.Map()` for transformations (T → U)
- [ ] Use `.Tap()` for side effects (logging, events)
- [ ] Use `.UnlessAsync()` for business rule validation
- [ ] Use `.Log()` for structured logging

### Error Handling
- [ ] No try-catch for validation/business errors (use Result pattern)
- [ ] Try-catch only for unexpected exceptions (infrastructure failures)
- [ ] Return Result.Failure with appropriate error type
- [ ] Error types: ValidationError, NotFoundError, ConflictError, etc.

## Queries

### Query Structure
- [ ] Inherits from `RequestBase<TResponse>`
- [ ] Uses primary constructor for parameters
- [ ] Properties are init-only or have private setters
- [ ] Located in `Application/Queries/`
- [ ] File name: `[Entity]Find[One|All]Query.cs`

### FindOne Query
- [ ] Single parameter: ID (string representation of Guid)
- [ ] Validator checks ID is valid Guid: `MustNotBeDefaultOrEmptyGuid()`
- [ ] Returns `RequestBase<[Entity]Model>`

### FindAll Query
- [ ] Optional `FilterModel` property for pagination/sorting/filtering
- [ ] No required parameters
- [ ] No validator (FilterModel validates itself)
- [ ] Returns `RequestBase<IEnumerable<[Entity]Model>>`

### Example Query
```csharp
public class CustomerFindOneQuery(string id) : RequestBase<CustomerModel>
{
    public string Id { get; } = id;
    
    public class Validator : AbstractValidator<CustomerFindOneQuery>
    {
        public Validator()
        {
            this.RuleFor(c => c.Id).MustNotBeDefaultOrEmptyGuid();
        }
    }
}
```

## Query Handlers

### Handler Structure
- [ ] Inherits from `RequestHandlerBase<TRequest, TResponse>`
- [ ] Located in `Application/Queries/`
- [ ] File name: `[Entity]Find[One|All]QueryHandler.cs`
- [ ] Inject: ILogger, IMapper, IGenericRepository

### FindOne Handler Pattern
- [ ] Load entity: `repository.FindOneResultAsync([Entity]Id.Create(id))`
- [ ] Log audit trail (optional)
- [ ] Map domain → DTO: `.MapResult<[Entity], [Entity]Model>(mapper)`

### FindAll Handler Pattern
- [ ] Load entities: `repository.FindAllResultAsync(filter)`
- [ ] Log count and filter criteria
- [ ] Map collection: `.Map(mapper.Map<[Entity], [Entity]Model>)`

### Read-Only Operations
- [ ] No state changes (no repository Insert/Update/Delete)
- [ ] No domain events registered
- [ ] No business rule validation (queries are read-only)

## DTO Models

### Model Structure
- [ ] Located in `Application/Models/`
- [ ] File name: `[Entity]Model.cs`
- [ ] Properties are primitives (string, int, bool, etc.)
- [ ] No domain types (value objects, enumerations become strings)

### Model Properties
- [ ] `Id` as string (Guid representation)
- [ ] `ConcurrencyVersion` as string (Guid representation)
- [ ] Value objects as primitives (e.g., EmailAddress → string)
- [ ] Enumerations as strings (e.g., CustomerStatus → string)
- [ ] Child collections as List<[Child]Model>

### XML Documentation
- [ ] `<summary>` on all public properties
- [ ] `<example>` tags with sample values for OpenAPI/Swagger

### Example Model
```csharp
public class CustomerModel
{
    /// <summary>Gets or sets the unique identifier.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Id { get; set; }
    
    /// <summary>Gets or sets the concurrency version.</summary>
    /// <example>8f7a9b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c</example>
    public string ConcurrencyVersion { get; set; }
    
    /// <summary>Gets or sets the email address.</summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; }
}
```

## Business Rules

### Rule Pattern
- [ ] Simple rules use `RuleSet`: `RuleSet.IsNotEmpty()`, `RuleSet.NotEqual()`
- [ ] Complex rules implement `IBusinessRule` or inherit from `RuleBase`
- [ ] Rules located in `Application/Rules/` or `Domain/Rules/`

### Rule Implementation
- [ ] Async rules return `Task<bool>`
- [ ] Rules accept necessary dependencies (repository, services)
- [ ] Rules return descriptive error messages
- [ ] Rules are testable in isolation

### Using Rules in Handlers
- [ ] Rules applied via `.UnlessAsync()`
- [ ] Multiple rules chained: `await Rule.Add(...).Add(...).CheckAsync()`
- [ ] Rules fail fast (first failure stops validation)

### Example Rule
```csharp
public class EmailShouldBeUniqueRule : IBusinessRule
{
    private readonly string email;
    private readonly IGenericRepository<Customer> repository;
    
    public EmailShouldBeUniqueRule(string email, IGenericRepository<Customer> repository)
    {
        this.email = email;
        this.repository = repository;
    }
    
    public async Task<bool> CheckAsync(CancellationToken ct)
    {
        var exists = await repository.FindAllAsync(
            new Specification<Customer>(c => c.Email.Value == email), ct);
        return !exists.Any();
    }
    
    public IResultError CreateError() => 
        new ValidationError("Email address already exists", "Email");
}
```

## Pipeline Behaviors

### Automatic Behaviors
- [ ] ValidationPipelineBehavior: Executes FluentValidation validators
- [ ] RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute)
- [ ] TimeoutPipelineBehavior: Enforces timeouts (if [Timeout] attribute)
- [ ] ModuleScopeBehavior: Ensures module context isolation

### Custom Behaviors (if needed)
- [ ] Implement `IPipelineBehavior<TRequest, TResponse>`
- [ ] Register in DI container with correct order
- [ ] Keep behaviors focused (single responsibility)

## Clean Architecture Boundaries

### Dependencies
- [ ] Application references:
  - Domain layer (for entities, value objects, events)
  - bITdevKit.Application
  - FluentValidation
  - Mapster (via abstractions)
- [ ] Application does NOT reference:
  - Infrastructure layer (only abstractions like IRepository)
  - Presentation layer
  - EF Core directly

### Abstractions
- [ ] Repository abstractions used (not concrete implementations)
- [ ] No DbContext references
- [ ] No HTTP/API concerns
- [ ] No serialization concerns

## File Organization

### Folder Structure
- [ ] `Application/Commands/` - Commands and handlers
- [ ] `Application/Queries/` - Queries and handlers
- [ ] `Application/Models/` - DTO models
- [ ] `Application/Rules/` - Business rules (if not in Domain)

### Naming Conventions
- [ ] Command: `[Entity][Action]Command` (e.g., CustomerCreateCommand)
- [ ] Command handler: `[Entity][Action]CommandHandler`
- [ ] Query: `[Entity]Find[One|All]Query`
- [ ] Query handler: `[Entity]Find[One|All]QueryHandler`
- [ ] Model: `[Entity]Model`
- [ ] Rule: `[Description]Rule` (e.g., EmailShouldBeUniqueRule)

## Testing

### Unit Tests
- [ ] Handler tests with repository mocks/stubs (NSubstitute)
- [ ] Test success scenarios (valid input → expected output)
- [ ] Test failure scenarios (validation errors, not found, conflicts)
- [ ] Test business rule violations
- [ ] Test Result pattern (IsSuccess, IsFailure, Errors)

### Test Structure
- [ ] Arrange: Create mocks, setup data
- [ ] Act: Call handler.HandleAsync()
- [ ] Assert: Verify result, verify mock calls

### Example Test
```csharp
[Fact]
public async Task Handle_WithValidData_ShouldCreateCustomer()
{
    // Arrange
    var logger = Substitute.For<ILogger<CustomerCreateCommandHandler>>();
    var mapper = Substitute.For<IMapper>();
    var repository = Substitute.For<IGenericRepository<Customer>>();
    
    repository.InsertResultAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>())
        .Returns(call => Result<Customer>.Success(call.Arg<Customer>()));
    
    mapper.Map<Customer, CustomerModel>(Arg.Any<Customer>())
        .Returns(new CustomerModel { Id = "123", FirstName = "John" });
    
    var handler = new CustomerCreateCommandHandler(logger, mapper, repository);
    var command = new CustomerCreateCommand(new CustomerModel 
    { 
        FirstName = "John", 
        LastName = "Doe", 
        Email = "john@example.com" 
    });
    
    // Act
    var result = await handler.HandleAsync(command, new SendOptions(), CancellationToken.None);
    
    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.FirstName.ShouldBe("John");
    await repository.Received(1).InsertResultAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
}
```

## Common Anti-Patterns to Avoid

### Anemic Handlers
- [ ] WRONG: Handler only calls repository, no business logic
- [ ] CORRECT: Handler validates business rules, applies domain logic

### God Commands
- [ ] WRONG: Single command for multiple operations
- [ ] CORRECT: One command per operation (Create, Update, Delete separate)

### Direct Domain Manipulation
- [ ] WRONG: Setting entity properties directly in handler
- [ ] CORRECT: Using change methods on aggregate

### Business Logic in Handlers
- [ ] WRONG: Complex business logic in handler (should be in domain)
- [ ] CORRECT: Handler orchestrates domain operations

### Missing Validation
- [ ] WRONG: No FluentValidation, relying only on domain validation
- [ ] CORRECT: FluentValidation for structure, domain for business rules

## Code Quality

### Readability
- [ ] Handlers follow consistent structure
- [ ] Result chaining is clear and logical
- [ ] Comments explain "why", not "what"

### Maintainability
- [ ] Handlers focused on single responsibility
- [ ] Easy to add new commands/queries
- [ ] Consistent patterns across all handlers

### Performance
- [ ] Async/await used correctly
- [ ] No unnecessary database roundtrips
- [ ] Proper cancellation token propagation

## Final Review

### Before Committing
- [ ] All tests pass (unit tests)
- [ ] No compiler warnings
- [ ] Code follows .editorconfig rules
- [ ] FluentValidation validators defined for all commands/queries with required parameters
- [ ] Business rules implemented and tested
- [ ] Result pattern used consistently

### CQRS Validation
- [ ] Commands modify state, queries read state
- [ ] No state changes in query handlers
- [ ] Commands return Result<T> with errors
- [ ] Queries return Result<T> with not found errors

### Architecture Validation
- [ ] No infrastructure references
- [ ] No presentation layer references
- [ ] Repository abstractions used (not concrete types)
- [ ] Domain logic in domain, orchestration in application
