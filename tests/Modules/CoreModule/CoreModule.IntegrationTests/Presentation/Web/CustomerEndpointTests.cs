// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

using System.Net.Http.Json;
using System.Text.Json;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;

[IntegrationTest("Presentation.Web")]
[Category("integration")]
[Collection(nameof(EndpointCollection))]
public class CustomerEndpointTests
{
    private const string Route = "/api/coremodule/customers";
    private static readonly JsonSerializerOptions JsonOptions = Common.DefaultJsonSerializerOptions.Create();
    private readonly EndpointTestFixture<Program> fixture;

    public CustomerEndpointTests(ITestOutputHelper output, EndpointTestFixture<Program> fixture)
    {
        this.fixture = fixture;
        this.fixture.Attach(output);
    }

    [Fact]
    public async Task GetById_ExistingCustomer_ReturnsExactCustomer()
    {
        var expected = await this.SeedCustomerAsync();

        using var response = await this.fixture.Client.GetAsync($"{Route}/{expected.Id}");

        response.Should().Be200Ok();
        var actual = await ReadCustomerAsync(response);
        AssertCustomer(actual, expected);
    }

    [Fact]
    public async Task GetById_MissingCustomer_ReturnsNotFound()
    {
        using var response = await this.fixture.Client.GetAsync($"{Route}/{Guid.NewGuid()}");

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GetAll_ExistingCustomer_ReturnsCustomerInCollection()
    {
        var expected = await this.SeedCustomerAsync();

        using var response = await this.fixture.Client.GetAsync(Route);

        response.Should().Be200Ok();
        var customers = await ReadCustomersAsync(response);
        var actual = customers.Single(customer => customer.Id == expected.Id);
        AssertCustomer(actual, expected);
    }

    [Fact]
    public async Task Search_MatchingFilters_ReturnsOnlyMatchingCustomers()
    {
        var expected = await this.SeedCustomerAsync();
        var filter = FilterModelBuilder.For<CustomerModel>()
            .AddFilter(customer => customer.Email, FilterOperator.Equal, expected.Email)
            .AddFilter(customer => customer.LastName, FilterOperator.Equal, expected.LastName)
            .Build();

        using var response = await this.fixture.Client.PostAsJsonAsync($"{Route}/search", filter, JsonOptions);

        response.Should().Be200Ok();
        var customers = await ReadCustomersAsync(response);
        customers.ShouldNotBeEmpty();
        customers.ShouldAllBe(customer => customer.Email == expected.Email && customer.LastName == expected.LastName);
        customers.ShouldContain(customer => customer.Id == expected.Id);
    }

    [Fact]
    public async Task GetAll_MatchingQueryFilter_ReturnsOnlyMatchingCustomers()
    {
        var expected = await this.SeedCustomerAsync();
        var filter = FilterModelBuilder.For<CustomerModel>()
            .AddFilter(customer => customer.Email, FilterOperator.Equal, expected.Email)
            .Build();

        var filterJson = JsonSerializer.Serialize(filter, JsonOptions);
        using var response = await this.fixture.Client.GetAsync($"{Route}?filter={Uri.EscapeDataString(filterJson)}");

        response.Should().Be200Ok();
        var customers = await ReadCustomersAsync(response);
        customers.ShouldNotBeEmpty();
        customers.ShouldAllBe(customer => customer.Email == expected.Email);
        customers.ShouldContain(customer => customer.Id == expected.Id);
    }

    [Fact]
    public async Task Create_ValidCustomer_ReturnsCreatedCustomerAndLocation()
    {
        var request = CreateCustomerRequest();

        using var response = await this.fixture.Client.PostAsJsonAsync(Route, request);

        response.Should().Be201Created();
        var created = await ReadCustomerAsync(response);
        AssertCreatedCustomer(created, request);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldBe($"{Route}/{created.Id}");

        var persisted = await this.GetCustomerAsync(created.Id);
        AssertCustomer(persisted, created);
    }

    [Fact]
    public async Task Create_InvalidCustomer_ReturnsValidationProblem()
    {
        var request = new CustomerModel
        {
            FirstName = string.Empty,
            LastName = string.Empty,
            Email = string.Empty,
            Addresses = []
        };

        using var response = await this.fixture.Client.PostAsJsonAsync(Route, request);

        response.Should().Be400BadRequest();
        await AssertValidationProblemAsync(response, nameof(CustomerModel.FirstName), nameof(CustomerModel.LastName), nameof(CustomerModel.Email));
    }

    [Fact]
    public async Task Update_ValidCustomer_PersistsChanges()
    {
        var customer = await this.SeedCustomerAsync();
        var originalConcurrencyVersion = customer.ConcurrencyVersion;
        customer.FirstName = $"Updated{Guid.NewGuid():N}";
        customer.LastName = $"Updated{Guid.NewGuid():N}";

        using var response = await this.fixture.Client.PutAsJsonAsync($"{Route}/{customer.Id}", customer);

        response.Should().Be200Ok();
        var updated = await ReadCustomerAsync(response);
        updated.FirstName.ShouldBe(customer.FirstName);
        updated.LastName.ShouldBe(customer.LastName);
        updated.ConcurrencyVersion.ShouldNotBe(originalConcurrencyVersion);

        var persisted = await this.GetCustomerAsync(customer.Id);
        AssertCustomer(persisted, updated);
    }

    [Fact]
    public async Task Update_MismatchedRouteAndBodyIds_ReturnsBadRequest()
    {
        var customer = await this.SeedCustomerAsync();

        using var response = await this.fixture.Client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", customer);

        response.Should().Be400BadRequest();
        (await response.Content.ReadAsStringAsync()).ShouldContain("ID in the route must match");
    }

    [Fact]
    public async Task Update_StaleConcurrencyVersion_ReturnsConflict()
    {
        var staleCustomer = await this.SeedCustomerAsync();
        var currentCustomer = Copy(staleCustomer);
        currentCustomer.FirstName = $"Current{Guid.NewGuid():N}";
        using var firstResponse = await this.fixture.Client.PutAsJsonAsync($"{Route}/{currentCustomer.Id}", currentCustomer);
        firstResponse.Should().Be200Ok();

        staleCustomer.LastName = $"Stale{Guid.NewGuid():N}";
        using var staleResponse = await this.fixture.Client.PutAsJsonAsync($"{Route}/{staleCustomer.Id}", staleCustomer);

        staleResponse.Should().Be409Conflict();
    }

    [Fact]
    public async Task Update_MissingCustomer_ReturnsNotFound()
    {
        var customer = CreateCustomerRequest();
        customer.Id = Guid.NewGuid().ToString();
        customer.ConcurrencyVersion = Guid.NewGuid().ToString();

        using var response = await this.fixture.Client.PutAsJsonAsync($"{Route}/{customer.Id}", customer);

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_PersistsStatus()
    {
        var customer = await this.SeedCustomerAsync();
        var request = new CustomerUpdateStatusRequestModel { Status = "Active" };

        using var response = await this.fixture.Client.PutAsJsonAsync($"{Route}/{customer.Id}/status", request);

        response.Should().Be200Ok();
        var updated = await ReadCustomerAsync(response);
        updated.Status.ShouldBe("Active");

        var persisted = await this.GetCustomerAsync(customer.Id);
        persisted.Status.ShouldBe("Active");
    }

    [Fact]
    public async Task UpdateStatus_InvalidStatus_ReturnsValidationProblem()
    {
        var customer = await this.SeedCustomerAsync();
        var request = new CustomerUpdateStatusRequestModel { Status = "Unknown" };

        using var response = await this.fixture.Client.PutAsJsonAsync($"{Route}/{customer.Id}/status", request);

        response.Should().Be400BadRequest();
        await AssertValidationProblemAsync(response, nameof(CustomerUpdateStatusRequestModel.Status));
    }

    [Fact]
    public async Task Delete_ExistingCustomer_RemovesCustomer()
    {
        var customer = await this.SeedCustomerAsync();

        using var response = await this.fixture.Client.DeleteAsync($"{Route}/{customer.Id}");

        response.Should().Be204NoContent();
        using var getResponse = await this.fixture.Client.GetAsync($"{Route}/{customer.Id}");
        getResponse.Should().Be404NotFound();
    }

    [Fact]
    public async Task Delete_MissingCustomer_ReturnsNotFound()
    {
        using var response = await this.fixture.Client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        response.Should().Be404NotFound();
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = this.fixture.CreateUnauthenticatedClient();

        using var response = await client.GetAsync(Route);

        response.Should().Be401Unauthorized();
    }

    private static CustomerModel CreateCustomerRequest()
    {
        var suffix = Guid.NewGuid().ToString("N");
        return new CustomerModel
        {
            FirstName = $"First{suffix}",
            LastName = $"Last{suffix}",
            Email = $"customer.{suffix}@example.com",
            DateOfBirth = new DateOnly(1990, 5, 15),
            Status = "Lead",
            Addresses = []
        };
    }

    private static CustomerModel Copy(CustomerModel source) => new()
    {
        Id = source.Id,
        FirstName = source.FirstName,
        LastName = source.LastName,
        Number = source.Number,
        DateOfBirth = source.DateOfBirth,
        Email = source.Email,
        Status = source.Status,
        ConcurrencyVersion = source.ConcurrencyVersion,
        Addresses = source.Addresses?.Select(address => new CustomerAddressModel
        {
            Id = address.Id,
            Name = address.Name,
            Line1 = address.Line1,
            Line2 = address.Line2,
            PostalCode = address.PostalCode,
            City = address.City,
            Country = address.Country,
            IsPrimary = address.IsPrimary
        }).ToList() ?? []
    };

    private static void AssertCreatedCustomer(CustomerModel actual, CustomerModel request)
    {
        Guid.TryParse(actual.Id, out var id).ShouldBeTrue();
        id.ShouldNotBe(Guid.Empty);
        actual.Number.ShouldMatch("^CUS-[0-9]{4}-[0-9]{6}$");
        Guid.TryParse(actual.ConcurrencyVersion, out var concurrencyVersion).ShouldBeTrue();
        concurrencyVersion.ShouldNotBe(Guid.Empty);
        actual.FirstName.ShouldBe(request.FirstName);
        actual.LastName.ShouldBe(request.LastName);
        actual.Email.ShouldBe(request.Email);
        actual.DateOfBirth.ShouldBe(request.DateOfBirth);
        actual.Status.ShouldBe(request.Status);
    }

    private static void AssertCustomer(CustomerModel actual, CustomerModel expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.FirstName.ShouldBe(expected.FirstName);
        actual.LastName.ShouldBe(expected.LastName);
        actual.Number.ShouldBe(expected.Number);
        actual.DateOfBirth.ShouldBe(expected.DateOfBirth);
        actual.Email.ShouldBe(expected.Email);
        actual.Status.ShouldBe(expected.Status);
        actual.ConcurrencyVersion.ShouldBe(expected.ConcurrencyVersion);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, params string[] expectedProperties)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;
        root.GetProperty("status").GetInt32().ShouldBe(400);
        root.GetProperty("title").GetString().ShouldBe("Validation Error");

        var errors = root.GetProperty("data").GetProperty("errors");
        var propertyNames = errors.EnumerateObject().Select(property => property.Name).ToArray();
        foreach (var expectedProperty in expectedProperties)
        {
            propertyNames.ShouldContain(name => name.EndsWith(expectedProperty, StringComparison.Ordinal));
        }
    }

    private static async Task<CustomerModel> ReadCustomerAsync(HttpResponseMessage response)
    {
        var customer = await response.Content.ReadFromJsonAsync<CustomerModel>();
        customer.ShouldNotBeNull();
        return customer;
    }

    private static async Task<IReadOnlyCollection<CustomerModel>> ReadCustomersAsync(HttpResponseMessage response)
    {
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerModel>>();
        customers.ShouldNotBeNull();
        return customers;
    }

    private async Task<CustomerModel> SeedCustomerAsync()
    {
        var request = CreateCustomerRequest();
        using var response = await this.fixture.Client.PostAsJsonAsync(Route, request);
        response.Should().Be201Created();

        var created = await ReadCustomerAsync(response);
        AssertCreatedCustomer(created, request);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldBe($"{Route}/{created.Id}");
        return created;
    }

    private async Task<CustomerModel> GetCustomerAsync(string id)
    {
        using var response = await this.fixture.Client.GetAsync($"{Route}/{id}");
        response.Should().Be200Ok();
        return await ReadCustomerAsync(response);
    }
}
