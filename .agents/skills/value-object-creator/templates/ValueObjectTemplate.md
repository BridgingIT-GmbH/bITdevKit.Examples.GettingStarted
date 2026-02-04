# Value Object Template

This template provides a starting point for creating domain value objects in bITdevKit projects. Replace the placeholders (e.g., `{ValueObjectName}`) with your specific values.

## Template Code

```csharp
// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents a [describe the value object, e.g., unique customer number in the format CUS-YYYY-NNNNNN].
/// </summary>
[DebuggerDisplay("Value={Value}")]
public class {ValueObjectName} : ValueObject
{
    private {ValueObjectName}() { }

    private {ValueObjectName}(string value) => this.Value = value;

    /// <summary>
    /// Gets the string value of the [value object name].
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Implicitly converts a <see cref="{ValueObjectName}"/> to its string representation.
    /// </summary>
    /// <param name="obj"></param>
    public static implicit operator string({ValueObjectName} obj) => obj.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="{ValueObjectName}"/> instance.
    /// Performs the same validation as <see cref="Create(string)"/> and throws a <see cref="ResultException"/> on failure.
    /// </summary>
    /// <param name="value">The value as a string.</param>
    /// <exception cref="RuleException">Thrown when the provided value is not a valid [value object name] format.</exception>
    public static implicit operator {ValueObjectName}(string value)
    {
        var result = Create(value);
        if (result.IsFailure)
        {
            var message = string.Join("; ", result.Messages ?? []);
            throw new ResultException(string.IsNullOrWhiteSpace(message) ? "Invalid {value object name}." : message);
        }

        return result.Value;
    }

    /// <summary>
    /// Creates a <see cref="{ValueObjectName}"/> instance after checking the input string.
    /// Normalizes the string to [case, e.g., uppercase] and trims whitespace.
    /// </summary>
    /// <param name="value">The [value object name] string to create from.</param>
    /// <returns>A success result wrapping the <see cref="{ValueObjectName}"/> or a failure result with errors.</returns>
    public static Result<{ValueObjectName}> Create(string value)
    {
        return Result<string>.Success(value?.Trim()?.ToUpperInvariant())
            .Bind(v => Rule
                .Add(RuleSet.IsNotEmpty(v, "Resources.Validator_{ValueObjectName}CannotBeEmpty"))
                .Add(RuleSet.IsTrue(/* validation logic */, "Resources.Validator_{ValueObjectName}InvalidFormat"))
                .Check()
                .ToResult(new {ValueObjectName}(v)));
    }

    /// <summary>
    /// Factory to generate a new [value object name] using [describe parameters].
    /// </summary>
    /// <param name="param1">[describe param1]</param>
    /// <param name="param2">[describe param2]</param>
    public static Result<{ValueObjectName}> Create(/* parameters */)
    {
        // Validation logic
        return Rule
            .Add(/* validation */)
            .Check()
            .ToResult(new {ValueObjectName}(/* formatted value */));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    public override string ToString() => this.Value;
}
```

## Usage Instructions

1. Copy the code block above into a new `.cs` file in your domain model folder.
2. Replace all `{ValueObjectName}` placeholders with your actual class name (e.g., `EmailAddress`).
3. Update the namespace to match your module's domain model.
4. Customize the validation logic in the `Create` methods.
5. Add any additional properties or methods as needed.
6. Ensure the class inherits from `ValueObject` (from bITdevKit).