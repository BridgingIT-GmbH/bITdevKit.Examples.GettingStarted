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
    /// Implicitly converts a <see cref="string"/> to an <see cref="EmailAddress"/> instance.
    /// Performs the same validation as <see cref="Create(string)"/> and throws a <see cref="ResultException"/> on failure.
    /// </summary>
    /// <param name="value">The raw email address string.</param>
    /// <exception cref="RuleException">Thrown when the provided value is not a valid email format.</exception>
    public static implicit operator EmailAddress(string value)
    {
        var result = Create(value);
        if (result.IsFailure)
        {
            var message = string.Join("; ", result.Messages ?? []);
            throw new ResultException(string.IsNullOrWhiteSpace(message) ? "Invalid email address." : message);
        }

        return result.Value;
    }

    /// <summary>
    /// Creates a new <see cref="EmailAddress"/> instance after checking the input string.
    /// Normalizes the string to lowercase and trims whitespace.
    /// </summary>
    /// <param name="value">The email address string to create from.</param>
    /// <returns>A success result wrapping the <see cref="EmailAddress"/> or a failure result with errors.</returns>
    public static Result<EmailAddress> Create(string value)
    {
        return Result<string>.Success(value?.Trim()?.ToLowerInvariant())
            .Bind(v => Rule
                .Add(RuleSet.IsValidEmail(v))
                .Check()
                .ToResult(new EmailAddress(v)));
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