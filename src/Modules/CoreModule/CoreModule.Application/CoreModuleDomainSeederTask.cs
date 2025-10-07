// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System.Diagnostics.CodeAnalysis;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Represents a startup task responsible for seeding core domain data into the database.
/// </summary>
/// <remarks>
/// This class initializes important domain entities, such as dienstleister, into the database
/// during application startup to ensure that the core domain has essential data available.
/// </remarks>
[ExcludeFromCodeCoverage]
public class CoreModuleDomainSeederTask(
    ILoggerFactory loggerFactory,
    IGenericRepository<Customer> customerRepository,
    //IServiceScopeFactory scopeFactory,
    IDatabaseReadyService databaseReadyService) : IStartupTask
    {
    private readonly ILogger<CoreModuleDomainSeederTask> logger =
        loggerFactory?.CreateLogger<CoreModuleDomainSeederTask>() ??
        NullLoggerFactory.Instance.CreateLogger<CoreModuleDomainSeederTask>();

    /// <summary>
    /// Executes the startup task asynchronously to seed core domain data into the database.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await databaseReadyService.WaitForReadyAsync(cancellationToken: cancellationToken);

        this.logger.LogInformation("{LogKey} seed core (task={StartupTaskType})", "IFR", this.GetType().PrettyName());

        //using var scope = scopeFactory.CreateScope();
        //var customerRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Customer>>();

        await this.SeedCustomers(customerRepository, cancellationToken);
    }

    private async Task<Customer[]> SeedCustomers(IGenericRepository<Customer> repository, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("{LogKey} seed customer (task={StartupTaskType})", "IFR", this.GetType().PrettyName());

        var entities = CoreModuleSeedEntities.CreateCustomer();
        foreach (var entity in entities)
        {
            if (!await repository.ExistsAsync(entity.Id, cancellationToken))
            {
                entity.AuditState.SetCreated("seed", nameof(CoreModuleDomainSeederTask));
                await repository.InsertAsync(entity, cancellationToken);
            }
            else
            {
                return entities;
            }
        }

        return entities;
    }
}
