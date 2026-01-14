# Architecture Overview

This document provides a comprehensive overview of the Clean/Onion Architecture pattern used in the bITdevKit GettingStarted example, with specific focus on how aggregate implementations span across all layers.

**Purpose**: Understand the layered architecture, dependency flow, and request/response flow  
**Audience**: Developers implementing new aggregates or understanding the system architecture

---

## Table of Contents

- [Clean Architecture Principles](#clean-architecture-principles)
- [Layer Structure](#layer-structure)
- [Dependency Rules](#dependency-rules)
- [Request Flow](#request-flow)
- [Module Boundaries](#module-boundaries)
- [References](#references)

---

## Clean Architecture Principles

The architecture follows **Clean Architecture** (also known as **Onion Architecture**) principles:

1. **Independence**: Business logic independent of frameworks, UI, database, external agencies
2. **Testability**: Business rules testable without UI, database, web server, or external elements
3. **UI Independence**: UI changeable without changing the rest of the system
4. **Database Independence**: Domain logic not bound to database implementation
5. **External Agency Independence**: Business rules don't know anything about outside world

### Core Tenets

- **Dependency Inversion**: Dependencies point inward, toward domain
- **Separation of Concerns**: Each layer has distinct responsibility
- **Encapsulation**: Domain encapsulates business rules, Application orchestrates use cases
- **Abstraction**: Outer layers depend on abstractions defined by inner layers

---

## Layer Structure

The application is organized into 5 distinct layers, arranged concentrically from inside out:

```
┌─────────────────────────────────────────────────────────────────┐
│                       Presentation Layer                        │
│  (Web API Endpoints, Minimal APIs, Mapping Configurations)      │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │               Infrastructure Layer                         │ │
│  │  (EF Core, Repositories, Jobs, External Services)          │ │
│  │                                                             │ │
│  │  ┌────────────────────────────────────────────────────────┐│ │
│  │  │            Application Layer                           ││ │
│  │  │  (Commands, Queries, Handlers, Validators, DTOs)       ││ │
│  │  │                                                         ││ │
│  │  │  ┌────────────────────────────────────────────────────┐││ │
│  │  │  │           Domain Layer                             │││ │
│  │  │  │  (Aggregates, Entities, Value Objects,             │││ │
│  │  │  │   Enumerations, Domain Events, Business Rules)     │││ │
│  │  │  │                                                     │││ │
│  │  │  │  ┌────────────────────────────────────────────────┐│││ │
│  │  │  │  │   Host Layer (Composition Root)                ││││ │
│  │  │  │  │   (Program.cs, DI Configuration, Middleware)   ││││ │
│  │  │  │  └────────────────────────────────────────────────┘│││ │
│  │  │  └────────────────────────────────────────────────────┘││ │
│  │  └────────────────────────────────────────────────────────┘│ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

---

## Layer Descriptions

### 1. Domain Layer (Core)

**Purpose**: Contains enterprise business logic and domain model  
**Location**: `src/Modules/[Module]/[Module].Domain/`

**Responsibilities**:
- Define aggregates, entities, and their behavior
- Enforce business rules and invariants
- Define value objects (immutable, validated primitives)
- Define enumerations (typed constants with behavior)
- Raise domain events for significant occurrences
- Provide factory methods for aggregate creation
- No dependencies on outer layers

**Key Concepts**:
- **Aggregates**: Cluster of entities and value objects with root entity as entry point
- **Entities**: Objects with identity that persist over time
- **Value Objects**: Objects defined by their attributes, not identity
- **Enumerations**: Type-safe constants with additional properties
- **Domain Events**: Record of something that happened in the domain

**Example Files**:
- `Customer.cs` (aggregate root)
- `CustomerId.cs` (typed entity ID) or `[TypedEntityId<Guid>]` attribute
- `EmailAddress.cs` (value object)
- `CustomerStatus.cs` (enumeration)
- `CustomerCreatedDomainEvent.cs` (domain event)

**Dependencies**: NONE (pure C# + bITdevKit.Domain abstractions)

---

### 2. Application Layer (Use Cases)

**Purpose**: Orchestrates domain objects to implement use cases  
**Location**: `src/Modules/[Module]/[Module].Application/`

**Responsibilities**:
- Define commands (write operations) and queries (read operations)
- Implement command/query handlers using domain objects
- Validate inputs via FluentValidation
- Coordinate repository operations
- Map domain objects to DTOs (via IMapper)
- Return Result<T> for success/failure handling
- No UI or infrastructure concerns

**Key Concepts**:
- **CQRS**: Commands change state, Queries return data, separated
- **Handlers**: Process commands/queries, orchestrate domain operations
- **Validators**: Validate command/query inputs using FluentValidation
- **DTOs (Models)**: Data transfer objects for external communication
- **Result Pattern**: Return success/failure explicitly, no exceptions for business errors

**Example Files**:
- `CustomerCreateCommand.cs` + `CustomerCreateCommandHandler.cs` + `CustomerCreateCommandValidator.cs`
- `CustomerUpdateCommand.cs` + handler + validator
- `CustomerDeleteCommand.cs` + handler
- `CustomerFindOneQuery.cs` + `CustomerFindOneQueryHandler.cs`
- `CustomerFindAllQuery.cs` + handler
- `CustomerModel.cs` (DTO)

**Dependencies**: Domain layer only (references `[Module].Domain`)

---

### 3. Infrastructure Layer (External Concerns)

**Purpose**: Implements interfaces defined by Application layer, provides data access  
**Location**: `src/Modules/[Module]/[Module].Infrastructure/`

**Responsibilities**:
- Implement data persistence (EF Core DbContext, configurations)
- Implement repository interfaces (IGenericRepository<T>)
- Execute background jobs (Quartz)
- Integrate with external services (email, message bus, etc.)
- Provide startup tasks (data seeding, migrations)
- Configure entity mappings (EF Core type configurations)

**Key Concepts**:
- **DbContext**: EF Core database context for module
- **Entity Type Configuration**: Fluent API configuration for entities
- **Repositories**: Concrete implementations of IGenericRepository<T>
- **Repository Behaviors**: Logging, audit, domain event publishing
- **Migrations**: EF Core code-first database migrations

**Example Files**:
- `CoreModuleDbContext.cs`
- `CustomerTypeConfiguration.cs` (EF Core entity configuration)
- Repository registration in module startup (via `AddEntityFrameworkRepository<T, TDbContext>()`)
- Migrations folder (e.g., `20260114_AddCustomer.cs`)

**Dependencies**: Domain + Application layers (references both)

---

### 4. Presentation Layer (API Surface)

**Purpose**: Expose application functionality via HTTP API  
**Location**: `src/Modules/[Module]/[Module].Presentation/`

**Responsibilities**:
- Define minimal API endpoints (EndpointsBase implementations)
- Map HTTP requests to commands/queries
- Delegate to Application layer via IRequester (mediator)
- Map domain objects to DTOs via Mapster
- Return HTTP responses (TypedResults)
- Document API via OpenAPI/Swagger
- No business logic (pure request/response translation)

**Key Concepts**:
- **Minimal APIs**: Lightweight endpoint definitions (no controllers)
- **EndpointsBase**: Base class for endpoint groups
- **IRequester**: Mediator pattern for sending commands/queries
- **Mapster**: Object-to-object mapping (domain ↔ DTO)
- **TypedResults**: Strongly-typed HTTP response helpers

**Example Files**:
- `CustomerEndpoints.cs` (endpoint definitions)
- `CoreModuleMapperRegister.cs` (Mapster mapping configuration)

**Dependencies**: Application + Domain (for DTOs) layers

---

### 5. Host Layer (Composition Root)

**Purpose**: Wire up all dependencies, configure middleware, start application  
**Location**: `src/Presentation.Web.Server/`

**Responsibilities**:
- Register all modules (DI container configuration)
- Configure middleware pipeline (Serilog, correlation, problem details, Swagger)
- Configure cross-cutting concerns (authentication, CORS, rate limiting)
- Start application (Program.cs)
- No business logic

**Key Concepts**:
- **Dependency Injection**: Register all services, configure lifetimes
- **Middleware Pipeline**: Request processing pipeline (logging, error handling, etc.)
- **Module Registration**: Call `services.AddCoreModule(configuration)` for each module

**Example Files**:
- `Program.cs`
- `appsettings.json`, `appsettings.Development.json`

**Dependencies**: All layers (references all module projects)

---

## Dependency Rules

**The Dependency Rule**: Source code dependencies can only point inward. Inner layers cannot know about outer layers.

### Allowed Dependencies

```
Domain         →  (none)
Application    →  Domain
Infrastructure →  Domain, Application
Presentation   →  Domain, Application
Host           →  Domain, Application, Infrastructure, Presentation
```

### Forbidden Dependencies

- Domain → Application, Infrastructure, Presentation, Host ❌
- Application → Infrastructure, Presentation, Host ❌
- Infrastructure → Presentation, Host ❌
- Presentation → Infrastructure, Host ❌

### Dependency Inversion

When Application layer needs something from Infrastructure (e.g., data access), Application defines an **interface** (e.g., `IGenericRepository<T>`), and Infrastructure provides the **implementation**.

**Example**:
```csharp
// Application layer defines abstraction
namespace Application
{
    public interface IGenericRepository<T>
    {
        Task<T> FindOneAsync(TId id, CancellationToken cancellationToken);
    }
}

// Infrastructure layer provides implementation
namespace Infrastructure
{
    public class GenericRepository<T> : IGenericRepository<T>
    {
        // EF Core implementation
    }
}

// Host layer wires them up
services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>();
```

---

## Request Flow

### Command Flow (Write Operation)

Example: **Create a Customer** via `POST /api/core/customers`

```
1. HTTP Request
   ↓
2. Minimal API Endpoint (CustomerEndpoints.Create)
   ↓
3. Deserialize JSON → CustomerCreateCommand
   ↓
4. IRequester.SendAsync(command)
   ↓
5. ModuleScopeBehavior (set module context)
   ↓
6. ValidationPipelineBehavior (run FluentValidation)
   ↓
7. RetryPipelineBehavior (retry on transient failures)
   ↓
8. TimeoutPipelineBehavior (enforce timeout)
   ↓
9. CustomerCreateCommandHandler.Handle(command)
   ↓
10. Customer.Create(...) (domain factory, returns Result<Customer>)
   ↓
11. Validate business rules (aggregate invariants)
   ↓
12. Register CustomerCreatedDomainEvent
   ↓
13. IGenericRepository<Customer>.InsertAsync(customer)
    ↓
14. RepositoryLoggingBehavior (log operation)
    ↓
15. RepositoryAuditStateBehavior (set CreatedDate, CreatedBy)
    ↓
16. RepositoryDomainEventBehavior (publish domain events)
    ↓
17. EF Core DbContext.SaveChangesAsync()
    ↓
18. Database INSERT
    ↓
19. Map Customer → CustomerModel (via IMapper/Mapster)
    ↓
20. Return Result<CustomerModel>
    ↓
21. Endpoint.Match(success → TypedResults.Created, failure → TypedResults.BadRequest)
    ↓
22. HTTP Response: 201 Created + Location header + CustomerModel JSON
```

---

### Query Flow (Read Operation)

Example: **Get Customer by ID** via `GET /api/core/customers/{id}`

```
1. HTTP Request
   ↓
2. Minimal API Endpoint (CustomerEndpoints.GetById)
   ↓
3. Parse route parameter → Guid id
   ↓
4. Construct CustomerFindOneQuery { Id = id }
   ↓
5. IRequester.SendAsync(query)
   ↓
6. ModuleScopeBehavior, ValidationPipelineBehavior, etc.
   ↓
7. CustomerFindOneQueryHandler.Handle(query)
   ↓
8. IGenericRepository<Customer>.FindOneAsync(CustomerId.Create(query.Id))
   ↓
9. EF Core DbContext.FindAsync<Customer>(id)
   ↓
10. Database SELECT
    ↓
11. Return Customer aggregate (or null if not found)
    ↓
12. Map Customer → CustomerModel (via IMapper/Mapster)
    ↓
13. Return Result<CustomerModel>
    ↓
14. Endpoint.Match(success → TypedResults.Ok, failure/null → TypedResults.NotFound)
    ↓
15. HTTP Response: 200 OK + CustomerModel JSON (or 404 Not Found)
```

---

## Module Boundaries

The application is structured as a **Modular Monolith**: logical separation of modules within a single deployable unit.

### Module Structure

```
src/
  Modules/
    CoreModule/
      CoreModule.Domain/          (Domain layer for Core module)
      CoreModule.Application/     (Application layer for Core module)
      CoreModule.Infrastructure/  (Infrastructure layer for Core module)
      CoreModule.Presentation/    (Presentation layer for Core module)
    [OtherModule]/
      [OtherModule].Domain/
      [OtherModule].Application/
      ...
  Presentation.Web.Server/        (Host layer, composition root)
```

### Module Isolation

- Each module is self-contained (Domain, Application, Infrastructure, Presentation)
- Modules communicate via domain events (not direct references)
- Modules share common infrastructure (bITdevKit libraries)
- Modules can be extracted to separate microservices if needed (future evolution)

### Module Registration

Each module provides extension methods for DI registration:

```csharp
// In CoreModule.Presentation or Infrastructure
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Domain, Application, Infrastructure, Presentation services
        services.AddRequesterHandlers<CustomerCreateCommand>(); // Application
        services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>(); // Infrastructure
        services.AddMapping().WithMapster<CoreModuleMapperRegister>(); // Presentation
        
        return services;
    }
}

// In Program.cs (Host)
services.AddCoreModule(configuration);
```

---

## Data Flow Summary

### Domain → Application → Infrastructure

- **Domain** defines `Customer` aggregate with business rules
- **Application** defines `CustomerCreateCommand` and handler
- **Infrastructure** persists `Customer` to database via EF Core

### Infrastructure → Application → Presentation

- **Infrastructure** retrieves `Customer` from database
- **Application** maps `Customer` → `CustomerModel`
- **Presentation** returns `CustomerModel` as JSON via HTTP API

---

## Cross-Cutting Concerns

### Handled by Pipeline Behaviors

- **Validation**: ValidationPipelineBehavior (FluentValidation)
- **Retry**: RetryPipelineBehavior (Polly)
- **Timeout**: TimeoutPipelineBehavior
- **Logging**: RepositoryLoggingBehavior
- **Audit**: RepositoryAuditStateBehavior (CreatedDate, UpdatedDate, etc.)
- **Domain Events**: RepositoryDomainEventBehavior (publish events after SaveChanges)

### Handled by Middleware

- **Request Correlation**: `app.UseRequestCorrelation()` (propagate correlation ID)
- **Logging**: Serilog middleware (structured logging)
- **Exception Handling**: Problem Details middleware (structured error responses)
- **Authentication**: JWT Bearer middleware (validate tokens)
- **Authorization**: Authorization middleware (enforce policies)

---

## Key Architectural Patterns

### 1. CQRS (Command Query Responsibility Segregation)
- Commands: Change state, return Result<Unit> or Result<Model>
- Queries: Return data, do not change state, return Result<Model> or Result<IEnumerable<Model>>

### 2. Repository Pattern
- Abstraction over data access (IGenericRepository<T>)
- Encapsulates query logic via specifications
- Decorated with behaviors (logging, audit, domain events)

### 3. Mediator Pattern (Requester/Notifier)
- IRequester: Send commands/queries to handlers
- INotifier: Publish domain events to handlers
- Decouples sender from receiver

### 4. Result Pattern
- Explicit success/failure handling
- Avoid exceptions for business errors
- Railway-oriented programming (chaining operations)

### 5. Specification Pattern
- Encapsulate query logic in reusable specifications
- Compose complex queries from simple specifications
- Testable, maintainable query logic

---

## Benefits of This Architecture

1. **Testability**: Each layer testable in isolation (unit tests, integration tests)
2. **Maintainability**: Clear separation of concerns, easy to locate code
3. **Flexibility**: Easy to swap infrastructure (e.g., EF Core → Dapper)
4. **Scalability**: Modular monolith can evolve to microservices
5. **Team Productivity**: Multiple teams can work on different modules without conflicts
6. **Code Reusability**: Shared domain logic, specifications, behaviors
7. **Business Alignment**: Domain layer reflects ubiquitous language

---

## Common Pitfalls to Avoid

1. **Leaking Domain Logic to Application**: Keep business rules in domain, handlers orchestrate only
2. **Bypassing Abstractions**: Don't access DbContext directly from Application, use repositories
3. **Fat Endpoints**: Don't put logic in endpoints, delegate to handlers
4. **Anemic Domain Model**: Domain objects should have behavior, not just properties
5. **Circular Dependencies**: Enforce dependency rules with architecture tests
6. **Ignoring Result Pattern**: Don't throw exceptions for business failures, return Result<T>
7. **Not Using Domain Events**: Don't couple aggregates with direct references, use events

---

## References

- **ADR-0001**: [Clean/Onion Architecture with Strict Layer Boundaries](../../docs/ADR/0001-clean-onion-architecture.md)
- **ADR-0003**: [Modular Monolith Architecture](../../docs/ADR/0003-modular-monolith-architecture.md)
- **Customer Aggregate Example**: `src/Modules/CoreModule/CoreModule.Domain/Model/CustomerAggregate/Customer.cs`
- **Clean Architecture (Robert C. Martin)**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- **Onion Architecture (Jeffrey Palermo)**: https://jeffreypalermo.com/2008/07/the-onion-architecture-part-1/

---

**Next Document**: naming-conventions.md
