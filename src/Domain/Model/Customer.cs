// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;

using BridgingIT.DevKit.Domain.Model;

public class Customer : AggregateRoot<Guid>
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}