// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace TestOutput.Modules.TestCore.Domain.Events;

using BridgingIT.DevKit.Domain;
using TestOutput.Modules.TestCore.Domain.Model;

/// <summary>
/// Domain event that is raised whenever an existing <see cref="Customer"/> aggregate has been updated.
/// </summary>
/// <remarks>
/// Follows the Domain Events pattern in DDD:
/// - Published by the <see cref="Customer"/> aggregate (e.g., in  <c>ChangeName</c>, <c>ChangeEmail</c>, <c>ChangeStatus</c>).
/// - Consumed by one or more <see cref="DomainEventHandlerBase{TEvent}"/>
///   implementations to trigger side effects such as updating projections, sending notifications or audit logging.
/// </remarks>
public partial class CustomerUpdatedDomainEvent(Customer model) : DomainEventBase
{
    /// <summary>
    /// Gets the updated <see cref="Customer"/> aggregate instance that triggered this event.
    /// </summary>
    public Customer Model { get; private set; } = model;
}