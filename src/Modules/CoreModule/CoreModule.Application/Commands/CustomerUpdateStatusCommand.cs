// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using FluentValidation;

/// <summary>
/// Command to change a customer's status to any valid <see cref="CustomerStatus"/>.
/// </summary>
/// <param name="customerId">The id of the customer.</param>
/// <param name="status">Target status identifier (Enumeration Id).</param>
public class CustomerUpdateStatusCommand(string customerId, int status) : RequestBase<CustomerModel>
{
    /// <summary>Gets or sets the customer id (Guid as string).</summary>
    public string CustomerId { get; set; } = customerId;

    /// <summary>Gets or sets target status (Enumeration Id).</summary>
    public int Status { get; set; } = status;

    /// <summary>Validator ensuring valid id and status.</summary>
    public class Validator : AbstractValidator<CustomerUpdateStatusCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.CustomerId).MustNotBeDefaultOrEmptyGuid();
            this.RuleFor(c => c.Status)
                .GreaterThan(0)
                .Must(id => CustomerStatus.GetAll().Any(s => s.Id == id))
                .WithMessage("Invalid status id.");
        }
    }
}
