// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace TestOutput.Modules.TestCore.Application;

using BridgingIT.DevKit.Common;
using TestOutput.Modules.TestCore.Domain.Events;
using TestOutput.Modules.TestCore.Domain.Model;
using FluentValidation;

/// <summary>
/// Command to delete an existing <see cref="Customer"/> aggregate
/// by its unique identifier.
/// Implements <see cref="RequestBase{TResponse}"/> with a <see cref="Unit"/> response,
/// indicating no return payload when successful.
/// </summary>
/// <param name="id">The string representation of the Customer's identifier (GUID).</param>
public class CustomerDeleteCommand(string id) : RequestBase<Unit>
{
    /// <summary>
    /// Gets the Customer identifier as a string.
    /// Will be validated as a <see cref="Guid"/> by the <see cref="Validator"/>.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Validation rules for <see cref="CustomerDeleteCommand"/> using FluentValidation.
    /// </summary>
    public class Validator : AbstractValidator<CustomerDeleteCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Id)
                .MustBeValidGuid()
                .WithMessage("Invalid guid.");
        }
    }
}
