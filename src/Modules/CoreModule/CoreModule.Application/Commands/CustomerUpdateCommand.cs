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

            // Address validation rules
            this.RuleFor(c => c.Model.Addresses)
                .Must(addresses => addresses == null || addresses.Count(a => a.IsPrimary) <= 1)
                .WithMessage("Only one address can be marked as primary");

            this.RuleForEach(c => c.Model.Addresses).ChildRules(address =>
            {
                address.RuleFor(a => a.Line1)
                    .NotEmpty().WithMessage("Address line 1 is required")
                    .MaximumLength(256).WithMessage("Address line 1 must not exceed 256 characters");

                address.RuleFor(a => a.Line2)
                    .MaximumLength(256).WithMessage("Address line 2 must not exceed 256 characters");

                address.RuleFor(a => a.City)
                    .NotEmpty().WithMessage("City is required")
                    .MaximumLength(100).WithMessage("City must not exceed 100 characters");

                address.RuleFor(a => a.Country)
                    .NotEmpty().WithMessage("Country is required")
                    .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

                address.RuleFor(a => a.Name)
                    .MaximumLength(128).WithMessage("Address name must not exceed 128 characters");

                address.RuleFor(a => a.PostalCode)
                    .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters");
            });
        }
    }
}
