// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Dashboard;

using System.Globalization;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using BridgingIT.DevKit.Presentation.Web.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Defines the CoreModule application dashboard pages.
/// </summary>
public sealed class CoreModuleDashboard(DashboardEndpointsOptions options) : DashboardPageSet(options)
{
    protected override void Configure(DashboardPageSetBuilder pages)
    {
        pages.WithTags("_bdk.Dashboard.GettingStarted.CoreModule");

        pages.Group("Application", 100)
            .Page("customer-management", "/app/coremodule/customers")
                .Title("Customers")
                .Icon("people")
                .Order(0)
                .Description("Create, view, edit, and delete GettingStarted customers")
                .Razor<Pages.Customers>()
                .Content<Pages.CustomersContent>()
                .Card(GetCustomerCardAsync)
                .Post("/create", CreateCustomerAsync)
                    .Name("_bdk.Dashboard.GettingStarted.CoreModule.CustomerCreate")
                .Post("/update", UpdateCustomerAsync)
                    .Name("_bdk.Dashboard.GettingStarted.CoreModule.CustomerUpdate")
                .Delete("/delete/{id:guid}", DeleteCustomerAsync)
                    .Name("_bdk.Dashboard.GettingStarted.CoreModule.CustomerDelete");
    }

    private static async ValueTask<DashboardPageCard> GetCustomerCardAsync(DashboardPageCardContext card)
    {
        var databaseReadyService = card.HttpContext.RequestServices.GetService<IDatabaseReadyService>();
        if (databaseReadyService?.IsReady(nameof(CoreModuleDbContext)) == false)
        {
            return card.Unavailable("Database starting");
        }

        var requester = card.HttpContext.RequestServices.GetService<IRequester>();
        if (requester is null)
        {
            return card.Unavailable("Requester unavailable");
        }

        var customers = await requester.SendAsync(
            new CustomerFindAllQuery { Filter = new FilterModel() },
            cancellationToken: card.HttpContext.RequestAborted);

        return customers.IsSuccess
            ? card.Value(
                customers.Value.Count().ToString("N0", CultureInfo.InvariantCulture),
                "customer records",
                "CoreModule")
            : card.Error("Could not load customers");
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> CreateCustomerAsync(
        [FromServices] IRequester requester,
        [FromBody] CustomerModel model,
        CancellationToken cancellationToken)
    {
        return (await requester.SendAsync(
                new CustomerCreateCommand(model),
                cancellationToken: cancellationToken))
            .MapHttpCreated(value => $"/api/coremodule/customers/{value.Id}");
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> UpdateCustomerAsync(
        [FromServices] IRequester requester,
        [FromBody] CustomerModel model,
        CancellationToken cancellationToken)
    {
        return (await requester.SendAsync(
                new CustomerUpdateCommand(model),
                cancellationToken: cancellationToken))
            .MapHttpOk();
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> DeleteCustomerAsync(
        [FromServices] IRequester requester,
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        return (await requester.SendAsync(
                new CustomerDeleteCommand(id),
                cancellationToken: cancellationToken))
            .MapHttpNoContent();
    }
}
