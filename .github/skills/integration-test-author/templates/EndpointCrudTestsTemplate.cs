// TEMPLATE: CRUD endpoint integration tests
// Replace placeholders in brackets with your module and entity names.

namespace [RootNamespace].Modules.[Module].IntegrationTests.Presentation.Web;

using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Xunit;

[IntegrationTest("Presentation.Web")]
[Collection(nameof(EndpointCollection))]
public class [Entity]EndpointTests
{
    private readonly EndpointTestFixture<Program> fixture;
    private readonly ITestOutputHelper output;

    public [Entity]EndpointTests(ITestOutputHelper output, EndpointTestFixture<Program> fixture)
    {
        this.fixture = fixture;
        this.output = output;
        this.fixture.Attach(output);
        this.fixture.Options(new()
        {
            TokenEndpoint = "/api/_system/identity/connect/token",
            ClientId = "test-client",
            Username = "test.user@example.com",
            Password = "password",
            Scope = "openid profile email roles"
        });
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Get_SingleExisting_ReturnsOk(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);

        // Act
        var response = await this.fixture.Client.GetAsync(route + $"/{model.Id}");

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.Id}*");
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Get_SingleNotExisting_ReturnsNotFound(string route)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var response = await this.fixture.Client.GetAsync(route + $"/{id}");

        // Assert
        response.Should().Be404NotFound();
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Get_MultipleExisting_ReturnsOk(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);

        // Act
        var response = await this.fixture.Client.GetAsync(route);

        // Assert
        response.Should().Be200Ok();
        response.Should().MatchInContent($"*{model.Id}*");
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Post_ValidModel_ReturnsCreated(string route)
    {
        // Arrange
        var model = new [Entity]Model
        {
            // TODO: set required fields
        };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.Client.PostAsync(route, content);

        // Assert
        response.Should().Be201Created();
        var responseModel = await response.Content.ReadAsAsync<[Entity]Model>();
        responseModel.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Post_InvalidModel_ReturnsBadRequest(string route)
    {
        // Arrange
        var model = new [Entity]Model
        {
            // TODO: set invalid fields
        };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.Client.PostAsync(route, content);

        // Assert
        response.Should().Be400BadRequest();
        response.Should().MatchInContent("*[FluentValidationError]*");
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Put_ValidModel_ReturnsOk(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.Client.PutAsync(route + $"/{model.Id}", content);

        // Assert
        response.Should().Be200Ok();
        var responseModel = await response.Content.ReadAsAsync<[Entity]Model>();
        responseModel.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Delete_SingleExisting_ReturnsNoContent(string route)
    {
        // Arrange
        var model = await this.SeedEntity(route);

        // Act
        var response = await this.fixture.Client.DeleteAsync(route + $"/{model.Id}");

        // Assert
        response.Should().Be204NoContent();
    }

    private async Task<[Entity]Model> SeedEntity(string route)
    {
        var model = new [Entity]Model
        {
            // TODO: set required fields
        };
        var json = JsonSerializer.Serialize(model, Common.DefaultJsonSerializerOptions.Create());
        var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await this.fixture.Client.PostAsync(route, content);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsAsync<[Entity]Model>();
    }
}
