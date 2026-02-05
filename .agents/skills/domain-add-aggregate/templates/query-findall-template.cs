// ============================================================================
// TEMPLATE: Find All Query for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines a query for retrieving all aggregate instances with optional filtering.
//   Returns collection of DTO representations.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Queries/
//   3. File name: [Entity]FindAllQuery.cs
//   4. Invoked from Presentation layer: await requester.SendAsync(new [Entity]FindAllQuery { Filter = filter })
//   5. Add custom filter properties as needed
//
// RELATED PATTERNS:
//   - RequestBase<T>: bITdevKit base class for CQRS commands/queries
//   - FilterModel: Standard filtering/pagination/sorting model from bITdevKit
//   - [Entity]FindAllQueryHandler: Processes this query
//   - CQRS: Queries are read-only operations that return data
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using System.Collections.Generic;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Query for retrieving all <see cref="[Entity]"/> Aggregates with optional filtering, sorting, and pagination.
/// </summary>
/// <remarks>
/// This query follows the CQRS pattern:
/// - Represents user intent to retrieve multiple entities
/// - Read-only operation (no state changes)
/// - Returns collection of DTOs ([Entity]Model) representations
/// - Supports filtering, sorting, and pagination via FilterModel
/// - Processed by <see cref="[Entity]FindAllQueryHandler"/>
///
/// FilterModel supports:
/// - Pagination: Page, PageSize
/// - Sorting: Orderings (field name + direction)
/// - Filtering: Filters (field name + operator + value)
/// </remarks>
public class [Entity]FindAllQuery : RequestBase<IEnumerable<[Entity]Model>>
{
    /// <summary>
    /// Gets or sets the optional filter criteria used when retrieving entities.
    /// Supports pagination, sorting, and field-based filtering.
    /// </summary>
    public FilterModel Filter { get; set; }

    // ================================================================
    // ADD CUSTOM FILTER PROPERTIES IF NEEDED
    // ================================================================
    // For strongly-typed filtering beyond FilterModel, add properties:

    // /// <summary>
    // /// Gets or sets a flag to include only active entities.
    // /// </summary>
    // public bool? ActiveOnly { get; set; }
    //
    // /// <summary>
    // /// Gets or sets a status filter.
    // /// </summary>
    // public string Status { get; set; }
    //
    // /// <summary>
    // /// Gets or sets a date range filter for creation date.
    // /// </summary>
    // public DateOnly? CreatedAfter { get; set; }
    // public DateOnly? CreatedBefore { get; set; }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. REQUESTBASE<IENUMERABLE<[ENTITY]MODEL>>:
//    - Generic type = return type (collection of DTOs)
//    - IEnumerable is appropriate for most read scenarios
//    - Alternative: IPagedResult<[Entity]Model> for paginated responses
//
// 2. FILTERMODEL:
//    - Standard filtering/pagination/sorting model from bITdevKit
//    - Properties: Page, PageSize, Orderings, Filters
//    - Passed directly to repository FindAllAsync methods
//
// 3. OPTIONAL FILTER:
//    - Filter property can be null (returns all entities)
//    - Handler applies default pagination if Filter is null
//
// 4. QUERY NAMING CONVENTION:
//    - [Entity]FindAllQuery (e.g., CustomerFindAllQuery)
//    - Alternative names: [Entity]GetAllQuery, [Entity]ListQuery
//
// 5. NO NESTED VALIDATOR:
//    - FindAll queries typically have no required fields to validate
//    - FilterModel validation is handled by FilterModel itself
//    - Add Validator class if custom properties need validation
//
// USAGE EXAMPLES:
//
//   // From Presentation layer (Minimal API endpoint with query string):
//   group.MapGet("",
//       async ([FromServices] IRequester requester,
//              [FromQuery] FilterModel filter, CancellationToken ct)
//              => (await requester
//                   .SendAsync(new [Entity]FindAllQuery { Filter = filter }, cancellationToken: ct))
//                   .MapHttpOkAll());
//
//   // From Presentation layer (Minimal API endpoint with body):
//   group.MapPost("search",
//       async ([FromServices] IRequester requester,
//              [FromBody] FilterModel filter, CancellationToken ct)
//              => (await requester
//                   .SendAsync(new [Entity]FindAllQuery { Filter = filter }, cancellationToken: ct))
//                   .MapHttpOkAll());
//
//   // From Application layer (another handler/service):
//   var result = await requester.SendAsync(
//       new [Entity]FindAllQuery { Filter = new FilterModel { Page = 1, PageSize = 20 } },
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var entities = result.Value; // IEnumerable<[Entity]Model>
//       logger.LogInformation("Found {Count} entities", entities.Count());
//   }
//
// ============================================================================
// FILTERMODEL USAGE EXAMPLES
// ============================================================================
//
// PAGINATION:
//   var filter = new FilterModel
//   {
//       Page = 1,        // 1-based page number
//       PageSize = 20    // Results per page
//   };
//
// SORTING:
//   var filter = new FilterModel
//   {
//       Orderings = new List<Ordering>
//       {
//           new Ordering { Field = "LastName", Direction = OrderDirection.Ascending },
//           new Ordering { Field = "FirstName", Direction = OrderDirection.Ascending }
//       }
//   };
//
// FILTERING:
//   var filter = new FilterModel
//   {
//       Filters = new List<Filter>
//       {
//           new Filter { Field = "Status", Operator = FilterOperator.Equal, Value = "Active" },
//           new Filter { Field = "Email", Operator = FilterOperator.Contains, Value = "@example.com" }
//       }
//   };
//
// COMBINED:
//   var filter = new FilterModel
//   {
//       Page = 1,
//       PageSize = 50,
//       Orderings = new List<Ordering> { new Ordering { Field = "CreatedDate", Direction = OrderDirection.Descending } },
//       Filters = new List<Filter> { new Filter { Field = "Status", Operator = FilterOperator.Equal, Value = "Active" } }
//   };
//
// ============================================================================
// ADVANCED QUERY PATTERNS
// ============================================================================
//
// 1. STRONGLY-TYPED FILTER PROPERTIES:
//    - Add custom properties to query for type-safe filtering
//    - Handler translates to specifications or FilterModel
//
//    Example:
//      public class [Entity]FindAllQuery : RequestBase<IEnumerable<[Entity]Model>>
//      {
//          public FilterModel Filter { get; set; }
//          public [Entity]Status? Status { get; set; }
//          public bool ActiveOnly { get; set; } = false;
//      }
//
//    Handler applies custom logic:
//      if (request.ActiveOnly)
//      {
//          spec = new [Entity]ActiveOnlySpecification();
//      }
//
// 2. PAGED RESULT WITH METADATA:
//    - Change return type: RequestBase<IPagedResult<[Entity]Model>>
//    - Handler uses: repository.FindAllResultPagedAsync(...)
//    - Returns: { Items, TotalCount, Page, PageSize, TotalPages }
//
// 3. PROJECTION TO DIFFERENT DTO:
//    - Add property: public string Projection { get; set; } = "Default";
//    - Handler maps to [Entity]SummaryModel or [Entity]DetailModel based on Projection
//
// 4. INCLUDE RELATED ENTITIES:
//    - Add property: public bool IncludeRelated { get; set; } = false;
//    - Handler uses specification with includes if true
//
// ============================================================================
