// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

[ExcludeFromCodeCoverage]
public static class ProgramExtensions
{
    //public static IServiceCollection AddAppAuthentication(this IServiceCollection services, bool enabled, IConfiguration configuration)
    //{
    //    services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
    //    services.AddAppIdentityProvider(enabled, configuration);
    //    services
    //        .AddJwtAuthentication(configuration);
    //    //.AddCookieAuthentication(); // optional cookie authentication for web applications

    //    return services;
    //}

    /// <summary>
    /// Configure default pipeline behaviors for requester/notifier.
    /// </summary>
    public static RequesterBuilder WithDefaultBehaviors(this RequesterBuilder builder)
    {
        return builder // https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-requester-notifier.md#part-3-pipeline-behaviors
            .WithBehavior(typeof(MetricsRequestBehavior<,>))
            .WithBehavior(typeof(TracingBehavior<,>))
            .WithBehavior(typeof(ModuleScopeBehavior<,>))
            .WithBehavior(typeof(ValidationPipelineBehavior<,>))
            .WithBehavior(typeof(RetryPipelineBehavior<,>))
            .WithBehavior(typeof(TimeoutPipelineBehavior<,>));
    }

    /// <summary>
    /// Configure default pipeline behaviors for requester/notifier.
    /// </summary>
    public static NotifierBuilder WithDefaultBehaviors(this NotifierBuilder builder)
    {
        return builder // https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-requester-notifier.md#part-3-pipeline-behaviors
            .WithBehavior(typeof(MetricsNotificationBehavior<,>))
            .WithBehavior(typeof(MetricsNotificationHandlerBehavior<,>))
            .WithBehavior(typeof(TracingBehavior<,>))
            .WithBehavior(typeof(ModuleScopeBehavior<,>))
            .WithBehavior(typeof(ValidationPipelineBehavior<,>))
            .WithBehavior(typeof(RetryPipelineBehavior<,>))
            .WithBehavior(typeof(TimeoutPipelineBehavior<,>));
    }

    /// <summary>
    /// Configure the internal oauth identity provider middleware with its endpoints and signin page.
    /// </summary>
    public static IServiceCollection AddAppIdentityProvider(this IServiceCollection services, bool enabled, IConfiguration configuration)
    {
        // https://github.com/BridgingIT-GmbH/bITdevKit/blob/main/docs/features-identityprovider.md
        return services.AddFakeIdentityProvider(o => o // configures the internal oauth identity provider with its endpoints and signin page
            .Enabled(enabled)
            .WithIssuer(configuration["Authentication:Authority"]) //
            .WithUsers(FakeUsers.Fantasy)
            //.WithClient( // optional client configuration
            //    "Blazor WASM Frontend",
            //    "blazor-wasm",
            //    $"{builder.Configuration["Authentication:Authority"]}/authentication/login-callback", $"{builder.Configuration["Authentication:Authority"]}/authentication/logout-callback")
            .WithClient("test", "test-client")
            .WithClient("Scalar", "scalar", $"{configuration["Authentication:Authority"]}/scalar/") // trailing slash is needed for login popup to close!?
            .WithClient(
                "bdk dashboard",
                "dashboard",
                $"{configuration["Authentication:Authority"]}/_bdk/dashboard/signin-oidc"));
    }

    /// <summary>
    /// Configure the DevKit dashboard shell and the Jobs dashboard pages.
    /// </summary>
    public static IServiceCollection AddAppDashboard(this IServiceCollection services, bool enabled, IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]?.TrimEnd('/');

        return services.AddDashboard(options => options
            .Enabled(enabled)
            .Authorize(authorization => authorization
                .UseOpenIdConnect(authority, openIdConnect => openIdConnect
                    .WithMetadataAddress($"{authority}/_bdk/api/identity/connect/.well-known/openid-configuration"))
                .RequireRole(Role.Administrators))
            .WithPluginAssemblyContaining<BridgingIT.DevKit.Presentation.Web.Jobs.Dashboard.DashboardEndpoints>());
    }

    /// <summary>
    /// Configure OpenAPI generation (openapi.json).
    /// </summary>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi
        var securityOptions = new OpenApiSecurityOptions
        {
            Authority = configuration["Authentication:Authority"]
        };
        services.AddSingleton(Options.Create(securityOptions)); // used by OpenApiSecurityDocumentTransformer

        return services.AddOpenApi(o =>
        {
            o.AddDocumentTransformer<DiagnosticDocumentTransformer>()
             .AddDocumentTransformer(
                new DocumentInfoTransformer(new DocumentInfoOptions
                {
                    Title = "BridgingIT.DevKit.Examples.GettingStarted API",
                }))
             .AddSchemaTransformer<DiagnosticSchemaTransformer>()
             .AddSchemaTransformer<ResultProblemDetailsSchemaTransformer>()
             .AddDocumentTransformer<OpenApiSecurityDocumentTransformer>();
        });
    }

    /// <summary>
    /// Configure health checks, including liveness and commented-out SQL/Redis checks for future use.
    /// </summary>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()); // liveness

        // .AddSqlServer(configuration.GetConnectionString("Default"),
        // name: "sql", failureStatus: HealthStatus.Unhealthy, timeout: TimeSpan.FromSeconds(2))
        // .AddRedis(configuration.GetConnectionString("Redis"), "redis")

        return services;
    }

    /// <summary>
    /// Configure OpenTelemetry metrics, tracing, and OTLP exporter.
    /// </summary>
    public static IServiceCollection AddAppOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(configuration["OpenTelemetry:ServiceName"]))
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(
                        Metrics.MeterName,
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "System.Net.Http");
            })
            .WithTracing(tracing =>
            {
                if (environment.IsLocalDevelopment()) // TODO: make configurable via configuration also the samplers
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSqlClientInstrumentation();
                //tracing.AddConsoleExporter(); // TODO: enable via configuration

                var otlpEndpoint = configuration["OpenTelemetry:ExporterEndpoint"];
                if (otlpEndpoint != null)
                {
                    Serilog.Log.Information("Configuring OpenTelemetry OTLP Exporter with endpoint: {OtlpEndpoint}", otlpEndpoint);
                    tracing.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otlpEndpoint);
                        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }
            });

        return services;
    }

    /// <summary>
    /// Map Scalar OpenAPI endpoint (UI)
    /// </summary>
    public static WebApplication MapScalar(this WebApplication app)
    {
        var securityOptions = app.Services.GetRequiredService<IOptions<OpenApiSecurityOptions>>().Value;

        app.MapScalarApiReference(o =>
        {
            o.OpenApiRoutePattern = "/openapi.json";
            o.WithTitle("Web API")
             .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            if (securityOptions.AddOAuth2Scheme)
            {
                o.AddPreferredSecuritySchemes(securityOptions.OAuth2SchemeName);
            }
            else if (securityOptions.AddBearerScheme)
            {
                o.AddPreferredSecuritySchemes(securityOptions.BearerSchemeName);
            }

            if (securityOptions.AddOAuth2Scheme &&
                !string.IsNullOrWhiteSpace(securityOptions.AuthorizationUrl) &&
                !string.IsNullOrWhiteSpace(securityOptions.TokenUrl))
            {
                o.AddOAuth2Authentication(
                    securityOptions.OAuth2SchemeName,
                    s => s.WithDefaultScopes(securityOptions.Scopes ?? []))
                 .AddAuthorizationCodeFlow(securityOptions.OAuth2SchemeName, f =>
                 {
                     var idpOptions = app.Services.GetService<FakeIdentityProviderEndpointsOptions>();
                     var idpClient = idpOptions?.Clients?.FirstOrDefault(c => string.Equals(c.Name, "Scalar", StringComparison.OrdinalIgnoreCase));
                     f.ClientId = idpClient?.ClientId;
                     f.AuthorizationUrl = securityOptions.AuthorizationUrl;
                     f.TokenUrl = securityOptions.TokenUrl;
                     f.RedirectUri = idpClient?.RedirectUris?.FirstOrDefault();
                 });
            }
        });

        return app;
    }

    /// <summary>
    /// Map health check endpoints for liveness, readiness, and general health.
    /// </summary>
    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        // Liveness: only confirms the app is running
        app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = r => r.Name == "self",
            //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Readiness: checks all except "self" or vice-versa depending on your naming
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = r => r.Name != "self",
            //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health");

        return app;
    }

    /// <summary>
    /// Map README endpoint for local development only
    /// </summary>
    public static WebApplication MapReadme(this WebApplication app)
    {
        if (app.Environment.IsLocalDevelopment())
        {
            app.MapGet("/readme", async (IWebHostEnvironment env) =>
            {
                var readmePath = Path.Combine(env.ContentRootPath, "..", "..", "README.md");
                if (!File.Exists(readmePath))
                {
                    return Results.NotFound("README.md not found");
                }
                var content = await File.ReadAllTextAsync(readmePath);
                return Results.Text(content, "text/markdown");
            })
            .ExcludeFromDescription()
            .WithName("GetReadme")
            .WithTags("Documentation");
        }

        return app;
    }
}
