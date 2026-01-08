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

        var customer = Customer.Create("John", "Doe", "john.update@example.com", CustomerNumber.Create("CN-100002").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
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
    }

    /// <summary>Verifies validation error for empty first name.</summary>
    [Fact]
    public async Task Process_EmptyFirstName_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.empty@example.com", CustomerNumber.Create("CN-100013").Value).Value;
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

        var customer = Customer.Create("John", "Doe", "john.notallowed@example.com", CustomerNumber.Create("CN-100003").Value).Value;
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

        var customer = Customer.Create("John", "Doe", "john.emptylast@example.com", CustomerNumber.Create("CN-100014").Value).Value;
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

    /// <summary>Verifies that update with non-existent ID creates new customer entity.</summary>
    [Fact]
    public async Task Process_NonExistentCustomer_CreatesNewCustomer()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var newId = Guid.NewGuid();
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = newId.ToString(),
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.new@example.com",
                Number = "CN-100099",
                ConcurrencyVersion = Guid.NewGuid().ToString()
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.FirstName.ShouldBe("Jane");
        response.Value.LastName.ShouldBe("Doe");
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

    /// <summary>Verifies that update with empty GUID creates customer entity.</summary>
    [Fact]
    public async Task Process_EmptyGuidCustomerId_CreatesWithEmptyId()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = Guid.Empty.ToString(),
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane.empty@example.com",
                Number = "CN-100098"
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeSuccess();
        response.Value.FirstName.ShouldBe("Jane");
    }

    /// <summary>Verifies failure when concurrency version token mismatches.</summary>
    [Fact]
    public async Task Process_ConcurrencyConflict_FailureResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.concurrency@example.com", CustomerNumber.Create("CN-100015").Value).Value;
        var inserted = await repository.InsertAsync(customer, CancellationToken.None);

        var command = new CustomerUpdateCommand(
            new CustomerModel
            {
                Id = inserted.Id.Value.ToString(),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                ConcurrencyVersion = Guid.NewGuid().ToString() // Wrong version
            });

        // Act
        var response = await requester.SendAsync(command, null, CancellationToken.None);

        // Assert
        response.ShouldBeFailure();
        response.Errors.ShouldNotBeEmpty();
    }

    /// <summary>Verifies successful email address update.</summary>
    [Fact]
    public async Task Process_UpdateEmailAddress_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.oldemail@example.com", CustomerNumber.Create("CN-100016").Value).Value;
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

    /// <summary>Verifies successful update of multiple customer fields simultaneously.</summary>
    [Fact]
    public async Task Process_UpdateMultipleFields_SuccessResult()
    {
        // Arrange
        var requester = this.ServiceProvider.GetService<IRequester>();
        var repository = this.ServiceProvider.GetService<IGenericRepository<Customer>>();

        var customer = Customer.Create("John", "Doe", "john.multi@example.com", CustomerNumber.Create("CN-100017").Value).Value;
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
