// TEMPLATE: Domain value object unit tests
// Replace placeholders in brackets with your value object name.

namespace [RootNamespace].Modules.[Module].UnitTests.Domain;

using Shouldly;
using Xunit;

[UnitTest("Domain")]
public class [ValueObject]Tests
{
    [Fact]
    public void Create_ValidValue_SuccessResult()
    {
        // Arrange
        var input = "valid";

        // Act
        var result = [ValueObject].Create(input);

        // Assert
        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public void Create_InvalidValue_FailureResult()
    {
        // Arrange
        var input = "";

        // Act
        var result = [ValueObject].Create(input);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Equality_SameValue_IsEqual()
    {
        // Arrange
        var left = [ValueObject].Create("value").Value;
        var right = [ValueObject].Create("value").Value;

        // Assert
        left.ShouldBe(right);
    }
}
