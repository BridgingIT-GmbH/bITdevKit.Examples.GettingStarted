﻿// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Domain;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles <see cref="CustomerUpdatedDomainEvent"/> notifications.
/// Triggered whenever a new <see cref="Domain.Model.Customer"/> is created.
/// Extend <see cref="Process"/> with integration, logging, or side-effects.
/// </summary>
/// <remarks>
/// Initializes a new instance of the handler with logging support.
/// </remarks>
/// <param name="loggerFactory">Factory used for creating loggers.</param>
public class CustomerUpdatedDomainEventHandler(ILoggerFactory loggerFactory)
        : DomainEventHandlerBase<CustomerUpdatedDomainEvent>(loggerFactory)
{
    /// <summary>
    /// Determines whether this handler can handle the given event.
    /// Returns <c>true</c> unconditionally in this template.
    /// </summary>
    public override bool CanHandle(CustomerUpdatedDomainEvent notification) => true;

    /// <summary>
    /// Processes the <see cref="CustomerUpdatedDomainEvent"/>.
    /// Add custom logic here (e.g., start workflows, send welcome mails).
    /// </summary>
    public override Task Process(CustomerUpdatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // implement event reaction logic (audit, notify, etc.)
        this.Logger.LogInformation("CustomerUpdatedDomainEvent handled in Application");

        return Task.CompletedTask;
    }
}