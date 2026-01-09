// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using FluentValidation.TestHelper;

/// <summary>
/// Tests for <see cref="CustomerUpdateStatusCommand.Validator"/> validating status update command input validation
/// including customer ID and status value validation.
/// </summary>
[UnitTest("Application")]
public class CustomerUpdateStatusCommandValidatorTests
{
    private readonly CustomerUpdateStatusCommand.Validator validator = new();

    /// <summary>Verifies validator accepts valid status update command.</summary>
    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), "Active");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Verifies validation error for null customer ID.</summary>
    [Fact]
    public void Validate_NullCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(null, "Active");

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
        var command = new CustomerUpdateStatusCommand("", "Active");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    /// <summary>Verifies validation error for empty GUID customer ID.</summary>
    [Fact]
    public void Validate_EmptyGuidCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.Empty.ToString(), "Active");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    /// <summary>Verifies validation error for null status value.</summary>
    [Fact]
    public void Validate_NullStatus_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), null);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validation error for empty status value.</summary>
    [Fact]
    public void Validate_EmptyStatus_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), "");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validation error for non-existent status value.</summary>
    [Fact]
    public void Validate_InvalidStatusValue_ShouldHaveValidationError()
    {
        // Arrange - Status "Invalid" doesn't exist
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), "Invalid");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validator accepts Lead status.</summary>
    [Fact]
    public void Validate_ValidStatusLead_ShouldNotHaveValidationError()
    {
        // Arrange - Status "Lead"
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), "Lead");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Verifies validator accepts Retired status.</summary>
    [Fact]
    public void Validate_ValidStatusRetired_ShouldNotHaveValidationError()
    {
        // Arrange - Status "Retired"
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), "Retired");

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
