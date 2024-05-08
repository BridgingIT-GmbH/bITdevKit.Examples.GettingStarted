// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.UnitTests.Application;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;

[UnitTest("GettingStarted.Application")]
public class CustomerCreateCommandHandlerTests
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var repository = Substitute.For<IGenericRepository<Customer>>();
        var command = new CustomerCreateCommand { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };
        var sut = new CustomerCreateCommandHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(command, CancellationToken.None);

        // Assert
        response?.Result.ShouldNotBeNull();
        response.Result.Value.FirstName.ShouldBe(command.FirstName);
        response.Result.Value.LastName.ShouldBe(command.LastName);
        await repository.Received(1).UpsertAsync(Arg.Any<Customer>(), CancellationToken.None);
    }
}
