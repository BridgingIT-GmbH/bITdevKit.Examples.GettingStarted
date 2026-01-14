// ============================================================================
// TEMPLATE: Find One Query Handler for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Processes [Entity]FindOneQuery to retrieve a single aggregate instance by ID.
//   Loads entity, logs audit trail, maps to DTO.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Queries/
//   3. File name: [Entity]FindOneQueryHandler.cs
//   4. Uncomment [Retry] and [Timeout] attributes if needed (recommended)
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
/// Handler for processing <see cref="[Entity]FindOneQuery"/>.
/// Loads a single [Entity] from the repository by ID, audits/logs the operation,
/// and maps the domain entity (<see cref="[Entity]"/>) to a DTO (<see cref="[Entity]Model"/>).
/// </summary>
/// <remarks>
/// Processing steps:
/// 1. Load entity from repository by ID (using typed ID value object)
/// 2. Perform audit/logging side-effects
/// 3. Map domain entity → DTO model
///
/// Pipeline behaviors (applied automatically):
/// - ValidationPipelineBehavior: Executes FluentValidation rules
/// - RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute present)
/// - TimeoutPipelineBehavior: Enforces max execution time (if [Timeout] attribute present)
///
/// Returns NotFoundError if entity does not exist.
/// </remarks>
// Uncomment these attributes for production use:
// [Retry(2)] // Retry 2 times on transient failures
// [Timeout(30)] // Timeout after 30 seconds
public class [Entity]FindOneQueryHandler(
    ILogger<[Entity]FindOneQueryHandler> logger,
    IMapper mapper,
    IGenericRepository<[Entity]> repository)
    : RequestHandlerBase<[Entity]FindOneQuery, [Entity]Model>
{
    /// <summary>
    /// Handles the <see cref="[Entity]FindOneQuery"/> request.
    /// </summary>
    /// <param name="request">The incoming query containing the Aggregate ID.</param>
    /// <param name="options">Pipeline send options (retries, context).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{[Entity]Model}"/> containing the mapped aggregate if found,
    /// or an error result (NotFoundError) if the aggregate does not exist.
    /// </returns>
    protected override async Task<Result<[Entity]Model>> HandleAsync(
        [Entity]FindOneQuery request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // ============================================================
            // STEP 1: Load entity from repository by ID
            // ============================================================
            // FindOneResultAsync returns Result<[Entity]>
            //   - Success: Entity found and loaded
            //   - Failure: NotFoundError if entity does not exist
            await repository.FindOneResultAsync(
                [Entity]Id.Create(request.Id),
                cancellationToken: cancellationToken)
            .Log(logger, "[Entity] {Id} loaded", r => [r.Value.Id])

            // ============================================================
            // STEP 2: Audit/logging side effects
            // ============================================================
            // Log retrieval for audit trails (optional, based on requirements)
            .Log(logger, "AUDIT - [Entity] {Id} retrieved", r => [r.Value.Id])

            // ============================================================
            // STEP 3: Map domain entity → DTO model
            // ============================================================
            // MapResult<TSource, TDest> converts Result<TSource> → Result<TDest>
            // Uses IMapper (Mapster) with configurations from [Module]MapperRegister
            .MapResult<[Entity], [Entity]Model>(mapper)
            .Log(logger, "[Entity] mapped to {@Model}", r => [r.Value]);
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. TYPED ID VALUE OBJECT:
//    - [Entity]Id.Create(request.Id) converts string → typed value object
//    - Type safety: prevents passing wrong ID type to repository
//    - Domain-driven design: IDs are value objects, not primitives
//
// 2. FINDONERESULTASYNC:
//    - Repository method returns Result<[Entity]> instead of [Entity]?
//    - Eliminates null checks, uses Result pattern for error handling
//    - NotFoundError returned automatically if entity not found
//
// 3. RESULT<T> CHAINING:
//    - await repository.FindOneResultAsync(...).Log(...).MapResult(...).Log(...)
//    - Fluent pipeline: load → log → map → log
//    - Short-circuits on error (no need for null checks or if/else)
//
// 4. MAPRESULT<TSOURCE, TDEST>:
//    - Converts Result<[Entity]> → Result<[Entity]Model>
//    - Uses Mapster configurations (defined in [Module]MapperRegister)
//    - Preserves error state if mapping fails
//
// 5. STRUCTURED LOGGING:
//    - .Log(logger, message, selector): Structured log entries with Result extraction
//    - Serilog message templates with destructuring (@Model, {Id})
//    - Logs only on success (Result<T> selector not called on failure)
//
// USAGE EXAMPLES:
//
//   // Success scenario:
//   var result = await requester.SendAsync(
//       new [Entity]FindOneQuery(entityId),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var entity = result.Value; // [Entity]Model DTO
//       Console.WriteLine($"Found: {entity.Id}");
//   }
//
//   // Not found scenario (404):
//   if (result.HasError<NotFoundError>())
//   {
//       logger.LogWarning("Entity not found: {Id}", entityId);
//       // Return 404 Not Found to client
//   }
//
//   // Generic error handling:
//   if (result.IsFailure)
//   {
//       foreach (var error in result.Errors)
//       {
//           logger.LogError("Error retrieving entity: {Message}", error.Message);
//       }
//   }
//
// ============================================================================
// ADVANCED QUERY PATTERNS
// ============================================================================
//
// For more complex retrieval scenarios:
//
// 1. INCLUDE RELATED ENTITIES:
//    - Use specifications with includes
//    - Example: new IncludeAllSpecification<[Entity]>()
//    - Handler: repository.FindOneResultAsync([Entity]Id.Create(id), new IncludeAllSpec(), ct)
//
// 2. CONDITIONAL INCLUDES:
//    if (request.IncludeRelated)
//    {
//        spec = new [Entity]WithRelatedSpecification();
//    }
//    await repository.FindOneResultAsync(id, spec, cancellationToken)
//
// 3. PROJECTION TO DIFFERENT DTO:
//    - Define multiple DTOs: [Entity]SummaryModel, [Entity]DetailModel
//    - Conditional mapping based on request.ProjectionType
//
// 4. CACHING (for frequently accessed entities):
//    - Check cache first, load from repository on miss
//    - Use CachingBehavior or manual IDistributedCache
//
// Example with specification:
//   protected override async Task<Result<[Entity]Model>> HandleAsync(...)
//   {
//       var spec = new [Entity]WithRelatedSpecification(); // Custom specification
//       return await repository
//           .FindOneResultAsync([Entity]Id.Create(request.Id), spec, cancellationToken)
//           .Log(...)
//           .MapResult<[Entity], [Entity]Model>(mapper);
//   }
//
// ============================================================================
