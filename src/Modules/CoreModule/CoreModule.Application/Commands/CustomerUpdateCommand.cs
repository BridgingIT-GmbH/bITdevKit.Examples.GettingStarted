// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Command to update an existing <see cref="Customer"/> Aggregate.
/// </summary>
public class CustomerUpdateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    /// <summary>Gets or sets the Model (<see cref="CustomerModel"/>) that contains data for the Aggregate to update.</summary>
    public CustomerModel Model { get; set; } = model;

    /// <summary>Validation rules for <see cref="CustomerUpdateCommand"/>.</summary>
    public class Validator : AbstractValidator<CustomerUpdateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();

            this.RuleFor(c => c.Model.Id).MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid guid.");

            this.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");
        }
    }
}
