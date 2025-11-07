// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

using BridgingIT.DevKit.Application.JobScheduling;
using Hellang.Middleware.ProblemDetails;

// ===============================================================================================
// Configure the host
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureLogging();
builder.Services.AddConsoleCommandsInteractive();

// ===============================================================================================
// Configure the modules. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-modules.md
builder.Services.AddModules(builder.Configuration, builder.Environment)
    .WithModule<CoreModuleModule>()
    .WithModuleContextAccessors()
    .WithRequestModuleContextAccessors();

// ===============================================================================================
// Configure the requester and notifier services. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-requester-notifier.md
builder.Services.AddRequester()
    .AddHandlers().WithDefaultBehaviors();
builder.Services.AddNotifier()
    .AddHandlers().WithDefaultBehaviors();

// ===============================================================================================
// Configure the job scheduling service. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-jobscheduling.md
builder.Services.AddJobScheduling(o => o
    .StartupDelay(builder.Configuration["JobScheduling:StartupDelay"]), builder.Configuration) // wait some time before starting the scheduler
    .WithSqlServerStore(builder.Configuration["JobScheduling:Quartz:quartz.dataSource.default.connectionString"])
    .WithBehavior<ModuleScopeJobSchedulingBehavior>();

// ===============================================================================================
// Configure the application endpoints. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-presentation-endpoints.md
builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized());

builder.Services.ConfigureJson(); // Configures the ASP.NET JSON serializer options
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(); // TODO: needed for openapi gen, even with no controllers
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));
#pragma warning restore CS0618 // Type or member is obsolete
builder.Services.AddTimeProvider();

// ===============================================================================================
// Configure OpenAPI generation (openapi.json)
builder.Services.AddAppOpenApi();

// ===============================================================================================
// Configure CORS
builder.Services.AddAppCors(); // TODO: not needed for pure APIs

// ===============================================================================================
// Configure API Authentication/Authorization
builder.Services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
builder.Services.AddJwtBearerAuthentication(builder.Configuration); //.AddCookieAuthentication(); // optional cookie authentication for web applications
builder.Services.AddAppIdentityProvider(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized(), builder.Configuration);

// ===============================================================================================
// Configure Health Checks
builder.Services.AddHealthChecks(builder.Configuration);

// ===============================================================================================
// Configure Observability
builder.Services.AddOpenTelemetry(builder);

// ===============================================================================================
// Configure the HTTP request pipeline
var app = builder.Build();
if (app.Environment.IsLocalDevelopment() || app.Environment.IsContainerized())
{
    app.MapOpenApi();
    app.MapScalar();
}

app.UseRuleLogger();
app.UseResultLogger();

app.UseStaticFiles();
app.UseRequestCorrelation();
app.UseRequestModuleContext();
app.UseRequestLogging();

app.UseCors();
app.UseProblemDetails();
app.UseHttpsRedirection();

app.UseModules();

app.UseAuthentication();
app.UseAuthorization();

app.UseCurrentUserLogging();

app.MapHealthChecks();
app.MapModules();
app.MapControllers();
app.MapEndpoints();

app.UseConsoleCommandsInteractiveStats();
app.UseConsoleCommandsInteractive();

app.Run();

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server
{
    public partial class Program // TODO: dotnet 10 has a fix for this see https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-9.0#better-support-for-testing-apps-with-top-level-statements
    {
        // this partial class is needed to set the accessibilty for the Program class to public
        // needed for endpoint testing when using the webapplicationfactory  https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#basic-tests-with-the-default-webapplicationfactory
    }
}