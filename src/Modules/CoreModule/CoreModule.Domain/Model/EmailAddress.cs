// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents an immutable email address value object in the domain model.
/// Provides validation, equality, and implicit conversion to <see cref="string"/>.
/// </summary>
[DebuggerDisplay("Value={Value}")]
public class EmailAddress : ValueObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddress"/> class.
    /// Private to enforce controlled creation via <see cref="Create(string)"/>.
    /// </summary>
    private EmailAddress()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddress"/> class
    /// with the specified string value.
    /// </summary>
    /// <param name="value">The validated email address string.</param>
    private EmailAddress(string value) => this.Value = value;

    /// <summary>
    /// Gets the string value of the email address.
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Implicitly converts an <see cref="EmailAddress"/> instance to its string value.
    /// </summary>
    /// <param name="email">The <see cref="EmailAddress"/> to convert.</param>
    /// <returns>The email address as a <see cref="string"/>.</returns>
    public static implicit operator string(EmailAddress email) => email.Value;

    /// <summary>
    /// Creates a new <see cref="EmailAddress"/> instance after validating the input string.
    /// Normalizes the string to lowercase and trims whitespace.
    /// </summary>
    /// <param name="value">The email address string to create from.</param>
    /// <exception cref="RuleValidationException">
    /// Thrown if <paramref name="value"/> is not a valid email format.
    /// </exception>
    /// <returns>A new <see cref="EmailAddress"/> value object.</returns>
    public static EmailAddress Create(string value)
    {
        value = value?.Trim()?.ToLowerInvariant();

        Rule.Add(RuleSet.IsValidEmail(value))
            .Throw();

        return new EmailAddress(value);
    }

    /// <summary>
    /// Provides atomic values for equality comparison.
    /// Ensures that two <see cref="EmailAddress"/> objects with the same
    /// <see cref="Value"/> are considered equal.
    /// </summary>
    /// <returns>A sequence containing the atomic values of the object.</returns>
    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
