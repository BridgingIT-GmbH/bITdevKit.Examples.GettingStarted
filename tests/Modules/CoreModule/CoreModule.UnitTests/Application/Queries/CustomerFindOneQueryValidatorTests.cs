// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

using FluentValidation.TestHelper;

/// <summary>
/// Tests for <see cref="CustomerFindOneQuery.Validator"/> validating query input validation
/// including customer ID format and null checks.
/// </summary>
[UnitTest("Application")]
public class CustomerFindOneQueryValidatorTests
{
    private readonly CustomerFindOneQuery.Validator validator = new();

    /// <summary>Verifies validator accepts valid customer ID.</summary>
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

    /// <summary>Verifies validation error for null customer ID.</summary>
    [Fact]
    public void Validate_NullCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery(null);

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    /// <summary>Verifies validation error for empty customer ID.</summary>
    [Fact]
    public void Validate_EmptyCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery("");

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    /// <summary>Verifies validation error for whitespace-only customer ID.</summary>
    [Fact]
    public void Validate_WhitespaceCustomerId_ShouldHaveValidationError()
    {
        // Arrange
        var query = new CustomerFindOneQuery("   ");

        // Act
        var result = this.validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Id);
    }
}
