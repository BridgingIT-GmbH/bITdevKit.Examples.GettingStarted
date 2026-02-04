// TEMPLATE: Authorization and authentication integration tests
// Replace placeholders in brackets with your module and entity names.

namespace [RootNamespace].Modules.[Module].IntegrationTests.Presentation.Web;

using Xunit;

[IntegrationTest("Presentation.Web")]
[Collection(nameof(EndpointCollection))]
public class [Entity]AuthorizationTests
{
    private readonly EndpointTestFixture<Program> fixture;

    public [Entity]AuthorizationTests(EndpointTestFixture<Program> fixture)
    {
        this.fixture = fixture;
    }

    [Theory]
    [InlineData("api/[module]/[entities]")]
    public async Task Get_WithoutToken_ReturnsUnauthorized(string route)
    {
        // Arrange
        using var client = new HttpClient();

        // Act
        var response = await client.GetAsync(route);

        // Assert
        response.Should().Be401Unauthorized();
    }
}
