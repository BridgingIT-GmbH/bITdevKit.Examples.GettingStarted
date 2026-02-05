// ============================================================================
// TEMPLATE: Delete Command Handler for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Processes [Entity]DeleteCommand to permanently delete an aggregate instance.
//   Loads entity, registers domain event, deletes from repository, publishes events.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Commands/
//   3. File name: [Entity]DeleteCommandHandler.cs
//   4. Add business rule checks if deletion should be conditional
//   5. Uncomment [Retry] and [Timeout] attributes if needed (recommended)
//
// RELATED PATTERNS:
//   - RequestHandlerBase<TRequest, TResponse>: bITdevKit base for handlers
//   - Result<Unit>: Represents successful operation with no return value
//   - IGenericRepository<T>: Repository abstraction
//   - INotifier: Event publishing abstraction (MediatR)
//   - Domain Events: [Entity]DeletedDomainEvent for side effects
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Events;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for processing <see cref="[Entity]DeleteCommand"/>.
/// Responsible for locating and deleting the specified <see cref="[Entity]"/> aggregate
/// from the repository.
/// </summary>
/// <remarks>
/// Processing steps:
/// 1. Load existing entity from repository by ID
/// 2. Validate business rules (e.g., "can this entity be deleted?")
/// 3. Register domain event for deletion
/// 4. Delete entity from repository
/// 5. Publish domain events for side effects
/// 6. Log audit trail
/// 7. Return Unit (successful completion)
///
/// Pipeline behaviors (applied automatically):
/// - ValidationPipelineBehavior: Executes FluentValidation rules
/// - RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute present)
/// - TimeoutPipelineBehavior: Enforces max execution time (if [Timeout] attribute present)
/// </remarks>
// Uncomment these attributes for production use:
// [Retry(2)] // Retry 2 times on transient failures
// [Timeout(30)] // Timeout after 30 seconds
public class [Entity]DeleteCommandHandler(
    ILogger<[Entity]DeleteCommandHandler> logger,
    IGenericRepository<[Entity]> repository,
    INotifier notifier)
    : RequestHandlerBase<[Entity]DeleteCommand, Unit>
{
    /// <summary>
    /// Handles the <see cref="[Entity]DeleteCommand"/> request.
    /// Deletes the <see cref="[Entity]"/> with the given Id if it exists.
    /// </summary>
    /// <param name="request">The delete command containing the [Entity] ID.</param>
    /// <param name="options">Pipeline send options (e.g. retry policy).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// A <see cref="Result{Unit}"/> indicating success if deleted, or failure if not found.
    /// </returns>
    protected override async Task<Result<Unit>> HandleAsync(
        [Entity]DeleteCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // ============================================================
            // STEP 1: Load existing entity from repository
            // ============================================================
            await repository.FindOneResultAsync(
                [Entity]Id.Create(request.Id),
                cancellationToken: cancellationToken)
            .Log(logger, "[Entity] {Id} loaded for deletion", r => [r.Value.Id])

            // ============================================================
            // STEP 2: Validate business rules for deletion
            // ============================================================
            // Add business rules if deletion should be conditional.
            // Examples:
            //   - Entity must not have active related entities (e.g., orders, subscriptions)
            //   - Entity status must allow deletion (e.g., not "Active", only "Archived")
            //   - User must have permission to delete this specific entity

            // .UnlessAsync(async (entity, ct) => await Rule
            //     .Add(RuleSet.Equal(entity.Status, [Entity]Status.Archived))
            //     .WithMessage("Only archived entities can be deleted")
            //     .CheckAsync(cancellationToken), cancellationToken: cancellationToken)

            .Log(logger, "Business rules validated for deletion")

            // ============================================================
            // STEP 3: Register domain event for deletion
            // ============================================================
            // Register [Entity]DeletedDomainEvent before deletion.
            // Event handlers can perform cleanup operations.
            .Tap(entity => entity.DomainEvents.Register(new [Entity]DeletedDomainEvent(entity)))
            .Log(logger, "[Entity]DeletedDomainEvent registered")

            // ============================================================
            // STEP 4: Delete entity from repository
            // ============================================================
            // DeleteResultAsync returns Result<([Entity] entity, bool deleted)>
            // - entity: The deleted entity instance
            // - deleted: true if deletion succeeded
            .BindAsync(async (entity, ct) =>
                await repository.DeleteResultAsync(entity, cancellationToken: ct), cancellationToken)
            .Log(logger, "AUDIT - [Entity] {Id} deleted", r => [r.Value.entity.Id])

            // ============================================================
            // STEP 5: Publish domain events for side effects
            // ============================================================
            // Domain event handlers can perform:
            //   - Audit logging to external systems
            //   - Cleanup of related data (projections, caches)
            //   - Notifications (email, webhooks)
            .TapAsync(async (result, ct) =>
                await result.entity.DomainEvents.PublishAsync(notifier, ct), cancellationToken: cancellationToken)
            .Log(logger, "Domain events published")

            // ============================================================
            // STEP 6: Return Unit (successful completion)
            // ============================================================
            // .Unwrap() converts Result<(entity, deleted)> → Result<Unit>
            .Unwrap();
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. LOAD-VALIDATE-DELETE PATTERN:
//    - Load: repository.FindOneResultAsync([Entity]Id.Create(id))
//    - Validate: .UnlessAsync(() => Rule.Add(...).CheckAsync())
//    - Delete: repository.DeleteResultAsync(entity)
//
// 2. DOMAIN EVENT REGISTRATION BEFORE DELETION:
//    - .Tap(e => e.DomainEvents.Register(new [Entity]DeletedDomainEvent(e)))
//    - Event contains full entity state before deletion
//    - Event handlers can perform cleanup operations
//
// 3. DELETE RESULT UNPACKING:
//    - DeleteResultAsync returns: Result<([Entity] entity, bool deleted)>
//    - Tuple contains: (deleted entity instance, success flag)
//    - .Unwrap() discards entity and returns Result<Unit>
//
// 4. EXPLICIT EVENT PUBLISHING:
//    - .TapAsync(async (r, ct) => await r.entity.DomainEvents.PublishAsync(notifier, ct))
//    - Required for delete operations (entity no longer in repository for behaviors to publish)
//    - Alternative: Let repository behavior publish events before deletion
//
// 5. RESULT<UNIT>:
//    - Unit = functional "void" (successful completion with no value)
//    - Caller checks result.IsSuccess for success/failure
//    - No entity data returned after deletion
//
// USAGE EXAMPLES:
//
//   // Success scenario:
//   var result = await requester.SendAsync(
//       new [Entity]DeleteCommand(entityId),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       logger.LogInformation("[Entity] deleted successfully");
//       // Return 204 No Content
//   }
//
//   // Not found scenario (404 Not Found):
//   if (result.HasError<NotFoundError>())
//   {
//       logger.LogWarning("[Entity] not found: {Id}", entityId);
//       // Return 404 Not Found
//   }
//
//   // Business rule violation (400 Bad Request):
//   if (result.HasError<ValidationError>())
//   {
//       logger.LogWarning("Cannot delete [Entity]: {Reason}", result.Errors.First().Message);
//       // Return 400 Bad Request with error details
//   }
//
// ============================================================================
// SOFT DELETE ALTERNATIVE
// ============================================================================
//
// For audit requirements or "undo" capabilities, implement soft delete instead:
//
// 1. Add properties to aggregate:
//    - public bool IsDeleted { get; private set; }
//    - public DateTime? DeletedDate { get; private set; }
//
// 2. Add domain method:
//    - public Result<[Entity]> Archive()
//    - {
//    -     return this.Change()
//    -         .Ensure(() => !this.IsDeleted, "Already deleted")
//    -         .Set(() => { this.IsDeleted = true; this.DeletedDate = DateTime.UtcNow; })
//    -         .Register(new [Entity]ArchivedDomainEvent(this))
//    -         .Apply();
//    - }
//
// 3. Update handler:
//    - .Bind(entity => entity.Archive())
//    - .BindAsync(async (entity, ct) => await repository.UpdateResultAsync(entity, ct))
//
// 4. EF Core global query filter:
//    - modelBuilder.Entity<[Entity]>().HasQueryFilter(e => !e.IsDeleted);
//
// ============================================================================
