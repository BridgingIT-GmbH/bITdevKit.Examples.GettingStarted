// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Dashboard;

/// <summary>
/// Provides authentication and dashboard registration extensions for the application.
/// </summary>
public static partial class ProgramExtensions
{
    /// <summary>
    /// Registers the application's development identity provider and its clients.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="enabled">A value indicating whether the identity provider is enabled.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddAppIdentityProvider(
        this IServiceCollection services,
        bool enabled,
        IConfiguration configuration)
    {
        return services.AddFakeIdentityProvider(options => options
            .Enabled(enabled)
            .WithIssuer(configuration["Authentication:Authority"])
            .WithUsers(FakeUsers.Fantasy)
            .WithClient("test", "test-client")
            .WithClient("Scalar", "scalar", $"{configuration["Authentication:Authority"]}/scalar/")
            .WithClient(
                "bdk dashboard",
                "dashboard",
                $"{configuration["Authentication:Authority"]}/_bdk/dashboard/signin-oidc"));
    }

    /// <summary>
    /// Registers the DevKit dashboard, authorization policy, and application dashboard plugins.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="enabled">A value indicating whether the dashboard is enabled.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddAppDashboard(
        this IServiceCollection services,
        bool enabled,
        IConfiguration configuration)
    {
        var authority = configuration["Authentication:Authority"]?.TrimEnd('/');

        return services.AddDashboard(options => options
            .Enabled(enabled)
            .Authorize(authorization => authorization
                .UseOpenIdConnect(authority, openIdConnect => openIdConnect
                    .WithMetadataAddress($"{authority}/_bdk/api/identity/connect/.well-known/openid-configuration"))
                .RequireRole(Role.Administrators))
            .WithPluginAssemblyContaining<BridgingIT.DevKit.Presentation.Web.Jobs.Dashboard.DashboardEndpoints>()
            .WithPluginAssemblyContaining<CoreModuleDashboard>());
    }
}
