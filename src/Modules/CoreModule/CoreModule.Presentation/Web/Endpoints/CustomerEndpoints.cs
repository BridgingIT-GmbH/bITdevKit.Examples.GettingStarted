// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines HTTP API endpoints for managing <see cref="Customer"/> aggregates.
/// Includes read, create, update, and delete operations.
/// Endpoints are grouped under <c>/api/coremodule/customers</c>.
/// </summary>
public partial class CustomerEndpoints : EndpointsBase
{
    /// <summary>
    /// Maps the <see cref="Customer"/> endpoints to the given <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public override void Map(IEndpointRouteBuilder app)
    {
        // Group all customer endpoints under a common route & tag for OpenAPI/Swagger
        var group = app
            .MapGroup("api/coremodule/customers")//.RequireAuthorization()
            .WithTags("CoreModule.Customers");

        // GET /api/core/customers/{id} -> Find one customer by ID
        group.MapGet("/{id:guid}", CustomerFindOne)
            //.RequireEntityPermission<Customer>(Permission.Read)
            .WithName("CoreModule.Customers.GetById")
            .Produces<CustomerModel>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // GET /api/core/customers -> Find all customers (with optional query filters)
        group.MapGet("", CustomerFindAll)
            //.RequireEntityPermission<Customer>(Permission.List)
            .WithName("CoreModule.Customers.GetAll")
            .WithFilterSchema()
            .Produces<IEnumerable<CustomerModel>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // POST /api/core/customers -> Create new customer
        group.MapPost("", CustomerCreate)
            //.RequireEntityPermission<Customer>(Permission.Write)
            .WithName("CoreModule.Customers.Create")
            .Produces<CustomerModel>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // PUT /api/core/customers/{id} -> Update existing customer
        group.MapPut("/{id:guid}", CustomerUpdate)
            //.RequireEntityPermission<Customer>(Permission.Write)
            .WithName("CoreModule.Customers.Update")
            .Produces<CustomerModel>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // PUT /api/core/customers/{id}/status -> Update customer status (Active, Retired, etc.)
        group.MapPut("/{id:guid}/status", CustomerUpdateStatus)
            //.RequireEntityPermission<Customer>(Permission.Write)
            .WithName("CoreModule.Customers.UpdateStatus")
            .Produces<CustomerModel>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // DELETE /api/core/customers/{id} -> Delete customer by ID
        group.MapDelete("/{id:guid}", CustomerDelete)
            //.RequireEntityPermission<Customer>(Permission.Delete)
            .WithName("CoreModule.Customers.Delete")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Finds a single <see cref="Customer"/> by its ID.
    /// </summary>
    private static async Task<
        Results<Ok<CustomerModel>, NotFound, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerFindOne(
        [FromServices] IRequester requester,
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerFindOneQuery(id), cancellationToken: cancellationToken))
            .MapHttpOk();
    }

    /// <summary>
    /// Returns all <see cref="Customer"/> entities.
    /// Supports filtering via query parameters (converted into <c>QueryFilter</c>).
    /// </summary>
    private static async Task<
        Results<Ok<IEnumerable<CustomerModel>>, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerFindAll(
        HttpContext context,
        [FromServices] IRequester requester,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerFindAllQuery { Filter = await context.FromQueryFilterAsync() }, cancellationToken: cancellationToken))
            .MapHttpOkAll();
    }

    /// <summary>
    /// Creates a new <see cref="Customer"/> from the request body.
    /// </summary>
    private static async Task<
        Results<Created<CustomerModel>, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerCreate(
        [FromServices] IRequester requester,
        [FromBody] CustomerModel model,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerCreateCommand(model), cancellationToken: cancellationToken))
            .MapHttpCreated(value => $"/api/core/customers/{value.Id}"); // return created resource URI
    }

    /// <summary>
    /// Updates an existing <see cref="Customer"/> with the provided data.
    /// </summary>
    private static async Task<
        Results<Ok<CustomerModel>, NotFound, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerUpdate(
        [FromServices] IRequester requester,
        [FromRoute] string id,
        [FromBody] CustomerModel model,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerUpdateCommand(model), cancellationToken: cancellationToken))
            .MapHttpOk();
    }

    /// <summary>
    /// Changes the status of a customer to the provided status id.
    /// </summary>
    private static async Task<
        Results<Ok<CustomerModel>, NotFound, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerUpdateStatus(
        [FromServices] IRequester requester,
        [FromRoute] string id,
        [FromBody] CustomerUpdateStatusRequestModel body,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerUpdateStatusCommand(id, body.StatusId), cancellationToken: cancellationToken))
            .MapHttpOk();
    }

    /// <summary>
    /// Deletes a <see cref="Customer"/> by its ID.
    /// </summary>
    private static async Task<
        Results<NoContent, NotFound, UnauthorizedHttpResult, BadRequest, ProblemHttpResult>> CustomerDelete(
        [FromServices] IRequester requester,
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        return (await requester.SendAsync(
            new CustomerDeleteCommand(id), cancellationToken: cancellationToken))
            .MapHttpNoContent();
    }
}