// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

/// <summary>
/// Provides OpenAPI and API reference extensions for the application.
/// </summary>
public static partial class ProgramExtensions
{
    /// <summary>
    /// Registers OpenAPI generation, security metadata, and application document transformers.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddAppOpenApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var securityOptions = new OpenApiSecurityOptions
        {
            Authority = configuration["Authentication:Authority"]
        };
        services.AddSingleton(Options.Create(securityOptions));

        return services.AddOpenApi(options => options
            .AddDocumentTransformer<DiagnosticDocumentTransformer>()
            .AddDocumentTransformer(new DocumentInfoTransformer(new DocumentInfoOptions
            {
                Title = "BridgingIT.DevKit.Examples.GettingStarted API"
            }))
            .AddSchemaTransformer<DiagnosticSchemaTransformer>()
            .AddSchemaTransformer<ResultProblemDetailsSchemaTransformer>()
            .AddDocumentTransformer<OpenApiSecurityDocumentTransformer>());
    }

    /// <summary>
    /// Maps the Scalar API reference and configures its preferred authentication flow.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication MapScalar(this WebApplication app)
    {
        var securityOptions = app.Services.GetRequiredService<IOptions<OpenApiSecurityOptions>>().Value;

        app.MapScalarApiReference(options =>
        {
            options.OpenApiRoutePattern = "/openapi.json";
            options.WithTitle("Web API")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            if (securityOptions.AddOAuth2Scheme)
            {
                options.AddPreferredSecuritySchemes(securityOptions.OAuth2SchemeName);
            }
            else if (securityOptions.AddBearerScheme)
            {
                options.AddPreferredSecuritySchemes(securityOptions.BearerSchemeName);
            }

            if (securityOptions.AddOAuth2Scheme &&
                !string.IsNullOrWhiteSpace(securityOptions.AuthorizationUrl) &&
                !string.IsNullOrWhiteSpace(securityOptions.TokenUrl))
            {
                options.AddOAuth2Authentication(
                        securityOptions.OAuth2SchemeName,
                        scheme => scheme.WithDefaultScopes(securityOptions.Scopes ?? []))
                    .AddAuthorizationCodeFlow(securityOptions.OAuth2SchemeName, flow =>
                    {
                        var identityProviderOptions = app.Services.GetService<FakeIdentityProviderEndpointsOptions>();
                        var scalarClient = identityProviderOptions?.Clients?.FirstOrDefault(client =>
                            string.Equals(client.Name, "Scalar", StringComparison.OrdinalIgnoreCase));
                        flow.ClientId = scalarClient?.ClientId;
                        flow.AuthorizationUrl = securityOptions.AuthorizationUrl;
                        flow.TokenUrl = securityOptions.TokenUrl;
                        flow.RedirectUri = scalarClient?.RedirectUris?.FirstOrDefault();
                    });
            }
        });

        return app;
    }
}
