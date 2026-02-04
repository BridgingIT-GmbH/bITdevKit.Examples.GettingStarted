// TEMPLATE: Endpoint integration test fixture usage
// Replace placeholders in brackets with your module and program types.

namespace [RootNamespace].Modules.[Module].IntegrationTests.Presentation.Web;

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
}
