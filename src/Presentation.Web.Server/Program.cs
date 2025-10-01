// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;
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
    ;

builder.Services.AddMapping()
    .WithMapster<CoreModuleMapperRegister>();

builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsDevelopment());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));

// ===============================================================================================
// Configure the HTTP request pipeline
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
    public partial class Program
    {
        // this partial class is needed to set the accessibilty for the Program class to public
        // needed for endpoint testing when using the webapplicationfactory  https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-7.0#basic-tests-with-the-default-webapplicationfactory
    }
}