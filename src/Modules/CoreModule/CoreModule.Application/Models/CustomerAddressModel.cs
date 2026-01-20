// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

/// <summary>
/// Data transfer object (DTO) representing a customer address.
/// Used by the application and presentation layers to expose address details to clients.
/// </summary>
public class CustomerAddressModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the address.
    /// </summary>
    /// <example>7fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the optional name or label for this address.
    /// </summary>
    /// <example>Home</example>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the first line of the address (street, number, etc.).
    /// </summary>
    /// <example>123 Main Street</example>
    public string Line1 { get; set; }

    /// <summary>
    /// Gets or sets the optional second line of the address (apartment, suite, etc.).
    /// </summary>
    /// <example>Apt 4B</example>
    public string Line2 { get; set; }

    /// <summary>
    /// Gets or sets the optional postal code.
    /// </summary>
    /// <example>12345</example>
    public string PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the city name.
    /// </summary>
    /// <example>New York</example>
    public string City { get; set; }

    /// <summary>
    /// Gets or sets the country name.
    /// </summary>
    /// <example>United States</example>
    public string Country { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary address.
    /// </summary>
    /// <example>true</example>
    public bool IsPrimary { get; set; }
}