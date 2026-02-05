# System Design Diagrams - bITdevKit GettingStarted

Comprehensive architecture and design diagrams for the bITdevKit GettingStarted Example project.

## Table of Contents

- [System Design Diagrams - bITdevKit GettingStarted](#system-design-diagrams---bitdevkit-gettingstarted)
  - [Table of Contents](#table-of-contents)
  - [Architecture Overview](#architecture-overview)
    - [Architecture Overview Diagram](#architecture-overview-diagram)
  - [Domain Model and Boundaries](#domain-model-and-boundaries)
  - [Module Architecture](#module-architecture)
  - [Layer Responsibilities and Dependencies](#layer-responsibilities-and-dependencies)
    - [Layer Responsibilities](#layer-responsibilities)
    - [Dependency Rules](#dependency-rules)
  - [Inter-Module Communication](#inter-module-communication)
  - [Domain Events and Messages](#domain-events-and-messages)
  - [Architectural Patterns](#architectural-patterns)
    - [Architectural Patterns Overview Diagram](#architectural-patterns-overview-diagram)
    - [Modular Monolith and Bounded Context Modules](#modular-monolith-and-bounded-context-modules)
    - [Domain Events (In-Process)](#domain-events-in-process)
    - [Messaging (Messages and Broker)](#messaging-messages-and-broker)
    - [Contracts API (Explicit Module Interfaces)](#contracts-api-explicit-module-interfaces)
    - [Outbox Pattern](#outbox-pattern)
    - [Vertical Slice / Package-by-Feature](#vertical-slice--package-by-feature)
    - [Rich Domain Model](#rich-domain-model)
    - [Behaviors (Decorators)](#behaviors-decorators)
    - [Database-per-Module (Logical Ownership)](#database-per-module-logical-ownership)
    - [Background Jobs and Startup Tasks](#background-jobs-and-startup-tasks)
    - [Architectural Tests (Fitness Functions)](#architectural-tests-fitness-functions)
    - [Result Pattern](#result-pattern)
    - [Requester/Notifier Pattern](#requesternotifier-pattern)
    - [Repository Pattern](#repository-pattern)
  - [Architecture Governance (ADRs)](#architecture-governance-adrs)
  - [Building Blocks and Context](#building-blocks-and-context)
    - [1. System Context Diagram](#1-system-context-diagram)
    - [2. Container Diagram](#2-container-diagram)
    - [3. Component Diagram - CoreModule](#3-component-diagram---coremodule)
  - [Architecture Diagrams](#architecture-diagrams)
    - [4. Clean Architecture Overview](#4-clean-architecture-overview)
    - [5. Clean Architecture Layers](#5-clean-architecture-layers)
    - [6. Module Structure](#6-module-structure)
  - [Interaction Diagrams](#interaction-diagrams)
    - [6. Sequence Diagram - Customer Creation](#6-sequence-diagram---customer-creation)
    - [7. Sequence Diagram - Domain Event Dispatching](#7-sequence-diagram---domain-event-dispatching)
  - [Data Model](#data-model)
    - [8. Entity Relationship Diagram](#8-entity-relationship-diagram)
  - [Request Processing](#request-processing)
    - [9. Request Pipeline Flow](#9-request-pipeline-flow)
  - [Diagram Usage](#diagram-usage)
  - [Related Documentation](#related-documentation)
  - [Updating These Diagrams](#updating-these-diagrams)

---

## Architecture Overview

This document captures the architectural intent of a modular, domain-driven system using Clean/Onion principles. The goal is to keep domain logic independent from delivery mechanisms and infrastructure while enabling independent evolution of modules. Architecture decisions prioritize clear boundaries, explicit dependencies, and predictable flows for both synchronous and asynchronous communication.

Key principles:

- Domain logic is isolated from infrastructure and delivery concerns.
- Modules are autonomous vertical slices with their own domain, application, infrastructure, and presentation layers.
- Dependency direction is strictly inward toward the domain core.
- Communication across modules is explicit and intentional.

### Architecture Overview Diagram

The following overview captures the layered flow from transport to domain and persistence, highlighting how presentation, application, domain, and infrastructure collaborate.

```mermaid
graph TB
    Client([HTTP Client]) --> Endpoints

    subgraph Presentation["Presentation Layer (Outer)"]
        Endpoints[API Endpoints]
        DTOs[Request/Response DTOs]
    end

    subgraph Application["Application Layer"]
        Requester[IRequester<br/>Mediator]
        CMD[Commands & Queries]
        BEHAV[Pipeline Behaviors<br/>Validation, Retry, Timeout]
        HAND[Handlers<br/>Business Orchestration]
        Jobs[Background Jobs]
    end

    subgraph Domain["Domain Layer (Inner Core)"]
        AGG[Aggregates]
        VO[Value Objects]
        EVENTS[Domain Events]
        RULES[Business Rules]
        SPECS[Specifications]
    end

    subgraph Infrastructure["Infrastructure Layer (Outer)"]
        Repos[Repositories]
        DB[(Relational Database)]
        Scheduler[Job Scheduler]
    end

    %% Request Flow
    Endpoints --> DTOs
    DTOs --> Requester
    Requester --> BEHAV
    BEHAV --> CMD
    CMD --> HAND

    %% Handler to Domain
    HAND --> AGG
    HAND --> RULES
    HAND --> SPECS

    %% Jobs to Domain & Infrastructure
    Jobs --> AGG
    Jobs --> Repos
    Jobs --> SPECS

    %% Domain Internal
    AGG --> VO
    AGG --> EVENTS

    %% Persistence Flow
    HAND --> Repos
    Repos --> DB
    Scheduler -.triggers.-> Jobs

    %% Styling
    style Domain fill:#E8F5E9,stroke:#4CAF50,stroke-width:3px
    style AGG fill:#66BB6A,color:#fff
    style VO fill:#66BB6A,color:#fff
    style EVENTS fill:#66BB6A,color:#fff
    style RULES fill:#66BB6A,color:#fff
    style SPECS fill:#66BB6A,color:#fff
    style Application fill:#E3F2FD,stroke:#2196F3,stroke-width:2px
    style Presentation fill:#F3E5F5,stroke:#9C27B0,stroke-width:2px
    style Infrastructure fill:#FFF3E0,stroke:#FF9800,stroke-width:2px
```

## Domain Model and Boundaries

Domain-driven design (DDD) focuses on modeling the core business domain with explicit boundaries. Each module represents a bounded context with its own ubiquitous language, invariants, and lifecycle.

- Aggregates encapsulate business rules and transactional consistency.
- Value objects capture immutable concepts and validation rules.
- Domain events represent significant state changes inside a module.
- Business rules enforce invariants at the domain boundary.
Domain events remain internal to their owning module. They are part of the domain model and are not used as a direct inter-module contract. When a module needs to communicate changes to other modules, it publishes messages through asynchronous channels (see Inter-Module Communication).

## Module Architecture

Modules are organized as vertical slices with full layering inside each module:

- Presentation: incoming requests and transport concerns.
- Application: use-case orchestration, command/query processing.
- Domain: aggregates, value objects, domain events, rules.
- Infrastructure: persistence and external system integrations.

This structure supports local reasoning, independent evolution, and consistent testing practices per module. Cross-module dependencies are minimized and made explicit.

## Layer Responsibilities and Dependencies

The architecture uses layered responsibilities to keep business logic stable and infrastructure replaceable:

### Layer Responsibilities

- Presentation: transport, request/response mapping, and API contracts.
- Application: use-case orchestration, commands/queries, and workflow coordination.
- Domain: business rules, aggregates, value objects, and domain events.
- Infrastructure: persistence and external system integrations.

### Dependency Rules

Dependencies flow inward toward the domain core:

- Domain has no dependencies on other layers.
- Application depends on Domain only.
- Infrastructure depends on Domain and Application.
- Presentation depends on Application.

This ensures business logic remains independent of delivery and infrastructure concerns.

## Inter-Module Communication

Modules can communicate in two primary ways, each with a clear intent and trade-offs:

- Synchronous (Contracts API): direct calls using stable contracts. This is appropriate for read-after-write scenarios, user-facing interactions, or strict consistency requirements.
- Asynchronous (Message Broker): publish/subscribe communication via messages. The message broker transports and dispatches messages to subscribers. This is appropriate for decoupling modules, reducing temporal coupling, and supporting eventual consistency.

Synchronous communication should remain limited to a well-defined Contracts API. Asynchronous communication should be used when domain autonomy and scalability are more important than immediate consistency.

## Domain Events and Messages

Domain events and messages are related but separate concepts:

- Domain events are internal signals produced by aggregates to indicate meaningful changes. They live in the domain model and enable intra-module workflows.
- Messages are external-facing contracts used for asynchronous communication between modules. They live at the application boundary (or an explicit messaging boundary) and are transported through a message broker.

Domain events may lead to messages, but they are not the same artifact and they are not handled through the same pipeline. Domain events express domain intent; messages express inter-module communication intent.

This separation ensures that internal domain evolution does not force downstream consumers to change, while still enabling reactive, event-driven workflows.

## Architectural Patterns

The architecture leverages proven patterns that support modularity, maintainability, and clear module boundaries. The patterns listed here capture both what is commonly implemented in this solution today and what the architecture is designed to support as the system grows.

### Architectural Patterns Overview Diagram

The following diagram shows how the patterns relate at a system level: bounded contexts (modules) encapsulate a rich domain model, may expose explicit synchronous contracts when needed, and communicate asynchronously via messages. Domain events remain internal to a module and can be processed reliably via a domain-event outbox. Message publishing can also use a message outbox for durability when delivering to a broker. Cross-cutting policies are applied consistently through mediator pipelines, and architectural tests protect boundaries.

- Modular monolith with bounded contexts (modules): strong boundaries, explicit dependencies, and local autonomy.
- Vertical slice / package-by-feature: use-cases modeled explicitly and grouped by intent.
- Rich domain model: aggregates and value objects enforce invariants inside the domain boundary.
- Request/notification dispatch (Mediator): decouples senders from handlers and enables a consistent pipeline.
- Pipeline behaviors (decorators): cross-cutting policies applied consistently around use-cases.
- Repository pattern with behaviors: persistence behind a domain-oriented abstraction with cross-cutting behaviors.
- Domain events: internal business signals for intra-module workflows.
- Messages: external-facing contracts for asynchronous module communication.
- Contracts API: explicit module interfaces for synchronous module-to-module calls.
- Outbox pattern: reliable processing of side effects after successful state changes using an outbox store and a worker/processor.
- Database-per-module (logical ownership): each module owns its persistence boundary.
- Background jobs and startup tasks: module-owned, scheduled/bootstrapping workflows.
- Architectural tests (fitness functions): automated checks that protect boundaries and dependency rules.

This architecture follows CQS (Command Query Separation) rather than a traditional CQRS approach. Commands and queries are modeled separately to clarify intent and processing flow, but they share the same underlying data store. The system does not aim to maintain separate read/write models or independent query storage.

### Modular Monolith and Bounded Context Modules

Modules represent bounded contexts with their own domain model and lifecycle. Each module is designed to be locally coherent and independently evolvable while still shipping as part of a single deployable unit.

Key constraint: modules communicate through explicit mechanisms (contracts or messages) rather than arbitrary cross-module references.

```mermaid
flowchart TB
    Host[Single Deployable Host]

    subgraph ModuleA["Module A - Bounded Context"]
        A_Domain[Domain Model]
        A_App[Application - Use-Cases]
        A_Infra[Infrastructure]
        A_Pres[Presentation]
    end

    subgraph ModuleB["Module B - Bounded Context"]
        B_Domain[Domain Model]
        B_App[Application - Use-Cases]
        B_Infra[Infrastructure]
        B_Pres[Presentation]
    end

    Host --> ModuleA
    Host --> ModuleB

    ModuleA -. explicit dependencies only .-> ModuleB
```

### Domain Events (In-Process)

Domain events capture meaningful state changes inside a bounded context. They are raised from aggregates and processed within the same module to trigger internal workflows.

Optionally, a domain-event outbox can be added to ensure domain events are dispatched only after successful persistence (and can be retried if dispatch fails).

```mermaid
flowchart LR
    Agg[Aggregate] --> DE[Domain Event]
    DE --> H1[Handler A]
    DE --> H2[Handler B]
```

### Messaging (Messages and Broker)

Messaging is a separate concern from domain events. Messages are application-boundary artifacts used for asynchronous communication between modules via a message broker.

Messages may be produced from application workflows or as a deliberate translation step from domain events.

Optionally, a message outbox can be added to make publishing to the broker more reliable (store the message as part of the state change and publish it asynchronously).

```mermaid
flowchart LR
    subgraph Producer["Producing Module"]
        Source[Use-Case Handler] --> Msg[Message]
    end

    Msg -->|publish| Broker[Message Broker]

    subgraph Consumer["Consuming Module"]
        Handler[Message Handler]
        Handler --> UseCase[Use-Case Handler]
    end

    Broker -->|subscribed consumer| Handler
```

### Contracts API (Explicit Module Interfaces)

Synchronous module interactions are modeled through explicit contracts. This prevents accidental coupling and provides a clear, versionable interface between bounded contexts.

In practice, these contracts are typically owned by the providing module (for example as a dedicated Contracts package/project) and referenced by consuming modules.

```mermaid
flowchart LR
    subgraph Provider["Providing Module"]
        Contracts[Contracts Project]
        Capability[Use-Cases / Capabilities]
        Contracts --> Capability
    end

    subgraph Consumer["Consuming Module"]
        ConsumerCode[Use-Case / Handler]
    end

    ConsumerCode -->|references| Contracts
    ConsumerCode -->|synchronous call| Capability
```

### Outbox Pattern

The outbox pattern ensures reliable handling of “something that must happen because state changed” (a side effect) without trying to perform the side effect inside the same request/transaction.

At a high level:

- In the same transaction as the domain state change, the application writes an outbox record describing the pending work.
- A separate worker/processor reads pending outbox records and performs the side effect.
- After successful handling, the worker marks the outbox record as processed.

This improves consistency because the state change and the creation of the outbox record are atomic, and the side effect can be retried independently.

Note: Outbox processing is typically at-least-once. Consumers/handlers should be idempotent.

```mermaid
sequenceDiagram
    participant UC as Use-Case Handler
    participant Repo as Repository / Unit of Work
    participant DB as Store
    participant Outbox as Outbox
    participant Worker as Outbox Worker
    participant Effect as Handler

    UC->>Repo: Persist state change
    Repo->>DB: Write domain state
    Repo->>Outbox: Write outbox record
    Note over DB,Outbox: Same transaction boundary

    Worker->>Outbox: Read pending records
    Worker->>Effect: Handle
    Effect-->>Worker: Success
    Worker->>Outbox: Mark processed
```

### Vertical Slice / Package-by-Feature

Use-cases are modeled explicitly (commands/queries + handlers) and grouped by intent, enabling local reasoning and limiting cross-feature coupling.

```mermaid
flowchart TB
    subgraph Slice["Vertical Slice (Use-Case)"]
        Endpoint[Endpoint / Adapter]
        Request[Command or Query]
        Validator[Input Validation]
        Handler[Use-Case Handler]
        Domain[Domain Model]
        Repo[Repository]
    end

    Endpoint --> Request
    Request --> Validator --> Handler
    Handler --> Domain
    Handler --> Repo
```

### Rich Domain Model

Business rules and invariants live in the domain model (aggregates/value objects/rules), not in endpoints or procedural scripts.

```mermaid
classDiagram
    class AggregateRoot {
      +enforceInvariants()
      +registerDomainEvents()
    }
    class Entity
    class ValueObject
    class DomainRule
    class DomainEvent

    AggregateRoot "1" o-- "*" Entity : contains
    AggregateRoot "1" o-- "*" ValueObject : uses
    AggregateRoot "1" --> "*" DomainRule : enforces
    AggregateRoot "1" --> "*" DomainEvent : raises
```

### Behaviors (Decorators)

Cross-cutting policies are applied consistently around use-cases via a behavior chain, keeping handlers focused on orchestration and domain interaction.

This is used for concerns like validation, retry, timeout, logging, and similar policies that should apply consistently across use-cases. The decorator idea can also be applied around persistence (repository behaviors) and around request dispatching (Requester/Notifier behaviors).

```mermaid
flowchart LR
    Req[Request] --> B1[Module Scope]
    B1 --> B2[Validation]
    B2 --> B3[Retry]
    B3 --> B4[Timeout]
    B4 --> H[Handler]
    H --> Res[Result]
```

### Database-per-Module (Logical Ownership)

Each bounded context owns its persistence boundary. Other modules do not directly access its data store; they collaborate through contracts or messages.

```mermaid
flowchart TB
    subgraph M1["Module A"]
        A_App[Use-Cases]
        A_DB[(A Data Store)]
        A_App --> A_DB
    end

    subgraph M2["Module B"]
        B_App[Use-Cases]
        B_DB[(B Data Store)]
        B_App --> B_DB
    end

    M2 -. no direct DB access .-> A_DB
    M1 -. collaborates via contracts/messages .-> M2
```

### Background Jobs and Startup Tasks

Time-triggered and bootstrapping workflows are treated as module-owned use-cases, applying the same domain and persistence boundaries as request-driven flows.

```mermaid
flowchart TB
    subgraph Runtime["Runtime"]
        Startup[Startup]
        Scheduler[Scheduler]
    end

    subgraph Module["Module"]
        Task[Startup Task]
        Job[Background Job]
        Handler[Use-Case Handler]
        Domain[Domain Model]
        Repo[Repository]
        DB[(Store)]
    end

    Startup --> Task
    Scheduler --> Job
    Task --> Handler
    Job --> Handler
    Handler --> Domain
    Handler --> Repo
    Repo --> DB
```

### Architectural Tests (Fitness Functions)

Architectural tests act as automated fitness functions to continuously validate dependency direction, layer boundaries, and m

```mermaid
flowchart LR
    Tests[Architectural Tests] -. enforce .-> Layers[Layer Dependency Rules]
    Tests -. enforce .-> Boundaries[Module Boundaries]
    Tests -. enforce .-> Coupling[Allowed Coupling Rules]

    Layers --> Outcome[Architectural Integrity]
    Boundaries --> Outcome
    Coupling --> Outcome
```

### Result Pattern

The Result pattern makes success and failure explicit, enabling composable workflows and eliminating exception-driven control flow.

```mermaid
graph LR
    Start([Start]) --> Step1{Step 1<br/>Validation}
    Step1 -->|Success| Step2{Step 2<br/>Business Rule}
    Step1 -->|Failure| Failure([Failure Path])
    Step2 -->|Success| Step3{Step 3<br/>Persistence}
    Step2 -->|Failure| Failure
    Step3 -->|Success| Step4[Step 4<br/>Mapping]
    Step3 -->|Failure| Failure
    Step4 --> Success([Success Path])

    style Success fill:#4CAF50
    style Failure fill:#f44336
```

### Requester/Notifier Pattern

The Requester/Notifier (Mediator) pattern decouples senders from handlers and enables cross-cutting behaviors in a consistent pipeline.

```mermaid
graph TB
    subgraph "Client Code (Endpoint)"
        Client[Endpoint]
    end

    subgraph "Mediator (IRequester)"
        Req[IRequester.SendAsync]
        Pipeline[Pipeline Behaviors]
    end

    subgraph "Handler Layer"
        HandlerNode[Command/Query Handler]
    end

    subgraph "Cross-Cutting Behaviors"
        B1[ModuleScopeBehavior]
        B2[ValidationBehavior]
        B3[RetryBehavior]
        B4[TimeoutBehavior]
    end

    Client -->|Command/Query| Req
    Req --> B1
    B1 --> B2
    B2 --> B3
    B3 --> B4
    B4 --> HandlerNode
    HandlerNode -->|Result| B4
    B4 --> B3
    B3 --> B2
    B2 --> B1
    B1 --> Req
    Req -->|Result| Client

    style HandlerNode fill:#4CAF50
    style Pipeline fill:#2196F3
```

### Repository Pattern

The repository pattern abstracts persistence behind a domain-oriented interface, keeping application and domain logic independent of storage details. It centralizes data access and enables consistent handling of cross-cutting concerns (logging, auditing, and outbox handling) through a behavior chain.

```mermaid
graph LR
    Handler[Handler] --> Tracing[TracingBehavior]
    Tracing --> Logging[LoggingBehavior]
    Logging --> Audit[AuditStateBehavior]
    Audit --> Outbox[Domain Event Outbox Behavior]
    Outbox --> Repo[Repository Implementation]
    Repo --> DB[(Database)]

    style Tracing fill:#2196F3
    style Logging fill:#2196F3
    style Audit fill:#2196F3
    style Outbox fill:#2196F3
    style Repo fill:#4CAF50
```

## Architecture Governance (ADRs)

Architectural Decision Records (ADRs) document key architectural choices, alternatives, and rationale. They serve as the authoritative source for why the system uses specific patterns, boundaries, and behaviors. Refer to the ADRs to understand the intent behind each diagram and section in this document.

In addition, the solution uses architectural tests as fitness functions. These automated checks continuously validate key constraints such as dependency direction, layer boundaries, and allowed module coupling, helping prevent architectural erosion over time.

## Building Blocks and Context

These diagrams provide a shared language for communicating architecture at different levels of detail:

- Context: align stakeholders on the system boundary and external collaborators.
- Container: explain the main runtime building blocks and how they interact.
- Component: help developers navigate responsibilities and dependencies inside a module.

They are primarily used for onboarding, architecture reviews, and as a stable reference when discussing changes to boundaries and responsibilities. The notation is based on the [C4 model](https://c4model.com/abstractions), which emphasizes simplicity and clarity while still capturing essential architectural information.

### 1. System Context Diagram

High-level view showing the system and its external actors.

```mermaid
C4Context
    title System Context - bITdevKit GettingStarted Example

    Person(api_user, "API Consumer", "Interacts with REST API")

    System(gettingstarted, "GettingStarted Application", "Modular monolith demonstrating DDD, Clean Architecture, and bITdevKit patterns")

    System_Ext(database, "Relational Database", "Relational database for persistence")
    System_Ext(seq, "Seq", "Centralized logging and diagnostics")

    Rel(api_user, gettingstarted, "Manages customers", "REST/JSON")
    Rel(gettingstarted, database, "Reads/Writes data")
    Rel(gettingstarted, seq, "Sends structured logs", "HTTP")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

This diagram frames the system boundary and its external collaborators. It is useful for understanding responsibilities and dependencies at the highest level.

### 2. Container Diagram

Shows the major containers (applications/processes) that make up the system.

```mermaid
C4Container
    title Container Diagram - GettingStarted Application

    Person(user, "API Consumer")

    Container_Boundary(app, "GettingStarted Application") {
        Container(webapi, "Web API", "HTTP API", "Exposes REST endpoints, handles HTTP requests")
        Container(modules, "Modules", "CoreModule + Others", "Business logic organized as vertical slices")
        Container(infrastructure, "Infrastructure Layer", ".NET Libraries", "Persistence, jobs, startup tasks")
    }

    ContainerDb(database, "Database", "Relational Database", "Stores customers, domain events, audit logs")
    Container_Ext(seq, "Seq Server", "Logging Platform", "Structured log aggregation")

    Rel(user, webapi, "HTTP/HTTPS", "JSON")
    Rel(webapi, modules, "Invokes", "Requester/Notifier")
    Rel(modules, infrastructure, "Uses", "Repositories, DbContext")
    Rel(infrastructure, database, "Reads/Writes")
    Rel(webapi, seq, "Logs")
    Rel(modules, seq, "Logs", "ILogger")

    UpdateLayoutConfig($c4ShapeInRow="2", $c4BoundaryInRow="1")
```

This diagram highlights the deployable/runtime units and how they collaborate, focusing on infrastructure and interaction boundaries.

### 3. Component Diagram - CoreModule

Internal structure of the CoreModule showing Clean Architecture layers.

```mermaid
C4Component
    title Component Diagram - CoreModule (Vertical Slice)

    Container_Boundary(presentation, "Presentation Layer") {
        Component(endpoints, "Endpoints", "HTTP API", "Customer CRUD endpoints")
        Component(mappers, "Mappers", "Mapping", "DTO <-> Domain mapping")
    }

    Container_Boundary(application, "Application Layer") {
        Component(commands, "Commands", "CQRS", "CustomerCreate, CustomerUpdate")
        Component(queries, "Queries", "CQRS", "CustomerFindAll, CustomerFindOne")
        Component(handlers, "Handlers", "Mediator Pattern", "Command/Query processors")
        Component(validators, "Validators", "Input validation", "Input validation")
        Component(behaviors, "Pipeline Behaviors", "Cross-cutting", "Validation, Retry, Timeout")
        Component(jobs, "Background Jobs", "Scheduling", "CustomerExportJob")
    }

    Container_Boundary(domain, "Domain Layer") {
        Component(aggregates, "Aggregates", "Customer", "Domain entities with business logic")
        Component(valueobjects, "Value Objects", "EmailAddress", "Immutable domain values")
        Component(events, "Domain Events", "CustomerCreatedEvent", "State change notifications")
        Component(rules, "Business Rules", "Domain Rules", "Invariants and validations")
        Component(specs, "Specifications", "Query Expressions", "Domain query logic")
    }

    Container_Boundary(infrastructure, "Infrastructure Layer") {
        Component(dbcontext, "DbContext", "Persistence", "Database access")
        Component(repositories, "Repositories", "Repository Pattern", "Data access abstraction")
        Component(scheduler, "Job Scheduler", "Scheduling", "Background job scheduling")
        Component(configurations, "EF Configurations", "Fluent API", "Entity mappings")
    }

    ContainerDb(db, "Relational Database", "Database")

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

This diagram zooms into a module to show how presentation, application, domain, and infrastructure components interact within a vertical slice.

---

## Architecture Diagrams

### 4. Clean Architecture Overview

High-level view of the Clean/Onion architecture and dependency direction.

```mermaid
graph TB
    subgraph "Outer Layer: Infrastructure & Presentation"
        UI[API Endpoints<br/>Presentation Layer]
        DB[Relational Database]
        EXT[External Services]
    end

    subgraph "Application Layer"
        CMD[Commands & Queries]
        HAND[Handlers]
        BEHAV[Pipeline Behaviors]
    end

    subgraph "Inner Core: Domain"
        AGG[Aggregates]
        VO[Value Objects]
        EVENTS[Domain Events]
        RULES[Business Rules]
    end

    UI --> CMD
    CMD --> HAND
    HAND --> AGG
    HAND --> DB
    AGG --> VO
    AGG --> EVENTS
    HAND --> RULES

    style AGG fill:#4CAF50
    style VO fill:#4CAF50
    style EVENTS fill:#4CAF50
    style RULES fill:#4CAF50
```

This diagram emphasizes the inner core and highlights that dependencies flow toward domain logic.

### 5. Clean Architecture Layers

Onion Architecture showing dependency direction (inward only).

```mermaid
flowchart TD
    subgraph outer["🌐 Presentation Layer"]
        endpoints[API Endpoints]
        dtos[DTOs & Models<br/>Request/Response]
    end

    subgraph application["📋 Application Layer"]
        commands[Commands & Queries]
        handlers[Request Handlers<br/>Business Orchestration]
        behaviors[Pipeline Behaviors]
        specs[Specifications]
    end

    subgraph domain["🎯 Domain Layer<br/><b>CORE - No Dependencies</b>"]
        aggregates[Aggregates<br/>Customer, Order]
        valueobjects[Value Objects<br/>EmailAddress, Money]
        events[Domain Events<br/>CustomerCreated]
        rules[Business Rules<br/>Domain Invariants]
        enums[Enumerations<br/>CustomerStatus]
    end

    subgraph infrastructure["🔧 Infrastructure Layer"]
        dbcontext[DbContext<br/>Database Access]
        repos[Repositories<br/>Data Operations]
        repobehaviors[Repository Behaviors]
        jobs[Background Jobs]
        migrations[Schema Evolution]
    end

    subgraph external["🗄️ External Systems"]
        database[(Relational Database)]
        logging[Logging Platform]
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

This diagram emphasizes dependency direction and the separation of concerns between layers.

### 6. Module Structure

Modular monolith organization with vertical slices.

```mermaid
flowchart TB
    subgraph host["🚀 Host"]
        program[Composition Root]
        middleware[Middleware Pipeline]
        startup[Startup Configuration]
    end

    subgraph coremodule["📦 CoreModule (Vertical Slice)"]
        direction TB

        subgraph corepresentation["Presentation"]
            coreendpoints[API Endpoints]
            coremappers[Mapping Profiles]
            coremoduleconfig[Module Registration]
        end

        subgraph coreapp["Application"]
            corecmds[Commands]
            corequeries[Queries]
            corehandlers[Handlers]
            corevalidators[Validation]
        end

        subgraph coredomain["Domain"]
            customer[Customer Aggregate]
            email[EmailAddress VO]
            status[CustomerStatus Enum]
            customerevents[Domain Events]
        end

        subgraph coreinfra["Infrastructure"]
            coredbcontext[DbContext]
            corerepos[Repository]
            corejobs[Background Jobs]
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

This diagram shows how modules are organized and how the host composes them into a single application.

---

## Interaction Diagrams

### 6. Sequence Diagram - Customer Creation

Complete flow from HTTP request to database persistence.

```mermaid
sequenceDiagram
    actor User
    participant API as API Gateway<br/>(Presentation)
    participant Endpoint as Customer Endpoint
    participant Requester as Requester<br/>(Mediator)
    participant Behaviors as Pipeline Behaviors
    participant Handler as CommandHandler<br/>(Application)
    participant Customer as Customer<br/>(Domain)
    participant Repo as Repository<br/>(Infrastructure)
    participant RepoBehaviors as Repository Behaviors
    participant DB as Relational Database

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

This sequence focuses on request orchestration across layers, highlighting behaviors, domain logic, and repository behaviors.

### 7. Sequence Diagram - Domain Event Dispatching

How domain events flow through the system (in-process).

```mermaid
sequenceDiagram
    participant Handler as Command Handler
    participant Customer as Customer Aggregate
    participant Repo as Repository
    participant RepoBehavior as DomainEvent Behavior
    participant Notifier as In-Process Notifier
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
    Repo->>Repo: Persist changes

    Note over RepoBehavior: Transaction committed

    RepoBehavior->>Notifier: Dispatch(CustomerCreatedEvent)
    activate Notifier

    Notifier->>EventHandler: Handle(CustomerCreatedEvent)
    activate EventHandler
    EventHandler->>Logger: Log event
    EventHandler->>EventHandler: Execute side effects
    EventHandler-->>Notifier: Complete
    deactivate EventHandler

    Notifier-->>RepoBehavior: Events dispatched
    deactivate Notifier

    RepoBehavior->>Customer: Clear domain events
    RepoBehavior-->>Repo: Complete
    deactivate RepoBehavior

    Repo-->>Handler: Success
    deactivate Repo

    Note over Handler,Logger: Events dispatched after successful<br/>transaction commit (Domain Event Outbox)
```

This sequence illustrates how domain events are collected and dispatched reliably after persistence.

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
        bit IsDispatched "Domain event outbox flag"
        datetime2 DispatchedAt "Dispatch timestamp"
    }

    MESSAGE_OUTBOX {
        uniqueidentifier Id PK
        nvarchar(256) Type "Message type"
        nvarchar(max) Content "Serialized message (JSON)"
        datetime2 CreatedAt
        datetime2 ProcessedAt
        int RetryCount
        nvarchar(max) ErrorMessage
    }

    DOMAIN_EVENT ||--o{ MESSAGE_OUTBOX : "may be translated into"
```

This diagram captures the persistence view of core domain concepts and the outbox mechanism.

**Notes:**

- Customer uses typed ID (CustomerId) via source generator
- EmailAddress is a value object but stored as string
- CustomerStatus is an enumeration stored as string
- Domain events are persisted in a domain-event outbox for reliable dispatch after persistence
- Messages may be persisted in a message outbox for reliable broker publication
- Audit fields populated by AuditStateBehavior

---

## Request Processing

### 9. Request Pipeline Flow

How requests flow through pipeline behaviors before reaching handlers.

```mermaid
flowchart TD
    Start([HTTP Request]) --> Endpoint[API Endpoint]
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

This flowchart shows the end-to-end request lifecycle, from transport to domain execution and response mapping.

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
