namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Presentation;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;
using Mapster;
using Shouldly;
using System;
using Xunit;

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
        var customer = Customer.Create("John", "Doe", "john.doe@example.com")
            .ChangeStatus(CustomerStatus.Active);
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
        customer.Status.ShouldBe(CustomerStatus.Active);
        customer.ConcurrencyVersion.ShouldBe(Guid.Parse(concurrencyVersion));
    }

    [Fact]
    public void EmailAddressToString_MapsCorrectly()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com");

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
            Id = Guid.NewGuid().ToString(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Status = CustomerStatus.Active.Id,
            ConcurrencyVersion = null
        };

        // Act
        var customer = model.Adapt<Customer>(this.config);

        // Assert
        customer.ConcurrencyVersion.ShouldBe(Guid.Empty);
    }
}