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
                .WithMessage(Resources.Validator_InvalidValue);

            this.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);

            this.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);

            this.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);

            // Address validation rules
            this.RuleFor(c => c.Model.Addresses)
                .Must(addresses => addresses.IsNullOrEmpty() || addresses.Count(a => a.IsPrimary) == 1)
                .WithMessage(Resources.Validator_OnePrimaryAddressRequired);

            this.RuleForEach(c => c.Model.Addresses).ChildRules(address =>
            {
                address.RuleFor(a => a.Line1)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(256).WithMessage(Resources.Validator_MustNotExceed256Characters);

                address.RuleFor(a => a.Line2)
                    .MaximumLength(256).WithMessage(Resources.Validator_MustNotExceed256Characters);

                address.RuleFor(a => a.City)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(100).WithMessage(Resources.Validator_MustNotExceed100Characters);

                address.RuleFor(a => a.Country)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(100).WithMessage(Resources.Validator_MustNotExceed100Characters);

                address.RuleFor(a => a.Name)
                    .MaximumLength(128).WithMessage(Resources.Validator_MustNotExceed128Characters);

                address.RuleFor(a => a.PostalCode)
                    .MaximumLength(20).WithMessage(Resources.Validator_MustNotExceed20Characters);
            });
        }
    }
}
