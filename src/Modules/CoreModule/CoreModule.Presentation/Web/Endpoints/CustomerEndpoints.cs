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
            .WithDescription("Gets a customer by its unique identifier.")
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
            .WithDescription("Gets all customers matching the specified filter criteria.")
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
            .WithDescription("Searches for customers matching the specified filter criteria.")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);

        // POST -> Create new customer
        group.MapPost("",
            async ([FromServices] IRequester requester,
                   [FromBody] CustomerModel model, CancellationToken ct)
                   => (await requester
                    .SendAsync(new CustomerCreateCommand(model), cancellationToken: ct))
                    .MapHttpCreated(v => $"/api/core/customers/{v.Id}"))
            .WithName("CoreModule.Customers.Create")
            .WithDescription("Creates a new customer with the provided details.")
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
            .WithDescription("Updates the details of an existing customer identified by its unique identifier.")
            .Produces(StatusCodes.Status401Unauthorized)
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
            .WithDescription("Updates the status of an existing customer identified by its unique identifier.")
            .Produces(StatusCodes.Status401Unauthorized)
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
            .WithDescription("Deletes an existing customer identified by its unique identifier.")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesResultProblem(StatusCodes.Status400BadRequest)
            .ProducesResultProblem(StatusCodes.Status500InternalServerError);
    }
}