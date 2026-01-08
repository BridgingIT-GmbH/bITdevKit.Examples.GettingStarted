// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Query for retrieving a single <see cref="Customer"/> Aggregate by its unique identifier.
/// </summary>
/// <param name="id">The string representation of the Aggregate's identifier.</param>
public class CustomerFindOneQuery(string id) : RequestBase<CustomerModel>
{
    /// <summary>Gets or sets the Aggregate id.</summary>
    public string Id { get; } = id;

    /// <summary>Validation rules for <see cref="CustomerFindOneQuery"/>.</summary>
    public class Validator : AbstractValidator<CustomerFindOneQuery>
    {
        public Validator()
        {
            this.RuleFor(c => c.Id).MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid guid.");
        }
    }
}
