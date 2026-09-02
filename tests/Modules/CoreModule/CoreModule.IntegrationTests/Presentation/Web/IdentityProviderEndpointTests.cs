// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using System.Text.Json;

[IntegrationTest("Presentation.Web")]
[Category("integration")]
[Collection(nameof(EndpointCollection))]
public class IdentityProviderEndpointTests
{
    private readonly EndpointTestFixture<Program> fixture;

    public IdentityProviderEndpointTests(ITestOutputHelper output, EndpointTestFixture<Program> fixture)
    {
        this.fixture = fixture;
        this.fixture.Attach(output);
    }

    [Fact]
    public async Task Token_ValidPasswordGrant_ReturnsAccessToken()
    {
        using var client = this.fixture.CreateUnauthenticatedClient();
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "test-client",
            ["username"] = "clever.dragon@example.com",
            ["password"] = "fantasy",
            ["scope"] = "openid profile email roles"
        });

        using var response = await client.PostAsync("/_bdk/api/identity/connect/token", request);

        response.Should().Be200Ok();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        document.RootElement.GetProperty("access_token").GetString().ShouldNotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("token_type").GetString().ShouldBe("Bearer");
    }
}
