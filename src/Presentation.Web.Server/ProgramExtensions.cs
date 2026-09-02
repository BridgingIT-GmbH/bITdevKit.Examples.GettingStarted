// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server;

/// <summary>
/// Provides shared requester and notifier configuration for the application.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ProgramExtensions
{
    /// <summary>
    /// Adds the application's standard request pipeline behaviors to the requester.
    /// </summary>
    /// <param name="builder">The requester builder to configure.</param>
    /// <returns>The configured requester builder.</returns>
    public static RequesterBuilder WithDefaultBehaviors(this RequesterBuilder builder)
    {
        return builder
            .WithBehavior(typeof(MetricsRequestBehavior<,>))
            .WithBehavior(typeof(TracingBehavior<,>))
            .WithBehavior(typeof(ModuleScopeBehavior<,>))
            .WithBehavior(typeof(ValidationPipelineBehavior<,>))
            .WithBehavior(typeof(RetryPipelineBehavior<,>))
            .WithBehavior(typeof(TimeoutPipelineBehavior<,>));
    }

    /// <summary>
    /// Adds the application's standard notification pipeline behaviors to the notifier.
    /// </summary>
    /// <param name="builder">The notifier builder to configure.</param>
    /// <returns>The configured notifier builder.</returns>
    public static NotifierBuilder WithDefaultBehaviors(this NotifierBuilder builder)
    {
        return builder
            .WithBehavior(typeof(MetricsNotificationBehavior<,>))
            .WithBehavior(typeof(MetricsNotificationHandlerBehavior<,>))
            .WithBehavior(typeof(TracingBehavior<,>))
            .WithBehavior(typeof(ModuleScopeBehavior<,>))
            .WithBehavior(typeof(ValidationPipelineBehavior<,>))
            .WithBehavior(typeof(RetryPipelineBehavior<,>))
            .WithBehavior(typeof(TimeoutPipelineBehavior<,>));
    }
}
