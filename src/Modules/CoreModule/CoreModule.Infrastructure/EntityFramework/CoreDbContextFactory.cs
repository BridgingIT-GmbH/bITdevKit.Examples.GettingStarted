// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Provides a factory for creating instances of <see cref="CoreDbContext"/> during design-time operations,
/// such as Entity Framework Core migrations. This class is used by the EF Core tools to instantiate
/// the DbContext without running the full application startup logic.
/// </summary>
public class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    /// <summary>
    /// Creates an instance of <see cref="CoreDbContext"/> for use in design-time operations,
    /// such as generating Entity Framework Core migrations. The connection string is sourced
    /// from the command-line arguments (if provided) or falls back to configuration settings.
    /// </summary>
    /// <param name="args">Command-line arguments passed by the EF Core tools. Supports a "--connection-string" argument to override the configuration.</param>
    /// <returns>An instance of <see cref="CoreDbContext"/> configured with the appropriate database connection.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no valid connection string is found in command-line arguments or configuration.</exception>
    public CoreDbContext CreateDbContext(string[] args)
    {
        // Check for connection string in command-line arguments
        var connectionString = args.FirstOrDefault(a => a.StartsWith("--connection-string=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Build configuration from appsettings.json if no command-line override is provided
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            // Retrieve the connection string from configuration
            connectionString = configuration["Modules:Core:ConnectionStrings:Default"]
                ?? throw new InvalidOperationException("Connection string for module Core not found in settings or command-line arguments.");
        }

        // Configure DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(typeof(CoreDbContext).Assembly.GetName().Name);
        });

        return new CoreDbContext(optionsBuilder.Options);
    }
}