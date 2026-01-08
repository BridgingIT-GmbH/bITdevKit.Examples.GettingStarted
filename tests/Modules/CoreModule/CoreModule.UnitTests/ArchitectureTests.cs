// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.ArchitectureTests;

using Dumpify;
using NetArchTest.Rules;

public class ArchitectureFixture
{
    public Types Types { get; } = Types.FromPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    public string BaseNamespace { get; } = "BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule";

    /// <summary>
    /// List of other modules in the solution that this module should NOT directly reference.
    /// Add new module names here as they are created (without layer suffixes like .Domain, .Application).
    /// Module boundary tests will derive specific layer namespaces from these base module names.
    /// </summary>
    public string[] ForbiddenModules { get; } =
    [
        "BridgingIT.DevKit.Examples.GettingStarted.Modules.OtherModule1",
        "BridgingIT.DevKit.Examples.GettingStarted.Modules.OtherModule2"
    ];
}

/// <summary>
/// <para>
/// Architecture tests that enforce Clean Architecture and Modular Monolith boundaries.
/// These tests act as automated guardrails to prevent architectural drift and ensure
/// the codebase adheres to the intended layered and modular design principles.
/// </para>
/// <para>
/// Based on the bITdevKit Getting Started guidelines, these tests validate:
/// - Clean Architecture layer dependencies (Domain → Application → Infrastructure → Presentation)
/// - Domain-Driven Design patterns (factory methods, value objects, aggregates)
/// - Module isolation boundaries (preparing for multi-module scenarios)
/// </para>
/// <para>
/// When these tests fail, they indicate architectural violations that should be addressed
/// before merging code. The violations are typically caused by shortcuts that bypass
/// proper dependency inversion, abstraction, or module boundaries.
/// </para>
/// </summary>
[UnitTest("Architecture")]
public class ArchitectureTests : IClassFixture<ArchitectureFixture>
{
    private readonly ITestOutputHelper output;
    private readonly ArchitectureFixture fixture;

    public ArchitectureTests(ITestOutputHelper output, ArchitectureFixture fixture)
    {
        this.output = output;
        this.fixture = fixture;
    }

    #region Clean Architecture Layer Dependency Tests

    /// <summary>
    /// <para>
    /// Validates that the Application layer does NOT depend on Infrastructure.
    /// This enforces Clean Architecture's dependency inversion principle where Application
    /// defines contracts (interfaces like IGenericRepository) and Infrastructure implements them.
    /// Application should only reference Domain and define abstractions.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Application handlers directly instantiating Infrastructure classes (e.g., new CoreModuleDbContext())
    /// - Application using Infrastructure-specific types (e.g., EF Core's DbSet, DbContextOptions, Migrations)
    /// - Application referencing concrete repository implementations instead of IGenericRepository abstraction
    /// - Application depending on database-specific query syntax or ORM features
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - CustomerCreateCommandHandler constructor: CustomerCreateCommandHandler(CoreModuleDbContext context)
    /// - Command using: using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
    /// - Handler calling: context.Database.ExecuteSqlRaw("DELETE FROM...")
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Handler depends on: IGenericRepository&lt;Customer&gt; repository
    /// - Infrastructure registers: services.AddEntityFrameworkRepository&lt;Customer, CoreModuleDbContext&gt;()
    /// - Application defines what it needs (interface), Infrastructure provides how it works (implementation)
    /// </para>
    /// </summary>
    [Fact]
    public void Application_ShouldNot_HaveDependencyOnInfrastructure()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Application")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Infrastructure").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Application layer must not depend on Infrastructure (violates Clean Architecture dependency inversion).\n" +
            "Application should define interfaces and depend on abstractions, Infrastructure should implement them.\n" +
            "Use IGenericRepository<T> instead of DbContext, define domain service interfaces in Application.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that the Application layer does NOT depend on Presentation.
    /// Application contains business logic (commands, queries, handlers) and should be independent
    /// of how that logic is exposed (HTTP endpoints, gRPC, message queues, CLI).
    /// Presentation depends on Application (calling IRequester), never the reverse.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Application handlers referencing HTTP status codes, route attributes, or controller types
    /// - Application using Presentation DTOs instead of defining its own models (CustomerModel should be in Application)
    /// - Application depending on endpoint-specific logic, middleware concerns, or HTTP context
    /// - Application handlers returning IActionResult or other web-specific types
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Command handler: using Microsoft.AspNetCore.Mvc;
    /// - Handler method: public async Task&lt;IActionResult&gt; Handle(...)
    /// - Handler using: HttpContext.User.Claims to get user info (should use abstraction)
    /// - Application referencing: CustomerEndpoints class or MapsterSettings from Presentation
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Application defines: CustomerModel (in Application.Models)
    /// - Presentation calls: await requester.SendAsync(new CustomerCreateCommand(model))
    /// - Presentation maps: Application models to/from HTTP request/response DTOs
    /// - Application returns: Result&lt;CustomerModel&gt; (framework-agnostic)
    /// </para>
    /// </summary>
    [Fact]
    public void Application_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Application")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Application layer must not depend on Presentation (business logic should be delivery-mechanism agnostic).\n" +
            "Application defines use cases, Presentation exposes them via HTTP/gRPC/messaging.\n" +
            "Handlers should return Result<T>, not IActionResult or HTTP-specific types.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that the Domain layer does NOT depend on Application.
    /// Domain is the innermost layer containing pure business rules, aggregates, value objects,
    /// and domain events. It must remain completely independent—no references to use cases,
    /// handlers, or application-level concerns.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Domain entities using command/query types in their methods (Customer.Create should not take CustomerCreateCommand)
    /// - Domain referencing application services, Result types from Application layer, or handlers
    /// - Domain events containing application-layer specific data structures or DTOs
    /// - Domain rules depending on application workflow logic
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Customer entity: public static Result&lt;Customer&gt; Create(CustomerCreateCommand command)
    /// - Domain event: CustomerCreatedDomainEvent { CustomerModel Model { get; } }
    /// - Value object: EmailAddress.Create using CustomerCreateCommand.Validator
    /// - Domain rule: new EmailShouldBeUniqueRule referencing ICustomerApplicationService
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Domain entities: public static Result&lt;Customer&gt; Create(string firstName, string lastName, string email, CustomerNumber number)
    /// - Domain events: CustomerCreatedDomainEvent { CustomerId Id { get; }, string Email { get; } } (primitives or domain types)
    /// - Domain rules: new EmailShouldBeUniqueRule(string email, IGenericRepository&lt;Customer&gt; repository)
    /// - Domain is framework-agnostic and testable in isolation
    /// </para>
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnApplication()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Application").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain layer must not depend on Application (domain should be pure business logic without use-case knowledge).\n" +
            "Domain defines business rules and entities, Application orchestrates use cases.\n" +
            "Domain should only use primitives, domain types, and abstractions from BridgingIT.DevKit.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that the Domain layer does NOT depend on Infrastructure.
    /// Domain must not be coupled to database technologies, external APIs, or infrastructure services.
    /// This ensures domain logic is testable in isolation and portable across different
    /// persistence strategies (SQL Server today, MongoDB tomorrow, in-memory for tests).
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Domain entities having EF Core attributes ([Column], [Table], [ForeignKey])
    /// - Domain using DbContext, DbSet, SQL, or ORM-specific types
    /// - Domain calling external services directly (email, payment APIs) instead of defining interfaces
    /// - Domain depending on Infrastructure configuration or startup logic
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Customer entity: [Table("Customers", Schema = "core")]
    /// - Customer property: [Column("FirstName", TypeName = "nvarchar(100)")]
    /// - Domain rule: using (var context = new CoreModuleDbContext()) { }
    /// - Value object: EmailAddress.Create calling EmailValidationService from Infrastructure
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Domain: public class Customer : AuditableAggregateRoot&lt;Guid&gt; { } (no EF attributes)
    /// - Infrastructure: CustomerTypeConfiguration.Configure(EntityTypeBuilder&lt;Customer&gt; builder) (EF config separate)
    /// - Domain interfaces: IEmailValidationService (defined in Domain if needed by domain rules)
    /// - Infrastructure implements: EmailValidationService : IEmailValidationService
    /// </para>
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnInfrastructure()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Infrastructure").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain layer must not depend on Infrastructure (domain must be persistence-ignorant).\n" +
            "Domain defines business entities and rules, Infrastructure handles persistence mapping.\n" +
            "Use IEntityTypeConfiguration<T> in Infrastructure for EF mappings, keep Domain clean.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that the Domain layer does NOT depend on Presentation.
    /// Domain should never know how it's being exposed—it contains timeless business rules
    /// that don't change whether accessed via REST API, gRPC, CLI, or message queue.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Domain entities returning HTTP-specific types (IActionResult, HttpResponse, StatusCodeResult)
    /// - Domain using presentation DTOs, view models, or request/response objects
    /// - Domain validating based on presentation concerns (e.g., route parameters, query strings)
    /// - Domain depending on web frameworks (ASP.NET Core, controllers, middleware)
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Customer method: public IActionResult Export() { return new OkResult(); }
    /// - Domain event: CustomerCreatedDomainEvent { HttpRequest Request { get; } }
    /// - Value object: EmailAddress using [FromBody] or [FromQuery] attributes
    /// - Domain rule: checking HttpContext.User.IsInRole("Admin")
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Domain: public Result&lt;byte[]&gt; Export() (returns domain result)
    /// - Presentation: maps domain results to HTTP responses (MapHttpOk, MapHttpCreated)
    /// - Domain validation: independent of how data arrives (HTTP, message, CLI)
    /// - Presentation: responsible for authentication/authorization checks before calling domain
    /// </para>
    /// </summary>
    [Fact]
    public void Domain_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Domain")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain layer must not depend on Presentation (domain business rules are independent of delivery mechanism).\n" +
            "Domain contains timeless business logic, Presentation adapts it to specific interfaces (HTTP/gRPC/CLI).\n" +
            "Domain should return Result<T>, Presentation maps to HTTP status codes and responses.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that the Infrastructure layer does NOT depend on Presentation.
    /// Infrastructure handles persistence, external services, and infrastructure concerns
    /// (database, file system, message queues). It should serve Application and Domain,
    /// not be coupled to how the system is exposed externally.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Repository implementations referencing HttpContext, controllers, or routing
    /// - DbContext using presentation-layer DTOs for queries or projections
    /// - Infrastructure startup tasks depending on endpoint configurations or middleware
    /// - Infrastructure services accessing session state, cookies, or HTTP-specific features
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Repository: CustomerRepository(CoreModuleDbContext context, IHttpContextAccessor httpContextAccessor)
    /// - DbContext: query projecting to CustomerEndpointResponseDto
    /// - Startup task: CoreModuleDomainSeederTask checking HttpContext.Request.Headers
    /// - Job: CustomerExportJob accessing RouteData or controller context
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Repository: CustomerRepository(CoreModuleDbContext context) (no presentation concerns)
    /// - Infrastructure: projects to domain entities or Application models only
    /// - Startup tasks: independent of HTTP context (can run in background worker)
    /// - Jobs: use Application commands/queries via IRequester, not direct presentation layer access
    /// </para>
    /// </summary>
    [Fact]
    public void Infrastructure_ShouldNot_HaveDependencyOnPresentation()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining($"{this.fixture.BaseNamespace}.Infrastructure")
            .ShouldNot().HaveDependencyOnAny(
                $"{this.fixture.BaseNamespace}.Presentation").GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Infrastructure layer must not depend on Presentation (persistence should not couple to delivery mechanism).\n" +
            "Infrastructure serves Application layer, Presentation orchestrates user interactions.\n" +
            "Repositories and DbContext should work independently of HTTP, suitable for background jobs and CLI.\n" +
            result.FailingTypes.DumpText());
    }

    #endregion

    #region Domain Design Pattern Tests

    /// <summary>
    /// <para>
    /// Validates that domain entities do NOT have public constructors.
    /// Entities should use static factory methods (e.g., Customer.Create()) to enforce
    /// invariants and validation rules at creation time. Public constructors allow
    /// invalid entity creation that bypasses business rules.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Direct entity instantiation: var customer = new Customer() { FirstName = "", ... }
    /// - Bypassing validation: new Customer("", "", "invalid-email") without Result pattern
    /// - Creating entities in invalid states that violate business rules
    /// - Missing initialization of required aggregate components or domain events
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - public Customer(string firstName, string lastName) { FirstName = firstName; ... }
    /// - public Customer() { } // Public parameterless constructor
    /// - Allowing: var customer = new Customer { Email = "invalid" } without validation
    /// </para>
    /// <para>
    /// CORRECT PATTERN (as seen in Customer.cs):
    /// - private Customer() { } // For EF Core materialization only
    /// - public static Result&lt;Customer&gt; Create(string firstName, string lastName, string email, CustomerNumber number)
    /// - Factory method validates inputs, ensures invariants, registers domain events
    /// - Entity is always in a valid state from construction
    /// </para>
    /// <para>
    /// TECHNICAL NOTE:
    /// The private/protected parameterless constructor is required for EF Core to materialize
    /// entities from the database, but it prevents application code from using it.
    /// </para>
    /// </summary>
    [Fact]
    public void DomainEntity_ShouldNot_HavePublicConstructor()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace).And()
                .ImplementInterface<IEntity>()
            .ShouldNot().HavePublicConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain entities must not have public constructors (use static factory methods to enforce invariants).\n" +
            "Example: public static Result<Customer> Create(...) with validation instead of public Customer(...).\n" +
            "Private/protected constructors are allowed for EF Core, but application code must use factory methods.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that domain entities HAVE a parameterless constructor (can be private/protected).
    /// EF Core requires a parameterless constructor for entity materialization from the database.
    /// This should be private or protected to prevent direct instantiation while allowing ORM functionality.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - EF Core runtime errors: "No suitable constructor found for entity type 'Customer'"
    /// - Missing ORM support: Unable to load entities from database queries
    /// - Serialization issues: Some serializers need parameterless constructors
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Only having: public Customer(string firstName, string lastName) // No parameterless variant
    /// - Missing: private Customer() { } that EF Core needs
    /// - Entity with only parameterized constructors prevents EF Core materialization
    /// </para>
    /// <para>
    /// CORRECT PATTERN (as seen in Customer.cs):
    /// - private Customer() { } // Required for EF Core, prevents application use
    /// - public static Result&lt;Customer&gt; Create(...) // For application code
    /// - Separation of concerns: ORM needs vs. domain invariants
    /// </para>
    /// <para>
    /// TECHNICAL NOTE:
    /// EF Core calls the parameterless constructor, then sets properties via reflection.
    /// The constructor should be private/protected (protected for mocking/testing).
    /// This pattern allows EF to work while enforcing factory methods for application code.
    /// </para>
    /// </summary>
    [Fact]
    public void DomainEntity_Should_HaveParameterlessConstructor()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace).And()
                .ImplementInterface<IEntity>()
            .Should().HaveParameterlessConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain entities must have a parameterless constructor (required for EF Core, should be private/protected).\n" +
            "This allows EF Core to materialize entities from database while preventing direct instantiation.\n" +
            "Pattern: private Customer() { } + public static Result<Customer> Create(...).\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that domain value objects do NOT have public constructors.
    /// Value objects should be immutable with creation encapsulated in factory methods
    /// to enforce validation rules and invariants. Public constructors allow creation
    /// of invalid value objects.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Invalid value objects: var email = new EmailAddress("not-an-email")
    /// - Bypassing validation: new CustomerNumber(2023, -1) with negative sequence
    /// - Creating value objects that violate domain rules
    /// - Mutable value objects that change after creation
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - public EmailAddress(string value) { Value = value; } // No validation
    /// - public CustomerNumber(int year, int sequence) { } // Allows invalid data
    /// - Allowing: var email = new EmailAddress { Value = "invalid" } without checks
    /// </para>
    /// <para>
    /// CORRECT PATTERN (as seen in EmailAddress.cs and CustomerNumber.cs):
    /// - private EmailAddress() { } // For serialization only
    /// - public static Result&lt;EmailAddress&gt; Create(string value)
    /// - Factory validates format, uniqueness constraints, business rules
    /// - Value objects are immutable after creation (init-only properties)
    /// </para>
    /// <para>
    /// VALUE OBJECT CHARACTERISTICS:
    /// - Immutable: No setters, properties are { get; init; } or { get; }
    /// - Self-validating: Factory method ensures valid state
    /// - Equality by value: Override Equals, GetHashCode, GetAtomicValues()
    /// - Side-effect free: Methods return new instances rather than mutating
    /// </para>
    /// </summary>
    [Fact]
    public void DomainValueObject_ShouldNot_HavePublicConstructor()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace).And()
                .Inherit<ValueObject>()
            .ShouldNot().HavePublicConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain value objects must not have public constructors (enforce immutability and validation via factory methods).\n" +
            "Value objects should be created through: public static Result<EmailAddress> Create(string value).\n" +
            "This ensures value objects are always in a valid, immutable state.\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that domain value objects HAVE a parameterless constructor (can be private/protected).
    /// Required for serialization (JSON, XML) and potentially for EF Core complex/owned types.
    /// Should be private/protected to prevent direct instantiation.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - Serialization errors: "Cannot deserialize EmailAddress, no parameterless constructor"
    /// - EF Core issues: When using value objects as owned entities or complex types
    /// - JSON deserialization failures in API responses or message handlers
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS:
    /// - Only having: private EmailAddress(string value) { Value = value; } // No parameterless version
    /// - Missing: private EmailAddress() { } for deserializers
    /// - Value object that can't be serialized/deserialized for DTOs or persistence
    /// </para>
    /// <para>
    /// CORRECT PATTERN (as seen in EmailAddress.cs and CustomerNumber.cs):
    /// - private EmailAddress() { } // For serializers and EF Core
    /// - public static Result&lt;EmailAddress&gt; Create(string value) // For application code
    /// - Separation: Serialization needs vs. domain creation logic
    /// </para>
    /// <para>
    /// TECHNICAL NOTE:
    /// Serializers (System.Text.Json, Newtonsoft.Json) call the parameterless constructor,
    /// then populate properties. For EF Core owned entities, this allows materialization.
    /// The constructor should be private/protected to prevent misuse in application code.
    /// </para>
    /// </summary>
    [Fact]
    public void DomainValueObject_Should_HaveParameterlessConstructor()
    {
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace).And()
                .Inherit<ValueObject>()
            .Should().HaveParameterlessConstructor().GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Domain value objects must have a parameterless constructor (required for serialization, should be private/protected).\n" +
            "This supports JSON serialization and EF Core owned entities while preventing direct instantiation.\n" +
            "Pattern: private EmailAddress() { } + public static Result<EmailAddress> Create(string value).\n" +
            result.FailingTypes.DumpText());
    }

    #endregion

    #region Module Boundary Tests

    /// <summary>
    /// <para>
    /// Validates that this module does NOT directly reference other modules at all.
    /// In a modular monolith architecture, modules should be completely isolated from each other's internals.
    /// Inter-module communication should only happen through:
    /// - Public Contracts assemblies ([Module].Contracts) containing interfaces and DTOs
    /// - Integration events via message bus (INotifier/INotificationHandler for cross-module events)
    /// - HTTP/gRPC APIs if modules are independently deployable
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - CoreModule.Application directly referencing InventoryModule.Domain.Product
    /// - CoreModule.Infrastructure querying InventoryModule.Infrastructure.InventoryDbContext
    /// - Any direct type coupling between modules (Domain, Application, Infrastructure, Presentation)
    /// - Tight coupling that prevents independent deployment or testing of modules
    /// </para>
    /// <para>
    /// EXAMPLE VIOLATIONS (when other modules exist):
    /// - using BridgingIT.DevKit.Examples.GettingStarted.Modules.InventoryModule.Domain;
    /// - using BridgingIT.DevKit.Examples.GettingStarted.Modules.InventoryModule.Application;
    /// - CustomerCreateCommandHandler(IGenericRepository&lt;Customer&gt; repo, IGenericRepository&lt;Product&gt; productRepo)
    /// - Direct call: var product = await inventoryModule.GetProduct(productId)
    /// </para>
    /// <para>
    /// CORRECT PATTERN:
    /// - Define: InventoryModule.Contracts project with IProductAvailabilityService interface
    /// - CoreModule references: InventoryModule.Contracts only (validated by next test)
    /// - Or use integration events: CustomerCreatedIntegrationEvent published by CoreModule
    /// - Or use HTTP API: CoreModule calls InventoryModule via HTTP client (if deployed separately)
    /// </para>
    /// <para>
    /// CURRENT STATUS:
    /// This test validates against potential future modules listed in TypesFixture.ForbiddenModules.
    /// When new modules are added, simply add their base namespace to that array.
    /// The test automatically enforces isolation from day one of the new module.
    /// </para>
    /// <para>
    /// MODULAR MONOLITH PRINCIPLES:
    /// - Each module is a vertical slice (Domain + Application + Infrastructure + Presentation)
    /// - Each module has its own DbContext (database-per-module pattern)
    /// - Modules communicate through well-defined public contracts only
    /// - No shared databases or direct cross-module queries
    /// - Integration events for eventual consistency across modules
    /// </para>
    /// </summary>
    [Fact]
    public void Module_ShouldNot_DirectlyReferenceOtherModulesInternals()
    {
        // Use the centralized list of forbidden modules from TypesFixture
        // This test prevents ANY direct reference to other modules' namespaces
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace)
            .ShouldNot().HaveDependencyOnAny(this.fixture.ForbiddenModules)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Modules must not directly reference other modules at all (use [Module].Contracts assemblies only).\n" +
            "For inter-module communication, reference [OtherModule].Contracts or use integration events.\n" +
            "This ensures modules remain loosely coupled and can evolve or be deployed independently.\n" +
            $"Forbidden modules: {string.Join(", ", this.fixture.ForbiddenModules.Select(m => m.Split('.').Last()))}\n" +
            result.FailingTypes.DumpText());
    }

    /// <summary>
    /// <para>
    /// Validates that if this module references other modules, it ONLY references their .Contracts assemblies.
    /// Modules must not reference internal layers (Domain, Application, Infrastructure, Presentation) of other modules.
    /// Only public contracts are allowed for cross-module communication.
    /// </para>
    /// <para>
    /// WHAT THIS PROTECTS AGAINST:
    /// - CoreModule references InventoryModule.Contracts (ALLOWED)
    /// - CoreModule references InventoryModule.Domain.Product (FORBIDDEN - internal domain model)
    /// - CoreModule references InventoryModule.Application handlers (FORBIDDEN - internal use cases)
    /// - CoreModule references InventoryModule.Infrastructure.InventoryDbContext (FORBIDDEN - internal persistence)
    /// </para>
    /// <para>
    /// EXAMPLE SCENARIO (when InventoryModule exists):
    /// </para>
    /// <code>
    /// // ALLOWED: Reference public contract
    /// using InventoryModule.Contracts;
    /// public class CustomerCreateCommandHandler
    /// {
    ///     private readonly IProductAvailabilityService productService; // From InventoryModule.Contracts
    ///
    ///     public async Task Handle(CustomerCreateCommand command)
    ///     {
    ///         var productResult = await productService.GetProductAsync(command.ProductId);
    ///         // ...
    ///     }
    /// }
    ///
    /// // FORBIDDEN: Direct reference to internal domain
    /// using InventoryModule.Domain;
    /// public class CustomerCreateCommandHandler
    /// {
    ///     private readonly IGenericRepository&lt;Product&gt; productRepo; // Product is internal to InventoryModule
    /// }
    /// </code>
    /// <para>
    /// CONTRACT ASSEMBLY PATTERN:
    /// - [Module].Contracts contains: Public interfaces, DTOs, enums for external consumption
    /// - [Module].Contracts does NOT contain: Entities, repositories, handlers, infrastructure (all internal)
    /// - Versioning: Use semantic versioning for contract assemblies to manage breaking changes
    /// - Stability: Contracts should change less frequently than internal implementation
    /// </para>
    /// <para>
    /// IMPLEMENTATION STEPS WHEN ADDING NEW MODULE:
    /// 1. Create [NewModule].Contracts project with public interfaces (e.g., IProductAvailabilityService)
    /// 2. Add module base namespace to TypesFixture.ForbiddenModules array
    /// 3. CoreModule can now reference [NewModule].Contracts (but not .Domain/.Application/.Infrastructure)
    /// 4. This test will automatically validate proper contract-only usage
    /// </para>
    /// </summary>
    [Fact]
    public void Module_Should_OnlyReferenceOtherModulesContracts()
    {
        // Derive Contracts namespaces from forbidden modules list
        var contractsNamespaces = this.fixture.ForbiddenModules
            .Select(module => $"{module}.Contracts").ToArray();

        // Derive forbidden internal layer namespaces from forbidden modules list
        var forbiddenInternalLayers = new[] { ".Domain", ".Application", ".Infrastructure", ".Presentation" };
        var forbiddenNamespaces = this.fixture.ForbiddenModules
            .SelectMany(module => forbiddenInternalLayers.Select(layer => $"{module}{layer}")).ToArray();

        // Find types that reference other modules' contracts
        var typesReferencingOtherModules = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace)
            .And().HaveDependencyOnAny(contractsNamespaces)
            .GetTypes();

        // If no types reference contracts, test passes automatically
        if (!typesReferencingOtherModules.Any())
        {
            return; // No cross-module references exist yet
        }

        // If types reference contracts, ensure they don't also reference internal layers
        var result = this.fixture.Types
            .That().ResideInNamespaceContaining(this.fixture.BaseNamespace)
            .ShouldNot().HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "When referencing other modules, ONLY use their public .Contracts assemblies.\n" +
            "Direct references to .Domain, .Application, .Infrastructure, or .Presentation violate module boundaries.\n" +
            "This ensures modules can be refactored, versioned, and deployed independently.\n" +
            $"Allowed: {string.Join(", ", contractsNamespaces.Select(ns => ns.Split('.').Last()))}\n" +
            $"Forbidden: .Domain, .Application, .Infrastructure, .Presentation layers of other modules\n" +
            result.FailingTypes.DumpText());
    }

    #endregion
}