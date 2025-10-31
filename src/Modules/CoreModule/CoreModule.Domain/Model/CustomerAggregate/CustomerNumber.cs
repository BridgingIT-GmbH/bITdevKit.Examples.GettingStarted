// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

[DebuggerDisplay("Value={Value}")]
public class CustomerNumber : ValueObject // TODO: refactor to use Result<CustomerNumber> for the create methods
{
    private static readonly Regex FormatRegex =
        new(@"^CUS-(\d{4})-(\d{6})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private CustomerNumber()
    {
    }

    private CustomerNumber(string value) => this.Value = value;

    /// <summary>
    /// Gets the string value of the customer number (e.g., CUS-2024-000001).
    /// </summary>
    public string Value { get; private set; }

    public static implicit operator string(CustomerNumber number) => number.Value;

    /// <summary>
    /// Creates a CustomerNumber from a validated value string.
    /// </summary>
    /// <exception cref="RuleException">
    /// Thrown if the provided value does not match the expected format CUS-YYYY-NNNNNN.
    /// </exception>
    public static CustomerNumber Create(string value)
    {
        value = value?.Trim()?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RuleException("Customer number cannot be empty.");
        }

        if (!FormatRegex.IsMatch(value))
        {
            throw new RuleException(
                "Customer number must match format CUS-YYYY-NNNNNN (e.g., CUS-2024-000001)."
            );
        }

        return new CustomerNumber(value);
    }

    /// <summary>
    /// Factory to generate a new sequential customer number for a given year.
    /// </summary>
    /// <param name="year">The year (e.g., 2025).</param>
    /// <param name="sequence">Sequence number starting from 1.</param>
    public static CustomerNumber Create(int year, long sequence)
    {
        ValidateYear(year);
        ValidateSequence(sequence);

        var value = $"CUS-{year:D4}-{sequence:D6}";
        return new CustomerNumber(value);
    }

    /// <summary>
    /// Factory to generate a new sequential customer number using a date to determine the year.
    /// </summary>
    /// <param name="date">The date from which the year will be extracted.</param>
    /// <param name="sequence">Sequence number starting from 1.</param>
    public static CustomerNumber Create(DateTimeOffset date, long sequence)
    {
        var year = date.Year;
        return Create(year, sequence);
    }

    private static void ValidateYear(int year)
    {
        var currentPlusOne = DateTime.UtcNow.Year + 1;
        if (year < 2000 || year > currentPlusOne)
        {
            throw new RuleException(
                $"Year out of valid range (2000�{currentPlusOne})."
            );
        }
    }

    private static void ValidateSequence(long sequence)
    {
        if (sequence < 100000 || sequence > 999999)
        {
            throw new RuleException("Sequence must be between 100000 and 999999.");
        }
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    public override string ToString() => this.Value;
}