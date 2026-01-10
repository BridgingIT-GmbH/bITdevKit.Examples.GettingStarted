// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Domain.Model;

[UnitTest("Domain")]
public class CustomerTests
{
    /// <summary>
    /// Verifies successful creation initializes expected properties and default status.
    /// </summary>
    [Fact]
    public void Create_WithValidData_ReturnsSuccessResultAndInitialState()
    {
        // Arrange
        var number = CustomerNumber.Create("CUS-2026-100000").Value;

        // Act
        var result = Customer.Create("John", "Doe", "john.doe@example.com", number);

        // Assert
        result.ShouldBeSuccess();
        var customer = result.Value;
        customer.FirstName.ShouldBe("John");
        customer.LastName.ShouldBe("Doe");
        customer.Email.Value.ShouldBe("john.doe@example.com");
        customer.Number.Value.ShouldBe("CUS-2026-100000");
        customer.Status.ShouldBe(CustomerStatus.Lead);
    }

    /// <summary>
    /// Ensures invalid email inputs cause creation failure.
    /// </summary>
    [Theory]
    [InlineData("invalid-email")]
    [InlineData("")]
    [InlineData("   ")] // whitespace
    public void Create_WithInvalidEmail_ReturnsFailureResult(string email)
    {
        // Arrange
        var number = CustomerNumber.Create("CUS-2026-100000").Value;

        // Act
        var result = Customer.Create("John", "Doe", email, number);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Confirms changing name updates both first and last names for valid input.
    /// </summary>
    [Theory]
    [InlineData("Jane", "Smith")]
    [InlineData("Alice", "Jones")]
    public void ChangeName_WithValidNewNames_UpdatesNames(string newFirst, string newLast)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeName(newFirst, newLast);

        // Assert
        result.ShouldBeSuccess();
        customer.FirstName.ShouldBe(newFirst);
        customer.LastName.ShouldBe(newLast);
    }

    /// <summary>
    /// Ensures name change fails when both values are missing or empty.
    /// </summary>
    [Theory]
    [InlineData("", "")]
    [InlineData("", null)]
    [InlineData(null, "")]
    [InlineData(null, null)]
    public void ChangeName_WithNoNamesProvided_ReturnsFailureResult(string first, string last)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeName(first, last);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies email change updates email for valid formats.
    /// </summary>
    [Theory]
    [InlineData("jane.smith@example.com")]
    [InlineData("alice.jones@example.org")]
    public void ChangeEmail_WithValidEmail_UpdatesEmail(string newEmail)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeEmail(newEmail);

        // Assert
        result.ShouldBeSuccess();
        customer.Email.Value.ShouldBe(newEmail);
    }

    /// <summary>
    /// Ensures invalid email inputs cause email change failure.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("   ")]
    public void ChangeEmail_WithInvalidEmail_ReturnsFailureResult(string newEmail)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeEmail(newEmail);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Confirms birth date change succeeds for a valid past date.
    /// </summary>
    [Fact]
    public void ChangeBirthDate_WithValidPastDate_UpdatesDate()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20));

        // Act
        var result = customer.ChangeBirthDate(date);

        // Assert
        result.ShouldBeSuccess();
        customer.DateOfBirth.ShouldBe(date);
    }

    /// <summary>
    /// Ensures birth date change fails when future date provided.
    /// </summary>
    [Fact]
    public void ChangeBirthDate_WithFutureDate_ReturnsFailureResult()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        // Act
        var result = customer.ChangeBirthDate(futureDate);

        // Assert
        result.ShouldBeFailure();
    }

    /// <summary>
    /// Verifies status change updates to new valid status values.
    /// </summary>
    [Theory]
    [InlineData(nameof(CustomerStatus.Active))]
    [InlineData(nameof(CustomerStatus.Retired))]
    public void ChangeStatus_WithValidStatus_UpdatesStatus(string statusName)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var status = statusName switch
        {
            nameof(CustomerStatus.Active) => CustomerStatus.Active,
            nameof(CustomerStatus.Retired) => CustomerStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(statusName)
        };

        // Act
        var result = customer.ChangeStatus(status);

        // Assert
        result.ShouldBeSuccess();
        customer.Status.ShouldBe(status);
    }

    /// <summary>
    /// Ensures status change fails when null is provided.
    /// </summary>
    [Fact]
    public void ChangeStatus_WithNullStatus_DoesNotChange()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeStatus(null);

        // Assert
        result.ShouldBeSuccess();
        customer.Status.ShouldBe(CustomerStatus.Lead); // remains unchanged (default)
    }

    /// <summary>
    /// Confirms no-op name change still returns success but retains original values.
    /// </summary>
    [Fact]
    public void ChangeName_WithSameValues_DoesNotChange()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeName("John", "Doe");

        // Assert
        result.ShouldBeSuccess();
        customer.FirstName.ShouldBe("John");
        customer.LastName.ShouldBe("Doe");
    }

    /// <summary>
    /// Ensures no-op email change returns success and retains original email.
    /// </summary>
    [Fact]
    public void ChangeEmail_WithSameValue_DoesNotChange()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeEmail("john.doe@example.com");

        // Assert
        result.ShouldBeSuccess();
        customer.Email.Value.ShouldBe("john.doe@example.com");
    }
}
