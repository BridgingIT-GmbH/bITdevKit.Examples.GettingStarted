// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

using BridgingIT.DevKit.Application;
using BridgingIT.DevKit.Application.JobScheduling;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;
using BridgingIT.DevKit.Infrastructure.EntityFramework;
using BridgingIT.DevKit.Presentation;
using Common;
using DevKit.Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class CoreModule() : WebModuleBase(nameof(CoreModule).ToLower())
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
            .WithTask<CoreModuleDomainSeederTask>(o => o
                .Enabled(environment.IsLocalDevelopment() || environment.IsContainerized()));

        // job scheduling setup
        services.AddJobScheduling(o => o
            .StartupDelay(configuration["JobScheduling:StartupDelay"]), configuration) // wait some time before starting the scheduler
            .WithSqlServerStore(configuration["JobScheduling:Quartz:quartz.dataSource.default.connectionString"])
            .WithBehavior<ModuleScopeJobSchedulingBehavior>()
            .WithJob<CustomerExportJob>()
                .Cron(CronExpressions.Every30Minutes)
                .Named(nameof(CustomerExportJob)).RegisterScoped();

        // entity framework setup
        services.AddSqlServerDbContext<CoreModuleDbContext>(o => o
                .UseConnectionString(moduleConfiguration.ConnectionStrings["Default"])
                .UseLogger(true, true) // TODO: does not work together with a ModuleDbContextBase
                /*.UseSimpleLogger()*/)
            .WithSequenceNumberGenerator()
            .WithDatabaseMigratorService(o => o // create the database and apply existing migrations
                .Enabled(environment.IsLocalDevelopment() || environment.IsContainerized())
                .DeleteOnStartup(environment.IsLocalDevelopment() || environment.IsContainerized()))
            .WithOutboxDomainEventService(o => o
                .ProcessingInterval("00:00:30")
                .ProcessingModeImmediate() // forwards the outbox event, through a queue, to the outbox worker
                .StartupDelay("00:00:15")
                .PurgeOnStartup());

        // repository setup
        services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>()
            .WithBehavior<RepositoryTracingBehavior<Customer>>()
            .WithBehavior<RepositoryLoggingBehavior<Customer>>()
            .WithBehavior<RepositoryAuditStateBehavior<Customer>>()
            .WithBehavior<RepositoryOutboxDomainEventBehavior<Customer, CoreModuleDbContext>>();
            //.WithBehavior<RepositoryDomainEventPublisherBehavior<Customer>>();
        services.AddScoped(_ => new RepositoryAuditStateBehaviorOptions { SoftDeleteEnabled = false });

        // mapping setup
        services.AddMapping()
            .WithMapster<CoreModuleMapperRegister>();

        // endpoints registration
        services.AddEndpoints<CustomerEndpoints>();

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