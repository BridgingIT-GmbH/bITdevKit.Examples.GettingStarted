// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

[UnitTest("Application")]
public class CustomerDeleteCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.delete@example.com", CustomerNumber.Create("CN-100001").Value).Value;
        await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerDeleteCommand(customer.Id.Value.ToString());

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        
        var deletedCustomer = await repository.FindOneAsync(customer.Id, cancellationToken: CancellationToken.None);
        deletedCustomer.ShouldBeNull();
    }

    [Fact]
    public async Task Process_InvalidId_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new CustomerDeleteCommand("invalid-guid");

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
    }

    [Fact]
    public async Task Process_NonExistentCustomer_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new CustomerDeleteCommand(Guid.NewGuid().ToString());

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
        response.Errors.ShouldNotBeEmpty();
    }
}
