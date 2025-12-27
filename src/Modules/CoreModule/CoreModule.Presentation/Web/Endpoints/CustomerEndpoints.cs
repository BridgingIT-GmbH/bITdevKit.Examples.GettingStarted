// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
public class CustomerEndpoints : EndpointsBase
{
    public override void Map(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("api/coremodule/customers")
            .RequireAuthorization()
            .WithTags("CoreModule.Customers");

        // GET /{id:guid} -> Find one customer by ID
        group.MapGet("/{id:guid}",
            async ([FromServices] IRequester requester, [FromServices] ILogger logger,
                   [FromRoute] string id, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerFindOneQuery(id), cancellationToken: ct))
                    .MapHttpOk(logger))
            .WithName("CoreModule.Customers.GetById")
            .WithSummary("Get customer by ID")
            .WithDescription("Retrieves a single customer by their unique identifier. Returns 404 if the customer is not found.")
            .Produces<CustomerModel>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // GET -> Find all customers (query filters)
        group.MapGet("",
            async (HttpContext context,
                   [FromServices] IRequester requester,
                   [FromQuery] FilterModel filter, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerFindAllQuery { Filter = filter }, cancellationToken: ct))
                    .MapHttpOkAll())
            .WithName("CoreModule.Customers.GetAll")
            .WithSummary("Get all customers")
            .WithDescription("Retrieves all customers matching the specified filter criteria. Supports pagination (page, pageSize), sorting (orderings), and filtering.")
            .Produces<IEnumerable<CustomerModel>>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // POST /search -> Search customers (body filters)
        group.MapPost("search",
            async (HttpContext context,
                   [FromServices] IRequester requester,
                   [FromBody] FilterModel filter, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerFindAllQuery { Filter = filter }, cancellationToken: ct))
                    .MapHttpOkAll())
            .WithName("CoreModule.Customers.Search")
            .WithSummary("Search customers with filters")
            .WithDescription("Searches for customers matching the specified filter criteria provided in the request body. Use this endpoint for complex filter combinations that don't fit in query strings. Supports pagination, sorting, and filtering.")
            .Accepts<FilterModel>("application/json")
            .Produces<IEnumerable<CustomerModel>>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // POST -> Create new customer
        group.MapPost("",
            async ([FromServices] IRequester requester,
                   [FromBody] CustomerModel model, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerCreateCommand(model), cancellationToken: ct))
                    .MapHttpCreated(v => $"/api/coremodule/customers/{v.Id}"))
            .WithName("CoreModule.Customers.Create")
            .WithSummary("Create a new customer")
            .WithDescription("Creates a new customer with the provided details. The customer number is automatically generated. Returns the created customer with a Location header pointing to the new resource.")
            .Accepts<CustomerModel>("application/json")
            .Produces<CustomerModel>(StatusCodes.Status201Created, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // PUT /{id} -> Update existing customer
        group.MapPut("/{id:guid}",
            async ([FromServices] IRequester requester,
                   [FromRoute] string id,
                   [FromBody] CustomerModel model, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerUpdateCommand(model), cancellationToken: ct))
                    .MapHttpOk())
            .WithName("CoreModule.Customers.Update")
            .WithSummary("Update an existing customer")
            .WithDescription("Updates all details of an existing customer. Requires the customer ID in both the route and the request body. The concurrencyVersion must match to prevent conflicting updates (optimistic concurrency). Returns 409 Conflict if the version doesn't match.")
            .Accepts<CustomerModel>("application/json")
            .Produces<CustomerModel>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status409Conflict)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // PUT /{id}/status -> Update customer status
        group.MapPut("/{id:guid}/status",
            async ([FromServices] IRequester requester,
                   [FromRoute] string id,
                   [FromBody] CustomerUpdateStatusRequestModel body, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerUpdateStatusCommand(id, body.Status), cancellationToken: ct))
                    .MapHttpOk())
            .WithName("CoreModule.Customers.UpdateStatus")
            .WithSummary("Update customer status")
            .WithDescription("Updates only the status of an existing customer (e.g., Lead, Active, Retired). This is a partial update operation that modifies only the status field. Valid status values: 1 = Lead, 2 = Active, 3 = Retired.")
            .Accepts<CustomerUpdateStatusRequestModel>("application/json")
            .Produces<CustomerModel>(StatusCodes.Status200OK, "application/json")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status409Conflict)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // DELETE /{id:guid} -> Delete customer
        group.MapDelete("/{id:guid}",
            async ([FromServices] IRequester requester,
                   [FromRoute] string id, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerDeleteCommand(id), cancellationToken: ct))
                    .MapHttpNoContent())
            .WithName("CoreModule.Customers.Delete")
            .WithSummary("Delete a customer")
            .WithDescription("Permanently deletes an existing customer identified by its unique identifier. This operation cannot be undone. Returns 204 No Content on success.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);
    }
}