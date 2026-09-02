// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server;

using Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides development-only endpoint extensions for the application.
/// </summary>
public static partial class ProgramExtensions
{
    /// <summary>
    /// Maps an endpoint that serves the repository README when the application runs in local development.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The configured web application.</returns>
    public static WebApplication MapReadme(this WebApplication app)
    {
        if (!app.Environment.IsLocalDevelopment())
        {
            return app;
        }

        app.MapGet("/readme", async (IWebHostEnvironment environment) =>
            {
                var readmePath = Path.Combine(environment.ContentRootPath, "..", "..", "README.md");
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

        return app;
    }
}
