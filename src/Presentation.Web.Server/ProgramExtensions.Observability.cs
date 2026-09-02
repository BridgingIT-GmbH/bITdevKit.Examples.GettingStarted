// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Server;

using System.Reflection;
using System.Runtime.InteropServices;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Provides observability registration extensions for the application.
/// </summary>
public static partial class ProgramExtensions
{
    /// <summary>
    /// Registers OpenTelemetry resources, metrics, tracing, instrumentation, and configured exporters.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing telemetry settings.</param>
    /// <param name="environment">The host environment used to describe the deployment and select tracing behavior.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddAppOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var assembly = Assembly.GetExecutingAssembly();
        var serviceName = assembly.GetName().Name ?? "GettingStarted.Presentation.Web.Server";
        var serviceVersion = assembly.GetName().Version?.ToString();
        var standardOtlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"].EmptyToNull();
        var legacyTraceEndpoint = configuration["OpenTelemetry:ExporterEndpoint"].EmptyToNull();
        var otlpHeaders = configuration["OTEL_EXPORTER_OTLP_HEADERS"].EmptyToNull();
        var consoleEnabled = configuration["Tracing:Console:Enabled"].To<bool>();

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddTelemetrySdk()
                .AddAttributes(
                [
                    new KeyValuePair<string, object>("host.name", Environment.MachineName),
                    new KeyValuePair<string, object>("os.description", RuntimeInformation.OSDescription),
                    new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName.ToLowerInvariant())
                ]))
            .WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(
                        Metrics.MeterName,
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "System.Net.Http");

                if (!standardOtlpEndpoint.IsNullOrEmpty())
                {
                    metrics.AddOtlpExporter(options => ConfigureStandardExporter(options, standardOtlpEndpoint, otlpHeaders));
                }

                if (consoleEnabled)
                {
                    metrics.AddConsoleExporter();
                }
            })
            .WithTracing(tracing =>
            {
                tracing.SetErrorStatusOnException()
                    .AddSource("*")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                            !context.Request.Path.ToString().MatchAny(new RequestLoggingOptions().PathBlackListPatterns);
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.FilterHttpRequestMessage = request =>
                            !request.RequestUri.PathAndQuery.MatchAny(new RequestLoggingOptions().PathBlackListPatterns);
                    })
                    .AddSqlClientInstrumentation(options => options.RecordException = true)
                    .SetSampler(environment.IsDevelopment()
                        ? new AlwaysOnSampler()
                        : new TraceIdRatioBasedSampler(1));

                if (!standardOtlpEndpoint.IsNullOrEmpty())
                {
                    tracing.AddOtlpExporter(options => ConfigureStandardExporter(options, standardOtlpEndpoint, otlpHeaders));
                }
                else if (!legacyTraceEndpoint.IsNullOrEmpty())
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(legacyTraceEndpoint);
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
                }

                if (consoleEnabled)
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }

    private static void ConfigureStandardExporter(
        OtlpExporterOptions options,
        string endpoint,
        string headers)
    {
        options.Endpoint = new Uri(endpoint);
        options.Protocol = OtlpExportProtocol.Grpc;

        if (!headers.IsNullOrEmpty())
        {
            options.Headers = headers;
        }
    }
}
