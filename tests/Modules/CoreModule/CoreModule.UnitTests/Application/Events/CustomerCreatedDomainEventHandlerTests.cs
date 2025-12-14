// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Events;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using Microsoft.Extensions.Logging;

[UnitTest("Application")]
public class CustomerCreatedDomainEventHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task Process_ValidEvent_HandlesSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerCreatedDomainEventHandler(loggerFactory);
        
        var customer = Customer.Create("John", "Doe", "john.event@example.com", CustomerNumber.Create("CN-100005").Value).Value;
        var domainEvent = new CustomerCreatedDomainEvent(customer);

        // Act
        var canHandle = handler.CanHandle(domainEvent);
        await handler.Process(domainEvent, CancellationToken.None);

        // Assert
        canHandle.ShouldBeTrue();
    }

    [Fact]
    public void CanHandle_ValidEvent_ReturnsTrue()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerCreatedDomainEventHandler(loggerFactory);
        
        var customer = Customer.Create("Jane", "Doe", "jane.event@example.com", CustomerNumber.Create("CN-100006").Value).Value;
        var domainEvent = new CustomerCreatedDomainEvent(customer);

        // Act
        var result = handler.CanHandle(domainEvent);

        // Assert
        result.ShouldBeTrue();
    }
}
