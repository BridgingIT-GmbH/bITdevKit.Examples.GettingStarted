// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
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
    IGenericRepository<Customer> repository,
    INotifier notifier)
    : RequestHandlerBase<CustomerDeleteCommand, Unit>
{
    private readonly INotifier notifier = notifier;

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
            await Result
                // Load existing entity
                .BindAsync(async ct =>
                    (await repository.FindOneResultAsync(request.Id, cancellationToken: ct)).Value, cancellationToken)

                // Register domain event
                .Tap(e => e.DomainEvents.Register(new CustomerDeletedDomainEvent(e)))

                // Attempt deletion in repository
                .BindAsync(async (e, ct) =>
                    await repository.DeleteResultAsync(e, cancellationToken: ct), cancellationToken)

                // Publish domain events
                .TapAsync(async (e, ct) =>
                    await e.entity.DomainEvents.PublishAsync(this.notifier, ct), cancellationToken: cancellationToken)

                // Side-effect: log/audit
                .Tap(_ => Console.WriteLine("AUDIT"))

                // Return unit on finish
                .Unwrap();
}