// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

[UnitTest("Application")]
public class CustomerFindAllQueryHandlerTests(ITestOutputHelper output) : TestsBase(output, s =>
    {
        s.AddMapping().WithMapster<CoreModuleMapperRegister>();
        s.AddRequester().AddHandlers();
        s.AddNotifier().AddHandlers();

        s.AddInMemoryRepository(new InMemoryContext<Customer>())
            .WithBehavior<RepositoryLoggingBehavior<Customer>>();
    })
{
    [Fact]
    public async Task Process_ValidQuery_ReturnsSuccessResultWithCustomers()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();
        await repository.InsertAsync(Customer.Create("John", "Doe", "john.doe@example.com"));
        await repository.InsertAsync(Customer.Create("Mary", "Jane", "mary.jane@example.com"));
        var query = new CustomerFindAllQuery();

        // Act
        var response = await requester.SendAsync(query, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
        response.Value.Count().ShouldBe(2);
    }
}
