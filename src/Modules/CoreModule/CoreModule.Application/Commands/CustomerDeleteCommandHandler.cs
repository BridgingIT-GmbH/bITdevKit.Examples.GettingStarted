// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

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

                    // Register domain event
                    //.Tap(e => e.DomainEvents.Register(new CustomerDeletedDomainEvent(e)))

                    // Side-effect: log/audit
                    .Tap(_ => Console.WriteLine("AUDIT"))

                    // Map repository action result → Unit (since no response body expected)
                    .Map(_ => Unit.Value),
                cancellationToken: cancellationToken);
}