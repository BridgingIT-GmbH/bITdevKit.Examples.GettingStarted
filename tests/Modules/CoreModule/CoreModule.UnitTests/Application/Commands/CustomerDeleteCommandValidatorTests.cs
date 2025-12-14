// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

using FluentValidation.TestHelper;

[UnitTest("Application")]
public class CustomerDeleteCommandValidatorTests
{
    private readonly CustomerDeleteCommand.Validator validator = new();

    [Fact]
    public void Validate_ValidGuid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CustomerDeleteCommand(Guid.NewGuid().ToString());

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidGuid_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerDeleteCommand("invalid-guid");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Validate_EmptyGuid_ShouldNotHaveValidationError()
    {
        // Arrange - Empty GUID is still a valid GUID format
        var command = new CustomerDeleteCommand(Guid.Empty.ToString());

        // Act
        var result = this.validator.TestValidate(command);

        // Assert - The validator only checks if it's a valid GUID, not if it's non-empty
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerDeleteCommand(null);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Validate_EmptyStringId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerDeleteCommand("");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }
}
