// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Events;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tests for <see cref="CustomerUpdatedDomainEventHandler"/> validating domain event handling
/// for customer update events.
/// </summary>
[UnitTest("Application")]
public class CustomerUpdatedDomainEventHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful processing of customer updated event.</summary>
    [Fact]
    public async Task Process_ValidEvent_HandlesSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerUpdatedDomainEventHandler(loggerFactory);
        
        var customer = Customer.Create("John", "Doe", "john.updated@example.com", CustomerNumber.Create("CN-100007").Value).Value;
        var domainEvent = new CustomerUpdatedDomainEvent(customer);

        // Act
        var canHandle = handler.CanHandle(domainEvent);
        await handler.Process(domainEvent, CancellationToken.None);

        // Assert
        canHandle.ShouldBeTrue();
    }

    /// <summary>Verifies handler accepts customer updated events.</summary>
    [Fact]
    public void CanHandle_ValidEvent_ReturnsTrue()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerUpdatedDomainEventHandler(loggerFactory);
        
        var customer = Customer.Create("Jane", "Smith", "jane.updated@example.com", CustomerNumber.Create("CN-100008").Value).Value;
        var domainEvent = new CustomerUpdatedDomainEvent(customer);

        // Act
        var result = handler.CanHandle(domainEvent);

        // Assert
        result.ShouldBeTrue();
    }
}

/// <summary>
/// Tests for <see cref="CustomerUpdatedDomainEventHandler2"/> validating generic entity updated event handling.
/// </summary>
[UnitTest("Application")]
public class CustomerUpdatedDomainEventHandler2Tests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful processing of generic entity updated event.</summary>
    [Fact]
    public async Task Process_EntityUpdatedEvent_HandlesSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerUpdatedDomainEventHandler2(loggerFactory);
        
        var customer = Customer.Create("John", "Doe", "john.entity@example.com", CustomerNumber.Create("CN-100009").Value).Value;
        var domainEvent = new EntityUpdatedDomainEvent<Customer>(customer);

        // Act
        var canHandle = handler.CanHandle(domainEvent);
        await handler.Process(domainEvent, CancellationToken.None);

        // Assert
        canHandle.ShouldBeTrue();
    }

    /// <summary>Verifies handler accepts generic entity updated events.</summary>
    [Fact]
    public void CanHandle_EntityUpdatedEvent_ReturnsTrue()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var handler = new CustomerUpdatedDomainEventHandler2(loggerFactory);
        
        var customer = Customer.Create("Jane", "Doe", "jane.entity@example.com", CustomerNumber.Create("CN-100010").Value).Value;
        var domainEvent = new EntityUpdatedDomainEvent<Customer>(customer);

        // Act
        var result = handler.CanHandle(domainEvent);

        // Assert
        result.ShouldBeTrue();
    }
}
