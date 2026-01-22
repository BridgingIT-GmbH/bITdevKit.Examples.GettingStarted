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
        var email = EmailAddress.Create("john.doe@example.com").Value;

        // Act
        var result = Customer.Create("John", "Doe", email, number);

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
    /// Confirms changing name updates both first and last names for valid input.
    /// </summary>
    [Theory]
    [InlineData("Jane", "Smith")]
    [InlineData("Alice", "Jones")]
    public void ChangeName_WithValidNewNames_UpdatesNames(string newFirst, string newLast)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var newEmailAddress = EmailAddress.Create(newEmail).Value;

        // Act
        var result = customer.ChangeEmail(newEmailAddress);

        // Assert
        result.ShouldBeSuccess();
        customer.Email.Value.ShouldBe(newEmail);
    }

    /// <summary>
    /// Confirms birth date change succeeds for a valid past date.
    /// </summary>
    [Fact]
    public void ChangeBirthDate_WithValidPastDate_UpdatesDate()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

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
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.ChangeName("John", "Doe");

        // Assert
        result.ShouldBeSuccess();
        customer.FirstName.ShouldBe("John");
        customer.LastName.ShouldBe("Doe");
    }

    /// <summary>
    /// Ensures adding address with missing required fields fails.
    /// </summary>
    [Theory]
    [InlineData("", "New York", "USA")]
    [InlineData("123 Main St", "", "USA")]
    [InlineData("123 Main St", "New York", "")]
    [InlineData(null, "New York", "USA")]
    public void AddAddress_WithMissingRequiredFields_ReturnsFailure(string line1, string city, string country)
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Act
        var result = customer.AddAddress("Home", line1, null, "12345", city, country);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies removing an address successfully removes it from collection.
    /// </summary>
    [Fact]
    public void RemoveAddress_WithValidId_RemovesAddress()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        customer.AddAddress("Home", "123 Main St", null, "12345", "New York", "USA");
        var addressId = customer.Addresses.First().Id;

        // Act
        var result = customer.RemoveAddress(addressId);

        // Assert
        result.ShouldBeSuccess();
        customer.Addresses.Count.ShouldBe(0);
    }

    /// <summary>
    /// Verifies removing a non-existent address fails.
    /// </summary>
    [Fact]
    public void RemoveAddress_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        customer.AddAddress("Home", "123 Main St", null, "12345", "New York", "USA");
        var invalidAddressId = AddressId.Create(Guid.NewGuid());

        // Act
        var result = customer.RemoveAddress(invalidAddressId);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies changing an address updates all properties correctly.
    /// </summary>
    [Fact]
    public void ChangeAddress_WithValidData_UpdatesAllProperties()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        customer.AddAddress("Home", "123 Main St", "Apt 4B", "12345", "New York", "USA");
        var addressId = customer.Addresses.First().Id;

        // Act
        var result = customer.ChangeAddress(addressId, "Work", "999 Corporate Blvd", "Suite 100", "99999", "Seattle", "USA");

        // Assert
        result.ShouldBeSuccess();
        var address = customer.Addresses.First();
        address.Name.ShouldBe("Work");
        address.Line1.ShouldBe("999 Corporate Blvd");
        address.Line2.ShouldBe("Suite 100");
        address.PostalCode.ShouldBe("99999");
        address.City.ShouldBe("Seattle");
        address.Country.ShouldBe("USA");
    }

    /// <summary>
    /// Ensures changing address with invalid required fields fails.
    /// </summary>
    [Fact]
    public void ChangeAddress_WithInvalidRequiredFields_ReturnsFailure()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        customer.AddAddress("Home", "123 Main St", null, "12345", "New York", "USA");
        var addressId = customer.Addresses.First().Id;

        // Act
        var result = customer.ChangeAddress(addressId, "Work", "", null, "99999", "Seattle", "USA");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies changing a non-existent address fails.
    /// </summary>
    [Fact]
    public void ChangeAddress_WithInvalidId_ReturnsFailure()
    {
        // Arrange
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;
        customer.AddAddress("Home", "123 Main St", null, "12345", "New York", "USA");
        var invalidAddressId = AddressId.Create(Guid.NewGuid());

        // Act
        var result = customer.ChangeAddress(invalidAddressId, "Work", "999 Corporate Blvd", null, "99999", "Seattle", "USA");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies customer with no addresses has empty collection.
    /// </summary>
    [Fact]
    public void Customer_WithNoAddresses_HasEmptyCollection()
    {
        // Arrange & Act
        var customer = Customer.Create("John", "Doe", EmailAddress.Create("john.doe@example.com").Value, CustomerNumber.Create("CUS-2026-100000").Value).Value;

        // Assert
        customer.Addresses.ShouldBeEmpty();
    }
}
