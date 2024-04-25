// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Core.UnitTests.Application.Queries;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Domain.Model;
using Microsoft.Extensions.Logging;

public class CustomerFindAllQueryHandlerTests
{
    [Fact]
    public async Task Process_ValidQuery_ReturnsSuccessResultWithCustomers()
    {
        // Arrange
        var expectedCustomers = new List<Customer>
        {
            new() { FirstName = "John", LastName = "Doe" },
            new() { FirstName = "Jane", LastName = "Smith" }
        };

        var repository = Substitute.For<IGenericRepository<Customer>>();
        repository.FindAllAsync(cancellationToken: CancellationToken.None)
            .Returns(expectedCustomers.AsEnumerable());

        var sut = new CustomerFindAllQueryHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(new CustomerFindAllQuery(), CancellationToken.None);

        // Assert
        response?.Result.ShouldNotBeNull();
        response.Result.Count().ShouldBe(expectedCustomers.Count);
        await repository.Received(1).FindAllAsync(
            cancellationToken: CancellationToken.None);
    }
}
