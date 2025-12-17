// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

/// <summary>
/// Tests for <see cref="CustomerCreateCommandHandler"/> validating customer creation scenarios
/// including success cases, validation failures, and business rule enforcement.
/// </summary>
[UnitTest("Application")]
public class CustomerCreateCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful customer creation with valid data.</summary>
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
