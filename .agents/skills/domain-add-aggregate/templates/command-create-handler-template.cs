// ============================================================================
// TEMPLATE: Create Command Handler for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Processes [Entity]CreateCommand to create and persist a new aggregate instance.
//   Applies business rules, maps DTO to domain, persists, and returns created DTO.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Property]     - Domain properties for Create method (e.g., firstName, lastName, email)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Commands/
//   3. File name: [Entity]CreateCommandHandler.cs
//   4. Customize Create() call parameters based on your aggregate's factory method
//   5. Add business rule checks as needed
//   6. Uncomment [Retry] and [Timeout] attributes if needed (recommended for production)
//
// RELATED PATTERNS:
//   - RequestHandlerBase<TRequest, TResponse>: bITdevKit base for handlers
//   - Result<T>: Functional error handling pattern
//   - IGenericRepository<T>: Repository abstraction
//   - IMapper: Mapster mapping abstraction
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="[Entity]CreateCommand"/> that performs business validation,
/// enforces rules, persists a new <see cref="[Entity]"/> entity, logs steps, and maps back to DTO.
/// </summary>
/// <remarks>
/// Processing steps:
/// 1. Validate business rules (uniqueness, invariants)
/// 2. Create domain aggregate via factory method
/// 3. Persist to repository
/// 4. Log audit trail
/// 5. Map domain aggregate → DTO model
///
/// Pipeline behaviors (applied automatically):
/// - ValidationPipelineBehavior: Executes FluentValidation rules
/// - RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute present)
/// - TimeoutPipelineBehavior: Enforces max execution time (if [Timeout] attribute present)
/// - ModuleScopeBehavior: Ensures module context isolation
/// </remarks>
// Uncomment these attributes for production use:
// [Retry(2)] // Retry 2 times on transient failures
// [Timeout(30)] // Timeout after 30 seconds
public class [Entity]CreateCommandHandler(
    ILogger<[Entity]CreateCommandHandler> logger,
    IMapper mapper,
    IGenericRepository<[Entity]> repository)
    : RequestHandlerBase<[Entity]CreateCommand, [Entity]Model>
{
    /// <summary>
    /// Handles the <see cref="[Entity]CreateCommand"/>. Steps:
    /// 1. Validate business rules using Rule pattern.
    /// 2. Create domain aggregate from DTO via factory method.
    /// 3. Persist new aggregate to repository.
    /// 4. Perform audit/logging side-effects.
    /// 5. Map created domain aggregate to <see cref="[Entity]Model"/> DTO.
    /// </summary>
    /// <param name="request">The create command containing the DTO model.</param>
    /// <param name="options">Pipeline options (retry policy, correlation context).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// A Result containing the created <see cref="[Entity]Model"/> if successful,
    /// or a failure result with validation/business rule errors.
    /// </returns>
    protected override async Task<Result<[Entity]Model>> HandleAsync(
        [Entity]CreateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            await Result<[Entity]Model>
                // ============================================================
                // STEP 1: Validate business rules using Rule pattern
                // ============================================================
                // The Rule pattern allows chaining multiple business rules.
                // Rules return ValidationError results when they fail.
                // Common rules: uniqueness checks, cross-entity validation, complex invariants
                .UnlessAsync(async (ct) => await Rule
                    // Example: Ensure required fields are not empty
                    // .Add(RuleSet.IsNotEmpty(request.Model.[Property]))

                    // Example: Ensure email is unique (if applicable)
                    // .Add(new EmailShouldBeUniqueRule(request.Model.Email, repository))

                    // Example: Ensure name meets business criteria
                    // .Add(RuleSet.NotEqual(request.Model.[Property], "forbidden"))

                    .CheckAsync(cancellationToken), cancellationToken: cancellationToken)
                .Log(logger, "Business rules validated for {@Model}", r => [request.Model])

                // ============================================================
                // STEP 2: Create domain aggregate from DTO
                // ============================================================
                // Call the aggregate's static Create factory method.
                // Factory returns Result<[Entity]> with validation built-in.
                // Customize parameters based on your aggregate's Create signature.
                .Bind(() => [Entity].Create(
                    // Add parameters matching your aggregate's Create method:
                    // request.Model.[Property1],
                    // request.Model.[Property2],
                    // request.Model.[Property3]
                ))
                .Log(logger, "[Entity] aggregate created")

                // ============================================================
                // STEP 3: Persist new aggregate to repository
                // ============================================================
                // InsertResultAsync returns Result<[Entity]> with the persisted entity.
                // Domain events are automatically published by repository behaviors.
                .BindAsync(async (entity, ct) =>
                    await repository.InsertResultAsync(entity, ct).AnyContext(), cancellationToken)
                .Log(logger, "AUDIT - [Entity] {Id} created", r => [r.Value.Id])

                // ============================================================
                // STEP 4: Map domain aggregate → DTO model
                // ============================================================
                // Use Mapster via IMapper abstraction to convert domain to DTO.
                // Mapping configurations are defined in [Module]MapperRegister.
                .MapResult<[Entity], [Entity]Model>(mapper)
                .Log(logger, "[Entity] mapped to {@Model}", r => [r.Value]);
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. RESULT<T> CHAINING (.Bind, .BindAsync, .Map, .Log):
//    - Functional railway-oriented programming pattern
//    - Short-circuits on first error (no need for nested if/else)
//    - Example: .Bind(() => [Entity].Create(...)) calls factory, propagates errors
//
// 2. RULE PATTERN FOR BUSINESS VALIDATION:
//    - await Rule.Add(...).Add(...).CheckAsync()
//    - Validates complex business rules beyond FluentValidation
//    - Returns ValidationError results with detailed messages
//
// 3. REPOSITORY RESULT METHODS:
//    - InsertResultAsync returns Result<[Entity]> instead of throwing exceptions
//    - Automatic domain event publishing via repository behaviors
//    - Consistent error handling across persistence operations
//
// 4. MAPRESULT<TSource, TDest>:
//    - Extension method for Result<TSource> → Result<TDest>
//    - Uses IMapper (Mapster) for type conversion
//    - Preserves error state if mapping fails
//
// 5. STRUCTURED LOGGING:
//    - .Log(logger, message, selector): Structured log entries
//    - Uses Serilog message templates with destructuring
//    - Selector extracts values from Result<T> safely
//
// 6. ASYNC CONTEXT PROPAGATION:
//    - .AnyContext(): Ensures correlation ID, tracing context flows through
//
// USAGE EXAMPLES:
//
//   // Handler is invoked automatically by IRequester (MediatR wrapper):
//   var result = await requester.SendAsync(
//       new [Entity]CreateCommand(model),
//       cancellationToken: cancellationToken);
//
//   // Success scenario:
//   if (result.IsSuccess)
//   {
//       var created[Entity] = result.Value; // [Entity]Model DTO
//       logger.LogInformation("Created [Entity] with ID: {Id}", created[Entity].Id);
//   }
//
//   // Failure scenario:
//   if (result.IsFailure)
//   {
//       foreach (var error in result.Errors)
//       {
//           logger.LogWarning("Validation error: {Message}", error.Message);
//       }
//   }
//
// ============================================================================
// CUSTOMIZATION CHECKLIST
// ============================================================================
//
// 1. Update [Entity].Create() parameters to match your aggregate's factory signature
// 2. Add business rule validation in STEP 1 (uniqueness, complex invariants)
// 3. Apply additional changes to aggregate after creation if needed:
//    - .Bind(e => e.ChangeStatus(request.Model.Status))
//    - .Bind(e => e.SetProperty(request.Model.Property))
// 4. Uncomment [Retry] and [Timeout] attributes for production
// 5. Add custom audit logging or side effects in STEP 3-4
// 6. Ensure mapping configuration exists in [Module]MapperRegister
//
// ============================================================================
