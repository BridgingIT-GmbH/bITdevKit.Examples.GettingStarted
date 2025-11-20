// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;

/// <summary>
/// Domain event that is raised whenever an existing <see cref="Customer"/> aggregate has been deleted.
/// </summary>
/// <remarks>
/// Follows the Domain Events pattern in DDD:
/// - Published by the <see cref="Customer"/> aggregate (e.g., in  <c>ChangeName</c>, <c>ChangeEmail</c>, <c>ChangeStatus</c>).
/// - Consumed by one or more <see cref="DomainEventHandlerBase{TEvent}"/>
///   implementations to trigger side effects such as updating projections, sending notifications or audit logging.
/// </remarks>
[ExcludeFromCodeCoverage]
public partial class CustomerDeletedDomainEvent(Customer model) : DomainEventBase
{
    /// <summary>
    /// Gets the deleted <see cref="Customer"/> aggregate instance that triggered this event.
    /// </summary>
    public Customer Model { get; private set; } = model;
}