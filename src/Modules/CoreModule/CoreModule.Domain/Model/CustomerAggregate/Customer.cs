// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents a customer aggregate root with personal details, email address, and lead/status information.
/// </summary>
[DebuggerDisplay("Id={Id}, Name={FirstName} {LastName}, Status={Status}")]
[TypedEntityId<Guid>]
public class Customer : AuditableAggregateRoot<CustomerId>, IConcurrency
{
    private readonly List<Address> addresses = [];

    private Customer() { }

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
    /// Gets the collection of addresses associated with this customer.
    /// </summary>
    public IReadOnlyCollection<Address> Addresses => this.addresses.AsReadOnly();

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
    public static Result<Customer> Create(string firstName, string lastName, string email, CustomerNumber number) // TODO: the email should be EmailAddress type directly
    {
        var emailAddressResult = EmailAddress.Create(email);
        if (emailAddressResult.IsFailure)
        {
            return emailAddressResult.Unwrap();
        }

        return Result<Customer>.Success()
            .Ensure(_ => !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName), Errors.Validation.Error(Resources.Validator_NameBothFirstAndLastRequired, nameof(firstName)))
            .Ensure(_ => lastName != "notallowed", Errors.Validation.Error(Resources.Validator_NotAllowedValue, nameof(lastName)))
            .Ensure(_ => email != null, Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(email)))
            .Ensure(_ => number != null, Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(number)))
            .Bind(_ => new Customer(firstName, lastName, emailAddressResult.Value, number))
            .Tap(e => e.DomainEvents
                .Register(new CustomerCreatedDomainEvent(e))
                .Register(new EntityCreatedDomainEvent<Customer>(e)));
    }

    /// <summary>
    /// Changes the name of the customer if different from the current value. At least one parameter must be provided.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> ChangeName(string firstName, string lastName)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName), Errors.Validation.Error(Resources.Validator_NameBothFirstAndLastRequired))
            .Ensure(_ => lastName != "notallowed", Errors.Validation.Error(Resources.Validator_NotAllowedValue))
            .Set(e => e.FirstName, firstName).Set(e => e.LastName, lastName)
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    /// <summary>
    /// Changes the email address of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="email">The new email address.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> ChangeEmail(string email)
    {
        return this.Change()
            .Set(e => e.Email, EmailAddress.Create(email))
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    /// <summary>
    /// Changes the date of birth of the customer if different from the current value.
    /// Checks validation rules regarding future dates and maximum age.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="dateOfBirth">The new date of birth.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> ChangeBirthDate(DateOnly? dateOfBirth)
    {
        var currentDate = TimeProviderAccessor.Current.GetUtcNow().ToDateOnly();

        return this.Change()
            .When(_ => dateOfBirth.HasValue)
            .Ensure(_ => dateOfBirth <= currentDate, Errors.Validation.Error(Resources.Validator_DateOfBirthCannotBeFuture))
            .Ensure(_ => dateOfBirth >= currentDate.AddYears(-150), Errors.Validation.Error(Resources.Validator_DateOfBirthAgeExceedsMaximum))
            .Ensure(_ => Rule // demonstrates complex rule composition with the Rules Feature
                .Add(RuleSet.LessThan(dateOfBirth.Value, DateOnly.MaxValue))
                .Add(RuleSet.GreaterThan(dateOfBirth.Value, DateOnly.MinValue))
                .Check(), Errors.Validation.Error(Resources.Validator_DateOfBirthOutOfRange))
            .Set(e => e.DateOfBirth, dateOfBirth)
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    /// <summary>
    /// Changes the status of the customer if different from the current value.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="status">The new customer status.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> ChangeStatus(CustomerStatus status)
    {
        return this.Change()
            .When(_ => status != null)
            .Set(e => e.Status, status)
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    /// <summary>
    /// Adds a new address to the customer's address collection.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/>.
    /// </summary>
    /// <param name="name">The optional name/label for the address.</param>
    /// <param name="line1">The first line of the address (required).</param>
    /// <param name="line2">The optional second line of the address.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="city">The city name (required).</param>
    /// <param name="country">The country name (required).</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> AddAddress(string name, string line1, string line2, string postalCode, string city, string country)
    {
        return this.Change()
            .Ensure(_ => !this.HasDuplicateAddress(name, line1, postalCode, city, country), Errors.Validation.Duplicate())
            .Add(e => this.addresses, Address.Create(name, line1, line2, postalCode, city, country))
            .Register(e => new CustomerUpdatedDomainEvent(e), this)
            .Apply();
    }

    /// <summary>
    /// Removes an address from the customer's address collection.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/>.
    /// </summary>
    /// <param name="id">The ID of the address to remove.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> RemoveAddress(AddressId id)
    {
        return this.Change()
            .RemoveById(e => this.addresses, id, error: Errors.Domain.EntityNotFound())
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Customer> SetPrimaryAddress(AddressId id)
    {
        return this.Change()
            .Ensure(_ => this.addresses.Any(a => a.Id == id), Errors.Domain.EntityNotFound())
            .Set(e => e.addresses, a => a.SetPrimary(a.Id == id))
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Address> FindAddress(AddressId id)
    {
        return this.addresses
            .Find(e => e.Id == id)
            .Match(
                some: Result<Address>.Success,
                none: () => Result<Address>.Failure()
                    .WithError(Errors.Domain.EntityNotFound()));
    }

    /// <summary>
    /// Updates an existing address with new values.
    /// Registers a <see cref="CustomerUpdatedDomainEvent"/>.
    /// </summary>
    /// <param name="id">The ID of the address to update.</param>
    /// <param name="name">The new name/label.</param>
    /// <param name="line1">The new first line.</param>
    /// <param name="line2">The new second line.</param>
    /// <param name="postalCode">The new postal code.</param>
    /// <param name="city">The new city.</param>
    /// <param name="country">The new country.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> ChangeAddress(AddressId id, string name, string line1, string line2, string postalCode, string city, string country)
    {
        return this.FindAddress(id).Bind(e => e.Change()
                .Ensure(_ => !this.HasDuplicateAddress(name, line1, postalCode, city, country, id), Errors.Validation.Duplicate())
                .Set(e => e.ChangeName(name))
                .Set(e => e.ChangeLine1(line1))
                .Set(e => e.ChangeLine2(line2))
                .Set(e => e.ChangePostalCode(postalCode))
                .Set(e => e.ChangeCity(city))
                .Set(e => e.ChangeCountry(country))
                .Register(e => new CustomerUpdatedDomainEvent(this), this) // TODO: the event should register on the Customer aggregate, not the Address entity
                //.Register(c, e=> new CustomerUpdatedDomainEvent(this))
                .Apply().Wrap(this));
    }

    private bool HasDuplicateAddress(string name, string line1, string postalCode, string city, string country, AddressId excludeId = null)
    {
        return this.addresses.Any(a =>
            (excludeId == null || a.Id != excludeId) &&
            a.Name == name &&
            a.Line1 == line1 &&
            a.PostalCode == postalCode &&
            a.City == city &&
            a.Country == country);
    }
}