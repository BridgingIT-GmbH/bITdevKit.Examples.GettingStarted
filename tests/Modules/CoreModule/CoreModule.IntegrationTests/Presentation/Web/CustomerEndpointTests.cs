// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

[IntegrationTest("Presentation.Web")]
public class CustomerEndpointTests : IClassFixture<CustomWebApplicationFactoryFixture<Program>>
{
    private readonly CustomWebApplicationFactoryFixture<Program> fixture;
    private const string AuthTokenEndpoint = "/api/_system/identity/connect/token";
    private const string AuthClientId = "test-client";
    private const string AuthUsername = "clever.dragon@example.com";
    private const string AuthPassword = "fantasy";

    private HttpClient client;
    private string accessToken;

    public CustomerEndpointTests(ITestOutputHelper output, CustomWebApplicationFactoryFixture<Program> fixture)
    {
        this.fixture = fixture.WithOutput(output);
    }

    /// <summary>
    /// Ensures that retrieving an existing entity by ID returns HTTP 200 (OK) and the response body contains the expected entity details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleExisting_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.GetOrCreateClientAsync();
        var model = await this.SeedEntity(route, client);

        // Act
        var response = await client.GetAsync(route + $"/{model.Id}");
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");

        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();

        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Ensures that retrieving a non-existing entity by a random ID returns HTTP 404 (Not Found).
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleNotExisting_ReturnsNotFound(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");

        // Act
        var client = await this.GetOrCreateClientAsync();
        var response = await client.GetAsync(route + $"/{Guid.NewGuid()}");
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be404NotFound();
    }

    /// <summary>
    /// Verifies that retrieving all entities returns HTTP 200 (OK) and contains at least one entity with valid details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_MultipleExisting_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.GetOrCreateClientAsync();
        var model = await this.SeedEntity(route, client);

        // Act
        var response = await client.GetAsync(route);
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok();
        response.Should().Satisfy<ICollection<CustomerModel>>(m => m.ShouldNotBeNull());
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");

        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();

        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Verifies that retrieving entities by filter returns HTTP 200 (OK) and contains at least one matching entity with valid details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_MultipleByFilter_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.GetOrCreateClientAsync();
        var model = await this.SeedEntity(route, client);
        var filter = FilterModelBuilder.For<CustomerModel>()
            .AddFilter(e => e.Email, FilterOperator.Equal, model.Email)
            .AddFilter(e => e.LastName, FilterOperator.Equal, model.LastName).Build();
        var content = new StringContent(JsonSerializer.Serialize(filter, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        this.fixture.Output.WriteLine($"RequestModel (Filter): {filter.DumpText()}");
        var response = await client.PostAsync(route + "/search", content);
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok();
        response.Should().Satisfy<ICollection<CustomerModel>>(m => m.ShouldNotBeNull());
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");

        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();

        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Confirms that posting a valid model returns HTTP 201 (Created) and the response includes the newly created entity.
    /// </summary>
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
        var content = new StringContent(JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        this.fixture.Output.WriteLine($"RequestModel: {model.DumpText()}");
        var client = await this.GetOrCreateClientAsync();
        var response = await client.PostAsync(route, content);
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be201Created();
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Ensures that posting an invalid entity model returns HTTP 400 (Bad Request) with validation error messages.
    /// </summary>
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
        var content = new StringContent(JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        this.fixture.Output.WriteLine($"RequestModel: {model.DumpText()}");
        var client = await this.GetOrCreateClientAsync();
        var response = await client.PostAsync(route, content);
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be400BadRequest();
        response.Should().MatchInContent("*[FluentValidationError]*");
        response.Should().MatchInContent($"*{nameof(model.FirstName)}*");
        response.Should().MatchInContent($"*{nameof(model.LastName)}*");
        response.Should().MatchInContent($"*{nameof(model.Email)}*");
    }

    /// <summary>
    /// Validates that updating an existing entity returns HTTP 200 (OK) and persists the modified entity details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Put_ValidModel_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var client = await this.GetOrCreateClientAsync();
        var model = await this.SeedEntity(route, client);
        model.FirstName += "changed";
        model.LastName += "changed";

        var content = new StringContent(JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        this.fixture.Output.WriteLine($"RequestModel: {model.DumpText()}");
        var response = await client.PutAsync(route + $"/{model.Id}", content);
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");

        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Creates and posts a new entity to the provided endpoint returning the created model.
    /// </summary>
    private async Task<CustomerModel> SeedEntity(string route, HttpClient client = null)
    {
        client ??= await this.GetOrCreateClientAsync();
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel
        {
            FirstName = $"John{ticks}",
            LastName = $"Doe{ticks}",
            Email = $"john.doe{ticks}@example.com"
        };

        var content = new StringContent(JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        var response = await client.PostAsync(route, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<CustomerModel>();
    }

    /// <summary>
    /// Lazily creates and caches a single <see cref="HttpClient"/> instance configured with an access token for authenticated requests.
    /// </summary>
    private async Task<HttpClient> GetOrCreateClientAsync()
    {
        if (this.client != null)
        {
            return this.client;
        }

        this.client = this.fixture.CreateClient();
        this.accessToken ??= await GetAccessTokenAsync(this.client);
        this.client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", this.accessToken);

        return this.client;
    }

    /// <summary>
    /// Asynchronously retrieves a valid JWT access token used for authentication.
    /// </summary>
    private static async Task<string> GetAccessTokenAsync(HttpClient client)
    {
        var content = new StringContent($"grant_type=password&client_id={AuthClientId}&username={AuthUsername}&password={AuthPassword}&scope=openid%20profile%20email%20roles", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = await client.PostAsync(AuthTokenEndpoint, content);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        if (doc.RootElement.TryGetProperty("access_token", out var tokenElement))
        {
            return tokenElement.GetString();
        }

        throw new InvalidOperationException("Access token not found in response.");
    }
}