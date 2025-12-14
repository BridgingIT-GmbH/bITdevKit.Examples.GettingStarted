// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Commands;

using FluentValidation.TestHelper;

[UnitTest("Application")]
public class CustomerUpdateCommandValidatorTests
{
    private readonly CustomerUpdateCommand.Validator validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullModel_ThrowsException()
    {
        // Arrange
        var command = new CustomerUpdateCommand(null);

        // Act & Assert
        Should.Throw<NullReferenceException>(() => this.validator.TestValidate(command));
    }

    [Fact]
    public void Validate_EmptyGuidId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.Empty.ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.Id);
    }

    [Fact]
    public void Validate_NullId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = null,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.Id);
    }

    [Fact]
    public void Validate_EmptyFirstName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "",
                LastName = "Doe",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.FirstName);
    }

    [Fact]
    public void Validate_EmptyLastName_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "John",
                LastName = "",
                Email = "john.doe@example.com"
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.LastName);
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = ""
            });

        // Act
        var result = this.validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Model.Email);
    }
}
