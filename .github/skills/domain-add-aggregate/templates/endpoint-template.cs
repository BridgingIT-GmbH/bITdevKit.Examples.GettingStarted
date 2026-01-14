// ============================================================================
// TEMPLATE: Minimal API Endpoints for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines all HTTP endpoints (CRUD operations) for the aggregate using ASP.NET Core Minimal APIs.
//   Maps HTTP requests to CQRS commands/queries via IRequester (MediatR abstraction).
//
// PLACEHOLDERS TO REPLACE:
//   [module]       - Module name in lowercase (e.g., coremodule, inventorymodule)
//   [entities]     - Aggregate name in lowercase plural (e.g., customers, products, orders)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Module]       - Module name (e.g., CoreModule, InventoryModule)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Presentation/Web/Endpoints/
//   3. File name: [Entity]Endpoints.cs
//   4. Registered automatically via EndpointsBase discovery
//   5. Customize route patterns, authorize/anonymous, and documentation
//
// RELATED PATTERNS:
//   - EndpointsBase: bITdevKit base class for endpoint registration
//   - IRequester: MediatR abstraction for sending commands/queries
//   - Result<T>: Functional error handling with HTTP mapping extensions
//   - Minimal APIs: ASP.NET Core route configuration pattern
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Presentation.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Defines HTTP endpoints for <see cref="[Entity]"/> aggregate operations.
/// Provides full CRUD functionality: Create, Read (single/all), Update, Delete.
/// </summary>
/// <remarks>
/// Endpoints:
/// - GET    /api/[module]/[entities]         - Find all entities (with filtering)
/// - GET    /api/[module]/[entities]/{id}    - Find one entity by ID
/// - POST   /api/[module]/[entities]         - Create new entity
/// - PUT    /api/[module]/[entities]/{id}    - Update existing entity
/// - DELETE /api/[module]/[entities]/{id}    - Delete entity
/// - POST   /api/[module]/[entities]/search  - Search with complex filters (body)
///
/// All endpoints:
/// - Require authorization (change to .AllowAnonymous() if needed)
/// - Return Result<T> mapped to HTTP status codes
/// - Include OpenAPI/Swagger documentation
/// </remarks>
[ExcludeFromCodeCoverage]
public class [Entity]Endpoints : EndpointsBase
{
    /// <summary>
    /// Maps all [Entity] endpoints to the application's route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public override void Map(IEndpointRouteBuilder app)
    {
        // Create route group with common prefix and settings
        var group = app
            .MapGroup("api/[module]/[entities]")
            .RequireAuthorization() // Change to .AllowAnonymous() for public endpoints
            .WithTags("[Module].[Entity]"); // Swagger/OpenAPI tag

        // ================================================================
        // GET /{id:guid} → Find one entity by ID
        // ================================================================
        group.MapGet("/{id:guid}",
            async ([FromServices] IRequester requester,
                   [FromServices] ILogger logger,
                   [FromRoute] string id,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]FindOneQuery(id), cancellationToken: ct))
                    .MapHttpOk(logger))
            .WithName("[Module].[Entity].GetById")
            .WithSummary("Get [entity] by ID")
            .WithDescription("Retrieves a single [entity] by their unique identifier. Returns 404 if the [entity] is not found.")
            .Produces<[Entity]Model>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // ================================================================
        // GET → Find all entities (query string filters)
        // ================================================================
        group.MapGet("",
            async ([FromServices] IRequester requester,
                   [FromQuery] FilterModel filter,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]FindAllQuery { Filter = filter }, cancellationToken: ct))
                    .MapHttpOkAll())
            .WithName("[Module].[Entity].GetAll")
            .WithSummary("Get all [entities]")
            .WithDescription("Retrieves all [entities] matching the specified filter criteria. Supports pagination (page, pageSize), sorting (orderings), and filtering.")
            .Produces<IEnumerable<[Entity]Model>>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // ================================================================
        // POST /search → Search entities (body filters)
        // ================================================================
        // Alternative to GET for complex filters that don't fit in query strings
        group.MapPost("search",
            async ([FromServices] IRequester requester,
                   [FromBody] FilterModel filter,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]FindAllQuery { Filter = filter }, cancellationToken: ct))
                    .MapHttpOkAll())
            .WithName("[Module].[Entity].Search")
            .WithSummary("Search [entities] with filters")
            .WithDescription("Searches for [entities] matching the specified filter criteria provided in the request body. Use this endpoint for complex filter combinations that don't fit in query strings. Supports pagination, sorting, and filtering.")
            .Accepts<FilterModel>("application/json")
            .Produces<IEnumerable<[Entity]Model>>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // ================================================================
        // POST → Create new entity
        // ================================================================
        group.MapPost("",
            async ([FromServices] IRequester requester,
                   [FromBody] [Entity]Model model,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]CreateCommand(model), cancellationToken: ct))
                    .MapHttpCreated(v => $"/api/[module]/[entities]/{v.Id}"))
            .WithName("[Module].[Entity].Create")
            .WithSummary("Create a new [entity]")
            .WithDescription("Creates a new [entity] with the provided details. Returns the created [entity] with a Location header pointing to the new resource.")
            .Accepts<[Entity]Model>("application/json")
            .Produces<[Entity]Model>(StatusCodes.Status201Created, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // ================================================================
        // PUT /{id:guid} → Update existing entity
        // ================================================================
        group.MapPut("/{id:guid}",
            async ([FromServices] IRequester requester,
                   [FromRoute] string id,
                   [FromBody] [Entity]Model model,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]UpdateCommand(model), cancellationToken: ct))
                    .MapHttpOk())
            .WithName("[Module].[Entity].Update")
            .WithSummary("Update an existing [entity]")
            .WithDescription("Updates all details of an existing [entity]. Requires the [entity] ID in both the route and the request body. The concurrencyVersion must match to prevent conflicting updates (optimistic concurrency). Returns 409 Conflict if the version doesn't match.")
            .Accepts<[Entity]Model>("application/json")
            .Produces<[Entity]Model>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status409Conflict)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // ================================================================
        // DELETE /{id:guid} → Delete entity
        // ================================================================
        group.MapDelete("/{id:guid}",
            async ([FromServices] IRequester requester,
                   [FromRoute] string id,
                   CancellationToken ct)
                   => (await requester
                    .SendAsync(new [Entity]DeleteCommand(id), cancellationToken: ct))
                    .MapHttpNoContent())
            .WithName("[Module].[Entity].Delete")
            .WithSummary("Delete a [entity]")
            .WithDescription("Permanently deletes an existing [entity] identified by its unique identifier. This operation cannot be undone. Returns 204 No Content on success.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. ENDPOINTSBASE INHERITANCE:
//    - Automatically discovered and registered via module infrastructure
//    - Override Map() to define endpoints
//    - Grouped by aggregate for organization
//
// 2. ROUTE GROUP (MAPGROUP):
//    - Common prefix: api/[module]/[entities]
//    - Shared settings: RequireAuthorization(), WithTags()
//    - Simplifies endpoint definitions
//
// 3. IREQUESTER (MEDIATOR PATTERN):
//    - await requester.SendAsync(new [Entity]CreateCommand(model), ct)
//    - Decouples endpoints from handler implementations
//    - Pipeline behaviors (validation, retry, timeout) applied automatically
//
// 4. RESULT<T> HTTP MAPPING:
//    - .MapHttpOk(): 200 OK with body
//    - .MapHttpOkAll(): 200 OK with collection body
//    - .MapHttpCreated(locationBuilder): 201 Created with Location header
//    - .MapHttpNoContent(): 204 No Content (for delete)
//    - Automatic error mapping: ValidationError → 400, NotFoundError → 404, ConcurrencyError → 409
//
// 5. OPENAPI/SWAGGER DOCUMENTATION:
//    - .WithName(): Endpoint identifier for link generation
//    - .WithSummary(): Short description
//    - .WithDescription(): Detailed documentation
//    - .Produces<T>(): Success response type
//    - .ProducesResultProblem(): Error response types
//    - .Accepts<T>(): Request body type
//
// 6. DEPENDENCY INJECTION:
//    - [FromServices]: Injects service from DI container
//    - [FromRoute]: Binds route parameter (e.g., {id})
//    - [FromBody]: Binds request body (JSON → DTO)
//    - [FromQuery]: Binds query string parameters
//
// ============================================================================
// HTTP STATUS CODE CONVENTIONS
// ============================================================================
//
// SUCCESS RESPONSES:
//   200 OK            - GET (single/collection), PUT (update)
//   201 Created       - POST (create) with Location header
//   204 No Content    - DELETE (successful deletion)
//
// CLIENT ERROR RESPONSES:
//   400 Bad Request   - Validation errors, malformed request
//   401 Unauthorized  - Authentication required
//   404 Not Found     - Entity does not exist
//   409 Conflict      - Optimistic concurrency conflict
//
// SERVER ERROR RESPONSES:
//   500 Internal Server Error - Unexpected server error
//
// ============================================================================
// RESULT<T> ERROR MAPPING
// ============================================================================
//
// Result errors are automatically mapped to HTTP status codes:
//
//   ValidationError        → 400 Bad Request
//   NotFoundError          → 404 Not Found
//   EntityNotFoundError    → 404 Not Found
//   ConflictError          → 409 Conflict
//   ConcurrencyError       → 409 Conflict
//   UnauthorizedError      → 401 Unauthorized
//   ForbiddenError         → 403 Forbidden
//   (Other errors)         → 500 Internal Server Error
//
// ProblemDetails response includes:
//   - title: Error type name
//   - detail: Error message
//   - status: HTTP status code
//   - errors: Detailed validation errors (for ValidationError)
//
// ============================================================================
// ADVANCED ENDPOINT PATTERNS
// ============================================================================
//
// 1. CUSTOM STATUS-SPECIFIC ENDPOINT:
//    - Specialized operation beyond CRUD
//
//    group.MapPut("/{id:guid}/activate",
//        async ([FromServices] IRequester requester,
//               [FromRoute] string id, CancellationToken ct)
//               => (await requester
//                .SendAsync(new [Entity]ActivateCommand(id), cancellationToken: ct))
//                .MapHttpOk())
//        .WithName("[Module].[Entity].Activate")
//        .WithSummary("Activate [entity]");
//
// 2. QUERY STRING PARAMETERS:
//    - Extract specific query parameters instead of FilterModel
//
//    group.MapGet("/search",
//        async ([FromServices] IRequester requester,
//               [FromQuery] string name,
//               [FromQuery] bool? activeOnly,
//               CancellationToken ct)
//               => (await requester
//                .SendAsync(new [Entity]SearchQuery
//                {
//                    Name = name,
//                    ActiveOnly = activeOnly
//                }, cancellationToken: ct))
//                .MapHttpOkAll());
//
// 3. FILE UPLOAD:
//    - Accept multipart/form-data for file uploads
//
//    group.MapPost("/import",
//        async ([FromServices] IRequester requester,
//               IFormFile file, CancellationToken ct)
//               => (await requester
//                .SendAsync(new [Entity]ImportCommand(file), cancellationToken: ct))
//                .MapHttpOk())
//        .Accepts<IFormFile>("multipart/form-data")
//        .DisableAntiforgery();
//
// 4. EXPORT/DOWNLOAD:
//    - Return file results for downloads
//
//    group.MapGet("/export",
//        async ([FromServices] IRequester requester, CancellationToken ct) =>
//        {
//            var result = await requester.SendAsync(
//                new [Entity]ExportQuery(), cancellationToken: ct);
//            return result.IsSuccess
//                ? Results.File(result.Value.Data, "text/csv", "entities.csv")
//                : Results.Problem(statusCode: 500);
//        });
//
// 5. ALLOW ANONYMOUS:
//    - Remove RequireAuthorization() from group, add to specific endpoints
//
//    var group = app
//        .MapGroup("api/[module]/[entities]")
//        .WithTags("[Module].[Entity]");
//
//    group.MapGet("/{id}").RequireAuthorization(); // Auth required
//    group.MapPost("").AllowAnonymous();           // Public endpoint
//
// ============================================================================
