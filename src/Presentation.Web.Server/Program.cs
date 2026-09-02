// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

using Hellang.Middleware.ProblemDetails;

// ===============================================================================================
// Create the app host
var builder = DevKitWebApplication.CreateBuilder(args)
    .AddConfiguration()
    .AddLogging()
    .AddModules(modules => modules
        .WithModule(new CoreModuleModule()))
        //WithModule<CoreModuleModule>())
    .AddMcp();

// ===============================================================================================
// Configure the requester and notifier services. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-requester-notifier.md
builder.Services.AddRequester()
    .AddHandlers().WithDefaultBehaviors();
builder.Services.AddNotifier()
    .AddHandlers().WithDefaultBehaviors();

// ===============================================================================================
// Configure the mapping service.
builder.Services.AddMapping().WithMapster();

// ===============================================================================================
// Configure the application endpoints. https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-presentation-endpoints.md
builder.Services.AddEndpoints<SystemEndpoints>(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized());

builder.Services.ConfigureJson(); // Configures the ASP.NET JSON serializer options
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers(); // TODO: needed for openapi gen, even with no controllers
#pragma warning disable CS0618 // Type or member is obsolete
builder.Services.AddProblemDetails(o => Configure.ProblemDetails(o, true));
// TODO: use builder.Services.AddExceptionHandler(); (uses new dotnet IExceptionHandler interface)
#pragma warning restore CS0618 // Type or member is obsolete
builder.Services.AddTimeProvider();

// ===============================================================================================
// Configure OpenAPI generation (openapi.json)
builder.Services.AddAppOpenApi(builder.Configuration);

// ===============================================================================================
// Configure CORS
builder.Services.AddCors(builder.Configuration);

// ===============================================================================================
// Configure API Authentication/Authorization
builder.Services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
builder.Services.AddJwtBearerAuthentication(builder.Configuration); //.AddCookieAuthentication(); // optional cookie authentication for web applications
builder.Services.AddAppIdentityProvider(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized(), builder.Configuration);
builder.Services.AddAppDashboard(builder.Environment.IsLocalDevelopment() || builder.Environment.IsContainerized(), builder.Configuration);

// ===============================================================================================
// Configure Health Checks
builder.Services.AddHealthChecks(builder.Configuration);

// ===============================================================================================
// Configure Metrics and Observability
builder.Services.AddMetrics(options => options
    .Enabled()
    .AddEndpoints());
builder.Services.AddAppOpenTelemetry(builder.Configuration, builder.Environment);

builder.Services.AddConsoleCommandsInteractive();

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

if (app.Environment.IsLocalDevelopment())
{
    app.UseDefaultFiles();
}
app.UseStaticFiles();
app.UseRequestCorrelation();
app.UseRequestModuleContext();
app.UseRequestLogging();
app.UseRequestMetrics();

app.UseCors(builder.Configuration);
app.UseProblemDetails();
app.UseHttpsRedirection();

app.UseModules();
//app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.UseCurrentUserLogging();

app.MapHealthChecks();
app.MapModules();
app.MapControllers();
app.MapEndpoints();
app.MapReadme();

app.UseConsoleCommandsInteractiveStats();
app.UseConsoleCommandsInteractive();

app.Run();
