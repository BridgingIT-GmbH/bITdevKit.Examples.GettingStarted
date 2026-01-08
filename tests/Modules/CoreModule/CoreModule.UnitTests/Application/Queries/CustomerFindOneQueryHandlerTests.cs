// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

/// <summary>
/// Tests for <see cref="CustomerFindOneQueryHandler"/> validating single customer retrieval scenarios
/// including successful lookups and error handling for missing customers.
/// </summary>
[UnitTest("Application")]
public class CustomerFindOneQueryHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful retrieval of existing customer by ID.</summary>
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.findone@example.com", CustomerNumber.Create("CN-100004").Value).Value;
        await repository.InsertAsync(customer, CancellationToken.None);

        var query = new CustomerFindOneQuery(customer.Id.Value.ToString());

        // Act
        var response = await requester.SendAsync(query, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
        response.Value.Id.ShouldBe(customer.Id.Value.ToString());
        response.Value.FirstName.ShouldBe("John");
        response.Value.LastName.ShouldBe("Doe");
        response.Value.Email.ShouldBe("john.findone@example.com");
    }

    /// <summary>Verifies failure when customer ID does not exist.</summary>
    [Fact]
    public async Task Process_NonExistentCustomer_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var query = new CustomerFindOneQuery(Guid.NewGuid().ToString());

        // Act
        var response = await requester.SendAsync(query, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
        response.Errors.ShouldNotBeEmpty();
    }

    /// <summary>Verifies failure when customer ID is empty.</summary>
    [Fact]
    public async Task Process_EmptyCustomerId_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var query = new CustomerFindOneQuery("");

        // Act
        var response = await requester.SendAsync(query, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
    }
}
