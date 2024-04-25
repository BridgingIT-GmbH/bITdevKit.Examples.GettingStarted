// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Core.IntegrationTests.Presentation.Web;

using System.Text.Json;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Presentation;

[IntegrationTest("GettingStarted.Presentation")]
[Module("Core")]
public class CustomerEndpointTests : IClassFixture<CustomWebApplicationFactoryFixture<Program>> // https://xunit.net/docs/shared-context#class-fixture
{
    private readonly CustomWebApplicationFactoryFixture<Program> fixture;

    public CustomerEndpointTests(ITestOutputHelper output, CustomWebApplicationFactoryFixture<Program> fixture)
    {
        this.fixture = fixture.WithOutput(output);
    }

    [Theory]
    [InlineData("api/customers")]
    public async Task Get_All_Customers_Returns_Ok(string route)
    {
        // Arrange & Act
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var response = await this.fixture.CreateClient()
            .GetAsync(route).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be200Ok(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Should().Satisfy<IEnumerable<CustomerModel>>(
            model =>
            {
                model.ShouldNotBeNull();
            });
    }

    [Theory]
    [InlineData("api/customers")]
    public async Task Post_ValidModel_ReturnsCreated(string route)
    {
        // Arrange
        this.fixture.Output.WriteLine($"Start Endpoint test for route: {route}");
        var ticks = DateTime.UtcNow.Ticks;
        var model = new CustomerModel { FirstName = $"John{ticks}", LastName = $"Doe{ticks}" };
        var content = new StringContent(
            JsonSerializer.Serialize(model, DefaultSystemTextJsonSerializerOptions.Create()), Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await this.fixture.CreateClient()
            .PostAsync(route, content).AnyContext();
        this.fixture.Output.WriteLine($"Finish Endpoint test for route: {route} (status={(int)response.StatusCode})");

        // Assert
        response.Should().Be201Created(); // https://github.com/adrianiftode/FluentAssertions.Web
        response.Headers.Location.Should().NotBeNull();
    }
}
