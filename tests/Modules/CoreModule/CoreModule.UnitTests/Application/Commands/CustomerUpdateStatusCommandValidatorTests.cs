// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

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
        // Arrange - Status 2 = Active
        var command = new CustomerUpdateStatusCommand(
            Guid.NewGuid().ToString(),
            2);

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
        var command = new CustomerUpdateStatusCommand(null, 2);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    /// <summary>Verifies validation error for empty customer ID.</summary>
    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand("", 2);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    /// <summary>Verifies validation error for empty GUID customer ID.</summary>
    [Fact]
    public void Validate_EmptyGuidCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.Empty.ToString(), 2);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CustomerId);
    }

    /// <summary>Verifies validation error for zero status value.</summary>
    [Fact]
    public void Validate_ZeroStatus_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), 0);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validation error for negative status value.</summary>
    [Fact]
    public void Validate_NegativeStatus_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), -1);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validation error for non-existent status ID.</summary>
    [Fact]
    public void Validate_InvalidStatusId_ShouldHaveValidationError()
    {
        // Arrange - Status 999 doesn't exist
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), 999);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    /// <summary>Verifies validator accepts Lead status.</summary>
    [Fact]
    public void Validate_ValidStatusLead_ShouldNotHaveValidationError()
    {
        // Arrange - Status 1 = Lead
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), 1);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Verifies validator accepts Retired status.</summary>
    [Fact]
    public void Validate_ValidStatusRetired_ShouldNotHaveValidationError()
    {
        // Arrange - Status 3 = Retired
        var command = new CustomerUpdateStatusCommand(Guid.NewGuid().ToString(), 3);

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

