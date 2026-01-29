---
name: value-object-creator
description: Guide for creating domain value objects with validation, equality, and Result<T> creation in bITdevKit projects. Use this when asked to implement value objects like EmailAddress or CustomerNumber with Rule-based validation.
---

# Value Object Creator

## Overview

This skill guides the creation of domain value objects in bITdevKit projects, following DDD principles with proper validation using Rules, `Result<T>` for creation, and equality implementation.

A DDD Value Object represents a concept defined by its attributes, not by an identity.
It is immutable, self-validating, and compared by value, meaning two value objects with the same data are considered equal.
Value Objects encapsulate domain rules, ensure consistency, and make the model more expressive by embedding behavior with the data it operates on.

Examples: Money, EmailAddress, DateRange.

## When to Use This Skill

Use this skill when:

- Creating new value objects that need validation and type safety
- Implementing domain primitives like EmailAddress, CustomerNumber, or Money
- Adding business rules to simple types
- Ensuring immutability and proper equality for domain concepts

## Instructions

1. **Create the class structure**:
    - Start from the [Value Object Template](./templates/ValueObjectTemplate.md) for a ready-to-use scaffold.
    - Inherit from `ValueObject`
    - Add `[DebuggerDisplay("Value={Value}")]` attribute
    - Make constructors private
    - See the [EmailAddress Example](./examples/EmailAddressExample.md) for a minimal implementation.

2. **Implement validation with Rules**:
    - Create public static `Create(string value)` method
    - Use `Rule.Add(RuleSet.IsValidEmail(v))` or custom rules (see [EmailAddress Example](./examples/EmailAddressExample.md))
    - For advanced scenarios, see [CustomerNumber Example](./examples/CustomerNumberExample.md) for custom regex and business rules.
    - Normalize input (trim, case) before validation
    - Return `Result<ValueObjectType>` with `.ToResult(new ValueObjectType(v))`

3. **Add implicit operators**:
    - `implicit operator string(ValueObject vo)` to return Value
    - `implicit operator ValueObject(string value)` calling Create() and throwing on failure
    - See [EmailAddress Example](./examples/EmailAddressExample.md) for usage.

4. **Implement equality**:
    - Override `GetAtomicValues()` to yield return Value
    - Ensure proper equality comparison
    - All examples demonstrate this pattern.

5. **Add factory methods** (optional):
    - For complex creation logic like `CustomerNumber.Create(int year, long sequence)` (see [CustomerNumber Example](./examples/CustomerNumberExample.md)).
    - For computed properties and multiple creation methods, see [PhoneNumber Example](./examples/PhoneNumberExample.md).

## Best Practices

- **Start with the [Value Object Template](./templates/ValueObjectTemplate.md)** for consistency and speed.
- **Validation**: Use RuleSet for common validations (see [EmailAddress Example](./examples/EmailAddressExample.md)), and custom rules for business logic (see [CustomerNumber Example](./examples/CustomerNumberExample.md)).
- **Normalization**: Always trim and normalize case in Create method (see all examples).
- **Error Handling**: Return `Result<T>` from Create, use WithError for specific failures.
- **Immutability**: Keep all properties readonly, no setters.
- **Equality**: Include all significant fields in GetAtomicValues (see all examples).
- **Naming**: Use descriptive names like EmailAddress, CustomerNumber.
- **Documentation**: Add XML comments explaining the value object's purpose and format.
- **For computed properties and advanced scenarios**: See [PhoneNumber Example](./examples/PhoneNumberExample.md).

## Examples

### Simple Value Object (EmailAddress)

```csharp
[DebuggerDisplay("Value={Value}")]
public class EmailAddress : ValueObject
{
    private EmailAddress() { }
    private EmailAddress(string value) => this.Value = value;

    public string Value { get; private set; }

    public static implicit operator string(EmailAddress email) => email.Value;
    public static implicit operator EmailAddress(string value)
    {
        var result = Create(value);
        if (result.IsFailure) throw new ResultException(result.Messages.FirstOrDefault() ?? "Invalid email");
        return result.Value;
    }

    public static Result<EmailAddress> Create(string value)
    {
        return Result<string>.Success(value?.Trim()?.ToLowerInvariant())
            .Bind(v => Rule.Add(RuleSet.IsValidEmail(v)).Check().ToResult(new EmailAddress(v)));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

### Complex Value Object (CustomerNumber)

```csharp
[DebuggerDisplay("Value={Value}")]
public class CustomerNumber : ValueObject
{
    private static readonly Regex FormatRegex = new(@"^CUS-(\d{4})-(\d{6})$", RegexOptions.Compiled);

    private CustomerNumber() { }
    private CustomerNumber(string value) => this.Value = value;

    public string Value { get; private set; }

    public static implicit operator string(CustomerNumber number) => number.Value;
    public static implicit operator CustomerNumber(string value)
    {
        var result = Create(value);
        if (result.IsFailure) throw new ResultException(result.Messages.FirstOrDefault() ?? "Invalid customer number");
        return result.Value;
    }

    public static Result<CustomerNumber> Create(string value)
    {
        return Result<string>.Success(value?.Trim()?.ToUpperInvariant())
            .Bind(v => Rule
                .Add(RuleSet.IsNotEmpty(v))
                .Add(RuleSet.IsTrue(FormatRegex.IsMatch(v), "Invalid format"))
                .Check()
                .ToResult(new CustomerNumber(v)));
    }

    public static Result<CustomerNumber> Create(int year, long sequence)
    {
        // Validation logic for year and sequence
        return Rule
            .Add(RuleSet.NumericRange(year, 2000, DateTime.Now.Year + 1))
            .Add(RuleSet.NumericRange(sequence, 100000, 999999))
            .Check()
            .ToResult(new CustomerNumber($"CUS-{year:D4}-{sequence:D6}"));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
```

## Common Pitfalls

- **Missing GetAtomicValues**: Causes incorrect equality behavior
- **Mutable properties**: Value objects should be immutable
- **No input normalization**: Leads to inconsistent data
- **Throwing exceptions in Create**: Use `Result<T>` instead for functional error handling
- **Complex validation in constructor**: Keep constructors simple, use factory methods
- **Not using Rule.ToResult()**: Forgets to convert Rule result to `Result<T>`

## References

- [bITdevKit Domain Features](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-domain.md)
- [Rules Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-rules.md)
- [`Result<T>` Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-results.md)
