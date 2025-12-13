// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using System.Text;
using System.Text.Json;
using System.Threading;
using BridgingIT.DevKit.Domain.Repositories;

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
    private CustomWebApplicationFactoryFixture<TProgram> factory;
    private bool initialized;
    private string accessToken;
    private bool optionsConfigured;
    private ITestOutputHelper output;
    private EndpointTestFixtureOptions options = new();
    private readonly List<string> logs = new();

    public HttpClient Client { get; private set; }

    public void Options(EndpointTestFixtureOptions options)
    {
        if (this.optionsConfigured)
        {
            return; // idempotent
        }

        this.options = options ?? new EndpointTestFixtureOptions();
        this.optionsConfigured = true;
        this.Log($"Auth configured: endpoint={this.options.TokenEndpoint}, clientId={this.options.ClientId}, user={this.options.Username}");

        // If initialization already completed, authenticate immediately (blocking) to avoid race conditions.
        if (this.initialized)
        {
            this.AuthenticateAsync().GetAwaiter().GetResult();
        }
    }

    public void Attach(ITestOutputHelper testOutput)
    {
        if (testOutput == null)
        {
            return;
        }

        this.output = testOutput;
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
        this.Log("Initializing Fixture (factory + client)...");
        this.factory = new CustomWebApplicationFactoryFixture<TProgram>();
        this.Client = this.factory.CreateClient();
        this.Log("HttpClient created from WebApplicationFactory.");
        this.initialized = true;

        this.Log("Waiting for database readiness...");
        var databaseReadyService = this.factory.ServiceProvider.GetRequiredService<IDatabaseReadyService>();
        await databaseReadyService?.WaitForReadyAsync();

        if (this.optionsConfigured)
        {
            await this.AuthenticateAsync();
        }
    }

    public Task DisposeAsync()
    {
        this.Log("Disposing Fixture...");
        try { this.Client?.Dispose(); } catch { }
        try { this.factory?.Dispose(); } catch { }
        this.Log("Fixture disposed.");

        return Task.CompletedTask;
    }

    private async Task AuthenticateAsync()
    {
        if (!this.optionsConfigured || this.accessToken != null)
        {
            return;
        }

        var form = $"grant_type=password&client_id={this.options.ClientId}&username={this.options.Username}&password={this.options.Password}&scope={Uri.EscapeDataString(this.options.Scope)}";
        var content = new StringContent(form, Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await this.Client.PostAsync(this.options.TokenEndpoint, content);
        this.Log($"Token Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            this.accessToken = tokenElement.GetString();
            this.Log("Access token acquired.");
            this.Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.accessToken);
            this.Log("Bearer token attached to HttpClient.");
            return;
        }

        throw new InvalidOperationException("Access token not found in response.");
    }
}