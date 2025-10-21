// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Microsoft.Extensions.DependencyInjection;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Presentation.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;

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
}