// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Command to change a Aggregate status to any valid <see cref="Domain.Model.CustomerStatus"/>.
/// </summary>
/// <param name="id">The string representation of the Aggregate's identifier.</param>
/// <param name="status">Target status value (e.g., "Lead", "Active", "Retired").</param>
public class CustomerUpdateStatusCommand(string id, string status) : RequestBase<CustomerModel>
{
    /// <summary>Gets or sets the Aggregate id.</summary>
    public string Id { get; set; } = id;

    /// <summary>Gets or sets target status value.</summary>
    public string Status { get; set; } = status;

    /// <summary>Validator ensuring valid id and status.</summary>
    public class Validator : AbstractValidator<CustomerUpdateStatusCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Id).MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid guid.");

            this.RuleFor(c => c.Status)
                .NotEmpty()
                .Must(value => CustomerStatus.GetAll().Any(s => s.Value == value))
                .WithMessage("Invalid status value. Valid values: Lead, Active, Retired.");
        }
    }
}
