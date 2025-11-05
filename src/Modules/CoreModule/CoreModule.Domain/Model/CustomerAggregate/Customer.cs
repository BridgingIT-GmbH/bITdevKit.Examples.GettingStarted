// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents a customer aggregate root with personal details, email address, and lead/status information.
/// Supports auditing and concurrency handling.
/// </summary>
[DebuggerDisplay("Id={Id}, Name={FirstName} {LastName}, Status={Status}")]
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>, IConcurrency
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Customer"/> class.
    /// Private constructor required by ORM frameworks (e.g. EF Core) or serializers.
    /// </summary>
    public Customer() // TODO: should be private, but Mapster needs it for mapping and creating a new instance
    {
    }

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
    public static Result<Customer> Create(string firstName, string lastName, string email, CustomerNumber number)
    {
        var emailAddressResult = EmailAddress.Create(email);
        if (emailAddressResult.IsFailure)
        {
            return emailAddressResult.Unwrap();
        }

        var customer = new Customer(firstName, lastName, emailAddressResult.Value, number);

        customer.DomainEvents.Register(
            new CustomerCreatedDomainEvent(customer));

        return customer;
    }

    /// <summary>
    /// Changes the name of the customer if different from the current value.
    /// At least one parameter must be provided.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="firstName">The new first name (optional).</param>
    /// <param name="lastName">The new last name (optional).</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeName(string firstName, string lastName)
    {
        if (string.IsNullOrEmpty(firstName) &&
            string.IsNullOrEmpty(lastName))
        {
            return Result<Customer>.Failure(this, "Invalid name");
        }

        return this.ApplyChange(this.FirstName, firstName, v => this.FirstName = v)
            .Bind(c => c.ApplyChange(this.LastName, lastName, v => this.LastName = v));
    }

    /// <summary>
    /// Changes the email address of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="email">The new email address.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Result<Customer>.Failure(this, "Invalid email");
        }

        var emailResult = EmailAddress.Create(email);
        if (emailResult.IsFailure)
        {
            return emailResult.Unwrap();
        }

        return this.ApplyChange(this.Email, emailResult.Value, v => this.Email = v);
    }

    /// <summary>
    /// Changes the date of birth of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="dateOfBirth">The new date of birth.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeBirthDate(DateOnly dateOfBirth)
    {
        if (dateOfBirth > TimeProviderAccessor.Current.GetUtcNow().ToDateOnly())
        {
            return Result<Customer>.Failure(this, "Invalid dateOfBirth");
        }

        return this.ApplyChange(this.DateOfBirth, dateOfBirth, v => this.DateOfBirth = v);
    }

    /// <summary>
    /// Changes the status of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="status">The new customer status.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    public Result<Customer> ChangeStatus(CustomerStatus status)
    {
        if (status == null)
        {
            return Result<Customer>.Failure(this, "Invalid status");
        }

        return this.ApplyChange(this.Status, status, v => this.Status = v);
    }

    /// <summary>
    /// Generic helper method to apply a change only if the new value
    /// differs from the current one. Registers a
    /// <see cref="CustomerUpdatedDomainEvent"/> when the value changes.
    /// </summary>
    /// <typeparam name="T">The type of the property being changed.</typeparam>
    /// <param name="currentValue">The current value of the property.</param>
    /// <param name="newValue">The new value to apply.</param>
    /// <param name="action">The assignment action if a change occurs.</param>
    /// <returns>The current <see cref="Customer"/> instance for chaining.</returns>
    private Result<Customer> ApplyChange<T>(T currentValue, T newValue, Action<T> action)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return Result<Customer>.Success(this); // nothing to do, keep current
        }

        action(newValue);

        this.DomainEvents.Register(
            new CustomerUpdatedDomainEvent(this), true);

        return this;
    }
}