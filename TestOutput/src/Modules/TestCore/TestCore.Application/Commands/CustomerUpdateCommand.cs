// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace TestOutput.Modules.TestCore.Application;

using BridgingIT.DevKit.Common;
using TestOutput.Modules.TestCore.Domain.Model;
using FluentValidation;

/// <summary>
/// Command to update an existing <see cref="Customer"/> aggregate.
/// Contains a <see cref="CustomerModel"/> with updated properties.
/// Expects the provided Id to already exist.
/// </summary>
public class CustomerUpdateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    /// <summary>
    /// Gets or sets the incoming DTO (<see cref="CustomerModel"/>) representing the updated state of a customer.
    /// Must include a valid non-empty Id.
    /// </summary>
    public CustomerModel Model { get; set; } = model;

    /// <summary>
    /// Validation rules for <see cref="CustomerUpdateCommand"/>.
    /// </summary>
    public class Validator : AbstractValidator<CustomerUpdateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();

            // Id must be set to a non-empty Guid (unlike CreateCommand where it's empty)
            this.RuleFor(c => c.Model.Id).MustNotBeDefaultOrEmptyGuid();

            this.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");
        }
    }
}
