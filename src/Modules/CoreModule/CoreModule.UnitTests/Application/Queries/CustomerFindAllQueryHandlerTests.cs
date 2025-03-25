// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Queries;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application.Queries;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

[UnitTest("GettingStarted.Application")]
public class CustomerFindAllQueryHandlerTests(ITestOutputHelper output) : TestsBase(output, s =>
    {
        s.AddInMemoryRepository(new InMemoryContext<Customer>())
            .WithBehavior<RepositoryLoggingBehavior<Customer>>();
    })
{
    [Fact]
    public async Task Process_ValidQuery_ReturnsSuccessResultWithCustomers()
    {
        // Arrange
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();
        await repository.InsertAsync(Customer.Create("John", "Doe", "john.doe@example.com"));
        await repository.InsertAsync(Customer.Create("Mary", "Jane", "mary.jane@example.com"));

        var sut = new CustomerFindAllQueryHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(new CustomerFindAllQuery(), CancellationToken.None);

        // Assert
        response?.Result.ShouldBeSuccess();
        response.Result.Value.ShouldNotBeNull();
        response.Result.Value.Count().ShouldBe(2);
    }
}
