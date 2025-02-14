// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.UnitTests.Application.Commands;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Application.Commands;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain.Model;

[UnitTest("GettingStarted.Application")]
public class CustomerCreateCommandHandlerTests(ITestOutputHelper output) : TestsBase(output, s =>
    {
        s.AddInMemoryRepository(new InMemoryContext<Customer>())
            .WithBehavior<RepositoryLoggingBehavior<Customer>>();
    })
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();
        var command = new CustomerCreateCommand { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" };
        var sut = new CustomerCreateCommandHandler(Substitute.For<ILoggerFactory>(), repository);

        // Act
        var response = await sut.Process(command, CancellationToken.None);

        // Assert
        response?.Result.ShouldBeSuccess();
        response.Result.Value.ShouldNotBeNull();
        response.Result.Value.FirstName.ShouldBe(command.FirstName);
        response.Result.Value.LastName.ShouldBe(command.LastName);
    }
}
