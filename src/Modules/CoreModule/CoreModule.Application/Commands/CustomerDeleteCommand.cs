// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
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

/// <summary>
/// Handler for processing <see cref="CustomerDeleteCommand"/>.
/// Responsible for locating and deleting the specified <see cref="Customer"/> aggregate
/// from the repository.
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) and timeout (<see cref="HandlerTimeoutAttribute"/>).
/// - Returns <see cref="Unit"/> on successful deletion.
/// - Produces <see cref="EntityNotFoundError"/> if the customer does not exist.
/// </remarks>
[HandlerRetry(2, 100)]   // retry on transient errors (2 attempts, 100ms wait)
[HandlerTimeout(500)]    // operation must complete within 500ms
public class CustomerDeleteCommandHandler(
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerDeleteCommand, Unit>
{
    /// <summary>
    /// Handles the <see cref="CustomerDeleteCommand"/> request.
    /// Deletes the <see cref="Customer"/> with the given Id if it exists.
    /// </summary>
    /// <param name="request">The delete command containing the Customer Id.</param>
    /// <param name="options">Pipeline send options (e.g. retry policy).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A <see cref="Result{Unit}"/> indicating success if deleted, or failure if not found.
    /// </returns>
    protected override async Task<Result<Unit>> HandleAsync(
        CustomerDeleteCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
        await Result<Unit>.Success()

            // Attempt deletion in repository
            .BindAsync(async (_, ct) =>
                await repository.DeleteResultAsync(CustomerId.Create(request.Id), cancellationToken)

                    // Ensure deletion actually occurred
                    .Ensure(e => e == RepositoryActionResult.Deleted, new EntityNotFoundError())

                    // Side-effect: log/audit
                    .Tap(_ => Console.WriteLine("AUDIT"))

                    // Map repository action result → Unit (since no response body expected)
                    .Map(_ => Unit.Value),
                cancellationToken: cancellationToken);
}