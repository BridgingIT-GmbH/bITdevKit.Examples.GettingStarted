// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
/// <summary>
/// Represents a customer address entity with location details and primary designation.
/// </summary>
[DebuggerDisplay("Id={Id}, City={City}, Country={Country}, IsPrimary={IsPrimary}")]
[TypedEntityId<Guid>]
public class Address : Entity<AddressId>
{
    private Address() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Address"/> class with the specified location details.
    /// This constructor is private to enforce controlled creation through the <see cref="Create"/> factory method.
    /// </summary>
    /// <param name="name">The name/label for this address.</param>
    /// <param name="line1">The first line of the address.</param>
    /// <param name="line2">The optional second line of the address.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="city">The city name.</param>
    /// <param name="country">The country name.</param>
    /// <param name="isPrimary">Indicates whether this is the primary address.</param>
    private Address(string name, string line1, string line2, string postalCode, string city, string country, bool isPrimary)
    {
        this.Name = name;
        this.Line1 = line1;
        this.Line2 = line2;
        this.PostalCode = postalCode;
        this.City = city;
        this.Country = country;
        this.IsPrimary = isPrimary;
    }

    /// <summary>
    /// Gets the name or label for this address (e.g., "Home", "Office").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the first line of the address (street, number, etc.).
    /// </summary>
    public string Line1 { get; private set; }

    /// <summary>
    /// Gets the optional second line of the address (apartment, suite, etc.).
    /// </summary>
    public string Line2 { get; private set; }

    /// <summary>
    /// Gets the optional postal code.
    /// </summary>
    public string PostalCode { get; private set; }

    /// <summary>
    /// Gets the city name.
    /// </summary>
    public string City { get; private set; }

    /// <summary>
    /// Gets the country name.
    /// </summary>
    public string Country { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary address for the customer.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Factory method to create a new <see cref="Address"/> entity.
    /// Validates that required fields (Name, Line1, City, Country) are provided.
    /// </summary>
    /// <param name="name">The name/label for this address (required).</param>
    /// <param name="line1">The first line of the address (required).</param>
    /// <param name="line2">The optional second line of the address.</param>
    /// <param name="postalCode">The optional postal code.</param>
    /// <param name="city">The city name (required).</param>
    /// <param name="country">The country name (required).</param>
    /// <param name="isPrimary">Indicates whether this is the primary address (default: false).</param>
    /// <returns>A new <see cref="Address"/> instance wrapped in a Result.</returns>
    public static Result<Address> Create(string name, string line1, string line2, string postalCode, string city, string country, bool isPrimary = false)
    {
        return Result<Address>.Success()
            .Ensure(_ => !string.IsNullOrWhiteSpace(name), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(name)))
            .Ensure(_ => !string.IsNullOrWhiteSpace(line1), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(line1)))
            .Ensure(_ => !string.IsNullOrWhiteSpace(city), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(city)))
            .Ensure(_ => !string.IsNullOrWhiteSpace(country), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty, nameof(country)))
            .Bind(_ => new Address(name, line1, line2, postalCode, city, country, isPrimary));
    }

    /// <summary>
    /// Changes the name/label of the address if different from the current value.
    /// </summary>
    /// <param name="name">The new name/label (required).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangeName(string name)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(name), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty))
            .Set(e => e.Name, name)
            .Apply();
    }

    /// <summary>
    /// Changes the first line of the address if different from the current value.
    /// </summary>
    /// <param name="line1">The new first line (required).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangeLine1(string line1)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(line1), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty))
            .Set(e => e.Line1, line1)
            .Apply();
    }

    /// <summary>
    /// Changes the second line of the address if different from the current value.
    /// </summary>
    /// <param name="line2">The new second line (optional).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangeLine2(string line2)
    {
        return this.Change()
            .Set(e => e.Line2, line2)
            .Apply();
    }

    /// <summary>
    /// Changes the postal code of the address if different from the current value.
    /// </summary>
    /// <param name="postalCode">The new postal code (optional).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangePostalCode(string postalCode)
    {
        return this.Change()
            .Set(e => e.PostalCode, postalCode)
            .Apply();
    }

    /// <summary>
    /// Changes the city of the address if different from the current value.
    /// </summary>
    /// <param name="city">The new city (required).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangeCity(string city)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(city), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty))
            .Set(e => e.City, city)
            .Apply();
    }

    /// <summary>
    /// Changes the country of the address if different from the current value.
    /// </summary>
    /// <param name="country">The new country (required).</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> ChangeCountry(string country)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(country), Errors.Validation.Error(Resources.Validator_MustNotBeEmpty))
            .Set(e => e.Country, country)
            .Apply();
    }

    /// <summary>
    /// Sets whether this address is the primary address.
    /// </summary>
    /// <param name="isPrimary">True to set as primary; false otherwise.</param>
    /// <returns>The updated <see cref="Address"/> wrapped in a Result.</returns>
    public Result<Address> SetPrimary(bool isPrimary)
    {
        return this.Change()
            .Set(e => e.IsPrimary, isPrimary)
            .Apply();
    }
}
