// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Represents the status of a <see cref="Customer"/> in the domain.
/// </summary>
[DebuggerDisplay("Id={Id}, Value={Value}")]
public partial class CustomerStatus : Enumeration
{
    /// <summary>Status indicating that a customer is a sales lead (default). </summary>
    public static readonly CustomerStatus Lead = new(1, nameof(Lead), true, "Lead customer");

    /// <summary>Status indicating that a customer is active. </summary>
    public static readonly CustomerStatus Active = new(2, nameof(Active), true, "Active customer");

    /// <summary>Status indicating that a customer is retired and no longer active. </summary>
    public static readonly CustomerStatus Retired = new(3, nameof(Retired), true, "Retired customer");

    /// <summary>Gets a flag indicating whether the status is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>Gets the description of this status.</summary>
    public string Description { get; }
}