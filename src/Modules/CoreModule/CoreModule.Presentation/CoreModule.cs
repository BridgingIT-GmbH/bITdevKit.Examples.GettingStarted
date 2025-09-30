// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

using BridgingIT.DevKit.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;
using BridgingIT.DevKit.Presentation;
using Common;
using DevKit.Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class CoreModule : WebModuleBase
{
    /// <summary>
    /// Registers the core module's services, database context, repositories, and endpoints with the specified service
    /// collection.
    /// </summary>
    /// <param name="services">The service collection to which the core module's services will be added. Must not be null.</param>
    /// <param name="configuration">An optional configuration source used to configure the core module. If null, default configuration values are used.</param>
    /// <param name="environment">An optional web host environment that determines environment-specific service behaviors, such as enabling development-only features.</param>
    /// <returns>The service collection with the core module's services, database context, repositories, and endpoints registered.</returns>
    public override IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration = null,
        IWebHostEnvironment environment = null)
    {
        // get the module configuration from appsettings.json or environment variables
        var moduleConfiguration = this.Configure<CoreModuleConfiguration, CoreModuleConfiguration.Validator>(services, configuration);

        // startup tasks setup
        services.AddStartupTasks(o => o
            /*.StartupDelay(moduleConfiguration.SeederTaskStartupDelay)*/) // wait some time before starting the tasks
            .WithTask<CoreDomainSeederTask>(o => o
                .Enabled(environment.IsLocalDevelopment()));

        // job scheduling setup
        services.AddJobScheduling(o => o
            .StartupDelay(configuration["JobScheduling:StartupDelay"]), configuration) // wait some time before starting the scheduler
            .WithSqlServerStore(configuration["JobScheduling:Quartz:quartz.dataSource.default.connectionString"])
            .WithJob<CustomerExportJob>()
                .Cron(CronExpressions.EveryHour)
                .Named(nameof(CustomerExportJob)).RegisterScoped();

        // entity framework setup
        services.AddSqlServerDbContext<CoreDbContext>(o => o
                .UseConnectionString(moduleConfiguration.ConnectionStrings["Default"])
                .UseLogger()/*.UseSimpleLogger()*/)
            .WithDatabaseMigratorService(o => o // create the database and apply existing migrations
                .Enabled(environment.IsLocalDevelopment())
                .DeleteOnStartup(environment.IsLocalDevelopment()));

        // repository setup
        services.AddEntityFrameworkRepository<Customer, CoreDbContext>()
            .WithBehavior<RepositoryLoggingBehavior<Customer>>()
            .WithBehavior<RepositoryAuditStateBehavior<Customer>>()
            .WithBehavior<RepositoryDomainEventPublisherBehavior<Customer>>();

        // endpoints registration
        services.AddEndpoints<CoreCustomerEndpoints>();

        return services;
    }

    /// <summary>
    /// Configures the specified application builder with optional configuration and hosting environment settings.
    /// </summary>
    /// <param name="app">The application builder to configure. Cannot be null.</param>
    /// <param name="configuration">An optional configuration instance that provides application settings. If null, default configuration is used.</param>
    /// <param name="environment">An optional hosting environment instance that describes the application's environment. If null, the default environment is assumed.</param>
    /// <returns>The configured application builder instance.</returns>
    public override IApplicationBuilder Use(
        IApplicationBuilder app,
        IConfiguration configuration = null,
        IWebHostEnvironment environment = null)
    {
        return app;
    }

    /// <summary>
    /// Configures endpoint routing for the application and returns the provided route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder used to define application routes. Cannot be null.</param>
    /// <param name="configuration">The configuration settings for the application. Optional; may be null if no configuration is required.</param>
    /// <param name="environment">The hosting environment information for the application. Optional; may be null if environment-specific configuration is not needed.</param>
    /// <returns>The same <paramref name="app"/> instance provided as input, with any configured endpoints.</returns>
    public override IEndpointRouteBuilder Map(
        IEndpointRouteBuilder app,
        IConfiguration configuration = null,
        IWebHostEnvironment environment = null)
    {
        return app;
    }
}