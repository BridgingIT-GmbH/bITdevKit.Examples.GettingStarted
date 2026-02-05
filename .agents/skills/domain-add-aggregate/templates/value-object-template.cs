// Template: Value Object
// Replace placeholders: [ValueObject], [Property], [PropertyType]
// Example: [ValueObject] = EmailAddress, [Property] = Value, [PropertyType] = string

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Represents an immutable [value object] value object in the domain model.
/// Value objects are identified by their attributes, not by an ID.
/// </summary>
[DebuggerDisplay("Value={Value}")] // Pattern: Shows value in debugger
public class [ValueObject] : ValueObject
{
    /// <summary>
    /// Private parameterless constructor for EF Core materialization.
    /// EF Core requires this to create instances when reading from database.
    /// </summary>
    private [ValueObject]()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="[ValueObject]"/> class
    /// with the specified value.
    /// Private to enforce controlled creation via <see cref="Create(string)"/>.
    /// </summary>
    /// <param name="value">The validated [value object] value.</param>
    private [ValueObject]([PropertyType] value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the value of the [value object].
    /// </summary>
    public [PropertyType] Value { get; private set; }

    /// <summary>
    /// Implicitly converts a <see cref="[ValueObject]"/> instance to its primitive value.
    /// Allows: string email = emailAddress; (without .Value)
    /// </summary>
    /// <param name="[valueObject]">The [ValueObject] to convert.</param>
    /// <returns>The [value object] as a primitive type.</returns>
    public static implicit operator [PropertyType]([ValueObject] [valueObject]) => [valueObject].Value;

    /// <summary>
    /// Implicitly converts a primitive value to a <see cref="[ValueObject]"/> instance.
    /// Performs validation and throws <see cref="ResultException"/> on failure.
    /// Allows: EmailAddress email = "test@example.com"; (without Create())
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <exception cref="ResultException">Thrown when validation fails.</exception>
    public static implicit operator [ValueObject]([PropertyType] value)
    {
        var result = Create(value);
        if (result.IsFailure)
        {
            var message = string.Join("; ", result.Messages ?? []);
            throw new ResultException(string.IsNullOrWhiteSpace(message) ? "Invalid [value object]." : message);
        }

        return result.Value;
    }

    /// <summary>
    /// Creates a new <see cref="[ValueObject]"/> instance after validating the input.
    /// Normalizes the value (trim, lowercase, etc.) before validation.
    /// </summary>
    /// <param name="value">The [value object] value to create from.</param>
    /// <returns>A Result containing the new [ValueObject] or validation errors.</returns>
    public static Result<[ValueObject]> Create([PropertyType] value)
    {
        // Pattern: Normalize input before validation
        value = value?.Trim()?.ToLowerInvariant(); // Adjust normalization as needed

        // Pattern: Use bITdevKit Rule/RuleSet for validation
        var ruleResult = Rule.Add(RuleSet.IsNotEmpty(value)) // Built-in rule
            // Add more rules: RuleSet.IsValidEmail(value), RuleSet.HasMinLength(value, 3), etc.
            .Check();

        if (ruleResult.IsFailure)
        {
            return Result<[ValueObject]>.Failure()
                .WithMessages(ruleResult.Messages)
                .WithErrors(ruleResult.Errors);
        }

        // Pattern: Implicit conversion to Result<[ValueObject]>.Success()
        return new [ValueObject](value);
    }

    /// <summary>
    /// Provides atomic values for equality comparison.
    /// Two <see cref="[ValueObject]"/> objects with same values are considered equal.
    /// </summary>
    /// <returns>A sequence containing the atomic values of the object.</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        // Pattern: Yield all properties that define equality
        yield return this.Value;
        // For multi-property value objects:
        // yield return this.Amount;
        // yield return this.Currency;
    }
}

// Key Patterns Summary:
// 1. Immutability: All properties have private setters
// 2. Private Constructors: Enforce factory method usage
// 3. Factory Method: Create() returns Result<T> with validation
// 4. Implicit Operators: Enable seamless conversion to/from primitives
// 5. Equality: Based on values (GetAtomicValues), not reference
// 6. Validation: Use bITdevKit RuleSet for common rules
//
// Common RuleSets (bITdevKit):
// - RuleSet.IsNotEmpty(value)
// - RuleSet.IsValidEmail(value)
// - RuleSet.HasMinLength(value, min)
// - RuleSet.HasMaxLength(value, max)
// - RuleSet.GreaterThan(value, threshold)
// - RuleSet.LessThan(value, threshold)
// - RuleSet.IsInRange(value, min, max)
//
// Usage Examples:
// - Explicit: var email = EmailAddress.Create("test@example.com");
// - Implicit: EmailAddress email = "test@example.com"; (calls implicit operator)
// - To primitive: string str = emailAddress; (calls implicit operator)
//
// EF Core Mapping (in TypeConfiguration):
// - Owned: builder.OwnsOne(e => e.Address, b => { b.Property(a => a.Street)... });
// - Conversion: builder.Property(e => e.Email).HasConversion(e => e.Value, v => EmailAddress.Create(v).Value);
