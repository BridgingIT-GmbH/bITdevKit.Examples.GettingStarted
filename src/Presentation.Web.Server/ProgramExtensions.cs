// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Microsoft.Extensions.DependencyInjection;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    /// Configure the internal oauth identity provider middleware with its endpoints and signin page.
    /// </summary>
    public static IServiceCollection AddAppIdentityProvider(this IServiceCollection services, bool enabled, IConfiguration configuration)
    {
        return services.AddFakeIdentityProvider(o => o // configures the internal oauth identity provider with its endpoints and signin page
            .Enabled(enabled)
            .WithIssuer(configuration["Authentication:Authority"]) //
            .WithUsers(FakeUsers.Fantasy)
            //.WithClient( // optional client configuration
            //    "Blazor WASM Frontend",
            //    "blazor-wasm",
            //    $"{builder.Configuration["Authentication:Authority"]}/authentication/login-callback", $"{builder.Configuration["Authentication:Authority"]}/authentication/logout-callback")
            .WithClient("test", "test-client")
            .WithClient(
                "Scalar",
                "scalar",
                $"{configuration["Authentication:Authority"]}/scalar/")); // trailing slash is needed for login popup to close!?
    }

    /// <summary>
    /// Configure OpenAPI generation (openapi.json)
    /// </summary>
    public static IServiceCollection AddAppOpenApi(this IServiceCollection services)
    {
        // ===============================================================================================
        // Configure OpenAPI generation (openapi.json)
        return services.AddOpenApi(o =>
        {
            o.AddDocumentTransformer<DiagnosticDocumentTransformer>()
             .AddDocumentTransformer(
                new DocumentInfoTransformer(new DocumentInfoOptions
                {
                    Title = "GettingsStarted API",
                }))
             .AddSchemaTransformer<DiagnosticSchemaTransformer>()
             .AddSchemaTransformer<ResultProblemDetailsSchemaTransformer>()
             .AddDocumentTransformer<BearerSecurityRequirementDocumentTransformer>();
        });
    }

    /// <summary>
    /// Configure CORS policies
    /// </summary>
    public static IServiceCollection AddAppCors(this IServiceCollection services)
    {
        // ===============================================================================================
        // Configure CORS
        return services.AddCors(o =>
        {
            o.AddDefaultPolicy(p =>
            {
                p.WithOrigins()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    /// <summary>
    /// Configure health checks, including liveness and commented-out SQL/Redis checks for future use.
    /// </summary>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        // readiness checks
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
        // https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example
        // https://datalust.co/docs/opentelemetry-net-sdk-traces
        var otel = builder.Services.AddOpenTelemetry()
         .ConfigureResource(r => r.AddService(builder.Configuration["OpenTelemetry:ServiceName"]))
         .WithMetrics(metrics =>
         {
             metrics.AddAspNetCoreInstrumentation();
             metrics.AddMeter("Microsoft.AspNetCore.Hosting");
             metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
         })
         .WithTracing(tracing =>
         {
             if (builder.Environment.IsLocalDevelopment())
             {
                 tracing.SetSampler(new AlwaysOnSampler());
             }

             tracing.AddAspNetCoreInstrumentation();
             tracing.AddHttpClientInstrumentation();
             tracing.AddSqlClientInstrumentation();
             //tracing.AddConsoleExporter();

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
             .AddAuthorizationCodeFlow(JwtBearerDefaults.AuthenticationScheme, flow =>
             {
                 var idpOptions = app.Services.GetService<FakeIdentityProviderEndpointsOptions>();
                 var idpClient = idpOptions?.Clients?.FirstOrDefault(c => string.Equals(c.Name, "Scalar", StringComparison.OrdinalIgnoreCase));
                 flow.ClientId = idpClient?.ClientId;
                 flow.AuthorizationUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/authorize";
                 flow.TokenUrl = $"{idpOptions?.Issuer}/api/_system/identity/connect/token";
                 flow.RedirectUri = idpClient?.RedirectUris?.FirstOrDefault();
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