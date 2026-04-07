// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Command to delete an existing <see cref="Customer"/> Aggregate by its unique identifier.
/// </summary>
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
[Command]
public partial class CustomerDeleteCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerDeleteCommand"/> class.
    /// </summary>
    /// <param name="id">The string representation of the Aggregate's identifier.</param>
    public CustomerDeleteCommand(string id)
    {
        Id = id;
    }

    /// <summary>Gets or sets the Aggregate id.</summary>
    [ValidateNotEmptyGuid("Invalid guid.")]
    public string Id { get; }

    /// <summary>
    /// Handles the <see cref="CustomerDeleteCommand"/> request.
    /// Deletes the <see cref="Customer"/> with the given Id if it exists.
    /// </summary>
    /// <param name="logger">Logger used for audit and diagnostic information.</param>
    /// <param name="repository">Repository used to load and delete the customer aggregate.</param>
    /// <param name="notifier">Notifier used to publish registered domain events.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A <see cref="Result{Unit}"/> indicating success if deleted, or failure if not found.
    /// </returns>
    [Handle]
    private async Task<Result<Unit>> HandleAsync(
        ILogger<CustomerDeleteCommand> logger,
        IGenericRepository<Customer> repository,
        INotifier notifier,
        CancellationToken cancellationToken) =>
            // STEP 1 - Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(Id), cancellationToken: cancellationToken)
            //.Unless((e) => e?.AuditState?.IsDeleted() == true, new NotFoundError("Entity already deleted"))

            // STEP 2 - Register domain event
            .Tap(e => e.DomainEvents.Register(new CustomerDeletedDomainEvent(e)))

            // STEP 3 - Attempt deletion in repository
            .BindAsync(async (e, ct) =>
                await repository.DeleteResultAsync(e, cancellationToken: ct), cancellationToken)

            // STEP 4 - Publish domain events
            .TapAsync(async (e, ct) =>
                await e.entity.DomainEvents.PublishAsync(notifier, ct), cancellationToken: cancellationToken)

            // STEP 5 - Side effects (audit/logging)
            .Log(logger, "AUDIT - Customer {Id} deleted", r => [r.Value.entity.Id])

            // STEP 6 - Finish and return
            .Log(logger, "Entity deleted")
            .Unwrap();
}
