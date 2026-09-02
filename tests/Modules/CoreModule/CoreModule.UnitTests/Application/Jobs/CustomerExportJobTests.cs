// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using BridgingIT.DevKit.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Tests for <see cref="CustomerExportJob"/> validating background job execution scenarios.
/// </summary>
[UnitTest("Application")]
public class CustomerExportJobTests
{
    /// <summary>Verifies successful export job execution with customer data.</summary>
    [Fact]
    public async Task DispatchAndWaitAsync_WithCustomers_CompletesWithExportCount()
    {
        // Arrange
        var repository = Substitute.For<IGenericRepository<Customer>>();
        var customers = new[]
        {
            Customer.Create("John", "Doe", "john.export@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value,
            Customer.Create("Jane", "Smith", "jane.export@example.com", CustomerNumber.Create("CUS-2026-100001").Value).Value
        };
        repository.FindAllAsync(Arg.Any<IFindOptions<Customer>>(), Arg.Any<CancellationToken>())
            .Returns(customers);
        using var harness = CreateHarness(repository);

        // Act
        var result = await harness.DispatchAndWaitAsync<CustomerExportJob>();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(JobExecutionStatus.Completed);
        result.Value.Messages.ShouldContain(message => message.Contains("Customers=2"));
    }

    /// <summary>Verifies successful job completion when no customers exist.</summary>
    [Fact]
    public async Task DispatchAndWaitAsync_NoCustomers_CompletesWithZeroExportCount()
    {
        // Arrange
        var repository = Substitute.For<IGenericRepository<Customer>>();
        repository.FindAllAsync(Arg.Any<IFindOptions<Customer>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        using var harness = CreateHarness(repository);

        // Act
        var result = await harness.DispatchAndWaitAsync<CustomerExportJob>();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(JobExecutionStatus.Completed);
        result.Value.Messages.ShouldContain(message => message.Contains("Customers=0"));
    }

    /// <summary>Verifies repository failures are returned to the Jobs runtime.</summary>
    [Fact]
    public async Task ExecuteAsync_RepositoryFailure_ReturnsFailure()
    {
        // Arrange
        var repository = Substitute.For<IGenericRepository<Customer>>();
        repository.FindAllAsync(Arg.Any<IFindOptions<Customer>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<Customer>>>(_ => throw new InvalidOperationException("Repository unavailable"));
        var context = new JobExecutionContextBuilder<Unit>()
            .WithJobName(CustomerExportJob.JobName)
            .WithTriggerName(CustomerExportJob.TriggerName)
            .WithData(Unit.Value)
            .Build();
        var sut = new CustomerExportJob(Substitute.For<ILogger<CustomerExportJob>>(), repository);

        // Act
        var result = await sut.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Message.Contains("Repository unavailable"));
    }

    private static JobSchedulerTestHarness CreateHarness(IGenericRepository<Customer> repository)
    {
        return JobSchedulerTestHarness.Create()
            .WithJob<CustomerExportJob>(CustomerExportJob.JobName, job => job
                .UseLifetime(ServiceLifetime.Scoped)
                .AddTrigger("manual", trigger => trigger.Manual()))
            .WithServices(services =>
            {
                services.AddLogging();
                services.AddSingleton(repository);
            })
            .Build();
    }
}
