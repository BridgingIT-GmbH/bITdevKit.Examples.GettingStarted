// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application.Domain.Model;

using Microsoft.Extensions.Time.Testing;

[UnitTest("Domain")]
public class CustomerNumberTests
{
    private readonly FakeTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    public CustomerNumberTests()
    {
        TimeProviderAccessor.Current = this.timeProvider; // deterministic
    }

    /// <summary>
    /// Verifies successful creation for valid raw customer number strings (case normalized).
    /// </summary>
    [Theory]
    [InlineData("CUS-2026-100000")]
    [InlineData("cus-2026-100000")] // case normalization
    public void Create_WithValidString_ReturnsSuccessResult(string value)
    {
        // Act
        var result = CustomerNumber.Create(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(value.ToUpperInvariant());
    }

    /// <summary>
    /// Ensures invalid raw strings produce failure results (empty, whitespace, bad format variations).
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")] // whitespace
    [InlineData("CUS-20-1")]
    [InlineData("CUS-20261-100000")] // year length wrong
    [InlineData("CUS-2026-10000")] // sequence length wrong
    [InlineData("ABC-2026-100000")] // wrong prefix
    public void Create_WithInvalidString_ReturnsFailureResult(string value)
    {
        // Act
        var result = CustomerNumber.Create(value);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Confirms creating by year + sequence succeeds within valid ranges.
    /// </summary>
    [Fact]
    public void Create_WithValidYearAndSequence_ReturnsSuccessResult()
    {
        // Arrange
        var year = this.timeProvider.GetUtcNow().Year;
        const long sequence = 100000L;

        // Act
        var result = CustomerNumber.Create(year, sequence);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe($"CUS-{year:D4}-{sequence:D6}");
    }

    /// <summary>
    /// Verifies year outside allowed range produces failure results.
    /// </summary>
    [Theory]
    [InlineData(1999, 100000L)] // below range
    [InlineData(3000, 100000L)] // beyond currentPlusOne (provider Year=2026 -> plusOne=2027)
    public void Create_WithInvalidYear_ReturnsFailureResult(int year, long sequence)
    {
        // Act
        var result = CustomerNumber.Create(year, sequence);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures invalid sequence boundaries produce failure results.
    /// </summary>
    [Theory]
    [InlineData(2026, 99999L)] // below sequence range
    [InlineData(2026, 1000000L)] // above sequence range
    public void Create_WithInvalidSequence_ReturnsFailureResult(int year, long sequence)
    {
        // Act
        var result = CustomerNumber.Create(year, sequence);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Validates implicit conversion from string succeeds for a valid format.
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromString_ValidValue_ReturnsInstance()
    {
        // Arrange
        const string value = "CUS-2026-100000";

        // Act
        CustomerNumber number = value; // implicit conversion

        // Assert
        number.Value.ShouldBe(value);
    }

    /// <summary>
    /// Verifies implicit conversion throws for invalid string formats.
    /// </summary>
    [Theory]
    [InlineData("INVALID")]
    [InlineData("CUS-2026-10000")] // bad sequence length
    [InlineData("CUS-20261-100000")] // bad year length
    public void ImplicitConversion_FromString_InvalidValue_ThrowsResultException(string value)
    {
        // Act / Assert
        Should.Throw<ResultException>(() => { CustomerNumber _ = value; });
    }

    /// <summary>
    /// Confirms value object equality compares underlying value correctly.
    /// </summary>
    [Fact]
    public void Equality_TwoSameValues_AreEqual()
    {
        // Arrange
        var first = CustomerNumber.Create("CUS-2026-100000").Value;
        var second = CustomerNumber.Create("CUS-2026-100000").Value;

        // Act / Assert
        first.ShouldBe(second);
        (first == second).ShouldBeTrue();
    }
}
