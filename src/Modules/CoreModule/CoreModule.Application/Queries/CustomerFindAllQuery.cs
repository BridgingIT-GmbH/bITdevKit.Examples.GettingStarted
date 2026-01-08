// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System.Collections.Generic;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Query for retrieving all <see cref="Customer"/> Aggregates.
/// </summary>
public class CustomerFindAllQuery : RequestBase<IEnumerable<CustomerModel>>
{
    /// <summary>Gets or sets the optional filter criteria used when retrieving customers.</summary>
    public FilterModel Filter { get; set; }
}
