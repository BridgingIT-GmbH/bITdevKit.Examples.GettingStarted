# PhoneNumber Example

This example illustrates a value object with computed properties and multiple creation methods.

## Key Features

- Value object with computed properties
- Multiple factory methods
- Complex validation with regex
- Property derivation from the main value

## Usage

This example can be used as a template for creating new value objects in your domain. Copy the code block into your domain layer and adapt it to your specific requirements.

## Key Patterns

- All value objects inherit from `ValueObject`
- Use `Result<T>` for creation methods
- Implement `GetAtomicValues()` for equality comparison
- Use `Rule` and `RuleSet` for validation
- Consider implicit operators for convenience
- Add computed properties when they add value

## Related Resources

- [Value Object Creator Skill](../SKILL.md)
- [Value Object Template](../templates/ValueObjectTemplate.md)
- [bITdevKit Documentation](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs)

## Code

```csharp
// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents a phone number value object with validation.
/// Supports international format (+XX XXX XXX XXXX).
/// </summary>
[DebuggerDisplay("Value={Value}")]
public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex =
        new(@"^\+\d{1,3} \d{3} \d{3} \d{4}$", RegexOptions.Compiled);

    private PhoneNumber() { }

    private PhoneNumber(string value) => this.Value = value;

    /// <summary>
    /// Gets the formatted phone number value.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Gets the country code (e.g., +1 for US).
    /// </summary>
    public string CountryCode => this.Value?.Split(' ')[0];

    /// <summary>
    /// Gets the local number without country code.
    /// </summary>
    public string LocalNumber => string.Join(" ", this.Value?.Split(' ').Skip(1) ?? []);

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    public static implicit operator PhoneNumber(string value)
    {
        var result = Create(value);
        if (result.IsFailure)
        {
            var message = string.Join("; ", result.Messages ?? []);
            throw new ResultException(string.IsNullOrWhiteSpace(message) ? "Invalid phone number." : message);
        }

        return result.Value;
    }

    /// <summary>
    /// Creates a PhoneNumber from a formatted string.
    /// </summary>
    /// <param name="value">The phone number in format +XX XXX XXX XXXX.</param>
    /// <returns>A Result containing the PhoneNumber or validation errors.</returns>
    public static Result<PhoneNumber> Create(string value)
    {
        return Result<string>.Success(value?.Trim())
            .Bind(v => Rule
                .Add(RuleSet.IsNotEmpty(v))
                .Add(RuleSet.IsTrue(PhoneRegex.IsMatch(v), "Phone number must be in format +XX XXX XXX XXXX"))
                .Check()
                .ToResult(new PhoneNumber(v)));
    }

    /// <summary>
    /// Creates a PhoneNumber from country code and local number.
    /// </summary>
    /// <param name="countryCode">Country code without + (e.g., 1).</param>
    /// <param name="areaCode">Area code (3 digits).</param>
    /// <param name="number">Local number (7 digits).</param>
    /// <returns>A Result containing the PhoneNumber or validation errors.</returns>
    public static Result<PhoneNumber> Create(string countryCode, string areaCode, string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || string.IsNullOrWhiteSpace(areaCode) || string.IsNullOrWhiteSpace(number))
        {
            return Result<PhoneNumber>.Failure("Country code, area code, and number are required");
        }

        var formatted = $"+{countryCode.Trim()} {areaCode.Trim()} {number.Trim()}";
        return Create(formatted);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    public override string ToString() => this.Value;
}
