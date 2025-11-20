// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Presentation;

using Microsoft.Extensions.Time.Testing;

[UnitTest("Presentation")]
public class CoreModuleMapperRegisterTests
{
    private readonly TypeAdapterConfig config;

    public CoreModuleMapperRegisterTests()
    {
        this.config = new TypeAdapterConfig();
        new CoreModuleMapperRegister().Register(this.config);
    }

    [Fact]
    public void CustomerToCustomerModel_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customerNumber = CustomerNumber.Create("CUS-2026-100000").Value;
        var customer = Customer.Create("John", "Doe", "john.doe@example.com", customerNumber).Value;
        customer.ChangeStatus(CustomerStatus.Active);
        customer.Id = CustomerId.Create(customerId);
        customer.ConcurrencyVersion = Guid.NewGuid();

        // Act
        var model = customer.Adapt<CustomerModel>(this.config);

        // Assert
        model.ShouldNotBeNull();
        model.Id.ShouldBe(customerId.ToString());
        model.FirstName.ShouldBe("John");
        model.LastName.ShouldBe("Doe");
        model.Email.ShouldBe("john.doe@example.com");
        model.Number.ShouldBe(customerNumber);
        model.Status.ShouldBe(CustomerStatus.Active.Id);
        model.ConcurrencyVersion.ShouldBe(customer.ConcurrencyVersion.ToString());
    }

    [Fact]
    public void CustomerModelToCustomer_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var customerId = Guid.NewGuid().ToString();
        var concurrencyVersion = Guid.NewGuid().ToString();
        var model = new CustomerModel
        {
            Id = customerId,
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Number = "CUS-2026-100000",
            Status = CustomerStatus.Active.Id,
            ConcurrencyVersion = concurrencyVersion
        };

        // Act
        var customer = model.Adapt<Customer>(this.config);

        // Assert
        customer.ShouldNotBeNull();
        customer.Id.Value.ShouldBe(Guid.Parse(customerId));
        customer.FirstName.ShouldBe("Jane");
        customer.LastName.ShouldBe("Smith");
        customer.Email.Value.ShouldBe("jane.smith@example.com");
        customer.Number.Value.ShouldBe("CUS-2026-100000");
        customer.Status.ShouldBe(CustomerStatus.Active);
        customer.ConcurrencyVersion.ShouldBe(Guid.Parse(concurrencyVersion));
    }

    [Fact]
    public void EmailAddressToString_MapsCorrectly()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com").Value;

        // Act
        var result = email.Adapt<string>(this.config);

        // Assert
        result.ShouldBe("test@example.com");
    }

    [Fact]
    public void StringToEmailAddress_MapsCorrectly()
    {
        // Arrange
        const string emailString = "test@example.com";

        // Act
        var result = emailString.Adapt<EmailAddress>(this.config);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("test@example.com");
    }

    [Fact]
    public void CustomerNumberToString_MapsCorrectly()
    {
        // Arrange
        var email = CustomerNumber.Create("CUS-2026-100000").Value;

        // Act
        var result = email.Adapt<string>(this.config);

        // Assert
        result.ShouldBe("CUS-2026-100000");
    }

    [Fact]
    public void StringToCustomerNumber_MapsCorrectly()
    {
        // Arrange
        const string customerNumber = "CUS-2026-100000";

        // Act
        var result = customerNumber.Adapt<CustomerNumber>(this.config);

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe("CUS-2026-100000");
    }

    [Fact]
    public void CustomerStatusToInt_MapsCorrectly()
    {
        // Arrange
        var status = CustomerStatus.Active;

        // Act
        var result = status.Adapt<int>(this.config);

        // Assert
        result.ShouldBe(CustomerStatus.Active.Id);
    }

    [Fact]
    public void IntToCustomerStatus_MapsCorrectly()
    {
        // Arrange
        var statusId = CustomerStatus.Active.Id;

        // Act
        var result = statusId.Adapt<CustomerStatus>(this.config);

        // Assert
        result.ShouldBe(CustomerStatus.Active);
    }

    [Fact]
    public void CustomerModelToCustomer_NullConcurrencyVersion_MapsToEmptyGuid()
    {
        // Arrange
        var model = new CustomerModel
        {
            ConcurrencyVersion = null
        };

        // Act
        var customer = model.Adapt<Customer>(this.config);

        // Assert
        customer.ConcurrencyVersion.ShouldBe(Guid.Empty);
    }
}