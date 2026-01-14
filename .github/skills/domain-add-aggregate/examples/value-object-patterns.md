# Value Object Patterns

This document explains value object patterns used in the bITdevKit GettingStarted example, demonstrating implementation techniques, validation strategies, and usage patterns.

## What is a Value Object?

A **value object** is a domain concept that:
- Has no conceptual identity (defined by its properties, not an ID)
- Is immutable (cannot change after creation)
- Enforces business rules and invariants
- Provides type safety over primitive obsession

**Example**: `EmailAddress` is a value object, not just a `string`. Two email addresses with the same value are considered equal, regardless of which instance you're comparing.

## Why Use Value Objects?

### Problem: Primitive Obsession

```csharp
// BAD: Primitive obsession
public class Customer
{
    public string Email { get; set; } // Any string accepted
    public string PhoneNumber { get; set; } // No validation
    public decimal Price { get; set; } // Could be negative
}

// Validation scattered everywhere:
if (string.IsNullOrEmpty(email)) throw new Exception("Email required");
if (!email.Contains('@')) throw new Exception("Invalid email");
if (price < 0) throw new Exception("Price cannot be negative");
```

### Solution: Value Objects

```csharp
// GOOD: Value objects encapsulate validation
public class Customer
{
    public EmailAddress Email { get; private set; } // Type-safe, always valid
    public PhoneNumber Phone { get; private set; } // Type-safe, always valid
    public Money Price { get; private set; } // Type-safe, always valid
}

// Validation happens once at creation:
var email = EmailAddress.Create("john@example.com"); // Returns Result<EmailAddress>
if (email.IsSuccess)
{
    // Email is guaranteed to be valid
}
```

## EmailAddress Value Object (Reference Implementation)

**Location**: `src/Modules/CoreModule/CoreModule.Domain/Model/EmailAddress.cs` (lines 1-96)

```csharp
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Value object representing an email address with validation.
/// </summary>
public class EmailAddress : ValueObject
{
    // Property: Read-only, set once in constructor
    public string Value { get; private set; }

    // Private constructor: Forces use of Create factory
    private EmailAddress(string value)
    {
        this.Value = value;
    }

    // Factory method: Validates and creates instance
    public static Result<EmailAddress> Create(string value)
    {
        return Result<EmailAddress>
            .Ensure(() => !string.IsNullOrWhiteSpace(value),
                new ValidationError("Email address cannot be empty", "Email"))
            .Ensure(() => value?.Contains('@') == true,
                new ValidationError("Email address must contain @", "Email"))
            .Ensure(() => value?.Length <= 256,
                new ValidationError("Email address too long", "Email"))
            .Map(() => new EmailAddress(value.ToLowerInvariant()));
    }

    // Implicit conversion: EmailAddress → string
    public static implicit operator string(EmailAddress email) => email?.Value;

    // Equality: Based on value, not reference
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    // String representation
    public override string ToString() => this.Value;
}
```

### Key Patterns

#### 1. ValueObject Base Class

```csharp
public class EmailAddress : ValueObject
```

**Provides**:
- `Equals()`: Value-based equality (not reference equality)
- `GetHashCode()`: Hash based on atomic values
- `==` and `!=` operators

**Implementation**:
```csharp
protected override IEnumerable<object> GetAtomicValues()
{
    yield return this.Value; // Properties used for equality
}
```

**Behavior**:
```csharp
var email1 = EmailAddress.Create("john@example.com").Value;
var email2 = EmailAddress.Create("john@example.com").Value;

email1 == email2; // TRUE (value equality)
email1.Equals(email2); // TRUE
```

#### 2. Private Constructor + Factory Method

```csharp
// Private: Cannot instantiate directly
private EmailAddress(string value) { ... }

// Public: Only way to create instance
public static Result<EmailAddress> Create(string value) { ... }
```

**Why This Matters**:
- **Enforces validation**: Cannot create invalid email addresses
- **Single source of truth**: Validation logic in one place
- **Result pattern**: Returns errors without throwing exceptions

**Usage**:
```csharp
// CORRECT: Using factory method
var result = EmailAddress.Create("john@example.com");
if (result.IsSuccess)
{
    var email = result.Value; // Guaranteed valid
}

// WRONG: Cannot compile
var email = new EmailAddress("john@example.com"); // Compiler error: constructor is private
```

#### 3. Result Pattern for Validation

```csharp
return Result<EmailAddress>
    .Ensure(() => !string.IsNullOrWhiteSpace(value),
        new ValidationError("Email address cannot be empty", "Email"))
    .Ensure(() => value?.Contains('@') == true,
        new ValidationError("Email address must contain @", "Email"))
    .Map(() => new EmailAddress(value.ToLowerInvariant()));
```

**Flow**:
```
Input: "john@example.com"
  ↓
Ensure: not empty → PASS
  ↓
Ensure: contains @ → PASS
  ↓
Map: new EmailAddress(value.ToLowerInvariant())
  ↓
Result<EmailAddress>.Success(email)
```

**Invalid Input**:
```
Input: "invalid"
  ↓
Ensure: not empty → PASS
  ↓
Ensure: contains @ → FAIL
  ↓
Result<EmailAddress>.Failure(ValidationError("Email address must contain @"))
```

#### 4. Normalization

```csharp
.Map(() => new EmailAddress(value.ToLowerInvariant()));
```

**Why**:
- Ensures consistency: `John@Example.com` → `john@example.com`
- Simplifies comparisons: No case-sensitivity issues
- Database uniqueness: Email columns can use case-sensitive collation

#### 5. Implicit Conversion Operator

```csharp
public static implicit operator string(EmailAddress email) => email?.Value;
```

**Usage**:
```csharp
EmailAddress email = EmailAddress.Create("john@example.com").Value;

// Implicit conversion to string
string emailString = email; // Works automatically

// Can use in string contexts
Console.WriteLine($"Email: {email}"); // Calls ToString() implicitly
```

**Why**:
- **Convenience**: No need to write `.Value` everywhere
- **String interpolation**: Works seamlessly in templates
- **Method arguments**: Can pass EmailAddress where string expected (in some cases)

**Caution**: Overuse can lead to loss of type safety. Use judiciously.

## CustomerNumber Value Object (Simplified Example)

**Location**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/CustomerNumber.cs`

```csharp
/// <summary>
/// Value object representing a customer number in format CUS-YYYY-NNNNNN.
/// Example: CUS-2024-100000
/// </summary>
public class CustomerNumber : ValueObject
{
    private static readonly Regex FormatRegex = new(@"^CUS-\d{4}-\d{6}$", RegexOptions.Compiled);

    public string Value { get; private set; }

    private CustomerNumber(string value)
    {
        this.Value = value;
    }

    // Factory: Create from full string
    public static Result<CustomerNumber> Create(string value)
    {
        return Result<CustomerNumber>
            .Ensure(() => !string.IsNullOrWhiteSpace(value),
                new ValidationError("Customer number cannot be empty", "CustomerNumber"))
            .Ensure(() => FormatRegex.IsMatch(value),
                new ValidationError("Customer number must match format CUS-YYYY-NNNNNN", "CustomerNumber"))
            .Map(() => new CustomerNumber(value));
    }

    // Factory: Create from year and sequence
    public static Result<CustomerNumber> Create(DateTime date, long sequence)
    {
        var value = $"CUS-{date.Year:0000}-{sequence:000000}";
        return Create(value); // Reuse validation
    }

    public static implicit operator string(CustomerNumber number) => number?.Value;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    public override string ToString() => this.Value;
}
```

### Advanced Patterns

#### 1. Multiple Factory Methods

```csharp
// From full string
CustomerNumber.Create("CUS-2024-100000")

// From components
CustomerNumber.Create(DateTime.UtcNow, 100000)
```

**Why**: Provides flexibility for different creation scenarios while maintaining single validation logic.

#### 2. Regular Expression Validation

```csharp
private static readonly Regex FormatRegex = new(@"^CUS-\d{4}-\d{6}$", RegexOptions.Compiled);

.Ensure(() => FormatRegex.IsMatch(value), ...)
```

**Why**:
- **Format enforcement**: Ensures consistent format across system
- **Database constraints**: Can add CHECK constraints matching regex
- **Client validation**: Same regex can be used in UI validation

#### 3. Extracting Components

```csharp
public int Year => int.Parse(this.Value.Substring(4, 4));
public long Sequence => long.Parse(this.Value.Substring(9, 6));
```

**Usage**:
```csharp
var number = CustomerNumber.Create("CUS-2024-100000").Value;
Console.WriteLine($"Year: {number.Year}"); // 2024
Console.WriteLine($"Sequence: {number.Sequence}"); // 100000
```

## Value Object Categories

### 1. Simple Value Objects (Single Property)

**Examples**: `EmailAddress`, `PhoneNumber`, `Url`, `IpAddress`

```csharp
public class PhoneNumber : ValueObject
{
    public string Value { get; private set; }
    
    private PhoneNumber(string value) => this.Value = value;
    
    public static Result<PhoneNumber> Create(string value)
    {
        return Result<PhoneNumber>
            .Ensure(() => !string.IsNullOrWhiteSpace(value), ...)
            .Ensure(() => Regex.IsMatch(value, @"^\+?[1-9]\d{1,14}$"), ...)
            .Map(() => new PhoneNumber(value));
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

### 2. Composite Value Objects (Multiple Properties)

**Examples**: `Address`, `Money`, `DateRange`, `GeoCoordinate`

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    private Money(decimal amount, string currency)
    {
        this.Amount = amount;
        this.Currency = currency;
    }
    
    public static Result<Money> Create(decimal amount, string currency)
    {
        return Result<Money>
            .Ensure(() => amount >= 0, new ValidationError("Amount cannot be negative"))
            .Ensure(() => !string.IsNullOrWhiteSpace(currency), ...)
            .Ensure(() => currency.Length == 3, new ValidationError("Currency must be 3-letter ISO code"))
            .Map(() => new Money(amount, currency.ToUpperInvariant()));
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Amount;
        yield return this.Currency;
    }
    
    public Money Add(Money other)
    {
        if (this.Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        
        return Create(this.Amount + other.Amount, this.Currency).Value;
    }
}
```

**Usage**:
```csharp
var price1 = Money.Create(100.00m, "USD").Value;
var price2 = Money.Create(50.00m, "USD").Value;
var total = price1.Add(price2); // $150.00 USD
```

### 3. Formatted Value Objects (With Parsing)

**Examples**: `CustomerNumber`, `OrderNumber`, `Sku`, `Ean`

```csharp
public class Sku : ValueObject
{
    private static readonly Regex Format = new(@"^[A-Z]{3}-\d{6}$");
    
    public string Value { get; private set; }
    public string Prefix => this.Value.Substring(0, 3);
    public int Number => int.Parse(this.Value.Substring(4, 6));
    
    private Sku(string value) => this.Value = value;
    
    public static Result<Sku> Create(string value)
    {
        return Result<Sku>
            .Ensure(() => Format.IsMatch(value), ...)
            .Map(() => new Sku(value.ToUpperInvariant()));
    }
    
    public static Result<Sku> Create(string prefix, int number)
    {
        var value = $"{prefix.ToUpperInvariant()}-{number:000000}";
        return Create(value);
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

## EF Core Persistence

### Value Object Conversion

```csharp
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Map EmailAddress value object → string in database
        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasConversion(
                email => email.Value,                      // When saving
                value => EmailAddress.Create(value).Value) // When loading
            .HasColumnName("Email");
    }
}
```

**Database Schema**:
```sql
CREATE TABLE Customers (
    Id uniqueidentifier NOT NULL,
    Email nvarchar(256) NOT NULL,
    -- Email stored as string, mapped to EmailAddress in domain
    ...
);
```

**Important**: When loading from database, we call `.Value` on the Result. This assumes database data is valid (enforced by CHECK constraints or application-level validation on write).

### Composite Value Object (Owned Type)

```csharp
builder.OwnsOne(e => e.Address, addressBuilder =>
{
    addressBuilder.Property(a => a.Street).HasMaxLength(256);
    addressBuilder.Property(a => a.City).HasMaxLength(100);
    addressBuilder.Property(a => a.PostalCode).HasMaxLength(20);
});
```

**Database Schema**:
```sql
CREATE TABLE Customers (
    Id uniqueidentifier NOT NULL,
    Address_Street nvarchar(256),
    Address_City nvarchar(100),
    Address_PostalCode nvarchar(20),
    ...
);
```

## Mapster Mapping (DTO Conversion)

### Value Object ↔ String

```csharp
public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // EmailAddress → string (for DTO output)
        config.NewConfig<EmailAddress, string>()
            .MapWith(src => src.Value);
        
        // string → EmailAddress (for DTO input)
        config.NewConfig<string, EmailAddress>()
            .MapWith(src => EmailAddress.Create(src).Value);
    }
}
```

**Usage in Handler**:
```csharp
// Domain → DTO (EmailAddress → string)
var dto = mapper.Map<Customer, CustomerModel>(customer);
// dto.Email is string

// DTO → Domain (string → EmailAddress)
var customer = mapper.Map<CustomerModel, Customer>(dto);
// customer.Email is EmailAddress
```

## Common Value Object Examples

### 1. Name

```csharp
public class PersonName : ValueObject
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    
    private PersonName(string firstName, string lastName)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
    }
    
    public static Result<PersonName> Create(string firstName, string lastName)
    {
        return Result<PersonName>
            .Ensure(() => !string.IsNullOrWhiteSpace(firstName), ...)
            .Ensure(() => !string.IsNullOrWhiteSpace(lastName), ...)
            .Ensure(() => firstName.Length <= 50, ...)
            .Ensure(() => lastName.Length <= 50, ...)
            .Map(() => new PersonName(firstName.Trim(), lastName.Trim()));
    }
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.FirstName;
        yield return this.LastName;
    }
}
```

### 2. Percentage

```csharp
public class Percentage : ValueObject
{
    public decimal Value { get; private set; }
    
    private Percentage(decimal value) => this.Value = value;
    
    public static Result<Percentage> Create(decimal value)
    {
        return Result<Percentage>
            .Ensure(() => value >= 0, new ValidationError("Percentage cannot be negative"))
            .Ensure(() => value <= 100, new ValidationError("Percentage cannot exceed 100"))
            .Map(() => new Percentage(value));
    }
    
    public decimal AsDecimal() => this.Value / 100m;
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

### 3. Quantity

```csharp
public class Quantity : ValueObject
{
    public int Value { get; private set; }
    
    private Quantity(int value) => this.Value = value;
    
    public static Result<Quantity> Create(int value)
    {
        return Result<Quantity>
            .Ensure(() => value > 0, new ValidationError("Quantity must be positive"))
            .Map(() => new Quantity(value));
    }
    
    public Quantity Add(Quantity other) => 
        Create(this.Value + other.Value).Value;
    
    public Quantity Subtract(Quantity other) =>
        Create(this.Value - other.Value).Value; // May fail if result is negative
    
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

## Testing Value Objects

```csharp
public class EmailAddressTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        // Act
        var result = EmailAddress.Create("john@example.com");
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe("john@example.com");
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_WithEmptyEmail_ShouldFail(string email)
    {
        // Act
        var result = EmailAddress.Create(email);
        
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("cannot be empty"));
    }
    
    [Fact]
    public void Create_WithoutAtSign_ShouldFail()
    {
        // Act
        var result = EmailAddress.Create("invalid");
        
        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("must contain @"));
    }
    
    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("john@example.com").Value;
        var email2 = EmailAddress.Create("john@example.com").Value;
        
        // Assert
        (email1 == email2).ShouldBeTrue();
        email1.Equals(email2).ShouldBeTrue();
        email1.GetHashCode().ShouldBe(email2.GetHashCode());
    }
}
```

## Summary

**Value Objects Checklist**:
- [ ] Inherit from `ValueObject` base class
- [ ] Private constructor + public factory method
- [ ] Factory returns `Result<T>` with validation
- [ ] Implement `GetAtomicValues()` for equality
- [ ] Properties have private setters (immutability)
- [ ] Validation rules in factory method
- [ ] Normalization applied (e.g., ToLowerInvariant)
- [ ] Implicit operator (optional, for convenience)
- [ ] EF Core conversion configured
- [ ] Mapster mapping configured
- [ ] Unit tests for creation and validation

**Benefits**:
- Type safety: Cannot use invalid values
- Encapsulation: Validation in one place
- Ubiquitous language: Domain concepts explicit
- Immutability: Thread-safe, predictable
- Equality: Value-based comparison
- Testability: Easy to test in isolation
