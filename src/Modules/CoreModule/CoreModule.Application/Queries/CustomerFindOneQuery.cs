// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using FluentValidation;

/// <summary>
/// Query for retrieving a single <see cref="Customer"/> by its unique identifier.
/// Returns a <see cref="CustomerModel"/> if found, otherwise may result in a failure.
/// </summary>
/// <param name="customerId">The identifier of the customer to retrieve (GUID string).</param>
public class CustomerFindOneQuery(string customerId) : RequestBase<CustomerModel>
{
    /// <summary>
    /// Gets the customer ID to look up as a string (expected to be a valid GUID).
    /// </summary>
    public string CustomerId { get; } = customerId;

    /// <summary>
    /// Validation rules for <see cref="CustomerFindOneQuery"/>.
    /// </summary>
    public class Validator : AbstractValidator<CustomerFindOneQuery>
    {
        public Validator()
        {
            this.RuleFor(c => c.CustomerId)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");
        }
    }
}
