// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain.Events;

using BridgingIT.DevKit.Domain;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain.Model;

public class CustomerCreatedDomainEvent : DomainEventBase
{
    public CustomerCreatedDomainEvent(Customer model) => this.Model = model;

    public Customer Model { get; }
}