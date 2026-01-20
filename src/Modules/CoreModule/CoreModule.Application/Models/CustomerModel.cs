// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

/// <summary>
/// Data transfer object (DTO) representing a <see cref="Domain.Model.Customer"/>.
/// Used by the application and presentation layers to expose Aggregate to clients.
/// </summary>
public class CustomerModel
{
    /// <summary>
    /// Gets or sets the unique identifier of the customer.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the customer's first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the customer's last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; }

    /// <summary>
    /// Gets or sets the customer number in format CUS-YYYY-NNNNNN (e.g., CUS-2024-100000).
    /// This is a system-generated sequential identifier unique per year.
    /// </summary>
    /// <example>CUS-2024-100000</example>
    public string Number { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the customer (optional).
    /// </summary>
    /// <example>1990-05-15</example>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the customer's email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    public string Email { get; set; }

    /// <summary>
    /// Gets or sets the current status of the customer as a string value.
    /// Valid values: "Lead", "Active", "Retired". See CustomerStatus enumeration for valid values.
    /// </summary>
    /// <example>Active</example>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets the concurrency version token (as a string Guid).
    /// Used for optimistic concurrency control to prevent conflicting updates.
    /// Must be provided when updating to ensure the entity hasn't been modified by another operation.
    /// </summary>
    /// <example>8f7a9b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c</example>
    public string ConcurrencyVersion { get; set; }

    /// <summary>
    /// Gets or sets the collection of addresses associated with this customer.
    /// </summary>
    public List<CustomerAddressModel> Addresses { get; set; }
}
