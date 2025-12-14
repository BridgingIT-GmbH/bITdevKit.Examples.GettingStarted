// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

using FluentValidation.TestHelper;

[UnitTest("Application")]
public class CustomerUpdateStatusCommandValidatorTests
{
    private readonly CustomerUpdateStatusCommand.Validator validator = new();

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
