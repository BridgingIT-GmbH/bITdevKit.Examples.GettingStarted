// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

[IntegrationTest("Presentation.Web")]
public class CustomerEndpointTests : IClassFixture<CustomWebApplicationFactoryFixture<GettingStarted.Presentation.Web.Server.Program>> // https://xunit.net/docs/shared-context#class-fixture
{
    private readonly CustomWebApplicationFactoryFixture<GettingStarted.Presentation.Web.Server.Program> fixture;
    private const string AuthTokenEndpoint = "/api/_system/identity/connect/token";
    private const string AuthClientId = "test-client";
    private const string AuthUsername = "clever.dragon@example.com";
    private const string AuthPassword = "fantasy";

    public CustomerEndpointTests(ITestOutputHelper output, CustomWebApplicationFactoryFixture<GettingStarted.Presentation.Web.Server.Program> fixture)
    {
        this.fixture = fixture.WithOutput(output);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        await Task.Delay(2000);
        var client = this.fixture.CreateClient();
        var content = new StringContent($"grant_type=password&client_id={AuthClientId}&username={AuthUsername}&password={AuthPassword}&scope=openid%20profile%20email%20roles", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync(AuthTokenEndpoint, content).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            return tokenElement.GetString();
        }

        throw new InvalidOperationException("Access token not found in response.");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var token = await this.GetAccessTokenAsync();
        var client = this.fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleExisting_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.CreateAuthenticatedClientAsync();
        var model = await this.PostCustomerCreate(route, client);

        // Act
        var response = await client.GetAsync(route + $"/{model.Id}").AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleNotExisting_ReturnsNotFound(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");

        // Act
        var client = await this.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync(route + $"/{Guid.NewGuid()}").AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be404NotFound(); // https://github.com/adrianiftode/FluentAssertions.Web
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_MultipleExisting_ReturnsOk(string route)
    {
        // Arrange & Act
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.CreateAuthenticatedClientAsync();
        var model = await this.PostCustomerCreate(route, client);

        var response = await client.GetAsync(route).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().Satisfy<ICollection<CustomerModel>>(model => model.ShouldNotBeNull());
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");
        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_ValidModel_ReturnsCreated(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel
        {
            FirstName = $"John{ticks}",
            LastName = $"Doe{ticks}",
            Email = $"john.doe{ticks}@example.com"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var client = await this.CreateAuthenticatedClientAsync();
        var response = await client.PostAsync(route, content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be201Created(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Headers.Location.Should().NotBeNull();
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_InvalidEntity_ReturnsBadRequest(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var model = new CustomerModel
        {
            FirstName = string.Empty,
            LastName = string.Empty,
            Email = string.Empty
        };
        var content = new StringContent(
            JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var client = await this.CreateAuthenticatedClientAsync();
        var response = await client.PostAsync(route, content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be400BadRequest(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().MatchInContent($"*[FluentValidationError]*");
        response.Should().MatchInContent($"*{nameof(model.FirstName)}*");
        response.Should().MatchInContent($"*{nameof(model.LastName)}*");
        response.Should().MatchInContent($"*{nameof(model.Email)}*");
    }

    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Put_ValidModel_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.CreateAuthenticatedClientAsync();
        var model = await this.PostCustomerCreate(route, client);
        model.FirstName += "changed";
        model.LastName += "changed";
        var content = new StringContent(
            JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await client.PutAsync(route + $"/{model.Id}", content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Headers.Location.Should().NotBeNull();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    private async Task<CustomerModel> PostCustomerCreate(string route, HttpClient client = null)
    {
        if (client == null)
        {
            client = await this.CreateAuthenticatedClientAsync();
        }
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel
        {
            FirstName = $"John{ticks}",
            LastName = $"Doe{ticks}",
            Email = $"john.doe{ticks}@example.com"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        var response = await client.PostAsync(route, content).AnyContext();
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<CustomerModel>();
    }
}