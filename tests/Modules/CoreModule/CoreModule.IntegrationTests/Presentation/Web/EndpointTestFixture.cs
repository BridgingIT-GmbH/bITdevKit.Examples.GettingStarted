// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using System.Net.Http.Headers;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Infrastructure.EntityFramework;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

[CollectionDefinition(nameof(EndpointCollection))]
public class EndpointCollection : ICollectionFixture<EndpointTestFixture<Program>>
{
}

/// <summary>
/// Collection fixture hosting a single WebApplicationFactory + authenticated HttpClient shared across endpoint tests.
/// Provides logging (Attach) and deferred JWT bearer token acquisition through configurable auth options.
/// </summary>
public class EndpointTestFixture<TProgram> : IAsyncLifetime where TProgram : class
{
    private static readonly FakeUser TestUser = new(
        "endpoint.tests@example.com",
        "Endpoint Tests",
        ["Administrator"]);

    private EndpointWebApplicationFactory<TProgram> factory;
    private SqlServerTestFixture database;
    private ITestOutputHelper output;
    private readonly List<string> logs = new();

    public HttpClient Client { get; private set; }

    public IServiceProvider Services => this.factory?.Services;

    public void Attach(ITestOutputHelper testOutput)
    {
        if (testOutput == null)
        {
            return;
        }

        this.output = testOutput;
        this.database?.Attach(testOutput);
        foreach (var m in this.logs)
        {
            try { this.output.WriteLine(m); } catch { }
        }
    }

    private void Log(string message)
    {
        var line = $"[Fixture] {DateTime.UtcNow:HH:mm:ss.fff} {message}";
        this.logs.Add(line);
        try { this.output?.WriteLine(line); } catch { }
    }

    public async Task InitializeAsync()
    {
        this.Log("Initializing isolated endpoint test database...");
        this.database = new SqlServerTestFixture();
        await this.database.InitializeAsync();
        if (!this.database.Available)
        {
            throw new InvalidOperationException($"No SQL Server is available for endpoint tests. {this.database.FailureReason}");
        }

        this.factory = new EndpointWebApplicationFactory<TProgram>(this.database.ConnectionString, TestUser);
        this.Client = this.factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "FakeUser",
            TestUser.Email);
        this.Log("Authenticated HttpClient created with DevKit fake authentication.");

        this.Log("Waiting for database readiness...");
        var databaseReadyService = this.factory.Services.GetRequiredService<IDatabaseReadyService>();
        await databaseReadyService.WaitForReadyAsync();
    }

    public HttpClient CreateUnauthenticatedClient() =>
        this.factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task DisposeAsync()
    {
        this.Log("Disposing Fixture...");
        try { this.Client?.Dispose(); } catch { }
        try { this.factory?.Dispose(); } catch { }
        if (this.database is not null)
        {
            await this.database.DisposeAsync();
        }
        this.Log("Fixture disposed.");
    }

    private sealed class EndpointWebApplicationFactory<TEntryPoint>(
        string connectionString,
        FakeUser user) : WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Modules:CoreModule:ConnectionStrings:Default"] = connectionString
                }));
            builder.ConfigureTestServices(services => services.AddFakeAuthentication([user]));
        }
    }
}
