// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

using BridgingIT.DevKit.Domain;

/// <summary>
/// Represents a customer aggregate root with personal details, email address, and lead/status information.
/// </summary>
[DebuggerDisplay("Id={Id}, Name={FirstName} {LastName}, Status={Status}")]
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>, IConcurrency
{
    private Customer()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class with the specified name and email address.
    /// This constructor is private to enforce controlled creation through the <see cref="Create(string, string, string, CustomerNumber)"/> factory method.
    /// </summary>
    /// <param name="firstName">The first name of the customer.</param>
    /// <param name="lastName">The last name of the customer.</param>
    /// <param name="email">The validated <see cref="EmailAddress"/> of the customer.</param>
    /// <param name="number">The number of the customer.</param>
    private Customer(string firstName, string lastName, EmailAddress email, CustomerNumber number)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Email = email;
        this.Number = number;
    }

    /// <summary>
    /// Gets the first name of the customer.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Gets the last name of the customer.
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Gets the unique customer number associated with this customer.
    /// </summary>
    public CustomerNumber Number { get; private set; }

    /// <summary>
    /// Gets the date of birth associated with the entity, if available.
    /// </summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>
    /// Gets the email address of the customer.
    /// </summary>
    public EmailAddress Email { get; private set; }

    /// <summary>
    /// Gets the current <see cref="CustomerStatus"/> of the customer.
    /// </summary>
    public CustomerStatus Status { get; private set; } = CustomerStatus.Lead;

    /// <summary>
    /// Gets or sets a concurrency version token for optimistic concurrency control.
    /// </summary>
    public Guid ConcurrencyVersion { get; set; }

    /// <summary>
    /// Factory method to create a new <see cref="Customer"/> aggregate.
    /// Registers a <see cref="CustomerCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="firstName">The first name of the customer.</param>
    /// <param name="lastName">The last name of the customer.</param>
    /// <param name="email">The email address of the customer.</param>
    /// <param name="number">The number of the customer.</param>
    /// <returns>A new <see cref="Customer"/> instance.</returns>
    public static Result<Customer> Create(string firstName, string lastName, string email, CustomerNumber number) // TODO: the email could be EmailAddress type directly
    {
        var emailAddressResult = EmailAddress.Create(email);
        if (emailAddressResult.IsFailure)
        {
            return emailAddressResult.Unwrap();
        }

        if (number == null)
        {
            return Result<Customer>.Failure()
                .WithError(new ValidationError("Customer number cannot be null."));
        }

        var customer = new Customer(firstName, lastName, emailAddressResult.Value, number);

        customer.DomainEvents
            .Register(new CustomerCreatedDomainEvent(customer))
            .Register(new EntityCreatedDomainEvent<Customer>(customer));

        return customer;
    }

    /// <summary>
    /// Changes the name of the customer if different from the current value. At least one parameter must be provided.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="firstName">The new first name (optional).</param>
    /// <param name="lastName">The new last name (optional).</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeName(string firstName, string lastName)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName), "Invalid name: both first and last name must be provided")
            .Ensure(_ => lastName != "notallowed", "Invalid last name: 'notallowed' is not permitted")
            .Set(c => c.FirstName, firstName).Set(c => c.LastName, lastName)
            .Register(c => new CustomerUpdatedDomainEvent(c))
            .Apply();
    }

    /// <summary>
    /// Changes the email address of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="email">The new email address.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeEmail(string email)
    {
        return this.Change()
            .Set(c => c.Email, EmailAddress.Create(email)) // Implicit guard inside Set()
            .Register(c => new CustomerUpdatedDomainEvent(c))
            .Apply();
    }

    /// <summary>
    /// Changes the date of birth of the customer if different from the current value.
    /// Checks validation rules regarding future dates and maximum age.
    /// Registers a <see cref="EntityUpdatedDomainEvent{Customer}"/> if changed.
    /// </summary>
    /// <param name="dateOfBirth">The new date of birth.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeBirthDate(DateOnly? dateOfBirth)
    {
        var currentDate = TimeProviderAccessor.Current.GetUtcNow().ToDateOnly();

        return this.Change()
            .When(_ => dateOfBirth.HasValue)
            .Ensure(_ => dateOfBirth <= currentDate, "Invalid date of birth: cannot be in the future")
            .Ensure(_ => dateOfBirth >= currentDate.AddYears(-150), "Invalid date of birth: age exceeds maximum")
            .Set(c => c.DateOfBirth, dateOfBirth)
            .Register(c => new CustomerUpdatedDomainEvent(c))
            .Apply();
    }

    /// <summary>
    /// Changes the status of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="status">The new customer status.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeStatus(CustomerStatus status)
    {
        return this.Change()
            .When(_ => status != null)
            .Set(c => c.Status, status)
            .Register(c => new CustomerUpdatedDomainEvent(c))
            .Apply();
    }
}