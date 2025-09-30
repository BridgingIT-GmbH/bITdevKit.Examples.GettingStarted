// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Application.JobScheduling;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

[DisallowConcurrentExecution]
public class CustomerExportJob(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory) : JobBase(loggerFactory), IRetryJobScheduling
{
    RetryJobSchedulingOptions IRetryJobScheduling.Options => new()
    {
        Attempts = 3,
        Backoff = TimeSpan.FromSeconds(1)
    };

    public override async Task Process(
        IJobExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Customer>>();

        this.Logger.LogInformation("{JobName}: Starting customer export operation", nameof(CustomerExportJob));

        var customersResult = await repository.FindAllResultAsync(cancellationToken: cancellationToken);
        if (customersResult.IsFailure)
        {
            this.Logger.LogError("{JobName}: Failed to retrieve customers for export: {CustomerResult}", nameof(CustomerExportJob), customersResult.ToString());

            return;
        }

        foreach (var customer in customersResult.Value)
        {
            this.Logger.LogInformation("{JobName}: Exporting customer (id={CustomerID}, email={CustomerEmail})", nameof(CustomerExportJob), customer.Id, customer.Email);
            // Here you would add the logic to export the customer data to an external system or file
        }
    }
}
