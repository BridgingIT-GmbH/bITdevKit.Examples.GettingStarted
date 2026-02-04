// TEMPLATE: Domain aggregate unit tests
// Replace placeholders in brackets with your aggregate name.

namespace [RootNamespace].Modules.[Module].UnitTests.Domain;

using Shouldly;
using Xunit;

[UnitTest("Domain")]
public class [Entity]Tests
{
    [Fact]
    public void Create_ValidInput_SuccessResult()
    {
        // Arrange
        var input1 = "value1";
        var input2 = "value2";

        // Act
        var result = [Entity].Create(input1, input2);

        // Assert
        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public void Create_InvalidInput_FailureResult()
    {
        // Arrange
        var invalid = "";

        // Act
        var result = [Entity].Create(invalid, invalid);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void ChangeProperty_ValidInput_UpdatesState()
    {
        // Arrange
        var entity = [Entity].Create("value1", "value2").Value;

        // Act
        var result = entity.ChangeProperty("new-value");

        // Assert
        result.ShouldBeSuccess();
        entity.Property.ShouldBe("new-value");
    }
}
