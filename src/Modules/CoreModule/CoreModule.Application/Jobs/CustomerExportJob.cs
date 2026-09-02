// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Application.Jobs;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Job that exports all customers from the repository.
/// <para>
/// This job demonstrates background processing using bITdevKit's Jobs infrastructure.
/// It retrieves all customers from the repository and logs each export operation. Intended as a template for
/// implementing real export logic to external systems or files. Configured with retry/backoff for transient failures.
/// </para>
/// </summary>
public class CustomerExportJob(
    ILogger<CustomerExportJob> logger,
    IGenericRepository<Customer> repository) : JobBase
{
    public const string JobName = "CoreModule_CustomerExportJob";

    public const string TriggerName = "cron";

    public override async Task<Result> ExecuteAsync(
        IJobExecutionContext<Unit> context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{JobName}: Starting customer export operation", JobName);

        var customersResult = await repository.FindAllResultAsync(cancellationToken: cancellationToken);
        if (customersResult.IsFailure)
        {
            logger.LogError("{JobName}: Failed to retrieve customers for export: {CustomerResult}", JobName, customersResult.ToString());

            return Result.Failure(customersResult.Messages, customersResult.Errors);
        }

        var customers = customersResult.Value.ToList();
        foreach (var customer in customers)
        {
            logger.LogInformation("{JobName}: Exporting customer (id={CustomerId})", JobName, customer.Id);
            // Here you would add the logic to export the customer data to an external system or file
        }

        var message = $"Customer export completed. Customers={customers.Count}";
        context.Messages.Add(message);

        return Result.Success(message);
    }
}
