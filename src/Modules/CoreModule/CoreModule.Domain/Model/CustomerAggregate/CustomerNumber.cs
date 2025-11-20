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

    /// <summary>
    /// Implicitly converts a <see cref="CustomerNumber"/> to its string representation.
    /// </summary>
    /// <param name="number"></param>
    public static implicit operator string(CustomerNumber number) => number.Value;

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="CustomerNumber"/> instance.
    /// Performs the same validation as <see cref="Create(string)"/> and throws a <see cref="ResultException"/> on failure.
    /// </summary>
    /// <param name="value">The raw email address string.</param>
    /// <exception cref="RuleException">Thrown when the provided value is not a valid email format.</exception>
    public static implicit operator CustomerNumber(string value)
    {
        var result = Create(value);
        if (result.IsFailure)
        {
            var message = string.Join("; ", result.Messages ?? []);
            throw new ResultException(string.IsNullOrWhiteSpace(message) ? "Invalid customer number." : message);
        }

        return result.Value;
    }

    /// <summary>
    /// Creates a <see cref="CustomerNumber"/> from a validated value string.
    /// Returns a <see cref="Result{T}"/> instead of throwing for expected validation failures.
    /// </summary>
    /// <param name="value">Raw customer number value.</param>
    /// <returns>A success result wrapping the <see cref="CustomerNumber"/> or a failure result with validation errors.</returns>
    public static Result<CustomerNumber> Create(string value)
    {
        value = value?.Trim()?.ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CustomerNumber>.Failure()
                .WithError(new ValidationError("Customer number cannot be empty."));
        }

        if (!FormatRegex.IsMatch(value))
        {
            return Result<CustomerNumber>.Failure()
                .WithError(new ValidationError("Customer number must match format CUS-YYYY-NNNNNN (e.g., CUS-2024-000001)."));
        }

        return new CustomerNumber(value); // implicitly wrapped in a successful Result
    }

    /// <summary>
    /// Factory to generate a new sequential customer number for a given year and sequence.
    /// </summary>
    /// <param name="year">The year (e.g., 2025).</param>
    /// <param name="sequence">Sequence number (expected between 100000 and 999999).</param>
    public static Result<CustomerNumber> Create(int year, long sequence)
    {
        var currentPlusOne = TimeProviderAccessor.Current.GetUtcNow().Year + 1; // use configured time provider for deterministic tests
        if (year < 2000 || year > currentPlusOne)
        {
            return Result<CustomerNumber>.Failure()
                .WithError(new ValidationError($"Year out of valid range (2000-{currentPlusOne})."));
        }

        if (sequence < 100000 || sequence > 999999)
        {
            return Result<CustomerNumber>.Failure()
                .WithError(new ValidationError("Sequence must be between 100000 and 999999."));
        }

        var value = $"CUS-{year:D4}-{sequence:D6}";

        return new CustomerNumber(value); // success
    }

    /// <summary>
    /// Factory to generate a new sequential customer number using a date to determine the year.
    /// </summary>
    /// <param name="date">The date from which the year will be extracted.</param>
    /// <param name="sequence">Sequence number between 100000 and 999999.</param>
    public static Result<CustomerNumber> Create(DateTimeOffset date, long sequence) => Create(date.Year, sequence);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }

    public override string ToString() => this.Value;
}