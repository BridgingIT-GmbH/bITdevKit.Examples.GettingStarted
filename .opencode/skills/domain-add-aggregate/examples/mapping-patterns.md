# Mapping Patterns with Mapster

This document explains mapping patterns using Mapster in the bITdevKit GettingStarted example, demonstrating configurations, conversions, and best practices for domain ↔ DTO transformations.

## Why Use Mapster?

**Mapster** is a high-performance object mapping library for .NET that:
- Generates mapping code at compile-time (via source generators)
- Provides fluent configuration API
- Supports complex scenarios (nested objects, collections, custom conversions)
- Integrates seamlessly with bITdevKit patterns

**Alternative**: AutoMapper (reflection-based, slower, more mature ecosystem)

**Project Choice**: Mapster for performance and clean configuration syntax

## CoreModuleMapperRegister (Reference Implementation)

**Location**: `src/Modules/CoreModule/CoreModule.Presentation/CoreModuleMapperRegister.cs` (lines 1-108)

```csharp
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

using Mapster;

public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Aggregate ↔ DTO mappings
        ConfigureCustomerMappings(config);
        
        // Value object conversions
        ConfigureValueObjectMappings(config);
        
        // Enumeration conversions
        ConfigureEnumerationMappings(config);
    }
}
```

## Registration in Startup

```csharp
// In ModuleExtensions or module registration:
services.AddMapping().WithMapster<CoreModuleMapperRegister>();
```

**What Happens**:
1. Mapster discovers all `IRegister` implementations
2. Calls `Register()` on each to build configuration
3. Registers `IMapper` abstraction in DI container
4. Mapping code generated at compile-time (if using source generators)

## Pattern 1: Aggregate → DTO Mapping

### Customer → CustomerModel (Domain → DTO)

```csharp
config.ForType<Customer, CustomerModel>()
    // Map typed ID to string
    .Map(dest => dest.Id, 
         src => src.Id.Value.ToString())
    
    // Map concurrency version Guid → string
    .Map(dest => dest.ConcurrencyVersion, 
         src => src.ConcurrencyVersion.ToString())
    
    // Map value object to primitive
    .Map(dest => dest.Email, 
         src => src.Email.Value)
    
    // Map enumeration to string
    .Map(dest => dest.Status, 
         src => src.Status.Value)
    
    // Map child collection (automatic if configured)
    .Map(dest => dest.Addresses, 
         src => src.Addresses)
    
    // Ignore null values
    .IgnoreNullValues(true);
```

**What Gets Mapped Automatically**:
- Properties with same name and compatible types
- Example: `src.FirstName` → `dest.FirstName`

**What Needs Explicit Configuration**:
- Type conversions (Guid → string, value objects → primitives)
- Custom transformations (formatting, calculations)
- Nested objects (if custom mapping needed)

### Usage in Handler

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await repository
        .FindOneResultAsync(customerId, cancellationToken)
        .MapResult<Customer, CustomerModel>(mapper);
}
```

**MapResult Extension**:
```csharp
public static Result<TDest> MapResult<TSource, TDest>(
    this Result<TSource> result, 
    IMapper mapper)
{
    return result.IsSuccess
        ? Result<TDest>.Success(mapper.Map<TSource, TDest>(result.Value))
        : Result<TDest>.Failure(result.Errors);
}
```

## Pattern 2: DTO → Aggregate Mapping

### CustomerModel → Customer (DTO → Domain)

```csharp
config.ForType<CustomerModel, Customer>()
    // Reconstruct aggregate using factory method
    .ConstructUsing(src => Customer.Create(
        src.FirstName,
        src.LastName,
        src.Email,
        src.Number).Value) // NOTE: Assumes valid data (validation in command validators)
    
    // Map concurrency version string → Guid
    .Map(dest => dest.ConcurrencyVersion,
         src => src.ConcurrencyVersion != null 
             ? Guid.Parse(src.ConcurrencyVersion) 
             : Guid.Empty)
    
    // Ignore child collections (managed via aggregate methods)
    .Ignore(dest => dest.Addresses)
    
    .IgnoreNullValues(true);
```

**Why ConstructUsing?**:
- Aggregates should be created via factory methods (enforces validation)
- Direct property mapping bypasses domain invariants
- Factory ensures aggregate is in valid state from construction

**Why Ignore Addresses?**:
- Child collections managed via aggregate methods: `AddAddress()`, `RemoveAddress()`
- Prevents mapping from overwriting collection directly
- Handler applies address changes explicitly after mapping

### Handler Pattern for Child Collections

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await repository
        .FindOneResultAsync(customerId, cancellationToken)
        
        // Apply changes to aggregate
        .Bind(customer => customer.ChangeName(request.Model.FirstName, request.Model.LastName))
        .Bind(customer => customer.ChangeEmail(request.Model.Email))
        
        // Process address changes separately
        .Bind(customer => this.ProcessAddressChanges(customer, request.Model.Addresses))
        
        // Persist
        .BindAsync(async (customer, ct) => 
            await repository.UpdateResultAsync(customer, ct), cancellationToken)
        
        // Map back to DTO
        .MapResult<Customer, CustomerModel>(mapper);
}

private Result<Customer> ProcessAddressChanges(
    Customer customer, 
    List<CustomerAddressModel> requestAddresses)
{
    // Remove addresses not in request
    // Add new addresses
    // Update existing addresses
    // Return Result<Customer>
}
```

## Pattern 3: Value Object Conversions

### EmailAddress ↔ string

```csharp
// EmailAddress → string (for DTO output)
config.NewConfig<EmailAddress, string>()
    .MapWith(src => src.Value);

// string → EmailAddress (for DTO input)
config.NewConfig<string, EmailAddress>()
    .MapWith(src => EmailAddress.Create(src).Value);
```

**MapWith**: Lambda function for simple conversions

**Usage**:
```csharp
// Automatic conversion in aggregate mapping
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.Email, src => src.Email); // Uses EmailAddress → string config
```

### CustomerNumber ↔ string

```csharp
config.NewConfig<CustomerNumber, string>()
    .MapWith(src => src.Value);

config.NewConfig<string, CustomerNumber>()
    .MapWith(src => CustomerNumber.Create(src).Value);
```

## Pattern 4: Enumeration Conversions

### Generic Enumeration Converter

```csharp
private static void RegisterEnumerationConverter<T>(TypeAdapterConfig config)
    where T : Enumeration
{
    // Enumeration → string (store Value for transport/DTO output)
    config.NewConfig<T, string>()
        .MapWith(src => src.Value);
    
    // string → Enumeration (reconstruct from Value)
    config.NewConfig<string, T>()
        .MapWith(src => Enumeration.GetAll<T>()
            .FirstOrDefault(x => x.Value == src));
}
```

**Usage**:
```csharp
public void Register(TypeAdapterConfig config)
{
    RegisterEnumerationConverter<CustomerStatus>(config);
    RegisterEnumerationConverter<OrderStatus>(config);
}
```

**Why Generic**:
- Reusable for all enumeration types
- Consistent conversion logic
- Type-safe

### Alternative: Enumeration → int

```csharp
// For compact API representation or database storage
config.NewConfig<CustomerStatus, int>()
    .MapWith(src => src.Id);

config.NewConfig<int, CustomerStatus>()
    .MapWith(src => Enumeration.FromId<CustomerStatus>(src));
```

## Pattern 5: Child Collection Mappings

### Address → CustomerAddressModel

```csharp
config.ForType<Address, CustomerAddressModel>()
    .Map(dest => dest.Id, 
         src => src.Id.Value.ToString())
    .IgnoreNullValues(true);
```

### CustomerAddressModel → Address

```csharp
config.ForType<CustomerAddressModel, Address>()
    .ConstructUsing(src => Address.Create(
        src.Name,
        src.Line1,
        src.Line2,
        src.PostalCode,
        src.City,
        src.Country,
        src.IsPrimary).Value)
    .IgnoreNullValues(true);
```

### Automatic Collection Mapping

```csharp
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.Addresses, src => src.Addresses);
    // Mapster automatically maps IEnumerable<Address> → List<CustomerAddressModel>
    // Using Address → CustomerAddressModel configuration
```

## Pattern 6: Conditional Mapping

### Map Property Only When Condition Met

```csharp
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.StatusName, 
         src => src.Status != null ? src.Status.Value : null)
    .When(src => src.Status != null);
```

**Usage Scenario**: Optional properties, nullable enumerations

### Map Different Values Based on Condition

```csharp
config.ForType<Order, OrderModel>()
    .Map(dest => dest.StatusDisplay, 
         src => src.Status.Id == 1 ? "New Order" :
                src.Status.Id == 2 ? "Processing" :
                src.Status.Id == 3 ? "Shipped" : "Unknown");
```

## Pattern 7: Flattening Nested Objects

### Nested Domain Model

```csharp
public class Customer
{
    public PersonName Name { get; set; } // Value object with FirstName, LastName
    public Address PrimaryAddress { get; set; }
}
```

### Flat DTO

```csharp
public class CustomerModel
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string AddressCity { get; set; }
    public string AddressCountry { get; set; }
}
```

### Mapping Configuration

```csharp
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.FirstName, src => src.Name.FirstName)
    .Map(dest => dest.LastName, src => src.Name.LastName)
    .Map(dest => dest.AddressCity, src => src.PrimaryAddress.City)
    .Map(dest => dest.AddressCountry, src => src.PrimaryAddress.Country);
```

## Pattern 8: Multiple DTO Variants

### Same Aggregate, Different DTOs

```csharp
// Minimal DTO for list views
public class CustomerSummaryModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Full DTO for detail views
public class CustomerDetailModel
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public List<CustomerAddressModel> Addresses { get; set; }
    public List<OrderModel> Orders { get; set; }
}
```

### Mapping Configurations

```csharp
// Customer → CustomerSummaryModel (minimal)
config.ForType<Customer, CustomerSummaryModel>()
    .Map(dest => dest.Id, src => src.Id.Value.ToString())
    .Map(dest => dest.Name, src => $"{src.FirstName} {src.LastName}")
    .Map(dest => dest.Email, src => src.Email.Value);

// Customer → CustomerDetailModel (full)
config.ForType<Customer, CustomerDetailModel>()
    .Map(dest => dest.Id, src => src.Id.Value.ToString())
    .Map(dest => dest.Email, src => src.Email.Value)
    .Map(dest => dest.Addresses, src => src.Addresses);
    // ... additional mappings
```

## Pattern 9: After Mapping Hook

### Execute Custom Logic After Mapping

```csharp
config.ForType<Customer, CustomerModel>()
    .AfterMapping((src, dest) =>
    {
        // Calculate derived properties
        dest.FullName = $"{dest.FirstName} {dest.LastName}";
        dest.IsActive = dest.Status == "Active";
        
        // Sanitize data
        dest.Email = dest.Email?.ToLowerInvariant();
        
        // Set defaults
        if (string.IsNullOrEmpty(dest.ConcurrencyVersion))
        {
            dest.ConcurrencyVersion = Guid.NewGuid().ToString();
        }
    });
```

**Use Cases**:
- Calculated properties
- Data sanitization
- Setting default values
- Logging/auditing

## Pattern 10: Two-Way Mapping Helper

### Extension Method for Bidirectional Configuration

```csharp
public static class MapsterExtensions
{
    public static void RegisterTwoWay<TSource, TDest>(
        this TypeAdapterConfig config,
        Action<TypeAdapterSetter<TSource, TDest>> forwardConfig = null,
        Action<TypeAdapterSetter<TDest, TSource>> reverseConfig = null)
    {
        var forward = config.ForType<TSource, TDest>();
        forwardConfig?.Invoke(forward);
        
        var reverse = config.ForType<TDest, TSource>();
        reverseConfig?.Invoke(reverse);
    }
}
```

### Usage

```csharp
config.RegisterTwoWay<EmailAddress, string>(
    forward: setter => setter.MapWith(src => src.Value),
    reverse: setter => setter.MapWith(src => EmailAddress.Create(src).Value)
);
```

## Usage in Handlers

### Query Handler (Domain → DTO)

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await repository
        .FindOneResultAsync(customerId, cancellationToken)
        .MapResult<Customer, CustomerModel>(mapper);
}
```

### Command Handler (DTO → Domain → DTO)

```csharp
protected override async Task<Result<CustomerModel>> HandleAsync(...)
{
    return await Customer
        .Create(
            request.Model.FirstName,
            request.Model.LastName,
            request.Model.Email,
            generatedNumber)
        .BindAsync(async (customer, ct) => 
            await repository.InsertResultAsync(customer, ct))
        .MapResult<Customer, CustomerModel>(mapper);
}
```

### Collection Mapping

```csharp
protected override async Task<Result<IEnumerable<CustomerModel>>> HandleAsync(...)
{
    return await repository
        .FindAllResultAsync(filter, cancellationToken)
        .Map(customers => mapper.Map<Customer, CustomerModel>(customers));
        // Mapster automatically handles IEnumerable mapping
}
```

## Testing Mappings

### Unit Tests

```csharp
public class MapperTests
{
    private readonly IMapper mapper;
    
    public MapperTests()
    {
        var config = new TypeAdapterConfig();
        new CoreModuleMapperRegister().Register(config);
        this.mapper = new Mapper(config);
    }
    
    [Fact]
    public void Map_CustomerToModel_ShouldMapAllProperties()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john@example.com", "CUS-001").Value;
        
        // Act
        var model = this.mapper.Map<Customer, CustomerModel>(customer);
        
        // Assert
        model.Id.ShouldBe(customer.Id.Value.ToString());
        model.FirstName.ShouldBe("John");
        model.LastName.ShouldBe("Doe");
        model.Email.ShouldBe("john@example.com");
    }
    
    [Fact]
    public void Map_ModelToCustomer_ShouldUseFactory()
    {
        // Arrange
        var model = new CustomerModel
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Number = "CUS-001"
        };
        
        // Act
        var customer = this.mapper.Map<CustomerModel, Customer>(model);
        
        // Assert
        customer.FirstName.ShouldBe("John");
        customer.Email.Value.ShouldBe("john@example.com");
        customer.Status.ShouldBe(CustomerStatus.Lead); // Default from factory
    }
}
```

## Performance Considerations

### Mapster Performance

**Compile-Time Code Generation**:
- Mapster generates IL code at runtime (or compile-time with source generators)
- No reflection overhead after initial configuration
- Performance comparable to hand-written mapping code

**Benchmarks** (relative to hand-written code):
- Simple mappings: ~1.0x (nearly identical)
- Complex mappings: ~1.1x (minimal overhead)
- AutoMapper (reflection): ~10x slower

### Optimization Tips

1. **Use Source Generators** (if available):
   ```xml
   <PackageReference Include="Mapster.Tool" Version="..." />
   ```

2. **Compile Configuration Once**:
   ```csharp
   // BAD: Creates new config every call
   var mapper = new Mapper(new TypeAdapterConfig());
   
   // GOOD: Reuse singleton config
   services.AddSingleton(TypeAdapterConfig.GlobalSettings);
   ```

3. **Avoid Excessive AfterMapping**:
   - Prefer mapping expressions over AfterMapping hooks
   - AfterMapping adds delegate invocation overhead

4. **Cache Compiled Adapters** (for hot paths):
   ```csharp
   private static readonly Func<Customer, CustomerModel> adapter = 
       TypeAdapter<Customer, CustomerModel>.Map;
   ```

## Common Pitfalls

### 1. Forgetting to Register Configuration

```csharp
// WRONG: Configuration not registered
var config = new TypeAdapterConfig();
// ... configurations ...
var mapper = new Mapper(config); // Using local config, not registered

// CORRECT: Register in DI
services.AddMapping().WithMapster<CoreModuleMapperRegister>();
```

### 2. Circular References

```csharp
// Customer has Orders
// Order has Customer
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.Orders, src => src.Orders); // Circular!

// Solution: Ignore one direction or use MaxDepth
config.ForType<Customer, CustomerModel>()
    .Map(dest => dest.Orders, src => src.Orders)
    .MaxDepth(2); // Limit recursion depth
```

### 3. Null Reference in Factory

```csharp
// WRONG: May throw NullReferenceException
config.ForType<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(
        src.FirstName,
        src.LastName,
        src.Email, // Could be null!
        src.Number).Value); // .Value will throw if Result is failure

// BETTER: Handle nulls
config.ForType<CustomerModel, Customer>()
    .ConstructUsing(src => Customer.Create(
        src.FirstName ?? "",
        src.LastName ?? "",
        src.Email ?? "",
        src.Number ?? "").Value);

// BEST: Let validation handle it (Result pattern)
```

## Summary

**Mapster Mapping Checklist**:
- [ ] IRegister implementation for module
- [ ] Aggregate → DTO mappings (domain to external)
- [ ] DTO → Aggregate mappings (with ConstructUsing factory)
- [ ] Value object conversions (↔ primitives)
- [ ] Enumeration conversions (↔ strings or ints)
- [ ] Child collection mappings (if applicable)
- [ ] Registered in DI container
- [ ] Unit tests for critical mappings
- [ ] Performance profiling (if hot path)

**Key Principles**:
- Use factories for aggregate construction (ConstructUsing)
- Ignore child collections in DTO → Aggregate (manage via methods)
- Keep mapping configuration close to presentation layer
- Test mappings to catch configuration errors early
- Use Result pattern to handle mapping failures gracefully
