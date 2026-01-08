// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Domain.Model;

[UnitTest("Domain")]
public class EmailAddressTests
{
    /// <summary>
    /// Verifies successful creation for valid email strings including trimming and lowercase normalization.
    /// </summary>
    [Theory]
    [InlineData("john.doe@example.com", "john.doe@example.com")]
    [InlineData("JOHN.DOE@EXAMPLE.COM", "john.doe@example.com")]
    [InlineData("  john.doe@example.com  ", "john.doe@example.com")]
    public void Create_WithValidString_ReturnsSuccessResultAndNormalized(string raw, string expected)
    {
        // Act
        var result = EmailAddress.Create(raw);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(expected);
    }

    /// <summary>
    /// Ensures invalid email strings produce failure results.
    /// </summary>
    [Theory]
    [InlineData("")] // empty
    [InlineData("   ")] // whitespace
    [InlineData("john.doeexample.com")] // missing @
    [InlineData("john.doe@")]
    [InlineData("@example.com")]
    [InlineData("john.doe@example")] // missing TLD
    public void Create_WithInvalidString_ReturnsFailureResult(string raw)
    {
        // Act
        var result = EmailAddress.Create(raw);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Validates implicit conversion to string returns original normalized value.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var email = EmailAddress.Create("John.Doe@Example.Com").Value;

        // Act
        string value = email; // implicit conversion

        // Assert
        value.ShouldBe("john.doe@example.com");
    }

    /// <summary>
    /// Verifies implicit conversion from valid string creates an instance.
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromString_ValidValue_ReturnsInstance()
    {
        // Arrange
        const string raw = "John.Doe@Example.Com";

        // Act
        EmailAddress email = raw; // implicit conversion

        // Assert
        email.Value.ShouldBe("john.doe@example.com");
    }

    /// <summary>
    /// Ensures implicit conversion from invalid string throws a ResultException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("john.doeexample.com")]
    [InlineData("john.doe@")]
    public void ImplicitConversion_FromString_InvalidValue_ThrowsResultException(string raw)
    {
        // Act / Assert
        Should.Throw<ResultException>(() => { EmailAddress _ = raw; });
    }

    /// <summary>
    /// Confirms equality of two EmailAddress instances with same normalized value.
    /// </summary>
    [Fact]
    public void Equality_TwoSameValues_AreEqual()
    {
        // Arrange
        var first = EmailAddress.Create("John.Doe@Example.Com").Value;
        var second = EmailAddress.Create("john.doe@example.com").Value;

        // Act / Assert
        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }

    /// <summary>
    /// Verifies inequality of two EmailAddress instances with different values.
    /// </summary>
    [Fact]
    public void Equality_TwoDifferentValues_AreNotEqual()
    {
        // Arrange
        var first = EmailAddress.Create("john.doe@example.com").Value;
        var second = EmailAddress.Create("jane.doe@example.com").Value;

        // Act / Assert
        first.ShouldNotBe(second);
        (first != second).ShouldBeTrue();
    }

    /// <summary>
    /// Tests GetHashCode consistency for equal objects.
    /// </summary>
    [Fact]
    public void GetHashCode_TwoEqualValues_HaveSameHashCode()
    {
        // Arrange
        var first = EmailAddress.Create("john.doe@example.com").Value;
        var second = EmailAddress.Create("john.doe@example.com").Value;

        // Act / Assert
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    /// <summary>
    /// Verifies Value property returns the normalized value.
    /// </summary>
    [Fact]
    public void Value_ReturnsNormalizedValue()
    {
        // Arrange
        var email = EmailAddress.Create("John.Doe@Example.Com").Value;

        // Act
        var result = email.Value;

        // Assert
        result.ShouldBe("john.doe@example.com");
    }

    /// <summary>
    /// Tests null value handling.
    /// </summary>
    [Fact]
    public void Create_WithNull_ReturnsFailureResult()
    {
        // Act
        var result = EmailAddress.Create(null);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies additional valid email formats.
    /// </summary>
    [Theory]
    [InlineData("user+tag@example.com")]
    [InlineData("user_name@example.com")]
    [InlineData("user.name@sub.example.com")]
    [InlineData("123@example.com")]
    public void Create_WithVariousValidFormats_ReturnsSuccessResult(string email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies additional invalid email formats.
    /// </summary>
    [Theory]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("user domain@example.com")]
    [InlineData("user@@example.com")]
    public void Create_WithVariousInvalidFormats_ReturnsFailureResult(string email)
    {
        // Act
        var result = EmailAddress.Create(email);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
