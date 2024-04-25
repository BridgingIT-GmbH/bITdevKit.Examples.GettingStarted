// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace Core.UnitTests.Application.Commands;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Domain.Model;
using Microsoft.Extensions.Logging;

public class CustomerCreateCommandHandlerTests
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var repository = Substitute.For<IGenericRepository<Customer>>();
        var command = new CustomerCreateCommand { FirstName = "John", LastName = "Doe" };
        var sut = new CustomerCreateCommandHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(command, CancellationToken.None);

        // Assert
        response?.Result.ShouldNotBeNull();
        response.Result.FirstName.ShouldBe(command.FirstName);
        response.Result.LastName.ShouldBe(command.LastName);
        await repository.Received(1).UpsertAsync(Arg.Any<Customer>(), CancellationToken.None);
    }
}
