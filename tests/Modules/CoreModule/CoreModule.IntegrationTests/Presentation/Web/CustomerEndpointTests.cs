// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using System.Text;
using System.Text.Json;

[IntegrationTest("Presentation.Web")]
[Collection(nameof(EndpointCollection))]
public class CustomerEndpointTests
{
    private readonly EndpointTestFixture<Program> fixture;
    private readonly ITestOutputHelper output;

    public CustomerEndpointTests(ITestOutputHelper output, EndpointTestFixture<Program> fixture)
    {
        this.fixture = fixture;
        this.output = output;
        this.fixture.Attach(output);
        this.fixture.Options(new()
        {
            TokenEndpoint = "/api/_system/identity/connect/token",
            ClientId = "test-client",
            Username = "clever.dragon@example.com",
            Password = "fantasy",
            Scope = "openid profile email roles"
        });
    }

    /// <summary>
    /// Ensures that retrieving an existing entity by ID returns HTTP 200 (OK) and the response body contains the expected entity details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleExisting_ReturnsOk(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);

        // Act
        var response = await this.fixture.Client.GetAsync(route + $"/{model.Id}");
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");

        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();

        this.output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Ensures that retrieving a non-existing entity by a random ID returns HTTP 404 (Not Found).
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Get_SingleNotExisting_ReturnsNotFound(string route)
    {
        // Act
        var response = await this.fixture.Client.GetAsync(route + $"/{Guid.NewGuid()}");
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

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
        var model = await this.SeedEntity(route);

        // Act
        var response = await this.fixture.Client.GetAsync(route);
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");
        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();

        this.output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Verifies that retrieving entities by filter returns HTTP 200 (OK) and contains at least one matching entity with valid details.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_MultipleByFilter_ReturnsOk(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);
        var filter = FilterModelBuilder.For<CustomerModel>()
            .AddFilter(e => e.Email, FilterOperator.Equal, model.Email)
            .AddFilter(e => e.LastName, FilterOperator.Equal, model.LastName).Build();
        var filterJson = JsonSerializer.Serialize(filter, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(filterJson, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        this.output.WriteLine($"RequestModel: {model.DumpText()}");

        // Act
        var response = await this.fixture.Client.PostAsync(route + "/search", content);
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.Email}*");
        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();

        this.output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Confirms that posting a valid model returns HTTP 201 (Created) and the response includes the newly created entity.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_ValidModel_ReturnsCreated(string route)
    {
        // Arrange
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel { FirstName = $"John{ticks}", LastName = $"Doe{ticks}", Email = $"john.doe{ticks}@example.com" };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        this.output.WriteLine($"RequestModel: {model.DumpText()}");

        // Act
        var response = await this.fixture.Client.PostAsync(route, content);
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

        // Assert
        response.Should().Be201Created();
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Ensures that posting an invalid entity model returns HTTP 400 (Bad Request) with validation error messages.
    /// </summary>
    [Theory]
    [InlineData("api/coremodule/customers")]
    public async Task Post_InvalidEntity_ReturnsBadRequest(string route)
    {
        // Arrange
        var model = new CustomerModel { FirstName = string.Empty, LastName = string.Empty, Email = string.Empty };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        this.output.WriteLine($"RequestModel: {model.DumpText()}");

        // Act
        var response = await this.fixture.Client.PostAsync(route, content);
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

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
        var model = await this.SeedEntity(route);
        model.FirstName += "changed";
        model.LastName += "changed";
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        this.output.WriteLine($"RequestModel: {model.DumpText()}");

        // Act
        var response = await this.fixture.Client.PutAsync(route + $"/{model.Id}", content);
        this.output.WriteLine($"Response: status={(int)response.StatusCode}, content={await response.Content.ReadAsStringAsync()}");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    /// <summary>
    /// Creates and posts a new entity to the provided endpoint returning the created model.
    /// </summary>
    private async Task<CustomerModel> SeedEntity(string route)
    {
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel { FirstName = $"John{ticks}", LastName = $"Doe{ticks}", Email = $"john.doe{ticks}@example.com" };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        var response = await this.fixture.Client.PostAsync(route, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<CustomerModel>();
    }
}