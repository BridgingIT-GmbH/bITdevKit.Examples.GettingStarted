# Mapping Layer Checklist

This checklist validates the Mapster mapping configuration between Domain aggregates and Application DTOs.

**Layer**: Presentation (MapperRegister)  
**Location**: `src/Modules/[Module]/[Module].Presentation/[Module]MapperRegister.cs`  
**Purpose**: Bidirectional transformation between rich domain objects and data transfer objects

---

## IRegister Implementation

- [ ] Class implements `IRegister` interface from Mapster
- [ ] Class is public and non-static
- [ ] `Register(TypeAdapterConfig config)` method is implemented
- [ ] Namespace follows pattern: `BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Presentation`
- [ ] File naming: `[Module]MapperRegister.cs` (e.g., `CoreModuleMapperRegister.cs`)

**CORRECT**:
```csharp
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Presentation;

public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Mapping configurations here
    }
}
```

**WRONG**:
```csharp
// Wrong: Static class
public static class CoreModuleMapperRegister
{
    public static void Register()
    {
        // Cannot implement IRegister
    }
}
```

---

## Aggregate to DTO Mapping (Domain → Model)

- [ ] Mapping configured for aggregate root → DTO model
- [ ] TypedEntityId converted to Guid: `.Map(dest => dest.Id, src => src.Id.Value)`
- [ ] Value objects converted to primitives (e.g., `EmailAddress → string`)
- [ ] Enumerations converted to strings: `.Map(dest => dest.Status, src => src.Status.Name)`
- [ ] Navigation properties mapped to child DTOs (if applicable)
- [ ] Audit properties mapped: `CreatedDate`, `CreatedBy`, `UpdatedDate`, `UpdatedBy`
- [ ] `ConcurrencyVersion` mapped for optimistic concurrency
- [ ] No domain events leaked to DTOs (`DomainEvents` ignored)

**CORRECT**:
```csharp
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Id, src => src.Id.Value)
    .Map(dest => dest.Email, src => src.Email.Value)
    .Map(dest => dest.Status, src => src.Status.Name)
    .Map(dest => dest.CreatedDate, src => src.AuditState.CreatedDate)
    .Map(dest => dest.UpdatedDate, src => src.AuditState.UpdatedDate);
```

**WRONG**:
```csharp
// Wrong: Not converting TypedEntityId
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Id, src => src.Id); // CustomerId cannot be assigned to Guid

// Wrong: Leaking domain events
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Events, src => src.DomainEvents); // Never expose domain events
```

---

## DTO to Aggregate Mapping (Model → Domain)

- [ ] Mapping configured for DTO → aggregate root (for updates)
- [ ] Uses `ConstructUsing()` to call aggregate factory method
- [ ] Factory method result unwrapped: `.Value` or `.GetValueOrDefault()`
- [ ] Does NOT map read-only properties (Id, audit fields)
- [ ] Uses aggregate's change methods in AfterMapping (if needed)
- [ ] Handles Result<T> return types from factories

**CORRECT**:
```csharp
config.NewConfig<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(
        src.FirstName,
        src.LastName,
        src.Email,
        CustomerStatus.FromName(src.Status)).Value)
    .AfterMapping((src, dest) =>
    {
        // Use aggregate's change methods if needed
        if (src.PhoneNumber != dest.PhoneNumber?.Value)
        {
            dest.ChangePhoneNumber(src.PhoneNumber);
        }
    });
```

**WRONG**:
```csharp
// Wrong: Not using factory method
config.NewConfig<CustomerModel, Customer>()
    .ConstructUsing(src => new Customer()); // Bypasses validation

// Wrong: Mapping read-only properties
config.NewConfig<CustomerModel, Customer>()
    .Map(dest => dest.Id, src => CustomerId.Create(src.Id))
    .Map(dest => dest.CreatedDate, src => src.CreatedDate); // Read-only
```

---

## Value Object Conversions

- [ ] Each value object has bidirectional mapping (→ primitive, ← primitive)
- [ ] Domain → DTO: Extract primitive via `.Value` property
- [ ] DTO → Domain: Use factory method (`.Create()`) and unwrap Result
- [ ] Null handling for optional value objects
- [ ] Complex value objects map all constituent parts

**CORRECT**:
```csharp
// EmailAddress <-> string
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Email, src => src.Email.Value);

config.NewConfig<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(
        src.FirstName,
        src.LastName,
        src.Email, // Factory will convert string → EmailAddress
        CustomerStatus.FromName(src.Status)).Value);

// Or explicit value object mapping:
config.NewConfig<string, EmailAddress>()
    .ConstructUsing(src => EmailAddress.Create(src).Value);

config.NewConfig<EmailAddress, string>()
    .MapWith(src => src.Value);
```

**WRONG**:
```csharp
// Wrong: Not converting value object
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Email, src => src.Email); // EmailAddress cannot assign to string

// Wrong: Not handling Result<T>
config.NewConfig<string, EmailAddress>()
    .ConstructUsing(src => EmailAddress.Create(src)); // Returns Result<EmailAddress>, not EmailAddress
```

---

## Enumeration Conversions

- [ ] Enumeration → string: Use `.Name` property
- [ ] String → enumeration: Use `.FromName()` or `.FromValue()` static method
- [ ] Handle case-insensitive lookups if needed
- [ ] Consider default enumeration for invalid strings
- [ ] Map additional properties (Id, Description) if DTO includes them

**CORRECT**:
```csharp
// CustomerStatus → string
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Status, src => src.Status.Name);

// string → CustomerStatus
config.NewConfig<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(
        src.FirstName,
        src.LastName,
        src.Email,
        CustomerStatus.FromName(src.Status)).Value); // FromName handles conversion
```

**WRONG**:
```csharp
// Wrong: Casting enumeration to string
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Status, src => (string)src.Status); // Invalid cast

// Wrong: Using new keyword on enumeration
config.NewConfig<CustomerModel, Customer>()
    .Map(dest => dest.Status, src => new CustomerStatus(src.Status)); // Constructor is private
```

---

## Collection Mappings

- [ ] Child entity collections mapped to DTO collections
- [ ] Collection mapping uses `.Map(dest => dest.Items, src => src.Items)`
- [ ] Individual item mappings configured (aggregate item → DTO item)
- [ ] Handles empty collections (null → empty list or vice versa)
- [ ] Consider read-only collection types (IReadOnlyCollection, IEnumerable)

**CORRECT**:
```csharp
// Parent aggregate with child collection
config.NewConfig<Order, OrderModel>()
    .Map(dest => dest.Id, src => src.Id.Value)
    .Map(dest => dest.Items, src => src.Items); // Mapster auto-maps if OrderItem -> OrderItemModel exists

// Child entity mapping
config.NewConfig<OrderItem, OrderItemModel>()
    .Map(dest => dest.Id, src => src.Id.Value)
    .Map(dest => dest.ProductName, src => src.Product.Value)
    .Map(dest => dest.Quantity, src => src.Quantity);
```

**WRONG**:
```csharp
// Wrong: Not configuring child item mapping
config.NewConfig<Order, OrderModel>()
    .Map(dest => dest.Items, src => src.Items); // OrderItem -> OrderItemModel not configured, will fail
```

---

## Null and Optional Property Handling

- [ ] Optional value objects handle null safely
- [ ] Use `?.Value` for nullable value objects
- [ ] Use `.IgnoreNullValues()` where appropriate
- [ ] Consider default values for missing data
- [ ] Map nullable primitives correctly (int? → int? not int? → int)

**CORRECT**:
```csharp
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.PhoneNumber, src => src.PhoneNumber != null ? src.PhoneNumber.Value : null);

// Or using Mapster's built-in handling:
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.Value)
    .IgnoreNullValues(true);
```

**WRONG**:
```csharp
// Wrong: NullReferenceException if PhoneNumber is null
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.Value); // Throws if PhoneNumber is null
```

---

## Special Case Mappings

- [ ] DateTime conversions (UTC, local, formatting) handled consistently
- [ ] Decimal precision maintained for monetary values
- [ ] Complex nested objects flattened or structured appropriately
- [ ] Polymorphic types handled with `.Include<TDerived>()` if needed
- [ ] Circular references avoided or configured with `.PreserveReference(true)`

**CORRECT**:
```csharp
// Flattening nested structure
config.NewConfig<Customer, CustomerModel>()
    .Map(dest => dest.Street, src => src.Address.Street)
    .Map(dest => dest.City, src => src.Address.City);

// Preserving references for circular relationships
config.NewConfig<Order, OrderModel>()
    .PreserveReference(true);
```

---

## Registration and Service Configuration

- [ ] MapperRegister registered in module startup: `services.AddMapping().WithMapster<[Module]MapperRegister>()`
- [ ] IMapper injected into handlers that need mapping
- [ ] Global Mapster config not modified outside IRegister implementation
- [ ] No static TypeAdapterConfig.GlobalSettings usage

**CORRECT** (in Module registration):
```csharp
public static IServiceCollection AddCoreModule(this IServiceCollection services, IConfiguration configuration)
{
    // ... other services ...
    
    services.AddMapping().WithMapster<CoreModuleMapperRegister>();
    
    return services;
}
```

**WRONG**:
```csharp
// Wrong: Modifying global config directly
public void Register(TypeAdapterConfig config)
{
    TypeAdapterConfig.GlobalSettings.Default.PreserveReference(true); // Don't modify global
}
```

---

## Testing Mapping Configurations

- [ ] Unit test verifies aggregate → DTO mapping
- [ ] Unit test verifies DTO → aggregate mapping (via factory)
- [ ] Test covers all properties (no unmapped properties)
- [ ] Test validates value object conversions
- [ ] Test validates enumeration conversions
- [ ] Test validates collection mappings
- [ ] Use `Adapt<TDestination>()` in tests to invoke Mapster

**CORRECT**:
```csharp
[Fact]
public void Should_Map_Customer_To_CustomerModel()
{
    // Arrange
    var customer = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active).Value;
    
    // Act
    var model = customer.Adapt<CustomerModel>();
    
    // Assert
    model.Id.ShouldBe(customer.Id.Value);
    model.FirstName.ShouldBe("John");
    model.LastName.ShouldBe("Doe");
    model.Email.ShouldBe("john@example.com");
    model.Status.ShouldBe("Active");
}

[Fact]
public void Should_Map_CustomerModel_To_Customer()
{
    // Arrange
    var model = new CustomerModel
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john@example.com",
        Status = "Active"
    };
    
    // Act
    var customer = model.Adapt<Customer>();
    
    // Assert
    customer.FirstName.ShouldBe("John");
    customer.Email.Value.ShouldBe("john@example.com");
    customer.Status.ShouldBe(CustomerStatus.Active);
}
```

---

## Common Mapping Anti-Patterns to Avoid

- [ ] **Primitive Obsession**: Not converting value objects to/from primitives
- [ ] **Leaking Domain Complexity**: Exposing DomainEvents, internal collections, or private state to DTOs
- [ ] **Bypassing Validation**: Using constructors instead of factory methods when mapping DTO → Domain
- [ ] **Ignoring Result<T>**: Not unwrapping Result<T> from factory methods (causes compile errors)
- [ ] **Bidirectional Inconsistency**: Having Domain → DTO work but not DTO → Domain (or vice versa)
- [ ] **Mapping Read-Only Properties**: Attempting to map Id, CreatedDate, UpdatedDate from DTO to Domain
- [ ] **Static Configuration**: Modifying TypeAdapterConfig.GlobalSettings instead of using IRegister
- [ ] **Null Handling Omission**: Not handling nullable value objects safely (NullReferenceException risk)

---

## Final Review

Before proceeding to the Presentation layer:

- [ ] All aggregate → DTO mappings compile without errors
- [ ] All DTO → domain mappings use factory methods and unwrap Results
- [ ] TypedEntityIds converted to/from Guid correctly
- [ ] Value objects converted to/from primitives correctly
- [ ] Enumerations converted to/from strings correctly
- [ ] Collections mapped bidirectionally (if applicable)
- [ ] Null handling tested for optional properties
- [ ] Mapping tests pass (unit tests)
- [ ] IMapper can be injected successfully into handlers
- [ ] No compiler warnings related to mapping code

---

## References

- **Customer Mapping Example**: `src/Modules/CoreModule/CoreModule.Presentation/CoreModuleMapperRegister.cs` (lines 1-108)
- **Value Object Pattern**: `.github/skills/domain-add-aggregate/examples/value-object-patterns.md`
- **Mapping Patterns Guide**: `.github/skills/domain-add-aggregate/examples/mapping-patterns.md`
- **Mapster Documentation**: [Mapster GitHub](https://github.com/MapsterMapper/Mapster)

---

**Next Checklist**: 05-presentation-layer.md (Minimal API Endpoints)
