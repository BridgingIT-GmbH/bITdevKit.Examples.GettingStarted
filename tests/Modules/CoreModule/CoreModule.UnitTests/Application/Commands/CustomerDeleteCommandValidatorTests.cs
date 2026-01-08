// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using FluentValidation.TestHelper;

/// <summary>
/// Tests for <see cref="CustomerDeleteCommand.Validator"/> validating delete command input validation
/// including GUID format checks and null handling.
/// </summary>
[UnitTest("Application")]
public class CustomerDeleteCommandValidatorTests
{
    private readonly CustomerDeleteCommand.Validator validator = new();

    /// <summary>Verifies validator accepts valid GUID format.</summary>
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

    /// <summary>Verifies validation error for invalid GUID format.</summary>
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

    /// <summary>Verifies validation error for empty customer ID.</summary>
    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
            var command = new CustomerDeleteCommand("");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    /// <summary>Verifies validation error for null ID.</summary>
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

    /// <summary>Verifies validation error for empty string ID.</summary>
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
