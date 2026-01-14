// ============================================================================
// TEMPLATE: Find One Query for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines a query for retrieving a single aggregate instance by ID.
//   Returns DTO representation of the entity.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Queries/
//   3. File name: [Entity]FindOneQuery.cs
//   4. Invoked from Presentation layer: await requester.SendAsync(new [Entity]FindOneQuery(id))
//
// RELATED PATTERNS:
//   - RequestBase<T>: bITdevKit base class for CQRS commands/queries
//   - AbstractValidator<T>: FluentValidation for input validation
//   - [Entity]FindOneQueryHandler: Processes this query
//   - CQRS: Queries are read-only operations that return data
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Query for retrieving a single <see cref="[Entity]"/> Aggregate by its unique identifier.
/// </summary>
/// <param name="id">The string representation of the Aggregate's identifier.</param>
/// <remarks>
/// This query follows the CQRS pattern:
/// - Represents user intent to retrieve a single entity
/// - Read-only operation (no state changes)
/// - Returns DTO ([Entity]Model) representation
/// - Validation is performed via FluentValidation rules
/// - Processed by <see cref="[Entity]FindOneQueryHandler"/>
/// </remarks>
public class [Entity]FindOneQuery(string id) : RequestBase<[Entity]Model>
{
    /// <summary>
    /// Gets the Aggregate id to retrieve.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Validation rules for <see cref="[Entity]FindOneQuery"/>.
    /// Executes before the handler via ValidationPipelineBehavior.
    /// </summary>
    public class Validator : AbstractValidator<[Entity]FindOneQuery>
    {
        public Validator()
        {
            // RULE: ID must be a valid non-empty GUID
            this.RuleFor(c => c.Id)
                .MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid or missing entity ID.");
        }
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. PRIMARY CONSTRUCTOR WITH PARAMETER:
//    - public class [Entity]FindOneQuery(string id) : RequestBase<[Entity]Model>
//    - Concise: parameter becomes a property automatically
//
// 2. REQUESTBASE<[ENTITY]MODEL>:
//    - Generic type = return type (DTO representation)
//    - Query returns single entity or NotFoundError
//
// 3. IMMUTABLE QUERY:
//    - public string Id { get; } = id; (getter only)
//    - Queries should be immutable after construction
//
// 4. MINIMAL VALIDATION:
//    - FindOne queries typically only validate ID format
//    - Complex query logic belongs in handler or specifications
//
// 5. QUERY NAMING CONVENTION:
//    - [Entity]FindOneQuery (e.g., CustomerFindOneQuery)
//    - Alternative names: [Entity]GetByIdQuery, [Entity]FindByIdQuery
//
// USAGE EXAMPLES:
//
//   // From Presentation layer (Minimal API endpoint):
//   group.MapGet("/{id:guid}",
//       async ([FromServices] IRequester requester,
//              [FromRoute] string id, CancellationToken ct)
//              => (await requester
//                   .SendAsync(new [Entity]FindOneQuery(id), cancellationToken: ct))
//                   .MapHttpOk(logger));
//
//   // From Application layer (another handler/service):
//   var result = await requester.SendAsync(
//       new [Entity]FindOneQuery(entityId),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var entity = result.Value; // [Entity]Model DTO
//       logger.LogInformation("Found [Entity]: {Id}", entity.Id);
//   }
//   else if (result.HasError<NotFoundError>())
//   {
//       logger.LogWarning("[Entity] not found: {Id}", entityId);
//   }
//
// ============================================================================
// ADVANCED QUERY PATTERNS
// ============================================================================
//
// For more complex retrieval scenarios, consider:
//
// 1. INCLUDE RELATED ENTITIES:
//    - Add property: public bool IncludeRelated { get; init; }
//    - Handler uses specification with includes
//
// 2. PROJECTION TO DIFFERENT DTO:
//    - Add property: public string ProjectionType { get; init; }
//    - Handler maps to different DTO based on projection type
//
// 3. VERSIONING (retrieve historical version):
//    - Add property: public int? Version { get; init; }
//    - Handler loads entity from event store or audit tables
//
// Example advanced query:
//   public class [Entity]FindOneQuery(string id) : RequestBase<[Entity]Model>
//   {
//       public string Id { get; } = id;
//       public bool IncludeRelated { get; init; } = false;
//       public string Projection { get; init; } = "Default"; // Default, Summary, Detail
//   }
//
// ============================================================================
