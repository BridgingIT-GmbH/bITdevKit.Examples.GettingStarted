// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.IntegrationTests.Presentation.Web;

using System.Text.Json;
using BridgingIT.DevKit.Examples.GettingStarted.Presentation;

[IntegrationTest("GettingStarted.Presentation.Web")]
public class CustomerEndpointTests(ITestOutputHelper output, CustomWebApplicationFactoryFixture<Program> fixture) : IClassFixture<CustomWebApplicationFactoryFixture<Program>> // https://xunit.net/docs/shared-context#class-fixture
{
    private readonly CustomWebApplicationFactoryFixture<Program> fixture = fixture.WithOutput(output);

    [Theory]
    [InlineData("api/customers")]
    public async Task Get_SingleExisting_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var model = await this.PostCustomerCreate(route);

        // Act
        var response = await this.fixture.CreateClient()
            .GetAsync(route + $"/{model.Id}").AnyContext();
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
    [InlineData("api/customers")]
    public async Task Get_SingleNotExisting_ReturnsNotFound(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");

        // Act
        var response = await this.fixture.CreateClient()
            .GetAsync(route + $"/{Guid.NewGuid()}").AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be404NotFound(); // https://github.com/adrianiftode/FluentAssertions.Web
    }

    [Theory]
    [InlineData("api/customers")]
    public async Task Get_MultipleExisting_ReturnsOk(string route)
    {
        // Arrange & Act
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var model = await this.PostCustomerCreate(route);

        var response = await this.fixture.CreateClient()
            .GetAsync(route).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().Satisfy<ICollection<CustomerModel>>(
            model =>
            {
                model.ShouldNotBeNull();
            });
        response.Should().MatchInContent($"*{model.FirstName}*");
        response.Should().MatchInContent($"*{model.LastName}*");
        response.Should().MatchInContent($"*{model.Email}*");
        var responseModel = await response.Content.ReadAsAsync<ICollection<CustomerModel>>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    [Theory]
    [InlineData("api/customers")]
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
            JsonSerializer.Serialize(model, DefaultSystemTextJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.CreateClient()
            .PostAsync(route, content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be201Created(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Headers.Location.Should().NotBeNull();
        var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
        responseModel.ShouldNotBeNull();
        this.fixture.Output.WriteLine($"ResponseModel: {responseModel.DumpText()}");
    }

    [Theory]
    [InlineData("api/customers")]
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
            JsonSerializer.Serialize(model, DefaultSystemTextJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.CreateClient()
            .PostAsync(route, content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be400BadRequest(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().MatchInContent($"*[ValidationException]*");
        response.Should().MatchInContent($"*{nameof(model.FirstName)}*");
        response.Should().MatchInContent($"*{nameof(model.LastName)}*");
        response.Should().MatchInContent($"*{nameof(model.Email)}*");
    }

    [Theory]
    [InlineData("api/customers")]
    public async Task Put_ValidModel_ReturnsOk(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var model = await this.PostCustomerCreate(route);
        model.FirstName += "changed";
        model.LastName += "changed";
        var content = new StringContent(
            JsonSerializer.Serialize(model, DefaultSystemTextJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.CreateClient()
            .PutAsync(route + $"/{model.Id}", content).AnyContext();
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

    private async Task<CustomerModel> PostCustomerCreate(string route)
    {
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel
        {
            FirstName = $"John{ticks}",
            LastName = $"Doe{ticks}",
            Email = $"john.doe{ticks}@example.com"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(model, DefaultSystemTextJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);
        var response = await this.fixture.CreateClient()
            .PostAsync(route, content).AnyContext();
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<CustomerModel>();
    }
}