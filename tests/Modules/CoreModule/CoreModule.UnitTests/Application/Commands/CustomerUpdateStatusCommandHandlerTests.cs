// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests;

[UnitTest("Application")]
public class CustomerUpdateStatusCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task ChangeStatus_FromLeadToActive_SetsActive()
    {
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

        var result = await requester.SendAsync(
            new CustomerUpdateStatusCommand(created.Value.Id, CustomerStatus.Active.Id), null, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Status.ShouldBe(CustomerStatus.Active.Id);
    }

    [Fact]
    public async Task ChangeStatus_ToRetired_SetsRetired()
    {
        var requester = this.ServiceProvider.GetService<IRequester>();
        var created = await requester.SendAsync(new CustomerCreateCommand(new CustomerModel
        {
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice2.smith@example.com"
        }), null, CancellationToken.None);
        created.ShouldBeSuccess();

        var result = await requester.SendAsync(
            new CustomerUpdateStatusCommand(created.Value.Id, CustomerStatus.Retired.Id), null, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Status.ShouldBe(CustomerStatus.Retired.Id);
    }
}
