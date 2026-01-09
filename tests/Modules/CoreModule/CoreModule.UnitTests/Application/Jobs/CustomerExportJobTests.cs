// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using BridgingIT.DevKit.Application.JobScheduling;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

/// <summary>
/// Tests for <see cref="CustomerExportJob"/> validating background job execution scenarios
/// including export operations and retry configuration.
/// </summary>
[UnitTest("Application")]
public class CustomerExportJobTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful export job execution with customer data.</summary>
    [Fact]
    public async Task Process_WithCustomers_ExportsSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var scopeFactory = this.ServiceProvider.GetService<IServiceScopeFactory>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer1 = Customer.Create("John", "Doe", "john.export@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var customer2 = Customer.Create("Jane", "Smith", "jane.export@example.com", CustomerNumber.Create("CUS-2026-100001").Value).Value;
        await repository.InsertAsync(customer1, CancellationToken.None);
        await repository.InsertAsync(customer2, CancellationToken.None);

        var job = new CustomerExportJob(loggerFactory, scopeFactory);
        var context = Substitute.For<IJobExecutionContext>();

        // Act
        await job.Process(context, CancellationToken.None);

        // Assert - no exception thrown means success
        var customers = await repository.FindAllAsync(cancellationToken: CancellationToken.None);
        customers.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    /// <summary>Verifies successful job completion when no customers exist.</summary>
    [Fact]
    public async Task Process_NoCustomers_CompletesSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var scopeFactory = this.ServiceProvider.GetService<IServiceScopeFactory>();
        var job = new CustomerExportJob(loggerFactory, scopeFactory);
        var context = Substitute.For<IJobExecutionContext>();

        // Act
        await job.Process(context, CancellationToken.None);

        // Assert - no exception thrown
    }
}
