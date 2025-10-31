// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

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
//[HandlerRetry(2, 100)]   // retry on transient errors (2 attempts, 100ms wait)
//[HandlerTimeout(500)]    // operation must complete within 500ms
public class CustomerDeleteCommandHandler(
    ILogger<CustomerDeleteCommandHandler> logger,
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
            // STEP 1 - Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(request.Id), cancellationToken: cancellationToken)
            .Unless((e) => !e?.AuditState?.IsDeleted() == true,
                new NotFoundError("Entity already deleted"))

            // STEP 2 - Register domain event
            .Tap(e => e.DomainEvents.Register(new CustomerDeletedDomainEvent(e)))

            // STEP 3 - Attempt deletion in repository
            .BindAsync(async (e, ct) =>
                await repository.DeleteResultAsync(e, cancellationToken: ct), cancellationToken)

            // STEP 4 - Publish domain events
            .TapAsync(async (e, ct) =>
                await e.entity.DomainEvents.PublishAsync(this.notifier, ct), cancellationToken: cancellationToken)

            // STEP 5 - Side-effect: log/audit
            .Tap(_ => Console.WriteLine("AUDIT"))

            // STEP 6 - Finish and return
            .Log(logger, "Entity deleted")
            .Unwrap();
}