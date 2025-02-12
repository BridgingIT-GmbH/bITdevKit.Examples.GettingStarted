namespace BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Model;

public class EmailAddress : ValueObject
{
    private EmailAddress()
    {
    }

    private EmailAddress(string value) => this.Value = value;

    public string Value { get; private set; }

    public static implicit operator string(EmailAddress email) => email.Value;

    public static EmailAddress Create(string value)
    {
        value = value?.Trim()?.ToLowerInvariant();

        Rule
            .Add(RuleSet.IsValidEmail(value))
            .Throw();

        return new EmailAddress(value);
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Value;
    }
}
