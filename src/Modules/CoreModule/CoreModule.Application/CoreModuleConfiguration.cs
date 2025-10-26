// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using FluentValidation;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Strongly typed configuration model for the CoreModule.
/// Used for binding configuration settings (e.g. from <c>appsettings.json</c> or environment variables)
/// into a strongly typed object and validating them at startup.
/// </summary>
/// <remarks>
/// #### Binding and validation
/// This configuration class is typically bound and validated in the module setup:
/// <code>
/// // In your module bootstrap (e.g. CoreModule.cs)
/// var moduleConfiguration = this.Configure&lt;CoreModuleConfiguration, CoreModuleConfiguration.Validator&gt;(
///     services, configuration);
/// </code>
/// - Binds the <c>"CoreModule"</c> section of <c>appsettings.json</c> into an instance of <see cref="CoreModuleConfiguration"/>.
/// - Validates it with <see cref="Validator"/> (FluentValidation).
/// - Registers it into the DI container for <c>IOptions&lt;CoreModuleConfiguration&gt;</c>.
/// - Returns the validated instance (<c>moduleConfiguration</c>) for immediate use.
///
/// #### Example appsettings.json
/// <code>
/// {
///   "CoreModule": {
///     "ConnectionStrings": {
///       "Default": "Server=(localdb)\\MSSQLLocalDB;Database=bit_devkit_gettingstarted;Trusted_Connection=True"
///     }
///   }
/// }
/// </code>
/// </remarks>
[ExcludeFromCodeCoverage]
public class CoreModuleConfiguration
{
    /// <summary>
    /// Gets or sets the connection strings available to the CoreModule.
    /// Must contain at least one entry with the key <c>"Default"</c>.
    /// Keys are arbitrary names (e.g. "Default", "ReadOnly"),
    /// values are actual connection string values.
    /// </summary>
    public IReadOnlyDictionary<string, string> ConnectionStrings { get; set; }

    /// <summary>
    /// Gets or sets the delay before starting the seeder task, represented as a time interval string.
    /// </summary>
    /// <remarks>The value should be specified in the format "hh:mm:ss". This delay determines how long the system waits after initialization before launching the seeder task.</remarks>
    public string SeederTaskStartupDelay { get; set; } = "00:00:05";

    /// <summary>
    /// FluentValidation validator for <see cref="CoreModuleConfiguration"/>.
    /// Ensures that the configuration is not null or empty and includes a
    /// connection string entry named <c>"Default"</c>.
    /// </summary>
    public class Validator : AbstractValidator<CoreModuleConfiguration>
    {
        public Validator()
        {
            this.RuleFor(c => c).NotNull();
            this.RuleFor(c => c.ConnectionStrings).NotNull().NotEmpty()
                .Must(c => c?.ContainsKey("Default") == true)
                .WithMessage("Connection string with name 'Default' is required");
        }
    }
}