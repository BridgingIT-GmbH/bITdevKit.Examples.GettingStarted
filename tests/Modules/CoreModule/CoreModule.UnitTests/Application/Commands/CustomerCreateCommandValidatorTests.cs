// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using FluentValidation.TestHelper;

/// <summary>
/// Tests for <see cref="CustomerCreateCommand.Validator"/> validating command input validation
/// including required fields, email format, and null checks.
/// </summary>
[UnitTest("Application")]
public class CustomerCreateCommandValidatorTests
{
    private readonly CustomerCreateCommand.Validator validator = new();

    /// <summary>Verifies validator accepts valid customer creation command.</summary>
    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>Verifies validator throws exception for null model.</summary>
    [Fact]
    public void Validate_NullModel_ThrowsException()
    {
        // Arrange
        var command = new CustomerCreateCommand(null);

        // Act & Assert
        Should.Throw<NullReferenceException>(() => this.validator.TestValidate(command));
    }

    /// <summary>Verifies validation error for empty first name.</summary>
    [Fact]
    public void Validate_EmptyFirstName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.FirstName);
    }

    /// <summary>Verifies validation error for null first name.</summary>
    [Fact]
    public void Validate_NullFirstName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = null,
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.FirstName);
    }

    /// <summary>Verifies validation error for empty last name.</summary>
    [Fact]
    public void Validate_EmptyLastName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = "",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.LastName);
    }

    /// <summary>Verifies validation error for null last name.</summary>
    [Fact]
    public void Validate_NullLastName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = null,
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.LastName);
    }

    /// <summary>Verifies validation error for empty email.</summary>
    [Fact]
    public void Validate_EmptyEmail_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = "Doe",
                Email = ""
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.Email);
    }

    /// <summary>Verifies validation error for null email.</summary>
    [Fact]
    public void Validate_NullEmail_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = "Doe",
                Email = null
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.Email);
    }

    /// <summary>Verifies validator accepts valid email format.</summary>
    [Fact]
    public void Validate_ValidEmailWithAtSymbol_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CustomerCreateCommand(
            new CustomerModel
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
