// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server.OpenApi;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

/// <summary>
/// Adds separate OAuth2 and HTTP bearer schemes for generated OpenAPI documents.
/// </summary>
public sealed class ScalarSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string OAuth2SchemeName = "OAuth2";

    private static readonly string[] DefaultScopes = ["openid", "profile", "email", "roles"];

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
        var authority = configuration["Authentication:Authority"]?.TrimEnd('/');

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);

        document.Components.SecuritySchemes.Remove(OAuth2SchemeName);
        document.Components.SecuritySchemes.Remove(JwtBearerDefaults.AuthenticationScheme);

        if (!string.IsNullOrWhiteSpace(authority))
        {
            document.Components.SecuritySchemes[OAuth2SchemeName] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "Authenticate using the OAuth2 authorization code flow.",
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{authority}/api/_system/identity/connect/authorize"),
                        TokenUrl = new Uri($"{authority}/api/_system/identity/connect/token"),
                        Scopes = DefaultScopes.ToDictionary(
                            scope => scope,
                            scope => $"Request the {scope} scope.",
                            StringComparer.OrdinalIgnoreCase),
                    },
                },
            };
        }

        document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The JWT token in the format: Bearer {token}",
        };

        document.Security =
        [
            .. (document.Security ?? [])
                .Where(requirement => !requirement.Keys.Any(scheme =>
                    string.Equals(scheme.Reference?.Id, OAuth2SchemeName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(scheme.Reference?.Id, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase))),
        ];

        if (!string.IsNullOrWhiteSpace(authority))
        {
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(OAuth2SchemeName, document)] = [.. DefaultScopes],
            });
        }

        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = [],
        });

        return Task.CompletedTask;
    }
}
