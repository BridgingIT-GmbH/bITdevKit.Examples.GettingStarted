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
public class CustomerCreateCommandHandlerTests(ITestOutputHelper output) : TestsBase(output, s =>
    {
        s.AddMapping().WithMapster<CoreModuleMapperRegister>();
        s.AddRequester().AddHandlers();
        s.AddNotifier().AddHandlers();

        s.AddInMemoryRepository(new InMemoryContext<Customer>())
            .WithBehavior<RepositoryLoggingBehavior<Customer>>();
    })
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new CustomerCreateCommand(
            new CustomerModel() { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
        response.Value.Id.ShouldNotBe(Guid.Empty.ToString());
        response.Value.FirstName.ShouldBe(command.Model.FirstName);
        response.Value.LastName.ShouldBe(command.Model.LastName);
    }
}
