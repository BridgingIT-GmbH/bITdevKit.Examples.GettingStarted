// TEMPLATE: FluentValidation validator unit tests
// Replace placeholders in brackets with your module and entity names.

namespace [RootNamespace].Modules.[Module].UnitTests.Application;

using FluentValidation.TestHelper;
using Shouldly;
using Xunit;

[UnitTest("Application")]
public class [Entity]CreateCommandValidatorTests
{
    private readonly [Entity]CreateCommand.Validator validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new [Entity]CreateCommand(
            new [Entity]Model
            {
                // TODO: initialize valid fields
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MissingRequiredField_ShouldHaveValidationError()
    {
        // Arrange
        var command = new [Entity]CreateCommand(
            new [Entity]Model
            {
                // TODO: missing required field
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.[RequiredProperty]);
    }
}
