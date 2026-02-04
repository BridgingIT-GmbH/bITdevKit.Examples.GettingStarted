# System Design Diagrams - bITdevKit GettingStarted

Comprehensive architecture and design diagrams for the bITdevKit GettingStarted Example project.

## Table of Contents

- [System Design Diagrams - bITdevKit GettingStarted](#system-design-diagrams---bitdevkit-gettingstarted)
  - [Table of Contents](#table-of-contents)
  - [C4 Model Diagrams](#c4-model-diagrams)
    - [1. System Context Diagram](#1-system-context-diagram)
    - [2. Container Diagram](#2-container-diagram)
    - [3. Component Diagram - CoreModule](#3-component-diagram---coremodule)
  - [Architecture Diagrams](#architecture-diagrams)
    - [4. Clean Architecture Layers](#4-clean-architecture-layers)
    - [5. Module Structure](#5-module-structure)
  - [Interaction Diagrams](#interaction-diagrams)
    - [6. Sequence Diagram - Customer Creation](#6-sequence-diagram---customer-creation)
    - [7. Sequence Diagram - Domain Event Publishing](#7-sequence-diagram---domain-event-publishing)
  - [Data Model](#data-model)
    - [8. Entity Relationship Diagram](#8-entity-relationship-diagram)
  - [Request Processing](#request-processing)
    - [9. Request Pipeline Flow](#9-request-pipeline-flow)
  - [Diagram Usage](#diagram-usage)
  - [Related Documentation](#related-documentation)
  - [Updating These Diagrams](#updating-these-diagrams)

---

## C4 Model Diagrams

### 1. System Context Diagram

High-level view showing the system and its external actors.

```mermaid
C4Context
    title System Context - bITdevKit GettingStarted Example

    Person(developer, "Developer", "Tests and learns bITdevKit patterns")
    Person(api_user, "API Consumer", "Interacts with REST API")

    System(gettingstarted, "GettingStarted Application", "Modular monolith demonstrating DDD, Clean Architecture, and bITdevKit patterns")

    System_Ext(database, "SQL Server", "Relational database for persistence")
    System_Ext(seq, "Seq", "Centralized logging and diagnostics")

    Rel(developer, gettingstarted, "Explores patterns", "HTTPS/Swagger")
    Rel(api_user, gettingstarted, "Manages customers", "REST/JSON")
    Rel(gettingstarted, database, "Reads/Writes data", "EF Core")
    Rel(gettingstarted, seq, "Sends structured logs", "HTTP")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

### 2. Container Diagram

Shows the major containers (applications/processes) that make up the system.

```mermaid
C4Container
    title Container Diagram - GettingStarted Application

    Person(user, "User/Developer")

    Container_Boundary(app, "GettingStarted Application") {
        Container(webapi, "Web API", "ASP.NET Core Minimal API", "Exposes REST endpoints, handles HTTP requests")
        Container(modules, "Modules", "CoreModule + Others", "Business logic organized as vertical slices")
        Container(infrastructure, "Infrastructure Layer", ".NET Libraries", "Persistence, jobs, startup tasks")
    }

    ContainerDb(database, "Database", "SQL Server", "Stores customers, domain events, audit logs")
    Container_Ext(seq, "Seq Server", "Logging Platform", "Structured log aggregation")

    Rel(user, webapi, "HTTP/HTTPS", "JSON")
    Rel(webapi, modules, "Invokes", "Requester/Notifier")
    Rel(modules, infrastructure, "Uses", "Repositories, DbContext")
    Rel(infrastructure, database, "Reads/Writes", "EF Core/SQL")
    Rel(webapi, seq, "Logs", "HTTP/Serilog")
    Rel(modules, seq, "Logs", "ILogger")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="1")
```

### 3. Component Diagram - CoreModule

Internal structure of the CoreModule showing Clean Architecture layers.

```mermaid
C4Component
    title Component Diagram - CoreModule (Vertical Slice)

    Container_Boundary(presentation, "Presentation Layer") {
        Component(endpoints, "Endpoints", "Minimal API", "Customer CRUD endpoints")
        Component(mappers, "Mappers", "Mapster", "DTO <-> Domain mapping")
    }

    Container_Boundary(application, "Application Layer") {
        Component(commands, "Commands", "CQRS", "CustomerCreate, CustomerUpdate")
        Component(queries, "Queries", "CQRS", "CustomerFindAll, CustomerFindOne")
        Component(handlers, "Handlers", "Mediator Pattern", "Command/Query processors")
        Component(validators, "Validators", "FluentValidation", "Input validation")
        Component(behaviors, "Pipeline Behaviors", "Cross-cutting", "Validation, Retry, Timeout")
        Component(jobs, "Background Jobs", "Quartz", "CustomerExportJob")
    }

    Container_Boundary(domain, "Domain Layer") {
        Component(aggregates, "Aggregates", "Customer", "Domain entities with business logic")
        Component(valueobjects, "Value Objects", "EmailAddress", "Immutable domain values")
        Component(events, "Domain Events", "CustomerCreatedEvent", "State change notifications")
        Component(rules, "Business Rules", "Domain Rules", "Invariants and validations")
        Component(specs, "Specifications", "Query Expressions", "Domain query logic")
    }

    Container_Boundary(infrastructure, "Infrastructure Layer") {
        Component(dbcontext, "DbContext", "EF Core", "Database access")
        Component(repositories, "Repositories", "Repository Pattern", "Data access abstraction")
        Component(scheduler, "Job Scheduler", "Quartz.NET", "Background job scheduling")
        Component(configurations, "EF Configurations", "Fluent API", "Entity mappings")
    }

    ContainerDb(db, "SQL Server", "Database")

    Rel(endpoints, commands, "Creates", "Requester")
    Rel(endpoints, queries, "Invokes", "Requester")
    Rel(endpoints, mappers, "Uses")

    Rel(commands, handlers, "Processed by")
    Rel(queries, handlers, "Processed by")
    Rel(handlers, behaviors, "Passes through")
    Rel(validators, commands, "Validates")

    Rel(handlers, aggregates, "Uses", "Factory methods")
    Rel(handlers, repositories, "Calls")
    Rel(handlers, rules, "Enforces")
    Rel(handlers, specs, "Uses")

    Rel(jobs, aggregates, "Uses")
    Rel(jobs, repositories, "Calls")
    Rel(jobs, specs, "Uses")

    Rel(aggregates, valueobjects, "Contains")
    Rel(aggregates, events, "Raises")

    Rel(repositories, dbcontext, "Uses")
    Rel(dbcontext, configurations, "Configured by")
    Rel(dbcontext, db, "Queries", "SQL")
    Rel(scheduler, jobs, "Triggers")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="2")
```

---

## Architecture Diagrams

### 4. Clean Architecture Layers

Onion Architecture showing dependency direction (inward only).

```mermaid
flowchart TD
    subgraph outer["🌐 Presentation Layer"]
        endpoints[Web API Endpoints<br/>Minimal API Routes]
        dtos[DTOs & Models<br/>Request/Response]
    end

    subgraph application["📋 Application Layer"]
        commands[Commands & Queries<br/>CQRS Requests]
        handlers[Request Handlers<br/>Business Orchestration]
        behaviors[Pipeline Behaviors<br/>Validation, Retry, Timeout]
        specs[Specifications<br/>Query Logic]
    end

    subgraph domain["🎯 Domain Layer<br/><b>CORE - No Dependencies</b>"]
        aggregates[Aggregates<br/>Customer, Order]
        valueobjects[Value Objects<br/>EmailAddress, Money]
        events[Domain Events<br/>CustomerCreated]
        rules[Business Rules<br/>Domain Invariants]
        enums[Enumerations<br/>CustomerStatus]
    end

    subgraph infrastructure["🔧 Infrastructure Layer"]
        dbcontext[EF Core DbContext<br/>Database Access]
        repos[Repositories<br/>Data Operations]
        repobehaviors[Repository Behaviors<br/>Logging, Audit, Events]
        jobs[Background Jobs<br/>Quartz Scheduling]
        migrations[EF Migrations<br/>Schema Evolution]
    end

    subgraph external["🗄️ External Systems"]
        database[(SQL Server<br/>Persistence)]
        logging[Seq<br/>Structured Logs]
    end

    endpoints --> commands
    endpoints --> queries
    endpoints --> dtos

    commands --> handlers
    queries --> handlers
    handlers --> behaviors
    handlers --> specs
    handlers --> aggregates
    handlers --> repos

    aggregates --> valueobjects
    aggregates --> events
    aggregates --> rules
    aggregates --> enums

    repos --> repobehaviors
    repos --> dbcontext
    repos --> specs
    dbcontext --> migrations
    jobs --> repos

    dbcontext --> database
    handlers -.logs.-> logging
    repos -.logs.-> logging

    classDef domainStyle fill:#FFE5B4,stroke:#FF8C00,stroke-width:3px,color:#000
    classDef appStyle fill:#B4D7FF,stroke:#1E90FF,stroke-width:2px,color:#000
    classDef infraStyle fill:#D4F1D4,stroke:#32CD32,stroke-width:2px,color:#000
    classDef presentationStyle fill:#E6E6FA,stroke:#9370DB,stroke-width:2px,color:#000

    class aggregates,valueobjects,events,rules,enums domainStyle
    class commands,queries,handlers,behaviors,specs appStyle
    class dbcontext,repos,repobehaviors,jobs,migrations infraStyle
    class endpoints,dtos presentationStyle
```

### 5. Module Structure

Modular monolith organization with vertical slices.

```mermaid
flowchart TB
    subgraph host["🚀 Host (Presentation.Web.Server)"]
        program[Program.cs<br/>Composition Root]
        middleware[Middleware Pipeline<br/>Logging, CORS, Auth, Swagger]
        startup[Startup Configuration<br/>DI Container Setup]
    end

    subgraph coremodule["📦 CoreModule (Vertical Slice)"]
        direction TB

        subgraph corepresentation["Presentation"]
            coreendpoints[Customer Endpoints]
            coremappers[Mapster Profiles]
            coremoduleconfig[Module Registration]
        end

        subgraph coreapp["Application"]
            corecmds[Commands<br/>Create, Update, Delete]
            corequeries[Queries<br/>FindAll, FindOne]
            corehandlers[Handlers]
            corevalidators[Validators]
        end

        subgraph coredomain["Domain"]
            customer[Customer Aggregate]
            email[EmailAddress VO]
            status[CustomerStatus Enum]
            customerevents[Domain Events]
        end

        subgraph coreinfra["Infrastructure"]
            coredbcontext[CoreDbContext]
            corerepos[Customer Repository]
            corejobs[Export Job]
            coremigrations[Migrations]
        end
    end

    subgraph futuremodule["📦 Future Module Example"]
        futureplaceholder[Order Module<br/>Product Module<br/>etc.]
    end

    subgraph shared["🔗 Shared/Common"]
        devkit[bITdevKit<br/>Abstractions & Utilities]
        crosscutting[Cross-Cutting Concerns<br/>Logging, Caching, etc.]
    end

    program --> coremoduleconfig
    program --> middleware
    middleware --> coreendpoints

    coreendpoints --> corecmds
    coreendpoints --> corequeries
    corecmds --> corehandlers
    corequeries --> corehandlers
    corehandlers --> customer
    corehandlers --> corerepos
    corerepos --> coredbcontext
    customer --> email
    customer --> status
    customer --> customerevents

    coremoduleconfig -.registers.-> devkit
    futuremodule -.future.-> program

    crosscutting -.used by.-> coremodule

    classDef hostStyle fill:#FFE4E1,stroke:#DC143C,stroke-width:2px
    classDef moduleStyle fill:#F0F8FF,stroke:#4682B4,stroke-width:2px
    classDef sharedStyle fill:#F5F5DC,stroke:#DAA520,stroke-width:2px

    class host hostStyle
    class coremodule,futuremodule moduleStyle
    class shared sharedStyle
```

---

## Interaction Diagrams

### 6. Sequence Diagram - Customer Creation

Complete flow from HTTP request to database persistence.

```mermaid
sequenceDiagram
    actor User
    participant API as Web API<br/>(Presentation)
    participant Endpoint as Customer Endpoint
    participant Requester as Requester<br/>(Mediator)
    participant Behaviors as Pipeline Behaviors
    participant Handler as CommandHandler<br/>(Application)
    participant Customer as Customer<br/>(Domain)
    participant Repo as Repository<br/>(Infrastructure)
    participant RepoBehaviors as Repository Behaviors
    participant DB as SQL Server

    User->>API: POST /api/core/customers
    activate API

    API->>Endpoint: Route to endpoint
    activate Endpoint

    Endpoint->>Endpoint: Map DTO to Command
    Endpoint->>Requester: Send CustomerCreateCommand
    activate Requester

    Requester->>Behaviors: ModuleScopeBehavior
    activate Behaviors
    Behaviors->>Behaviors: Set Module Context
    Behaviors->>Behaviors: ValidationBehavior

    alt Validation Fails
        Behaviors-->>Requester: Result.Failure (ValidationErrors)
        Requester-->>Endpoint: ValidationError
        Endpoint-->>API: 400 Bad Request
        API-->>User: Error Response
    else Validation Succeeds
        Behaviors->>Behaviors: RetryBehavior
        Behaviors->>Behaviors: TimeoutBehavior
        Behaviors->>Handler: Execute Command
        deactivate Behaviors

        activate Handler
        Handler->>Customer: Create(name, email)
        activate Customer

        Customer->>Customer: Validate Invariants
        Customer->>Customer: Generate CustomerId
        Customer->>Customer: Create EmailAddress VO
        Customer->>Customer: Register Domain Event
        Customer-->>Handler: Customer instance
        deactivate Customer

        Handler->>Repo: AddAsync(customer)
        activate Repo

        Repo->>RepoBehaviors: LoggingBehavior
        activate RepoBehaviors
        RepoBehaviors->>RepoBehaviors: Log operation
        RepoBehaviors->>RepoBehaviors: AuditStateBehavior
        RepoBehaviors->>RepoBehaviors: Set Created/Updated metadata
        RepoBehaviors->>RepoBehaviors: DomainEventPublishingBehavior
        RepoBehaviors->>DB: INSERT INTO Customers
        activate DB
        DB-->>RepoBehaviors: Row inserted
        deactivate DB

        RepoBehaviors->>RepoBehaviors: Publish CustomerCreated event
        RepoBehaviors-->>Repo: Success
        deactivate RepoBehaviors
        Repo-->>Handler: Customer with Id
        deactivate Repo

        Handler-->>Requester: Result.Success(customer)
        deactivate Handler
        Requester-->>Endpoint: Result<Customer>
        deactivate Requester

        Endpoint->>Endpoint: Map to ResponseModel
        Endpoint-->>API: 201 Created + Location
        deactivate Endpoint
        API-->>User: Success Response
        deactivate API
    end

    Note over User,DB: Full request-response cycle with<br/>validation, domain logic, and persistence
```

### 7. Sequence Diagram - Domain Event Publishing

How domain events flow through the system.

```mermaid
sequenceDiagram
    participant Handler as Command Handler
    participant Customer as Customer Aggregate
    participant Repo as Repository
    participant RepoBehavior as DomainEvent Behavior
    participant Notifier as Notifier<br/>(Event Bus)
    participant EventHandler as Domain Event Handler
    participant Logger as ILogger

    Handler->>Customer: Create/Update operation
    activate Customer
    Customer->>Customer: Business logic
    Customer->>Customer: DomainEvents.Register(event)
    Customer-->>Handler: Modified aggregate
    deactivate Customer

    Handler->>Repo: SaveChanges()
    activate Repo

    Repo->>RepoBehavior: Before SaveChanges
    activate RepoBehavior

    RepoBehavior->>RepoBehavior: Collect domain events
    RepoBehavior->>Repo: Persist changes to DB
    Repo->>Repo: EF Core SaveChangesAsync

    Note over RepoBehavior: Transaction committed

    RepoBehavior->>Notifier: Publish(CustomerCreatedEvent)
    activate Notifier

    Notifier->>EventHandler: Handle(CustomerCreatedEvent)
    activate EventHandler
    EventHandler->>Logger: Log event
    EventHandler->>EventHandler: Execute side effects
    EventHandler-->>Notifier: Complete
    deactivate EventHandler

    Notifier-->>RepoBehavior: Events published
    deactivate Notifier

    RepoBehavior->>Customer: Clear domain events
    RepoBehavior-->>Repo: Complete
    deactivate RepoBehavior

    Repo-->>Handler: Success
    deactivate Repo

    Note over Handler,Logger: Events published after successful<br/>transaction commit (Outbox pattern)
```

---

## Data Model

### 8. Entity Relationship Diagram

Current domain model for CoreModule.

```mermaid
erDiagram
    CUSTOMER ||--o{ DOMAIN_EVENT : triggers

    CUSTOMER {
        uniqueidentifier Id PK "Typed CustomerId"
        nvarchar(256) Name "Customer full name"
        nvarchar(256) Email UK "EmailAddress value object"
        nvarchar(50) Status "CustomerStatus enumeration"
        nvarchar(64) CustomerNumber UK "Unique customer number"
        datetime2 CreatedDate "Audit: Created timestamp"
        nvarchar(256) CreatedBy "Audit: Creator identifier"
        datetime2 UpdatedDate "Audit: Last update timestamp"
        nvarchar(256) UpdatedBy "Audit: Last updater"
    }

    DOMAIN_EVENT {
        uniqueidentifier Id PK
        nvarchar(256) EventType "Discriminator for event types"
        uniqueidentifier AggregateId FK "Reference to aggregate"
        nvarchar(max) EventData "Serialized event payload (JSON)"
        datetime2 OccurredAt "Event timestamp"
        bit IsPublished "Outbox pattern flag"
        datetime2 PublishedAt "Publication timestamp"
    }

    OUTBOX_MESSAGE {
        uniqueidentifier Id PK
        nvarchar(256) Type "Message type"
        nvarchar(max) Content "Serialized message (JSON)"
        datetime2 CreatedAt
        datetime2 ProcessedAt
        int RetryCount
        nvarchar(max) ErrorMessage
    }

    DOMAIN_EVENT ||--o| OUTBOX_MESSAGE : "may create"
```

**Notes:**
- Customer uses typed ID (CustomerId) via source generator
- EmailAddress is a value object but stored as string
- CustomerStatus is an enumeration stored as string
- Domain events support outbox pattern for reliability
- Audit fields populated by AuditStateBehavior

---

## Request Processing

### 9. Request Pipeline Flow

How requests flow through pipeline behaviors before reaching handlers.

```mermaid
flowchart TD
    Start([HTTP Request]) --> Endpoint[Minimal API Endpoint]
    Endpoint --> MapDTO[Map DTO to Command/Query]
    MapDTO --> SendRequest[Requester.SendAsync]

    SendRequest --> ModuleScope[ModuleScopeBehavior]
    ModuleScope --> SetContext[Set Module Context]

    SetContext --> Validation[ValidationPipelineBehavior]
    Validation --> ValidateCheck{Valid?}
    ValidateCheck -->|No| ValidationErrors[Collect Validation Errors]
    ValidationErrors --> FailureResult1[Return Result.Failure]

    ValidateCheck -->|Yes| Retry[RetryPipelineBehavior]
    Retry --> RetryConfig[Check Retry Attributes]

    RetryConfig --> Timeout[TimeoutPipelineBehavior]
    Timeout --> TimeoutConfig[Check Timeout Attributes]

    TimeoutConfig --> Handler[Command/Query Handler]
    Handler --> Execute[Execute Business Logic]

    Execute --> CheckResult{Success?}
    CheckResult -->|Exception| RetryCheck{Retry?}
    RetryCheck -->|Yes| RetryDelay[Wait & Retry]
    RetryDelay --> Handler
    RetryCheck -->|No| FailureResult2[Return Result.Failure]

    CheckResult -->|Timeout| TimeoutError[Return Timeout Error]

    CheckResult -->|Success| Domain[Domain Operations]
    Domain --> Persistence[Repository Operations]
    Persistence --> SuccessResult[Return Result.Success]

    FailureResult1 --> MapResponse
    FailureResult2 --> MapResponse
    TimeoutError --> MapResponse
    SuccessResult --> MapResponse[Map to Response DTO]

    MapResponse --> HTTPResponse{Result Type?}
    HTTPResponse -->|Success| HTTP200[200 OK / 201 Created]
    HTTPResponse -->|Validation| HTTP400[400 Bad Request]
    HTTPResponse -->|Not Found| HTTP404[404 Not Found]
    HTTPResponse -->|Error| HTTP500[500 Server Error]

    HTTP200 --> End([Return to Client])
    HTTP400 --> End
    HTTP404 --> End
    HTTP500 --> End

    classDef behaviorStyle fill:#B4E7FF,stroke:#0077BE,stroke-width:2px
    classDef errorStyle fill:#FFB4B4,stroke:#DC143C,stroke-width:2px
    classDef successStyle fill:#B4FFB4,stroke:#228B22,stroke-width:2px
    classDef handlerStyle fill:#FFE4B4,stroke:#FF8C00,stroke-width:2px

    class ModuleScope,Validation,Retry,Timeout behaviorStyle
    class ValidationErrors,FailureResult1,FailureResult2,TimeoutError,HTTP400,HTTP404,HTTP500 errorStyle
    class SuccessResult,HTTP200 successStyle
    class Handler,Execute handlerStyle
```

---

## Diagram Usage

These diagrams serve different purposes:

1. **C4 Diagrams (1-3)**: For architectural understanding and stakeholder communication
2. **Architecture Diagrams (4-5)**: For understanding layer boundaries and module organization
3. **Sequence Diagrams (6-7)**: For understanding runtime behavior and request flows
4. **ERD (8)**: For database schema understanding and domain model relationships
5. **Flow Diagram (9)**: For understanding request processing pipeline

## Related Documentation

- [Clean/Onion Architecture ADR](./ADR/0001-clean-onion-architecture.md)
- [Modular Monolith ADR](./ADR/0003-modular-monolith-architecture.md)
- [Requester/Notifier Pattern ADR](./ADR/0005-requester-notifier-mediator-pattern.md)
- [Domain Events Outbox Pattern ADR](./ADR/0006-outbox-pattern-domain-events.md)
- [Repository Behaviors ADR](./ADR/0004-repository-decorator-behaviors.md)

## Updating These Diagrams

When the architecture changes:

1. Update relevant diagram(s) in this file
2. Test rendering in your markdown viewer
3. Commit diagram source (not images) with code changes
4. Reference in pull request description
5. Update related ADRs if architectural decisions change

---

*Generated: February 3, 2026*
*Project: bITdevKit GettingStarted Example*
*Architecture: Clean/Onion with Modular Monolith*
