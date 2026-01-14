// ============================================================================
// TEMPLATE: Update Command Handler for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Processes [Entity]UpdateCommand to update an existing aggregate instance.
//   Loads entity, applies changes via domain methods, handles concurrency, persists.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Property]     - Domain properties to update (e.g., FirstName, LastName, Email, Status)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Commands/
//   3. File name: [Entity]UpdateCommandHandler.cs
//   4. Customize change methods based on your aggregate's public API
//   5. Add business rule checks as needed
//   6. Uncomment [Retry] and [Timeout] attributes if needed (recommended)
//
// RELATED PATTERNS:
//   - RequestHandlerBase<TRequest, TResponse>: bITdevKit base for handlers
//   - Result<T>: Functional error handling pattern
//   - IGenericRepository<T>: Repository abstraction
//   - Optimistic Concurrency: ConcurrencyVersion prevents lost updates
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="[Entity]UpdateCommand"/>.
/// Maps DTO → domain, checks business rules, updates the entity in the repository,
/// and maps back to <see cref="[Entity]Model"/> for returning to the client.
/// </summary>
/// <remarks>
/// Processing steps:
/// 1. Load existing entity from repository by ID
/// 2. Validate business rules (uniqueness excluding current entity, invariants)
/// 3. Apply changes via aggregate's change methods
/// 4. Set concurrency version for optimistic concurrency check
/// 5. Persist updated entity (EF Core checks concurrency version)
/// 6. Log audit trail
/// 7. Map domain aggregate → DTO model
///
/// Pipeline behaviors (applied automatically):
/// - ValidationPipelineBehavior: Executes FluentValidation rules
/// - RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute present)
/// - TimeoutPipelineBehavior: Enforces max execution time (if [Timeout] attribute present)
/// </remarks>
// Uncomment these attributes for production use:
// [Retry(2)] // Retry 2 times on transient failures
// [Timeout(30)] // Timeout after 30 seconds
public class [Entity]UpdateCommandHandler(
    ILogger<[Entity]UpdateCommandHandler> logger,
    IMapper mapper,
    IGenericRepository<[Entity]> repository)
    : RequestHandlerBase<[Entity]UpdateCommand, [Entity]Model>
{
    /// <summary>
    /// Handles the <see cref="[Entity]UpdateCommand"/>. Steps:
    /// 1. Load existing aggregate from repository.
    /// 2. Validate business rules using Rule pattern.
    /// 3. Apply changes via aggregate change methods.
    /// 4. Set concurrency version for optimistic concurrency check.
    /// 5. Persist updated aggregate to repository.
    /// 6. Perform audit/logging side-effects.
    /// 7. Map updated domain aggregate to <see cref="[Entity]Model"/> DTO.
    /// </summary>
    /// <param name="request">The update command containing the DTO model with changes.</param>
    /// <param name="options">Pipeline options (retry policy, correlation context).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// A Result containing the updated <see cref="[Entity]Model"/> if successful,
    /// or a failure result with validation/business rule/concurrency errors.
    /// </returns>
    protected override async Task<Result<[Entity]Model>> HandleAsync(
        [Entity]UpdateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // ============================================================
            // STEP 1: Load existing entity from repository
            // ============================================================
            await repository.FindOneResultAsync(
                [Entity]Id.Create(request.Model.Id),
                cancellationToken: cancellationToken)
            .Log(logger, "[Entity] {Id} loaded for update", r => [r.Value.Id])

            // ============================================================
            // STEP 2: Validate business rules using Rule pattern
            // ============================================================
            // The Rule pattern allows chaining multiple business rules.
            // Common update rules: uniqueness (excluding current entity), state transitions
            .UnlessAsync(async (entity, ct) => await Rule
                // Example: Ensure required fields are not empty
                // .Add(RuleSet.IsNotEmpty(entity.[Property]))

                // Example: Ensure status transition is valid
                // .Add(RuleSet.NotEqual(entity.[Property], "forbidden"))

                // Example: Check email uniqueness excluding current entity
                // Note: EmailShouldBeUniqueRule needs custom implementation to exclude current ID
                // .Add(new EmailShouldBeUniqueRule(request.Model.Email, repository, excludeId: entity.Id))

                .CheckAsync(cancellationToken), cancellationToken: cancellationToken)
            .Log(logger, "Business rules validated for update")

            // ============================================================
            // STEP 3: Apply changes via aggregate change methods
            // ============================================================
            // Call aggregate's public change methods (e.g., ChangeName, ChangeEmail).
            // Each change method returns Result<[Entity]> with built-in validation.
            // Chain multiple .Bind() calls for multiple property changes.
            // Customize based on your aggregate's public API:

            // .Bind(e => e.Change[Property1](request.Model.[Property1]))
            // .Bind(e => e.Change[Property2](request.Model.[Property2]))
            // .Bind(e => e.Change[Property3](request.Model.[Property3]))

            .Log(logger, "[Entity] changes applied")

            // ============================================================
            // STEP 4: Set concurrency version for optimistic concurrency
            // ============================================================
            // The concurrency version must match the database version.
            // EF Core will throw DbUpdateConcurrencyException if mismatch occurs.
            .Tap(entity =>
            {
                if (!string.IsNullOrWhiteSpace(request.Model.ConcurrencyVersion))
                {
                    entity.ConcurrencyVersion = Guid.Parse(request.Model.ConcurrencyVersion);
                }
            })
            .Log(logger, "Concurrency version set")

            // ============================================================
            // STEP 5: Persist updated entity to repository
            // ============================================================
            // UpdateResultAsync returns Result<[Entity]> with the updated entity.
            // Domain events are automatically published by repository behaviors.
            // EF Core checks concurrency version and throws if conflict detected.
            .BindAsync(async (entity, ct) =>
                await repository.UpdateResultAsync(entity, ct), cancellationToken)
            .Log(logger, "AUDIT - [Entity] {Id} updated", r => [r.Value.Id])

            // ============================================================
            // STEP 6: Map domain aggregate → DTO model
            // ============================================================
            // Use Mapster via IMapper abstraction to convert domain to DTO.
            .MapResult<[Entity], [Entity]Model>(mapper)
            .Log(logger, "[Entity] mapped to {@Model}", r => [r.Value]);
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. LOAD-MODIFY-SAVE PATTERN:
//    - Load: repository.FindOneResultAsync([Entity]Id.Create(id))
//    - Modify: .Bind(e => e.Change[Property](...))
//    - Save: repository.UpdateResultAsync(entity)
//
// 2. RESULT<T> CHAINING:
//    - .Bind(): Calls domain method, propagates errors
//    - .BindAsync(): Async operations (load, save)
//    - .Tap(): Side effects without changing result value
//    - .Log(): Structured logging
//
// 3. OPTIMISTIC CONCURRENCY:
//    - Client sends ConcurrencyVersion in DTO
//    - Handler sets: entity.ConcurrencyVersion = Guid.Parse(...)
//    - EF Core compares with database value
//    - Mismatch → DbUpdateConcurrencyException → Result.Failure with ConcurrencyError
//
// 4. AGGREGATE CHANGE METHODS:
//    - Domain exposes change methods (e.g., ChangeName, ChangeEmail)
//    - Each method: validates → modifies state → registers domain event → returns Result
//    - Pattern: this.Change().Ensure().Set().Register().Apply()
//
// 5. RULE PATTERN FOR BUSINESS VALIDATION:
//    - Similar to create, but may need to exclude current entity from uniqueness checks
//    - Example: EmailShouldBeUniqueRule with excludeId parameter
//
// USAGE EXAMPLES:
//
//   // Success scenario:
//   var result = await requester.SendAsync(
//       new [Entity]UpdateCommand(model),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var updated[Entity] = result.Value;
//       logger.LogInformation("Updated [Entity] {Id}", updated[Entity].Id);
//   }
//
//   // Concurrency conflict scenario (409 Conflict):
//   if (result.HasError<ConcurrencyError>())
//   {
//       logger.LogWarning("Concurrency conflict: entity was modified by another user");
//       // Return 409 Conflict to client with latest version
//   }
//
//   // Not found scenario (404 Not Found):
//   if (result.HasError<NotFoundError>())
//   {
//       logger.LogWarning("Entity not found: {Id}", request.Model.Id);
//   }
//
// ============================================================================
// CUSTOMIZATION CHECKLIST
// ============================================================================
//
// 1. Update STEP 3 to call your aggregate's change methods:
//    - .Bind(e => e.ChangeName(request.Model.Name))
//    - .Bind(e => e.ChangeStatus(request.Model.Status))
//    - etc.
// 2. Add business rule validation in STEP 2 (uniqueness excluding current, state transitions)
// 3. Uncomment [Retry] and [Timeout] attributes for production
// 4. Ensure mapping configuration exists in [Module]MapperRegister
// 5. Handle child collections if your aggregate has them (add/remove/update patterns)
//
// ============================================================================
