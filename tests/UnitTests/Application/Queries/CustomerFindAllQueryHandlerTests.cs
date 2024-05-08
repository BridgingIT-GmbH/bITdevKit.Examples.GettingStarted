// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.UnitTests.Application;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;

[UnitTest("GettingStarted.Application")]
public class CustomerFindAllQueryHandlerTests
{
    [Fact]
    public async Task Process_ValidQuery_ReturnsSuccessResultWithCustomers()
    {
        // Arrange
        var expectedCustomers = new List<Customer>
        {
            Customer.Create("John", "Doe", "john.doe@example.com"),
            Customer.Create("Mary", "Jane", "mary.jane@example.com"),
        };

        var repository = Substitute.For<IGenericRepository<Customer>>();
        repository.FindAllAsync(cancellationToken: CancellationToken.None)
            .Returns(expectedCustomers.AsEnumerable());

        var sut = new CustomerFindAllQueryHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(new CustomerFindAllQuery(), CancellationToken.None);

        // Assert
        response?.Result.ShouldNotBeNull();
        response.Result.Value.Count().ShouldBe(expectedCustomers.Count);
        await repository.Received(1).FindAllAsync(
            cancellationToken: CancellationToken.None);
    }
}
