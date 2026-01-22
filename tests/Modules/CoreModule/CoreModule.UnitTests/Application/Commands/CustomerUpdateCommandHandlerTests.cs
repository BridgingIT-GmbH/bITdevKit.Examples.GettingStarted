// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.UnitTests.Application;

/// <summary>
/// Tests for <see cref="CustomerUpdateCommandHandler"/> validating customer update scenarios
/// including field updates, validation failures, concurrency conflicts, and business rules.
/// </summary>
[UnitTest("Application")]
public class CustomerUpdateCommandHandlerTests(ITestOutputHelper output) : CoreModuleTestsBase(output)
{
    /// <summary>Verifies successful update of customer with valid data and concurrency token.</summary>
    [Fact]
    public async Task Process_ValidRequest_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.update@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Number = "CUS-2026-999999", // to test that number remains unchanged
                // Note that DateOfBirth and Status are not updated in this test
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.ShouldNotBeNull();
        response.Value.FirstName.ShouldBe("Jane");
        response.Value.LastName.ShouldBe("Smith");
        response.Value.Email.ShouldBe("jane.smith@example.com");
        response.Value.Number.ShouldBe("CUS-2026-100000"); // unchanged
    }

    /// <summary>Verifies validation error for empty first name.</summary>
    [Fact]
    public async Task Process_EmptyFirstName_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.empty@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = customer.Id.Value.ToString(),
                FirstName = "",
                LastName = "Doe",
                Email = "test@example.com"
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
        response.Errors.ShouldNotBeEmpty();
    }

    /// <summary>Verifies business rule failure for disallowed last name.</summary>
    [Fact]
    public async Task Process_NotAllowedLastName_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.notallowed@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "John",
                LastName = "notallowed",
                Email = "john@example.com",
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
    }

    /// <summary>Verifies validation error for empty last name.</summary>
    [Fact]
    public async Task Process_EmptyLastName_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.emptylast@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "John",
                LastName = "",
                Email = "test@example.com",
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
        response.Errors.ShouldNotBeEmpty();
    }

    /// <summary>Verifies validation error for null model.</summary>
    [Fact]
    public async Task Process_NullModel_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new CustomerUpdateCommand(null);

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
    }

    /// <summary>Verifies successful email address update.</summary>
    [Fact]
    public async Task Process_UpdateEmailAddress_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.oldemail@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.newemail@example.com",
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.Email.ShouldBe("john.newemail@example.com");
    }

    /// <summary>Verifies successful email address update.</summary>
    [Fact]
    public async Task Process_UpdateInvalidEmailAddress_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.oldemail@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "John",
                LastName = "Doe",
                Email = "invalid-email",
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
    }

    /// <summary>Verifies successful update of multiple customer fields simultaneously.</summary>
    [Fact]
    public async Task Process_UpdateMultipleFields_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.multi@example.com", CustomerNumber.Create("CUS-2026-100000").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "Jonathan",
                LastName = "Johnson",
                Email = "jonathan.johnson@example.com",
                ConcurrencyVersion = inserted.ConcurrencyVersion.ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.FirstName.ShouldBe("Jonathan");
        response.Value.LastName.ShouldBe("Johnson");
        response.Value.Email.ShouldBe("jonathan.johnson@example.com");
    }
}
