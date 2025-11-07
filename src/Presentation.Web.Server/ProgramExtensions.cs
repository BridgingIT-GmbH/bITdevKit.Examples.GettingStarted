// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
            .WithClient("Scalar", "scalar", $"{configuration["Authentication:Authority"]}/scalar/")); // trailing slash is needed for login popup to close!?
    }

    /// <summary>
    /// Configure OpenAPI generation (openapi.json).
    /// </summary>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services)
    {
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi
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
             .AddDocumentTransformer<BearerSecurityRequirementDocumentTransformer>();
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
    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, WebApplicationBuilder builder)
    {
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(builder.Configuration["OpenTelemetry:ServiceName"]))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
                // TODO: allow for extra meters via configuration
            })
            .WithTracing(tracing =>
            {
                if (builder.Environment.IsLocalDevelopment()) // TODO: make configurable via configuration also the samplers
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }

                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSqlClientInstrumentation();
                //tracing.AddConsoleExporter(); // TODO: enable via configuration

                var otlpEndpoint = builder.Configuration["OpenTelemetry:ExporterEndpoint"];
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
        app.MapScalarApiReference(o =>
        {
            o.OpenApiRoutePattern = "/openapi.json";
            o.WithTitle("Web API")
             .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
             .AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme)
             .AddAuthorizationCodeFlow(JwtBearerDefaults.AuthenticationScheme, f =>
             {
                 var idpOptions = app.Services.GetService<FakeIdentityProviderEndpointsOptions>();
                 var idpClient = idpOptions?.Clients?.FirstOrDefault(c => string.Equals(c.Name, "Scalar", StringComparison.OrdinalIgnoreCase));
                 f.ClientId = idpClient?.ClientId;
                 f.AuthorizationUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/authorize";
                 f.TokenUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/token";
                 f.RedirectUri = idpClient?.RedirectUris?.FirstOrDefault();
             });
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
}