// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

/// <summary>
/// Tests for <see cref="CustomerUpdateStatusCommandHandler"/> validating status update scenarios
/// including valid status transitions and business rule enforcement.
/// </summary>
[UnitTest("Application")]
public class CustomerUpdateStatusCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful status transition from Lead to Active.</summary>
    [Fact]
    public async Task ChangeStatus_FromLeadToActive_SetsActive()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var sequenceNumberGenerator = this.ServiceProvider.GetService<ISequenceNumberGenerator>();
        var i = await sequenceNumberGenerator.GetNextAsync(CodeModuleConstants.CustomerNumberSequenceName);
        var created = await requester.SendAsync(new CustomerCreateCommand(new CustomerModel
        {
            FirstName = "Bob",
            LastName = "Miller",
            Email = "bob.miller@example.com"
        }), null, CancellationToken.None);
        created.ShouldBeSuccess();

        // Act
        var result = await requester.SendAsync(
            new CustomerUpdateStatusCommand(created.Value.Id, CustomerStatus.Active.Value), null, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value.Status.ShouldBe(CustomerStatus.Active.Value);
    }

    /// <summary>Verifies successful status transition to Retired.</summary>
    [Fact]
    public async Task ChangeStatus_ToRetired_SetsRetired()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var created = await requester.SendAsync(new CustomerCreateCommand(new CustomerModel
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice2.smith@example.com"
        }), null, CancellationToken.None);
        created.ShouldBeSuccess();

        // Act
        var result = await requester.SendAsync(
            new CustomerUpdateStatusCommand(created.Value.Id, CustomerStatus.Retired.Value), null, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value.Status.ShouldBe(CustomerStatus.Retired.Value);
    }
}
