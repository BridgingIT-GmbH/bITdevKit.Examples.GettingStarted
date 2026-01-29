// ============================================================================
// TEMPLATE: Find All Query Handler for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Processes [Entity]FindAllQuery to retrieve all aggregate instances with filtering.
//   Loads entities, logs audit trail, maps to DTOs.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Queries/
//   3. File name: [Entity]FindAllQueryHandler.cs
//   4. Uncomment [Retry] and [Timeout] attributes if needed (recommended)
//   5. Consider using FindAllResultPagedAsync for large datasets
//
// RELATED PATTERNS:
//   - RequestHandlerBase<TRequest, TResponse>: bITdevKit base for handlers
//   - Result<T>: Functional error handling pattern
//   - IGenericRepository<T>: Repository abstraction
//   - IMapper: Mapster mapping abstraction
//   - FilterModel: Standard filtering/pagination/sorting
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using System.Collections.Generic;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for processing <see cref="[Entity]FindAllQuery"/>.
/// Responsible for loading all entities from the repository with optional filters,
/// auditing/logging the operation, and mapping results from domain entities
/// (<see cref="[Entity]"/>) to application DTOs (<see cref="[Entity]Model"/>).
/// </summary>
/// <remarks>
/// Processing steps:
/// 1. Load entities from repository (with optional filter/pagination/sorting)
/// 2. Perform audit/logging side-effects
/// 3. Map domain entities → DTO models (collection mapping)
///
/// Pipeline behaviors (applied automatically):
/// - ValidationPipelineBehavior: Executes FluentValidation rules (if validator exists)
/// - RetryPipelineBehavior: Retries on transient failures (if [Retry] attribute present)
/// - TimeoutPipelineBehavior: Enforces max execution time (if [Timeout] attribute present)
///
/// Returns empty collection if no entities match filter criteria.
/// </remarks>
// Uncomment these attributes for production use:
// [Retry(2)] // Retry 2 times on transient failures
// [Timeout(30)] // Timeout after 30 seconds
public class [Entity]FindAllQueryHandler(
    ILogger<[Entity]FindAllQueryHandler> logger,
    IMapper mapper,
    IGenericRepository<[Entity]> repository)
    : RequestHandlerBase<[Entity]FindAllQuery, IEnumerable<[Entity]Model>>()
{
    /// <summary>
    /// Handles the <see cref="[Entity]FindAllQuery"/> asynchronously.
    /// </summary>
    /// <param name="request">The incoming query containing filter settings (can be null).</param>
    /// <param name="options">Request send options (retry handling, pipeline context).</param>
    /// <param name="cancellationToken">Cancellation token for async workflow.</param>
    /// <returns>
    /// A Result containing an enumerable of model instances representing all aggregates found,
    /// or a failure result in case of an error.
    /// </returns>
    protected override async Task<Result<IEnumerable<[Entity]Model>>> HandleAsync(
        [Entity]FindAllQuery request,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        // ============================================================
        // STEP 1: Load entities from repository
        // ============================================================
        // FindAllResultAsync returns Result<IEnumerable<[Entity]>>
        //   - Success: Collection of entities (may be empty)
        //   - Failure: Error if repository access fails
        // Filter parameter supports:
        //   - Pagination: Page, PageSize
        //   - Sorting: Orderings (field name + direction)
        //   - Filtering: Filters (field name + operator + value)
        return await repository.FindAllResultAsync(
            request.Filter, // Can be null (returns all entities)
            cancellationToken: cancellationToken)
        .Log(logger, "[Entity] entities loaded (count: {Count})", r => [r.Value.Count()])

        // ============================================================
        // STEP 2: Audit/logging side effects
        // ============================================================
        // Log retrieval for audit trails (optional, based on requirements)
        .Log(logger, "AUDIT - [Entity] entities retrieved (count: {Count}, filter: {@Filter})",
            r => [r.Value.Count(), request.Filter])

        // ============================================================
        // STEP 3: Map domain entities → DTO models (collection)
        // ============================================================
        // .Map() transforms Result<IEnumerable<[Entity]>> → Result<IEnumerable<[Entity]Model>>
        // Uses IMapper (Mapster) with configurations from [Module]MapperRegister
        // Collection mapping: each domain entity → DTO model
        .Map(mapper.Map<[Entity], [Entity]Model>)
        .Log(logger, "[Entity] entities mapped to models (count: {Count})", r => [r.Value.Count()]);
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. FINDALLRESULTASYNC WITH FILTER:
//    - repository.FindAllResultAsync(filter, cancellationToken)
//    - Returns Result<IEnumerable<[Entity]>> instead of throwing exceptions
//    - FilterModel is optional (null returns all entities)
//
// 2. RESULT<IENUMERABLE<T>> CHAINING:
//    - await repository.FindAllResultAsync(...).Log(...).Map(...).Log(...)
//    - Fluent pipeline: load → log → map → log
//    - Short-circuits on error (no need for null checks or if/else)
//
// 3. COLLECTION MAPPING:
//    - .Map(mapper.Map<[Entity], [Entity]Model>)
//    - Mapster automatically handles collection mapping (IEnumerable → IEnumerable)
//    - Each entity in collection is mapped individually
//
// 4. STRUCTURED LOGGING WITH COLLECTIONS:
//    - r.Value.Count(): Extracts count from Result<IEnumerable<T>>
//    - {@Filter}: Destructures FilterModel for structured logging
//    - Logs only on success (Result<T> selector not called on failure)
//
// 5. EMPTY COLLECTION HANDLING:
//    - No special handling needed for empty results
//    - Returns Result.Success(Enumerable.Empty<[Entity]Model>())
//    - Client receives 200 OK with empty array []
//
// USAGE EXAMPLES:
//
//   // Success scenario (with results):
//   var result = await requester.SendAsync(
//       new [Entity]FindAllQuery { Filter = new FilterModel { Page = 1, PageSize = 20 } },
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var entities = result.Value; // IEnumerable<[Entity]Model>
//       Console.WriteLine($"Found {entities.Count()} entities");
//       foreach (var entity in entities)
//       {
//           Console.WriteLine($"- {entity.Id}");
//       }
//   }
//
//   // Success scenario (empty results):
//   if (result.IsSuccess && !result.Value.Any())
//   {
//       logger.LogInformation("No entities found matching filter criteria");
//   }
//
//   // Generic error handling:
//   if (result.IsFailure)
//   {
//       foreach (var error in result.Errors)
//       {
//           logger.LogError("Error retrieving entities: {Message}", error.Message);
//       }
//   }
//
// ============================================================================
// PAGINATION BEST PRACTICES
// ============================================================================
//
// For large datasets, consider using paged results instead of returning all entities:
//
// 1. CHANGE QUERY RETURN TYPE:
//    - public class [Entity]FindAllQuery : RequestBase<IPagedResult<[Entity]Model>>
//
// 2. UPDATE HANDLER:
//    - Use FindAllResultPagedAsync instead of FindAllResultAsync
//    - Returns: { Items, TotalCount, Page, PageSize, TotalPages }
//
//    Example:
//      return await repository
//          .FindAllResultPagedAsync(
//              request.Filter ?? new FilterModel { Page = 1, PageSize = 50 },
//              cancellationToken: cancellationToken)
//          .Log(logger, "Loaded page {Page} of {TotalPages} ({Count} items)",
//              r => [r.Value.Page, r.Value.TotalPages, r.Value.Items.Count()])
//          .Map(paged => new PagedResult<[Entity]Model>
//          {
//              Items = mapper.Map<[Entity], [Entity]Model>(paged.Items),
//              TotalCount = paged.TotalCount,
//              Page = paged.Page,
//              PageSize = paged.PageSize
//          });
//
// 3. BENEFITS:
//    - Improved performance for large datasets
//    - Consistent pagination metadata for clients
//    - Prevents loading entire table into memory
//
// ============================================================================
// ADVANCED QUERY PATTERNS
// ============================================================================
//
// 1. SPECIFICATIONS FOR COMPLEX FILTERING:
//    - Define custom specifications (e.g., [Entity]ActiveOnlySpecification)
//    - Combine specifications: spec1.And(spec2).Or(spec3)
//    - Pass to repository: FindAllResultAsync(specification, cancellationToken)
//
//    Example:
//      ISpecification<[Entity]> spec = new TrueSpecification<[Entity]>();
//      if (request.ActiveOnly)
//      {
//          spec = spec.And(new [Entity]ActiveOnlySpecification());
//      }
//      if (!string.IsNullOrEmpty(request.Status))
//      {
//          spec = spec.And(new [Entity]StatusSpecification(request.Status));
//      }
//      return await repository.FindAllResultAsync(spec, cancellationToken);
//
// 2. INCLUDE RELATED ENTITIES:
//    - Use specifications with includes
//    - Example: new [Entity]WithRelatedSpecification()
//
// 3. PROJECTION TO DIFFERENT DTO:
//    - Define multiple DTOs: [Entity]SummaryModel, [Entity]DetailModel
//    - Conditional mapping based on query properties
//
// 4. CACHING:
//    - Cache frequently accessed collections
//    - Use IDistributedCache with appropriate expiration
//    - Invalidate cache on create/update/delete commands
//
// ============================================================================
