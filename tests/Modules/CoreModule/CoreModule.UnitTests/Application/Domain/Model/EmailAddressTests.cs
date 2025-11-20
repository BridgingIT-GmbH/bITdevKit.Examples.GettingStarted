// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Domain.Model;

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
}
