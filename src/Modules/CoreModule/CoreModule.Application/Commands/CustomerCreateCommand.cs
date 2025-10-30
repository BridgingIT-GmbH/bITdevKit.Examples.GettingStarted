// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Command to create a new <see cref="Customer"/> aggregate.
/// Implements <see cref="RequestBase{TResponse}"/> to request a <see cref="CustomerModel"/> result.
/// Validates required input (id must be empty, first/last names, email must be provided).
/// </summary>
public class CustomerCreateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    /// <summary>
    /// Gets or sets the DTO (<see cref="CustomerModel"/>) that contains data for the new customer.
    /// Must not be null. The Id must be default/empty since it will be generated.
    /// </summary>
    public CustomerModel Model { get; set; } = model;

    /// <summary>
    /// Validation rules for <see cref="CustomerCreateCommand"/> using FluentValidation.
    /// </summary>
    public class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();
            this.RuleFor(c => c.Model.Id).MustBeDefaultOrEmptyGuid();

            this.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty().WithMessage("Must not be empty.");
        }
    }
}
