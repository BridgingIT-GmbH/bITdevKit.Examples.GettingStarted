// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Queries;

using FluentValidation.TestHelper;

[UnitTest("Application")]
public class CustomerFindOneQueryValidatorTests
{
    private readonly CustomerFindOneQuery.Validator validator = new();

    [Fact]
    public void Validate_ValidCustomerId_ShouldNotHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery(Guid.NewGuid().ToString());

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery(null);

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.CustomerId);
    }

    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery("");

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.CustomerId);
    }

    [Fact]
    public void Validate_WhitespaceCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery("   ");

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.CustomerId);
    }
}
