# Naming Conventions

This document defines the naming standards for classes, files, properties, methods, and folders when implementing aggregates in the bITdevKit GettingStarted example.

**Purpose**: Ensure consistency, readability, and maintainability across the codebase  
**Audience**: Developers implementing new aggregates or refactoring existing code

---

## Table of Contents

- [General Principles](#general-principles)
- [Domain Layer](#domain-layer)
- [Application Layer](#application-layer)
- [Infrastructure Layer](#infrastructure-layer)
- [Presentation Layer](#presentation-layer)
- [Test Projects](#test-projects)
- [Folder Structure](#folder-structure)
- [References](#references)

---

## General Principles

### C# Naming Conventions

- **PascalCase**: Classes, methods, properties, enums, namespaces (e.g., `Customer`, `CreateAsync`, `FirstName`)
- **camelCase**: Local variables, method parameters, private fields (e.g., `firstName`, `emailAddress`)
- **UPPERCASE**: Constants (e.g., `MAX_LENGTH`, `DEFAULT_TIMEOUT`)
- **Prefix `I`**: Interfaces (e.g., `IRepository`, `IMapper`)
- **Prefix `_`**: Private fields (optional, not used in this project - prefer `this.fieldName`)

### Entity and Aggregate Naming

- **Singular**: Use singular form for entity names (e.g., `Customer` not `Customers`)
- **Descriptive**: Use business domain terms, not technical jargon (e.g., `Customer` not `CustomerEntity`)
- **Concise**: Avoid unnecessary words (e.g., `EmailAddress` not `EmailAddressValueObject`)

### Consistency

- Follow existing patterns in the codebase (refer to `Customer` aggregate as reference)
- Use the same naming pattern across all layers for the same concept
- Maintain consistency within a module (don't mix styles)

---

## Domain Layer

### Aggregate Root

**Pattern**: `[Entity].cs`  
**Location**: `src/Modules/[Module]/[Module].Domain/Model/[Entity]Aggregate/`

**Examples**:
- `Customer.cs`
- `Order.cs`
- `Product.cs`

**Class Declaration**:
```csharp
public class Customer : AuditableAggregateRoot<CustomerId>
{
    // Implementation
}
```

**File Naming**: Match class name exactly (case-sensitive)

---

### Typed Entity ID

**Pattern**: `[Entity]Id.cs` OR use `[TypedEntityId<Guid>]` attribute  
**Location**: Same folder as aggregate OR inline in aggregate file

**Examples**:
- `CustomerId.cs` (separate file)
- `[TypedEntityId<Guid>] public partial class Customer` (attribute, no separate file needed)

**Class Declaration** (if separate file):
```csharp
public partial struct CustomerId : IEntityId<Guid>
{
    public Guid Value { get; }
    
    public static CustomerId Create(Guid value) => new(value);
}
```

**Attribute Usage** (if inline):
```csharp
[TypedEntityId<Guid>]
public partial class Customer : AuditableAggregateRoot<CustomerId>
{
    // Source generator creates CustomerId struct
}
```

**Recommendation**: Use attribute approach (less boilerplate)

---

### Value Objects

**Pattern**: Descriptive singular noun (e.g., `EmailAddress`, `PhoneNumber`, `Money`)  
**Location**: `src/Modules/[Module]/[Module].Domain/Model/`

**Examples**:
- `EmailAddress.cs`
- `PhoneNumber.cs`
- `Address.cs`
- `Money.cs`

**Class Declaration**:
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
        // Validation and creation
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

**Naming Guidelines**:
- Use business terms (not technical types): `EmailAddress` not `Email` or `EmailString`
- Avoid suffixes: `Money` not `MoneyValueObject` (layer is obvious from namespace)

---

### Enumerations

**Pattern**: `[Entity]Status` or descriptive noun (e.g., `CustomerStatus`, `OrderState`, `PaymentMethod`)  
**Location**: `src/Modules/[Module]/[Module].Domain/Model/[Entity]Aggregate/` (if entity-specific) OR `.../Model/` (if shared)

**Examples**:
- `CustomerStatus.cs`
- `OrderState.cs`
- `PaymentMethod.cs`

**Class Declaration**:
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
```

**Naming Guidelines**:
- Use `Status` for states (e.g., `CustomerStatus`, `OrderStatus`)
- Use `Type` for categories (e.g., `ProductType`, `AccountType`)
- Use specific nouns for methods (e.g., `PaymentMethod`, `ShippingMethod`)

---

### Domain Events

**Pattern**: `[Entity][PastTenseAction]DomainEvent.cs`  
**Location**: `src/Modules/[Module]/[Module].Domain/Events/`

**Examples**:
- `CustomerCreatedDomainEvent.cs`
- `CustomerUpdatedDomainEvent.cs`
- `CustomerDeletedDomainEvent.cs`
- `OrderPlacedDomainEvent.cs`
- `PaymentProcessedDomainEvent.cs`

**Class Declaration**:
```csharp
public partial class CustomerCreatedDomainEvent(Customer model) : DomainEventBase
{
    public Customer Model { get; private set; } = model;
}
```

**Naming Guidelines**:
- Always use past tense: `Created`, `Updated`, `Deleted`, `Placed`, `Processed`
- Include entity name: `CustomerCreated` not just `Created`
- Suffix with `DomainEvent`: `CustomerCreatedDomainEvent`

---

### Domain Methods

**Factory Methods**:
- Pattern: `Create([parameters])` (static method returning `Result<T>`)
- Example: `Customer.Create(string firstName, string lastName, string email, CustomerStatus status)`

**Change Methods**:
- Pattern: `Change[Property]([parameters])` or `[Verb][Noun]([parameters])`
- Examples:
  - `ChangeEmail(string email)`
  - `ChangeName(string firstName, string lastName)`
  - `Activate()`, `Retire()`, `Suspend()`
  - `AddAddress(Address address)`

**Query Methods** (if needed):
- Pattern: `Get[Property]()`, `Is[State]()`, `Has[Property]()`
- Examples:
  - `GetFullName() → string`
  - `IsActive() → bool`
  - `HasAddress(string city) → bool`

---

## Application Layer

### Commands

**Pattern**: `[Entity][Action]Command.cs`  
**Location**: `src/Modules/[Module]/[Module].Application/Commands/`

**Examples**:
- `CustomerCreateCommand.cs`
- `CustomerUpdateCommand.cs`
- `CustomerDeleteCommand.cs`
- `OrderPlaceCommand.cs`
- `PaymentProcessCommand.cs`

**Class Declaration**:
```csharp
public class CustomerCreateCommand : RequestBase<Result<CustomerModel>>
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string Email { get; init; }
    public string Status { get; init; }
}
```

**Naming Guidelines**:
- Use present tense verbs: `Create`, `Update`, `Delete`, `Place`, `Process`
- Avoid CRUD acronyms: `CustomerCreateCommand` not `CustomerCRUDCommand`
- Entity name first, action second: `CustomerCreate` not `CreateCustomer`

---

### Command Handlers

**Pattern**: `[Entity][Action]CommandHandler.cs`  
**Location**: Same folder as command (co-located)

**Examples**:
- `CustomerCreateCommandHandler.cs`
- `CustomerUpdateCommandHandler.cs`
- `CustomerDeleteCommandHandler.cs`

**Class Declaration**:
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
        // Implementation
    }
}
```

---

### Command Validators

**Pattern**: `[Entity][Action]CommandValidator.cs`  
**Location**: Same folder as command (co-located)

**Examples**:
- `CustomerCreateCommandValidator.cs`
- `CustomerUpdateCommandValidator.cs`

**Class Declaration**:
```csharp
public class CustomerCreateCommandValidator : AbstractValidator<CustomerCreateCommand>
{
    public CustomerCreateCommandValidator()
    {
        this.RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
        this.RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
```

---

### Queries

**Pattern**: `[Entity][Action]Query.cs`  
**Location**: `src/Modules/[Module]/[Module].Application/Queries/`

**Examples**:
- `CustomerFindOneQuery.cs`
- `CustomerFindAllQuery.cs`
- `CustomerSearchQuery.cs`
- `OrderGetByCustomerQuery.cs`

**Class Declaration**:
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

**Naming Guidelines**:
- Use descriptive verbs: `FindOne`, `FindAll`, `Search`, `GetBy`, `List`
- Query suffix: `CustomerFindOneQuery` not `CustomerFindOne`

---

### Query Handlers

**Pattern**: `[Entity][Action]QueryHandler.cs`  
**Location**: Same folder as query (co-located)

**Examples**:
- `CustomerFindOneQueryHandler.cs`
- `CustomerFindAllQueryHandler.cs`

---

### DTOs (Models)

**Pattern**: `[Entity]Model.cs`  
**Location**: `src/Modules/[Module]/[Module].Application/Models/`

**Examples**:
- `CustomerModel.cs`
- `OrderModel.cs`
- `ProductModel.cs`

**Class Declaration**:
```csharp
public class CustomerModel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

**Naming Guidelines**:
- Use `Model` suffix: `CustomerModel` not `CustomerDto` or `CustomerViewModel`
- Properties match domain properties (but primitives instead of value objects)

---

## Infrastructure Layer

### DbContext

**Pattern**: `[Module]DbContext.cs`  
**Location**: `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/`

**Examples**:
- `CoreModuleDbContext.cs`
- `InventoryModuleDbContext.cs`

**Class Declaration**:
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

---

### Entity Type Configuration

**Pattern**: `[Entity]TypeConfiguration.cs`  
**Location**: `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/Configurations/`

**Examples**:
- `CustomerTypeConfiguration.cs`
- `OrderTypeConfiguration.cs`

**Class Declaration**:
```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", CoreModuleConstants.Schema);
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => CustomerId.Create(value));
        
        // More configuration...
    }
}
```

---

### Migrations

**Pattern**: `[DateTime]_[Description].cs`  
**Location**: `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/Migrations/`

**Examples**:
- `20260114123456_AddCustomer.cs`
- `20260115084530_AddOrderAggr egate.cs`

**Generated by**: `dotnet ef migrations add [Description]`

---

## Presentation Layer

### Endpoints

**Pattern**: `[Entity]Endpoints.cs`  
**Location**: `src/Modules/[Module]/[Module].Presentation/Web/Endpoints/`

**Examples**:
- `CustomerEndpoints.cs`
- `OrderEndpoints.cs`

**Class Declaration**:
```csharp
public class CustomerEndpoints(IRequester requester) : EndpointsBase
{
    private readonly IRequester requester = requester;
    
    public override void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/core/customers")
            .WithTags("Customers");
        
        group.MapGet("", this.GetAll);
        group.MapGet("{id:guid}", this.GetById);
        group.MapPost("", this.Create);
        group.MapPut("{id:guid}", this.Update);
        group.MapDelete("{id:guid}", this.Delete);
    }
    
    private async Task<IResult> GetAll(...) { }
    private async Task<IResult> GetById(...) { }
    private async Task<IResult> Create(...) { }
    private async Task<IResult> Update(...) { }
    private async Task<IResult> Delete(...) { }
}
```

**Method Naming**:
- Use HTTP verbs: `GetAll`, `GetById`, `Create`, `Update`, `Delete`
- Use descriptive names for custom endpoints: `Search`, `Export`, `Activate`

---

### Mapper Register

**Pattern**: `[Module]MapperRegister.cs`  
**Location**: `src/Modules/[Module]/[Module].Presentation/`

**Examples**:
- `CoreModuleMapperRegister.cs`
- `InventoryModuleMapperRegister.cs`

**Class Declaration**:
```csharp
public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Customer, CustomerModel>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.Status, src => src.Status.Name);
        
        config.NewConfig<CustomerModel, Customer>()
            .ConstructUsing(src => Customer.Create(
                src.FirstName,
                src.LastName,
                src.Email,
                CustomerStatus.FromName(src.Status)).Value);
    }
}
```

---

## Test Projects

### Test Project Naming

**Pattern**: `[Module].[TestType]Tests`

**Examples**:
- `CoreModule.UnitTests`
- `CoreModule.IntegrationTests`
- `CoreModule.ArchitectureTests`

---

### Test Class Naming

**Pattern**: `[ClassUnderTest]Tests.cs`  
**Location**: Mirror source structure under `tests/`

**Examples**:
- `CustomerTests.cs` (tests `Customer.cs` aggregate)
- `EmailAddressTests.cs` (tests `EmailAddress.cs` value object)
- `CustomerCreateCommandHandlerTests.cs` (tests handler)
- `CustomerEndpointTests.cs` (tests endpoints)

**Class Declaration**:
```csharp
[UnitTest("Domain")]
public class CustomerTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsSuccessResult()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@example.com";
        
        // Act
        var result = Customer.Create(firstName, lastName, email, CustomerStatus.Active);
        
        // Assert
        result.ShouldBeSuccess();
        result.Value.FirstName.ShouldBe(firstName);
    }
}
```

---

### Test Method Naming

**Pattern**: `[MethodUnderTest]_[Scenario]_[ExpectedOutcome]`

**Examples**:
- `Create_ValidInputs_ReturnsSuccessResult`
- `Create_EmptyEmail_ReturnsFailureResult`
- `ChangeEmail_ValidEmail_UpdatesEmailAndRegistersDomainEvent`
- `Handle_ValidCommand_InsertsCustomerAndReturnsModel`
- `GetById_ExistingId_Returns200Ok`
- `Post_InvalidData_Returns400BadRequest`

---

## Folder Structure

### Domain Layer
```
src/Modules/[Module]/[Module].Domain/
  Model/
    [Entity]Aggregate/
      [Entity].cs
      [Entity]Id.cs (optional if using attribute)
      [Entity]Status.cs (enumeration)
    EmailAddress.cs (value object)
  Events/
    [Entity]CreatedDomainEvent.cs
    [Entity]UpdatedDomainEvent.cs
    [Entity]DeletedDomainEvent.cs
```

### Application Layer
```
src/Modules/[Module]/[Module].Application/
  Commands/
    [Entity]CreateCommand.cs
    [Entity]CreateCommandHandler.cs
    [Entity]CreateCommandValidator.cs
    [Entity]UpdateCommand.cs
    [Entity]UpdateCommandHandler.cs
    [Entity]UpdateCommandValidator.cs
    [Entity]DeleteCommand.cs
    [Entity]DeleteCommandHandler.cs
  Queries/
    [Entity]FindOneQuery.cs
    [Entity]FindOneQueryHandler.cs
    [Entity]FindAllQuery.cs
    [Entity]FindAllQueryHandler.cs
  Models/
    [Entity]Model.cs
```

### Infrastructure Layer
```
src/Modules/[Module]/[Module].Infrastructure/
  EntityFramework/
    [Module]DbContext.cs
    Configurations/
      [Entity]TypeConfiguration.cs
    Migrations/
      [DateTime]_[Description].cs
```

### Presentation Layer
```
src/Modules/[Module]/[Module].Presentation/
  Web/
    Endpoints/
      [Entity]Endpoints.cs
  [Module]MapperRegister.cs
```

### Test Projects
```
tests/Modules/[Module]/
  [Module].UnitTests/
    Domain/
      [Entity]Tests.cs
      [ValueObject]Tests.cs
    Application/
      Commands/
        [Entity]CreateCommandHandlerTests.cs
        [Entity]CreateCommandValidatorTests.cs
      Queries/
        [Entity]FindOneQueryHandlerTests.cs
  [Module].IntegrationTests/
    Infrastructure/
      EntityFramework/
        [Module]DbContextTests.cs
    Presentation/
      Web/
        [Entity]EndpointTests.cs
```

---

## Namespace Conventions

**Pattern**: `BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].[Layer].[SubFolder]`

**Examples**:
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain.Model.CustomerAggregate`
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Application.Commands`
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Infrastructure.EntityFramework.Configurations`
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Presentation.Web.Endpoints`

**Test Namespaces**:
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Domain`
- `BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web`

---

## Route Naming

**Pattern**: `/api/[module]/[entities]`

**Examples**:
- `/api/core/customers`
- `/api/inventory/products`
- `/api/orders/orders`

**Guidelines**:
- Use lowercase
- Use plural entity names
- Include module name for disambiguation
- Use kebab-case for multi-word entities: `/api/core/customer-orders`

**Endpoint Routes**:
- `GET /api/core/customers` → GetAll
- `GET /api/core/customers/{id:guid}` → GetById
- `POST /api/core/customers` → Create
- `PUT /api/core/customers/{id:guid}` → Update
- `DELETE /api/core/customers/{id:guid}` → Delete

---

## Swagger Operation IDs

**Pattern**: `[Verb][Entity][Qualifier]`

**Examples**:
- `GetAllCustomers` (`.WithName("GetAllCustomers")`)
- `GetCustomerById` (`.WithName("GetCustomerById")`)
- `CreateCustomer` (`.WithName("CreateCustomer")`)
- `UpdateCustomer` (`.WithName("UpdateCustomer")`)
- `DeleteCustomer` (`.WithName("DeleteCustomer")`)

---

## Constants and Configuration

**Pattern**: `[Module]Constants.cs` or `[Module]ModuleConstants.cs`

**Examples**:
- `CoreModuleConstants.cs`

**Class Declaration**:
```csharp
public static class CoreModuleConstants
{
    public const string Schema = "Core";
    public const string ModuleName = "CoreModule";
    public const int DefaultPageSize = 10;
}
```

---

## Summary

| Concept | Pattern | Example |
|---------|---------|---------|
| Aggregate Root | `[Entity].cs` | `Customer.cs` |
| Typed Entity ID | `[Entity]Id.cs` | `CustomerId.cs` |
| Value Object | Descriptive noun | `EmailAddress.cs` |
| Enumeration | `[Entity]Status` | `CustomerStatus.cs` |
| Domain Event | `[Entity][PastTense]DomainEvent.cs` | `CustomerCreatedDomainEvent.cs` |
| Command | `[Entity][Action]Command.cs` | `CustomerCreateCommand.cs` |
| Command Handler | `[Entity][Action]CommandHandler.cs` | `CustomerCreateCommandHandler.cs` |
| Validator | `[Entity][Action]CommandValidator.cs` | `CustomerCreateCommandValidator.cs` |
| Query | `[Entity][Action]Query.cs` | `CustomerFindOneQuery.cs` |
| Query Handler | `[Entity][Action]QueryHandler.cs` | `CustomerFindOneQueryHandler.cs` |
| DTO | `[Entity]Model.cs` | `CustomerModel.cs` |
| Endpoints | `[Entity]Endpoints.cs` | `CustomerEndpoints.cs` |
| EF Configuration | `[Entity]TypeConfiguration.cs` | `CustomerTypeConfiguration.cs` |
| DbContext | `[Module]DbContext.cs` | `CoreModuleDbContext.cs` |
| Mapper Register | `[Module]MapperRegister.cs` | `CoreModuleMapperRegister.cs` |
| Test Class | `[ClassUnderTest]Tests.cs` | `CustomerTests.cs` |
| Test Method | `[Method]_[Scenario]_[Outcome]` | `Create_ValidInputs_ReturnsSuccess` |

---

## References

- **Customer Aggregate Example**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/Customer.cs`
- **C# Coding Conventions**: [Microsoft Docs](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- **.editorconfig**: Project root (enforces formatting rules)
- **AGENTS.md**: Project root (AI coding standards)

---

**Next Document**: common-pitfalls.md
