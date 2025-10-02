// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;
using BridgingIT.DevKit.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using Hellang.Middleware.ProblemDetails;

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

builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsLocalDevelopment());

builder.Services.AddControllers();
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ===============================================================================================
// Configure the HTTP request pipeline
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi.json", "v1"));
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseRequestCorrelation();
app.UseRequestLogging();

app.UseCors();
app.UseProblemDetails();
app.UseHttpsRedirection();
app.UseAuthorization();

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