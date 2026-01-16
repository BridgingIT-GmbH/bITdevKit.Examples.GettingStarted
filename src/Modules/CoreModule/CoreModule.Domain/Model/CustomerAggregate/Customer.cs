// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

using BridgingIT.DevKit.Domain;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Xml.Linq;

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
            .Ensure(_ => !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName), new ValidationError("Invalid name: both first and last name must be provided"))
            .Ensure(_ => lastName != "notallowed", new ValidationError("Invalid last name: 'notallowed' is not permitted"))
            .Ensure(_ => email != null, new ValidationError("Email cannot be null"))
            .Ensure(_ => number != null, new ValidationError("Number cannot be null"))
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
            .Ensure(_ => !string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName), "Invalid name: both first and last name must be provided")
            .Ensure(_ => lastName != "notallowed", "Invalid last name: 'notallowed' is not permitted")
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
            .Ensure(_ => dateOfBirth <= currentDate, "Invalid date of birth: cannot be in the future")
            .Ensure(_ => dateOfBirth >= currentDate.AddYears(-150), "Invalid date of birth: age exceeds maximum")
            .Ensure(_ => Rule // demonstrates complex rule composition with the Rules Feature
                .Add(RuleSet.LessThan(dateOfBirth.Value, DateOnly.MaxValue))
                .Add(RuleSet.GreaterThan(dateOfBirth.Value, DateOnly.MinValue))
                .Check(), "Invalid date of birth: out of valid range")
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
    /// <param name="isPrimary">Indicates if this should be the primary address.</param>
    /// <returns>The updated <see cref="Customer"/> wrapped in a Result.</returns>
    public Result<Customer> AddAddress(string name, string line1, string line2, string postalCode, string city, string country)
    {
        return this.Change()
            .Add(e => this.addresses, Address.Create(name, line1, line2, postalCode, city, country))
            .Register(e => new CustomerUpdatedDomainEvent(e))
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
            //.When(_ => id != null)
            .RemoveById(e => this.addresses, id, errorMessage: "Address with specified ID not found")
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Customer> SetPrimaryAddress(AddressId id)
    {
        return this.Change()
            //.When(_ => id != null)
            .Ensure(_ => this.addresses.Any(a => a.Id == id), "Address with specified ID not found")
            .Set(e => e.addresses, a => a.SetPrimary(a.Id == id))
            .Register(e => new CustomerUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Address> FindAddress(AddressId id)
    {
        return this.addresses.AsEnumerable().Find(e => e.Id == id)
            .Match(
                some: e => Result<Address>.Success(e),
                none: () => Result<Address>.Failure()
                    .WithError("Address with specified ID not found"));
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
        return this.FindAddress(id).Bind(e => e
            .Change()
                .Set(e => e.ChangeName(name))
                .Set(e => e.ChangeLine1(line1))
                .Set(e => e.ChangeLine2(line2))
                .Set(e => e.ChangePostalCode(postalCode))
                .Set(e => e.ChangeCity(city))
                .Set(e => e.ChangeCountry(country))
                .Register(e => new CustomerUpdatedDomainEvent(this))
                .Apply().Wrap(this));
    }
}