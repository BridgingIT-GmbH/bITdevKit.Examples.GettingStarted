// TEMPLATE: Application handler unit test via IRequester
// Replace placeholders in brackets with your module and entity names.

namespace [RootNamespace].Modules.[Module].UnitTests.Application;

using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

[UnitTest("Application")]
public class [Entity]CreateCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new [Entity]CreateCommand(
            new [Entity]Model
            {
                // TODO: initialize required fields
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task Process_InvalidRequest_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new [Entity]CreateCommand(
            new [Entity]Model
            {
                // TODO: set invalid values
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.IsFailure.ShouldBeTrue();
        response.Messages.ShouldNotBeEmpty();
    }
}
