// TEMPLATE: Quartz job unit tests
// Replace placeholders in brackets with your job name.

namespace [RootNamespace].Modules.[Module].UnitTests.Application;

using NSubstitute;
using Quartz;
using Shouldly;
using Xunit;

[UnitTest("Application")]
public class [JobName]Tests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task Process_WithValidState_CompletesSuccessfully()
    {
        // Arrange
        var loggerFactory = this.ServiceProvider.GetService<ILoggerFactory>();
        var scopeFactory = this.ServiceProvider.GetService<IServiceScopeFactory>();
        var job = new [JobName](loggerFactory, scopeFactory);
        var context = Substitute.For<IJobExecutionContext>();

        // Act
        await job.Process(context, CancellationToken.None);

        // Assert
        context.ShouldNotBeNull();
    }
}
