# Presentation Layer Checklist

This checklist validates the Minimal API endpoints that expose aggregate operations via HTTP.

**Layer**: Presentation (Web Endpoints)  
**Location**: `src/Modules/[Module]/[Module].Presentation/Web/Endpoints/[Entity]Endpoints.cs`  
**Purpose**: HTTP API surface that delegates to Application layer (CQRS) via IRequester

---

## EndpointsBase Implementation

- [ ] Class inherits from `EndpointsBase`
- [ ] Constructor accepts `IRequester requester` parameter
- [ ] Requester stored in private readonly field: `private readonly IRequester requester`
- [ ] `Register(IEndpointRouteBuilder app)` method implemented
- [ ] Namespace follows pattern: `BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Presentation.Web.Endpoints`
- [ ] File naming: `[Entity]Endpoints.cs` (e.g., `CustomerEndpoints.cs`)

**CORRECT**:
```csharp
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Presentation.Web.Endpoints;

public class CustomerEndpoints(IRequester requester) : EndpointsBase
{
    private readonly IRequester requester = requester;

    public override void Register(IEndpointRouteBuilder app)
    {
        // Endpoint mappings here
    }
}
```

**WRONG**:
```csharp
// Wrong: Not inheriting from EndpointsBase
public class CustomerEndpoints
{
    public void Register(IEndpointRouteBuilder app) { } // Missing base class
}

// Wrong: Not using primary constructor syntax
public class CustomerEndpoints : EndpointsBase
{
    private IRequester requester;
    
    public CustomerEndpoints(IRequester requester)
    {
        this.requester = requester; // Verbose, use primary constructor
    }
}
```

---

## Route Group Configuration

- [ ] Create route group using `app.MapGroup()`
- [ ] Group prefix follows pattern: `/api/[module]/[entities]` (e.g., `/api/core/customers`)
- [ ] Group assigned to variable for chaining endpoints
- [ ] Use plural entity name in route (e.g., `customers` not `customer`)
- [ ] Tags applied via `.WithTags()` for Swagger grouping
- [ ] Group-level attributes applied if needed (authorization, rate limiting)

**CORRECT**:
```csharp
public override void Register(IEndpointRouteBuilder app)
{
    var group = app.MapGroup("api/core/customers")
        .WithTags("Customers");

    // Map individual endpoints to group
    group.MapGet("", GetAll);
    group.MapGet("{id:guid}", GetById);
    group.MapPost("", Create);
    group.MapPut("{id:guid}", Update);
    group.MapDelete("{id:guid}", Delete);
}
```

**WRONG**:
```csharp
// Wrong: Singular entity name
var group = app.MapGroup("api/core/customer"); // Use plural

// Wrong: Missing module in path
var group = app.MapGroup("api/customers"); // Should be api/core/customers

// Wrong: Not using group, mapping directly
app.MapGet("api/core/customers", GetAll); // No group, harder to apply shared config
```

---

## GET All Endpoint (List/FindAll)

- [ ] Route: `GET /api/[module]/[entities]` (empty string in MapGet)
- [ ] Method accepts query parameters: `int page = 1, int pageSize = 10, string orderBy = null`
- [ ] Constructs `[Entity]FindAllQuery` with filter parameters
- [ ] Calls `requester.SendAsync(query)` to get Result<IEnumerable<[Entity]Model>>
- [ ] Returns Results<T> types: `TypedResults.Ok()`, `TypedResults.BadRequest()`, `TypedResults.Problem()`
- [ ] Uses `.Match()` or `.IsSuccess` to handle Result pattern
- [ ] Returns 200 OK with collection on success
- [ ] Returns 400 Bad Request or 500 Problem on failure
- [ ] OpenAPI documentation via `.WithName()`, `.WithSummary()`, `.WithDescription()`
- [ ] Produces metadata: `.Produces<IEnumerable<[Entity]Model>>(200)`, `.ProducesProblem(400)`

**CORRECT**:
```csharp
private async Task<IResult> GetAll(
    int page = 1,
    int pageSize = 10,
    string orderBy = null,
    CancellationToken cancellationToken = default)
{
    var query = new CustomerFindAllQuery
    {
        Page = page,
        PageSize = pageSize,
        OrderBy = orderBy
    };

    var result = await this.requester.SendAsync(query, cancellationToken);

    return result.Match(
        success => TypedResults.Ok(success),
        failure => TypedResults.Problem(failure.ToString()));
}

// Registration
group.MapGet("", GetAll)
    .WithName("GetAllCustomers")
    .WithSummary("Get all customers")
    .Produces<IEnumerable<CustomerModel>>(200)
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: Returning concrete IActionResult (not TypedResults)
private async Task<IActionResult> GetAll()
{
    return new OkObjectResult(data); // Use TypedResults in minimal APIs
}

// Wrong: Not handling Result pattern
private async Task<IResult> GetAll()
{
    var result = await this.requester.SendAsync(query);
    return TypedResults.Ok(result); // Result<T> exposed, should unwrap via Match
}
```

---

## GET by ID Endpoint (FindOne)

- [ ] Route: `GET /api/[module]/[entities]/{id:guid}`
- [ ] Method accepts `Guid id` parameter with route constraint
- [ ] Constructs `[Entity]FindOneQuery` with id parameter
- [ ] Calls `requester.SendAsync(query)` to get Result<[Entity]Model>
- [ ] Returns 200 OK with model on success
- [ ] Returns 404 Not Found if entity does not exist (check result.IsFailure and error message)
- [ ] Returns 400 Bad Request or 500 Problem on other failures
- [ ] OpenAPI documentation via `.WithName()`, `.WithSummary()`
- [ ] Produces metadata: `.Produces<[Entity]Model>(200)`, `.Produces(404)`, `.ProducesProblem(400)`

**CORRECT**:
```csharp
private async Task<IResult> GetById(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var query = new CustomerFindOneQuery { Id = id };

    var result = await this.requester.SendAsync(query, cancellationToken);

    return result.Match(
        success => success is not null
            ? TypedResults.Ok(success)
            : TypedResults.NotFound(),
        failure => TypedResults.Problem(failure.ToString()));
}

// Registration
group.MapGet("{id:guid}", GetById)
    .WithName("GetCustomerById")
    .WithSummary("Get customer by ID")
    .Produces<CustomerModel>(200)
    .Produces(404)
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: Not checking for null (404 scenario)
return result.Match(
    success => TypedResults.Ok(success), // May be null, should return 404
    failure => TypedResults.Problem(failure.ToString()));

// Wrong: Route constraint missing
group.MapGet("{id}", GetById); // Should be {id:guid}
```

---

## POST Endpoint (Create)

- [ ] Route: `POST /api/[module]/[entities]` (empty string in MapPost)
- [ ] Method accepts `[Entity]CreateCommand` from body: `[FromBody] [Entity]CreateCommand command`
- [ ] Calls `requester.SendAsync(command)` to get Result<[Entity]Model>
- [ ] Returns 201 Created with `Location` header on success
- [ ] Location header points to GET by ID endpoint: `/api/[module]/[entities]/{newId}`
- [ ] Returns 400 Bad Request for validation failures
- [ ] Returns 500 Problem for other failures
- [ ] OpenAPI documentation via `.WithName()`, `.WithSummary()`
- [ ] Produces metadata: `.Produces<[Entity]Model>(201)`, `.ProducesProblem(400)`, `.ProducesValidationProblem()`

**CORRECT**:
```csharp
private async Task<IResult> Create(
    CustomerCreateCommand command,
    CancellationToken cancellationToken = default)
{
    var result = await this.requester.SendAsync(command, cancellationToken);

    return result.Match(
        success => TypedResults.Created($"/api/core/customers/{success.Id}", success),
        failure => TypedResults.BadRequest(failure.ToString()));
}

// Registration
group.MapPost("", Create)
    .WithName("CreateCustomer")
    .WithSummary("Create a new customer")
    .Produces<CustomerModel>(201)
    .ProducesValidationProblem()
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: Returning 200 OK instead of 201 Created
return result.Match(
    success => TypedResults.Ok(success), // Should be Created with Location
    failure => TypedResults.BadRequest(failure.ToString()));

// Wrong: Missing Location header
return TypedResults.Created(string.Empty, success); // Location is empty
```

---

## PUT Endpoint (Update)

- [ ] Route: `PUT /api/[module]/[entities]/{id:guid}`
- [ ] Method accepts `Guid id` from route and `[Entity]UpdateCommand` from body
- [ ] Command id property set from route parameter: `command.Id = id`
- [ ] Calls `requester.SendAsync(command)` to get Result<[Entity]Model>
- [ ] Returns 200 OK with updated model on success
- [ ] Returns 404 Not Found if entity does not exist
- [ ] Returns 400 Bad Request for validation failures
- [ ] Returns 409 Conflict for concurrency violations (check error message for concurrency)
- [ ] Returns 500 Problem for other failures
- [ ] OpenAPI documentation via `.WithName()`, `.WithSummary()`
- [ ] Produces metadata: `.Produces<[Entity]Model>(200)`, `.Produces(404)`, `.Produces(409)`, `.ProducesProblem(400)`

**CORRECT**:
```csharp
private async Task<IResult> Update(
    Guid id,
    CustomerUpdateCommand command,
    CancellationToken cancellationToken = default)
{
    command = command with { Id = id }; // Ensure route id matches command

    var result = await this.requester.SendAsync(command, cancellationToken);

    return result.Match(
        success => TypedResults.Ok(success),
        failure => failure.ToString().Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? TypedResults.NotFound()
            : failure.ToString().Contains("concurrency", StringComparison.OrdinalIgnoreCase)
                ? TypedResults.Conflict()
                : TypedResults.BadRequest(failure.ToString()));
}

// Registration
group.MapPut("{id:guid}", Update)
    .WithName("UpdateCustomer")
    .WithSummary("Update an existing customer")
    .Produces<CustomerModel>(200)
    .Produces(404)
    .Produces(409)
    .ProducesValidationProblem()
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: Not setting id from route
private async Task<IResult> Update(Guid id, CustomerUpdateCommand command)
{
    // command.Id might be different from route id, security issue
    var result = await this.requester.SendAsync(command);
}

// Wrong: Not handling 404 or 409 scenarios
return result.Match(
    success => TypedResults.Ok(success),
    failure => TypedResults.BadRequest(failure.ToString())); // All errors as 400
```

---

## DELETE Endpoint (Delete)

- [ ] Route: `DELETE /api/[module]/[entities]/{id:guid}`
- [ ] Method accepts `Guid id` parameter with route constraint
- [ ] Constructs `[Entity]DeleteCommand` with id parameter
- [ ] Calls `requester.SendAsync(command)` to get Result<Unit> or Result<bool>
- [ ] Returns 204 No Content on success
- [ ] Returns 404 Not Found if entity does not exist
- [ ] Returns 400 Bad Request or 500 Problem on other failures
- [ ] OpenAPI documentation via `.WithName()`, `.WithSummary()`
- [ ] Produces metadata: `.Produces(204)`, `.Produces(404)`, `.ProducesProblem(400)`

**CORRECT**:
```csharp
private async Task<IResult> Delete(
    Guid id,
    CancellationToken cancellationToken = default)
{
    var command = new CustomerDeleteCommand { Id = id };

    var result = await this.requester.SendAsync(command, cancellationToken);

    return result.Match(
        success => TypedResults.NoContent(),
        failure => failure.ToString().Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? TypedResults.NotFound()
            : TypedResults.Problem(failure.ToString()));
}

// Registration
group.MapDelete("{id:guid}", Delete)
    .WithName("DeleteCustomer")
    .WithSummary("Delete a customer")
    .Produces(204)
    .Produces(404)
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: Returning 200 OK instead of 204 No Content
return result.Match(
    success => TypedResults.Ok(), // Should be NoContent for DELETE
    failure => TypedResults.Problem(failure.ToString()));

// Wrong: Returning deleted entity in body
return TypedResults.Ok(deletedCustomer); // DELETE should return 204 with no body
```

---

## Result Pattern Handling

- [ ] All handler results checked via `.Match()` or `.IsSuccess` property
- [ ] Success path returns appropriate TypedResults (Ok, Created, NoContent)
- [ ] Failure path inspects error messages to determine status code
- [ ] Validation errors return 400 Bad Request
- [ ] Not found errors return 404 Not Found
- [ ] Concurrency errors return 409 Conflict
- [ ] Generic errors return 500 Problem
- [ ] Error messages sanitized (no stack traces or sensitive data exposed)

**CORRECT**:
```csharp
return result.Match(
    success => TypedResults.Ok(success),
    failure =>
    {
        var errorMessage = failure.ToString();
        
        if (errorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return TypedResults.NotFound();
        
        if (errorMessage.Contains("validation", StringComparison.OrdinalIgnoreCase))
            return TypedResults.BadRequest(errorMessage);
        
        if (errorMessage.Contains("concurrency", StringComparison.OrdinalIgnoreCase))
            return TypedResults.Conflict();
        
        return TypedResults.Problem(errorMessage);
    });
```

**WRONG**:
```csharp
// Wrong: Throwing exceptions from Result failures
return result.Match(
    success => TypedResults.Ok(success),
    failure => throw new Exception(failure.ToString())); // Don't throw, return Problem

// Wrong: Exposing Result<T> to HTTP response
return TypedResults.Ok(result); // Result<T> exposed, should unwrap with Match
```

---

## OpenAPI / Swagger Documentation

- [ ] Each endpoint has `.WithName()` with unique operation ID
- [ ] Each endpoint has `.WithSummary()` with brief description
- [ ] Complex endpoints have `.WithDescription()` with detailed info
- [ ] Response types documented via `.Produces<T>(statusCode)`
- [ ] Error responses documented via `.ProducesProblem()`, `.ProducesValidationProblem()`
- [ ] Request body types inferred from command/query parameters
- [ ] Tags applied via `.WithTags()` for logical grouping in Swagger UI
- [ ] Example values provided via attributes or filters (optional)

**CORRECT**:
```csharp
group.MapPost("", Create)
    .WithName("CreateCustomer")
    .WithSummary("Create a new customer")
    .WithDescription("Creates a new customer entity with the provided details. Returns 201 Created with Location header on success.")
    .WithTags("Customers")
    .Produces<CustomerModel>(201)
    .ProducesValidationProblem()
    .ProducesProblem(400);
```

**WRONG**:
```csharp
// Wrong: No documentation
group.MapPost("", Create); // Missing WithName, WithSummary, Produces

// Wrong: Duplicate operation IDs
group.MapGet("", GetAll).WithName("GetCustomers");
group.MapGet("{id:guid}", GetById).WithName("GetCustomers"); // Duplicate name
```

---

## Authorization and Authentication

- [ ] Consider adding `.RequireAuthorization()` for protected endpoints
- [ ] Policy-based authorization: `.RequireAuthorization("PolicyName")`
- [ ] Role-based authorization: `.RequireAuthorization(roles: "Admin")`
- [ ] Anonymous endpoints explicitly marked: `.AllowAnonymous()` (if needed)
- [ ] Authorization applied at group level if all endpoints require same policy
- [ ] Produces 401 Unauthorized and 403 Forbidden documented via `.Produces(401)`, `.Produces(403)`

**CORRECT** (if authentication is enabled):
```csharp
var group = app.MapGroup("api/core/customers")
    .WithTags("Customers")
    .RequireAuthorization(); // All endpoints require authentication

// Or per-endpoint:
group.MapPost("", Create)
    .RequireAuthorization("CreateCustomerPolicy");

group.MapGet("", GetAll)
    .AllowAnonymous(); // Public read access
```

**Note**: If the project does not yet implement authentication, this section can be skipped. Mark as N/A in checklist.

---

## Validation and Error Handling

- [ ] FluentValidation runs automatically via ValidationPipelineBehavior (no manual validation in endpoints)
- [ ] Endpoints assume command/query validation happens in Application layer
- [ ] ProblemDetails format used for error responses (via `.ProducesProblem()`)
- [ ] Validation errors return 400 Bad Request with structured problem details
- [ ] Unhandled exceptions caught by global exception middleware (not in endpoints)

**CORRECT**:
```csharp
// Validation happens automatically in pipeline, no manual checks needed
private async Task<IResult> Create(CustomerCreateCommand command)
{
    var result = await this.requester.SendAsync(command); // Validator runs here
    
    return result.Match(
        success => TypedResults.Created($"/api/core/customers/{success.Id}", success),
        failure => TypedResults.BadRequest(failure.ToString())); // Validation errors already in Result
}
```

**WRONG**:
```csharp
// Wrong: Manual validation in endpoint
private async Task<IResult> Create(CustomerCreateCommand command)
{
    if (string.IsNullOrEmpty(command.FirstName))
        return TypedResults.BadRequest("FirstName is required"); // Use FluentValidation
    
    var result = await this.requester.SendAsync(command);
}
```

---

## Endpoint Registration in Module

- [ ] Endpoints class registered in module's AddPresentation method
- [ ] Uses `.MapEndpoints<[Entity]Endpoints>()` or similar registration helper
- [ ] Endpoints automatically discovered and registered at application startup
- [ ] No manual `app.MapGet/Post/Put/Delete` calls in Program.cs

**CORRECT** (in Module registration):
```csharp
public static IServiceCollection AddCoreModulePresentation(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... other services ...
    
    services.AddScoped<CustomerEndpoints>();
    
    return services;
}

// In Program.cs or module's UseModule method:
app.MapEndpoints(); // Auto-discovers all EndpointsBase implementations
```

**WRONG**:
```csharp
// Wrong: Manual registration in Program.cs
app.MapGet("/api/core/customers", async (IRequester requester) =>
{
    // Inline endpoint logic, not using EndpointsBase
});
```

---

## Common Presentation Anti-Patterns to Avoid

- [ ] **Fat Endpoints**: Business logic in endpoint methods (move to handlers)
- [ ] **Direct DbContext Access**: Endpoints should use IRequester, not repositories or DbContext
- [ ] **Exposing Result<T>**: Return unwrapped success/failure via Match, not Result<T> objects
- [ ] **Throwing Exceptions**: Use Result pattern, return TypedResults.Problem() instead of throwing
- [ ] **Missing Status Codes**: Always return correct HTTP codes (200, 201, 204, 400, 404, 409, 500)
- [ ] **Poor Routing**: Use plural entity names, include module prefix, use route constraints
- [ ] **No Documentation**: Every endpoint needs WithName, WithSummary, Produces metadata
- [ ] **Inconsistent Error Responses**: Use ProblemDetails format, structured error messages
- [ ] **Security Issues**: Validate route id matches command id, sanitize error messages

---

## Final Review

Before completing the aggregate implementation:

- [ ] All CRUD endpoints registered (GET all, GET by id, POST, PUT, DELETE)
- [ ] Route group configured with correct module and entity path
- [ ] All endpoints use IRequester to delegate to Application layer
- [ ] Result<T> pattern handled via Match in all endpoints
- [ ] Correct HTTP status codes returned for all scenarios
- [ ] 200 OK for successful GET/PUT
- [ ] 201 Created with Location for POST
- [ ] 204 No Content for DELETE
- [ ] 400 Bad Request for validation errors
- [ ] 404 Not Found when entity missing
- [ ] 409 Conflict for concurrency violations
- [ ] OpenAPI documentation complete (WithName, WithSummary, Produces)
- [ ] Endpoints testable via integration tests (WebApplicationFactory)
- [ ] No business logic in endpoints (only request/response transformation)
- [ ] Authorization attributes applied (if authentication enabled)

---

## References

- **Customer Endpoints Example**: `src/Modules/CoreModule/CoreModule.Presentation/Web/Endpoints/CustomerEndpoints.cs` (lines 1-143)
- **Result Chaining Patterns**: `.github/skills/domain-add-aggregate/examples/result-chaining-patterns.md`
- **Minimal API Documentation**: [Microsoft Docs - Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- **TypedResults**: [Microsoft Docs - TypedResults](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses)

---

**Next Checklist**: 06-tests.md (Testing Strategy)
