# Testing Strategy Checklist

This checklist outlines optional but recommended testing practices for aggregate implementations.

**Testing Levels**: Unit Tests (Domain, Application), Integration Tests (Infrastructure, Endpoints)  
**Testing Framework**: xUnit + Shouldly (assertions) + NSubstitute (mocking)  
**Purpose**: Validate correctness, enforce business rules, ensure layer integration

**Note**: Testing is OPTIONAL but highly recommended. High test coverage increases confidence in code quality and maintainability.

---

## General Testing Principles

- [ ] Follow Arrange-Act-Assert (AAA) pattern in all tests
- [ ] Each test validates a single behavior or scenario
- [ ] Test names clearly describe the scenario: `Method_Scenario_ExpectedOutcome`
- [ ] Use `[Fact]` for single-case tests, `[Theory]` with `[InlineData]` for parameterized tests
- [ ] Tests are isolated (no shared state between tests)
- [ ] Tests are deterministic (same input always produces same output)
- [ ] Tests run quickly (unit tests < 100ms, integration tests < 5s)
- [ ] Tests clean up resources (database transactions, mocks, etc.)

**CORRECT**:
```csharp
[Fact]
public void Create_ValidInputs_ReturnsSuccessResult()
{
    // Arrange
    var firstName = "John";
    var lastName = "Doe";
    var email = "john.doe@example.com";
    
    // Act
    var result = Customer.Create(firstName, lastName, email, CustomerStatus.Active);
    
    // Assert
    result.ShouldBeSuccess();
    result.Value.FirstName.ShouldBe(firstName);
    result.Value.Email.Value.ShouldBe(email);
}
```

**WRONG**:
```csharp
[Fact]
public void TestCustomer() // Vague test name
{
    var customer = Customer.Create("John", "Doe", "john@test.com", CustomerStatus.Active);
    customer.Value.FirstName.ShouldBe("John");
    customer.Value.ChangeEmail("new@test.com"); // Testing multiple behaviors
    customer.Value.Email.Value.ShouldBe("new@test.com");
}
```

---

## Domain Layer Testing

### Aggregate Root Tests

- [ ] Test factory method with valid inputs returns Success Result
- [ ] Test factory method with invalid inputs returns Failure Result (validation)
- [ ] Test each change method (e.g., `ChangeEmail`, `ChangeName`) with valid data
- [ ] Test change methods reject invalid data (business rules enforced)
- [ ] Test domain events registered after aggregate creation or changes
- [ ] Test aggregate state transitions (e.g., Lead → Active → Retired)
- [ ] Test aggregate invariants maintained after all operations

**CORRECT**:
```csharp
[Theory]
[InlineData("", "Doe", "john@example.com")] // Empty first name
[InlineData("John", "", "john@example.com")] // Empty last name
[InlineData("John", "Doe", "")] // Empty email
[InlineData("John", "Doe", "invalid-email")] // Invalid email format
public void Create_InvalidInputs_ReturnsFailureResult(string firstName, string lastName, string email)
{
    // Act
    var result = Customer.Create(firstName, lastName, email, CustomerStatus.Active);
    
    // Assert
    result.ShouldBeFailure();
    result.Messages.ShouldNotBeEmpty();
}

[Fact]
public void ChangeEmail_ValidEmail_UpdatesEmailAndRegistersDomainEvent()
{
    // Arrange
    var customer = Customer.Create("John", "Doe", "old@example.com", CustomerStatus.Active).Value;
    var newEmail = "new@example.com";
    
    // Act
    var result = customer.ChangeEmail(newEmail);
    
    // Assert
    result.ShouldBeSuccess();
    customer.Email.Value.ShouldBe(newEmail);
    customer.DomainEvents.ShouldContain(e => e is CustomerUpdatedDomainEvent);
}
```

---

### Value Object Tests

- [ ] Test factory method with valid input returns Success Result
- [ ] Test factory method with invalid input returns Failure Result
- [ ] Test equality (two value objects with same value are equal)
- [ ] Test inequality (value objects with different values are not equal)
- [ ] Test implicit conversion to primitive (if applicable)
- [ ] Test ToString returns expected representation

**CORRECT**:
```csharp
[Fact]
public void Create_ValidEmail_ReturnsSuccessResult()
{
    // Arrange
    var email = "john.doe@example.com";
    
    // Act
    var result = EmailAddress.Create(email);
    
    // Assert
    result.ShouldBeSuccess();
    result.Value.Value.ShouldBe(email);
}

[Theory]
[InlineData("")] // Empty
[InlineData("   ")] // Whitespace
[InlineData("invalid-email")] // Missing @
[InlineData("@example.com")] // Missing local part
[InlineData("john@")] // Missing domain
public void Create_InvalidEmail_ReturnsFailureResult(string email)
{
    // Act
    var result = EmailAddress.Create(email);
    
    // Assert
    result.ShouldBeFailure();
}

[Fact]
public void Equality_SameValue_ReturnsTrue()
{
    // Arrange
    var email1 = EmailAddress.Create("john@example.com").Value;
    var email2 = EmailAddress.Create("john@example.com").Value;
    
    // Act & Assert
    email1.ShouldBe(email2);
    (email1 == email2).ShouldBeTrue();
}
```

---

### Enumeration Tests

- [ ] Test FromName returns correct enumeration instance
- [ ] Test FromValue returns correct enumeration instance
- [ ] Test FromName with invalid name throws or returns null
- [ ] Test equality between enumeration instances
- [ ] Test additional properties (Enabled, Description, etc.)

**CORRECT**:
```csharp
[Theory]
[InlineData("Lead", 1)]
[InlineData("Active", 2)]
[InlineData("Retired", 3)]
public void FromName_ValidName_ReturnsCorrectInstance(string name, int expectedValue)
{
    // Act
    var status = CustomerStatus.FromName(name);
    
    // Assert
    status.ShouldNotBeNull();
    status.Name.ShouldBe(name);
    status.Value.ShouldBe(expectedValue);
}

[Fact]
public void FromName_InvalidName_ReturnsNull()
{
    // Act
    var status = CustomerStatus.FromName("InvalidStatus");
    
    // Assert
    status.ShouldBeNull();
}
```

---

## Application Layer Testing

### Command Handler Tests

- [ ] Test handler with valid command returns Success Result
- [ ] Test handler creates aggregate via factory method
- [ ] Test handler saves aggregate to repository
- [ ] Test handler returns mapped DTO model
- [ ] Test handler with invalid command returns Failure Result (validation)
- [ ] Test handler publishes domain events (if applicable)
- [ ] Test handler respects retry/timeout behaviors (optional)

**CORRECT**:
```csharp
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccessResult()
{
    // Arrange
    var repository = Substitute.For<IGenericRepository<Customer>>();
    var mapper = Substitute.For<IMapper>();
    var handler = new CustomerCreateCommandHandler(repository, mapper);
    
    var command = new CustomerCreateCommand
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com",
        Status = "Active"
    };
    
    mapper.Map<CustomerModel>(Arg.Any<Customer>()).Returns(new CustomerModel
    {
        Id = Guid.NewGuid(),
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com"
    });
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.ShouldBeSuccess();
    result.Value.ShouldNotBeNull();
    result.Value.FirstName.ShouldBe(command.FirstName);
    await repository.Received(1).InsertAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
}
```

**Example from Codebase** (simplified test with IRequester):
```csharp
[Fact]
public async Task Process_ValidRequest_SuccessResult()
{
    // Arrange
    var requester = this.ServiceProvider.GetService<IRequester>();
    var command = new CustomerCreateCommand(
        new CustomerModel() { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" });

    // Act
    var response = await requester.SendAsync(command, null, CancellationToken.None);

    // Assert
    response.ShouldBeSuccess();
    response.Value.ShouldNotBeNull();
    response.Value.FirstName.ShouldBe(command.Model.FirstName);
}
```

---

### Command Validator Tests

- [ ] Test validator with valid command has no validation errors
- [ ] Test validator with invalid command has expected validation errors
- [ ] Test each validation rule individually (one test per rule)
- [ ] Test error messages are descriptive

**CORRECT**:
```csharp
[Fact]
public void Validate_ValidCommand_NoErrors()
{
    // Arrange
    var validator = new CustomerCreateCommandValidator();
    var command = new CustomerCreateCommand
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com"
    };
    
    // Act
    var result = validator.Validate(command);
    
    // Assert
    result.IsValid.ShouldBeTrue();
    result.Errors.ShouldBeEmpty();
}

[Fact]
public void Validate_EmptyFirstName_HasValidationError()
{
    // Arrange
    var validator = new CustomerCreateCommandValidator();
    var command = new CustomerCreateCommand
    {
        FirstName = string.Empty,
        LastName = "Doe",
        Email = "john.doe@example.com"
    };
    
    // Act
    var result = validator.Validate(command);
    
    // Assert
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(command.FirstName));
}
```

---

### Query Handler Tests

- [ ] Test handler with valid query returns Success Result
- [ ] Test handler retrieves entity from repository
- [ ] Test handler maps entity to DTO model
- [ ] Test handler returns null/NotFound for non-existent entity (FindOne)
- [ ] Test handler returns empty collection for no results (FindAll)
- [ ] Test handler applies filters correctly (FindAll with filters)

**CORRECT**:
```csharp
[Fact]
public async Task Handle_ExistingId_ReturnsSuccessResult()
{
    // Arrange
    var repository = Substitute.For<IGenericRepository<Customer>>();
    var mapper = Substitute.For<IMapper>();
    var handler = new CustomerFindOneQueryHandler(repository, mapper);
    
    var customerId = CustomerId.Create(Guid.NewGuid());
    var customer = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active).Value;
    
    repository.FindOneAsync(customerId, Arg.Any<CancellationToken>()).Returns(customer);
    mapper.Map<CustomerModel>(customer).Returns(new CustomerModel
    {
        Id = customerId.Value,
        FirstName = "John",
        LastName = "Doe"
    });
    
    var query = new CustomerFindOneQuery { Id = customerId.Value };
    
    // Act
    var result = await handler.Handle(query, CancellationToken.None);
    
    // Assert
    result.ShouldBeSuccess();
    result.Value.ShouldNotBeNull();
    result.Value.FirstName.ShouldBe("John");
}

[Fact]
public async Task Handle_NonExistentId_ReturnsNull()
{
    // Arrange
    var repository = Substitute.For<IGenericRepository<Customer>>();
    var mapper = Substitute.For<IMapper>();
    var handler = new CustomerFindOneQueryHandler(repository, mapper);
    
    repository.FindOneAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>()).Returns((Customer)null);
    
    var query = new CustomerFindOneQuery { Id = Guid.NewGuid() };
    
    // Act
    var result = await handler.Handle(query, CancellationToken.None);
    
    // Assert
    result.ShouldBeSuccess();
    result.Value.ShouldBeNull();
}
```

---

## Infrastructure Layer Testing

### EF Core Configuration Tests

- [ ] Test entity can be added to DbContext without errors
- [ ] Test TypedEntityId conversion works correctly (persists as Guid)
- [ ] Test Value Object conversion works correctly (persists as primitive)
- [ ] Test Enumeration conversion works correctly (persists as int/string)
- [ ] Test AuditState owned entity persists correctly
- [ ] Test concurrency token configured (ConcurrencyVersion)
- [ ] Test domain events ignored (not persisted)
- [ ] Test navigation properties and relationships configured

**CORRECT**:
```csharp
[Fact]
public async Task CanAddAndRetrieveCustomer()
{
    // Arrange
    using var context = new CoreModuleDbContext(options);
    var customer = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active).Value;
    
    // Act
    context.Customers.Add(customer);
    await context.SaveChangesAsync();
    
    var retrieved = await context.Customers.FindAsync(customer.Id);
    
    // Assert
    retrieved.ShouldNotBeNull();
    retrieved.FirstName.ShouldBe("John");
    retrieved.Email.Value.ShouldBe("john@example.com");
    retrieved.Status.ShouldBe(CustomerStatus.Active);
}
```

---

### Repository Tests

- [ ] Test InsertAsync adds entity to database
- [ ] Test UpdateAsync modifies existing entity
- [ ] Test DeleteAsync removes entity from database
- [ ] Test FindOneAsync retrieves entity by id
- [ ] Test FindAllAsync retrieves collection of entities
- [ ] Test specification-based queries return correct results
- [ ] Test repository behaviors (logging, audit, domain events) execute correctly

**Note**: Repository tests are often integration tests requiring a real or in-memory database.

---

## Presentation Layer Testing

### Mapping Tests

- [ ] Test aggregate maps to DTO model correctly (all properties)
- [ ] Test DTO model maps to aggregate correctly (via factory)
- [ ] Test TypedEntityId converts to Guid and back
- [ ] Test Value Objects convert to primitives and back
- [ ] Test Enumerations convert to strings and back
- [ ] Test collection mappings (if applicable)
- [ ] Test null handling for optional properties

**CORRECT**:
```csharp
[Fact]
public void Should_Map_Customer_To_CustomerModel()
{
    // Arrange
    var customer = Customer.Create("John", "Doe", "john@example.com", CustomerStatus.Active).Value;
    var config = new TypeAdapterConfig();
    new CoreModuleMapperRegister().Register(config);
    
    // Act
    var model = customer.Adapt<CustomerModel>(config);
    
    // Assert
    model.ShouldNotBeNull();
    model.Id.ShouldBe(customer.Id.Value);
    model.FirstName.ShouldBe("John");
    model.Email.ShouldBe("john@example.com");
    model.Status.ShouldBe("Active");
}
```

---

### Endpoint Integration Tests

- [ ] Test GET all returns 200 OK with collection
- [ ] Test GET by id returns 200 OK with entity
- [ ] Test GET by id returns 404 Not Found for non-existent entity
- [ ] Test POST with valid data returns 201 Created with Location header
- [ ] Test POST with invalid data returns 400 Bad Request with validation errors
- [ ] Test PUT with valid data returns 200 OK with updated entity
- [ ] Test PUT with non-existent id returns 404 Not Found
- [ ] Test DELETE with existing id returns 204 No Content
- [ ] Test DELETE with non-existent id returns 404 Not Found
- [ ] Test authentication/authorization (if enabled)

**CORRECT** (from codebase):
```csharp
[Theory]
[InlineData("api/coremodule/customers")]
public async Task Get_SingleExisting_ReturnsOk(string route)
{
    // Arrange
    var model = await this.SeedEntity(route);

    // Act
    var response = await this.fixture.Client.GetAsync(route + $"/{model.Id}");

    // Assert
    response.Should().Be200Ok();
    response.Should().MatchInContent($"*{model.FirstName}*");
    
    var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
    responseModel.ShouldNotBeNull();
}

[Theory]
[InlineData("api/coremodule/customers")]
public async Task Post_ValidModel_ReturnsCreated(string route)
{
    // Arrange
    var ticks = DateTime.UtcNow.Ticks;
    var model = new CustomerModel { FirstName = $"John{ticks}", LastName = $"Doe{ticks}", Email = $"john.doe{ticks}@example.com" };
    var json = JsonSerializer.Serialize(model);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await this.fixture.Client.PostAsync(route, content);

    // Assert
    response.Should().Be201Created();
    
    var responseModel = await response.Content.ReadAsAsync<CustomerModel>();
    responseModel.ShouldNotBeNull();
}

[Theory]
[InlineData("api/coremodule/customers")]
public async Task Post_InvalidEntity_ReturnsBadRequest(string route)
{
    // Arrange
    var model = new CustomerModel { FirstName = string.Empty, LastName = string.Empty, Email = string.Empty };
    var json = JsonSerializer.Serialize(model);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    // Act
    var response = await this.fixture.Client.PostAsync(route, content);

    // Assert
    response.Should().Be400BadRequest();
    response.Should().MatchInContent("*FluentValidationError*");
}
```

---

## Test Organization

- [ ] Unit tests organized by layer: Domain, Application, Infrastructure, Presentation
- [ ] Test project naming: `[Module].UnitTests`, `[Module].IntegrationTests`
- [ ] Test file naming: `[ClassUnderTest]Tests.cs` (e.g., `CustomerTests.cs`, `CustomerCreateCommandHandlerTests.cs`)
- [ ] Test namespace mirrors source namespace with `.UnitTests` or `.IntegrationTests` suffix
- [ ] Shared test fixtures and helpers in separate folder (e.g., `TestBase.cs`, `TestFixture.cs`)

**CORRECT**:
```
tests/
  Modules/
    CoreModule/
      CoreModule.UnitTests/
        Domain/
          CustomerTests.cs
          EmailAddressTests.cs
        Application/
          Commands/
            CustomerCreateCommandHandlerTests.cs
            CustomerCreateCommandValidatorTests.cs
      CoreModule.IntegrationTests/
        Infrastructure/
          EntityFramework/
            CoreModuleDbContextTests.cs
        Presentation/
          Web/
            CustomerEndpointTests.cs
```

---

## Mocking Strategy

- [ ] Use NSubstitute for mocking dependencies (repositories, mapper, external services)
- [ ] Mock only external dependencies (I/O, network, database)
- [ ] Do NOT mock domain entities or value objects (use real instances)
- [ ] Mock IGenericRepository in handler tests
- [ ] Mock IMapper in handler tests (if not testing mapping)
- [ ] Use `Arg.Any<T>()` for parameter matching in mocks
- [ ] Verify interactions with mocks using `Received()` when important

**CORRECT**:
```csharp
var repository = Substitute.For<IGenericRepository<Customer>>();
var mapper = Substitute.For<IMapper>();

repository.FindOneAsync(Arg.Any<CustomerId>(), Arg.Any<CancellationToken>())
    .Returns(customer);

mapper.Map<CustomerModel>(Arg.Any<Customer>())
    .Returns(new CustomerModel { Id = Guid.NewGuid(), FirstName = "John" });

// Later, verify interaction
await repository.Received(1).InsertAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
```

**WRONG**:
```csharp
// Wrong: Mocking domain entity
var customer = Substitute.For<Customer>(); // Domain entities should be real instances

// Wrong: Not setting up mock return value
var repository = Substitute.For<IGenericRepository<Customer>>();
var customer = await repository.FindOneAsync(id); // Returns null, not configured
```

---

## Test Coverage Goals (Optional)

- [ ] Aim for 70-80% code coverage overall
- [ ] Domain layer: 90%+ coverage (critical business logic)
- [ ] Application layer: 80%+ coverage (command/query handlers)
- [ ] Infrastructure layer: 60%+ coverage (mostly integration tests)
- [ ] Presentation layer: 70%+ coverage (endpoint tests)
- [ ] Focus on testing behavior, not just coverage percentage
- [ ] Use coverage tools: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Generate coverage reports: ReportGenerator (HTML output)

---

## Common Testing Anti-Patterns to Avoid

- [ ] **Testing Implementation Details**: Test behavior (public API), not internal state or private methods
- [ ] **Fragile Tests**: Tests that break with minor refactoring (over-specified assertions)
- [ ] **Test Interdependence**: Tests that depend on execution order or shared state
- [ ] **Testing Too Much**: One test validates multiple unrelated behaviors
- [ ] **Testing Too Little**: Tests only happy path, ignoring edge cases and error scenarios
- [ ] **Mocking Everything**: Mocking domain objects or value objects (use real instances)
- [ ] **No Assertions**: Tests that run code but don't assert outcomes
- [ ] **Slow Tests**: Unit tests that take seconds to run (use mocks, in-memory DB)
- [ ] **Not Testing Result Pattern**: Not checking `.IsSuccess`/`.IsFailure` and unwrapping `.Value`

---

## Final Testing Review

Before considering the aggregate implementation complete:

- [ ] Domain layer tests pass (aggregate, value objects, enumerations)
- [ ] Application layer tests pass (command/query handlers, validators)
- [ ] Infrastructure layer tests pass (EF Core configuration, repositories)
- [ ] Presentation layer tests pass (mapping, endpoints)
- [ ] All tests are green (no failing tests)
- [ ] Test coverage meets target goals (70-80% overall)
- [ ] Tests run quickly (unit tests < 5s total, integration tests < 30s total)
- [ ] Tests are deterministic (no flaky tests)
- [ ] Tests are well-organized and easy to navigate
- [ ] Tests follow naming conventions and AAA pattern

---

## References

- **Unit Test Example**: `tests/Modules/CoreModule/CoreModule.UnitTests/Application/Commands/CustomerCreateCommandHandlerTests.cs` (lines 1-35)
- **Integration Test Example**: `tests/Modules/CoreModule/CoreModule.IntegrationTests/Presentation/Web/CustomerEndpointTests.cs` (lines 1-250)
- **xUnit Documentation**: [xUnit.net](https://xunit.net/)
- **Shouldly Documentation**: [Shouldly](https://docs.shouldly.org/)
- **NSubstitute Documentation**: [NSubstitute](https://nsubstitute.github.io/)

---

**Next Checklist**: build-checkpoints.md (Incremental Build Validation)
