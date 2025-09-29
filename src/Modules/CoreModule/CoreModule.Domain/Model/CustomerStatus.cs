// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

using BridgingIT.DevKit.Common;
using System.Diagnostics;

/// <summary>
/// Represents the status of a <see cref="Customer"/> in the domain.
/// Inherits from <see cref="Enumeration"/> to provide strongly typed
/// enumeration behavior with additional properties (description, enabled flag).
/// </summary>
[DebuggerDisplay("Id={Id}, Value={Value}")]
public class CustomerStatus : Enumeration
{
    /// <summary>
    /// Status indicating that a customer is a sales lead (default).
    /// </summary>
    public static readonly CustomerStatus Lead = new(1, nameof(Lead), "Lead customer");

    /// <summary>
    /// Status indicating that a customer is active.
    /// </summary>
    public static readonly CustomerStatus Active = new(2, nameof(Active), "Active customer");

    /// <summary>
    /// Status indicating that a customer is retired and no longer active.
    /// </summary>
    public static readonly CustomerStatus Retired = new(3, nameof(Retired), "Retired customer");

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerStatus"/> enumeration.
    /// </summary>
    /// <param name="id">The unique numeric identifier of the status.</param>
    /// <param name="value">The display value of the status.</param>
    /// <param name="description">A human-readable description of the status.</param>
    /// <param name="enabled">Indicates whether the status is enabled (default true).</param>
    private CustomerStatus(int id, string value, string description, bool enabled = true)
        : base(id, value)
    {
        this.Enabled = enabled;
        this.Description = description;
    }

    /// <summary>
    /// Gets a flag indicating whether the status is enabled.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the description of this status.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Returns all defined <see cref="CustomerStatus"/> values.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> of all customer statuses.</returns>
    public static IEnumerable<CustomerStatus> GetAll()
    {
        return GetAll<CustomerStatus>();
    }

    /// <summary>
    /// Implicitly converts an <see cref="int"/> identifier into a corresponding
    /// <see cref="CustomerStatus"/> if one exists.
    /// </summary>
    /// <param name="id">The identifier of the customer status.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if no customer status is defined with the specified <paramref name="id"/>.
    /// </exception>
    /// <returns>The <see cref="CustomerStatus"/> instance associated with the given <paramref name="id"/>.</returns>
    public static implicit operator CustomerStatus(int id)
    {
        return GetAll<CustomerStatus>().FirstOrDefault(e => e.Id == id)
            ?? throw new ArgumentException($"No CustomerStatus exists with Id {id}", nameof(id));
    }
}