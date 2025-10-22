// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;
using BridgingIT.DevKit.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Extensions.Hosting;
using System;

// ===============================================================================================
// Configure the host
var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureLogging();

// ===============================================================================================
// Configure the modules
builder.Services.AddModules(builder.Configuration, builder.Environment)
    .WithModule<CoreModule>()
    .WithModuleContextAccessors()
    .WithRequestModuleContextAccessors();

// ===============================================================================================
// Configure the services
builder.Services.AddRequester()
    .AddHandlers()
    .WithBehavior(typeof(ModuleScopeBehavior<,>))
    .WithBehavior(typeof(ValidationPipelineBehavior<,>))
    .WithBehavior(typeof(RetryPipelineBehavior<,>))
    .WithBehavior(typeof(TimeoutPipelineBehavior<,>));
builder.Services.AddNotifier()
    .AddHandlers()
    .WithBehavior(typeof(ModuleScopeBehavior<,>))
    .WithBehavior(typeof(ValidationPipelineBehavior<,>))
    .WithBehavior(typeof(RetryPipelineBehavior<,>))
    .WithBehavior(typeof(TimeoutPipelineBehavior<,>));

builder.Services.ConfigureJson(); // configure the json serializer options
builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized());
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers(); // TODO: needed for openapi gen, even with no controllers
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));
#pragma warning restore CS0618 // Type or member is obsolete
builder.Services.AddSingleton(TimeProvider.System);

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
// Configure the HTTP request pipeline
var app = builder.Build();
if (app.Environment.IsLocalDevelopment() || app.Environment.IsContainerized())
{
    app.MapOpenApi();
    app.MapScalar();
}

app.UseStaticFiles();
app.UseRequestCorrelation();
app.UseRequestLogging();

app.UseCors();
app.UseProblemDetails();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseModules();

app.UseCurrentUserLogging();

app.MapModules();
app.MapControllers();
app.MapEndpoints();

app.Run();

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server
{
    public partial class Program // TODO: dotnet 10 has a fix for this see https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-9.0#better-support-for-testing-apps-with-top-level-statements
    {
        // this partial class is needed to set the accessibilty for the Program class to public
        // needed for endpoint testing when using the webapplicationfactory  https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#basic-tests-with-the-default-webapplicationfactory
    }
}