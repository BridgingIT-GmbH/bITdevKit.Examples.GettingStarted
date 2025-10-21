// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Queries;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests;

[UnitTest("GettingStarted.Application")]
public class CustomerFindAllQueryHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task Process_ValidQuery_ReturnsSuccessResultWithCustomers()
    {
        // Arrange
        var timeProvider = this.ServiceProvider.GetService<TimeProvider>();
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();
        await repository.InsertAsync(Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create(timeProvider.GetUtcNow(), 100000)));
        await repository.InsertAsync(Customer.Create("Mary", "Jane", "mary.jane@example.com", CustomerNumber.Create(timeProvider.GetUtcNow(), 100001)));
        var query = new CustomerFindAllQuery();

        // Act
        var response = await requester.SendAsync(query, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
        response.Value.Count().ShouldBe(2);
    }
}
