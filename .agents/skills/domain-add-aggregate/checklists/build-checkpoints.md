# Build Checkpoints Checklist

This checklist provides incremental validation gates to catch errors early as you build the aggregate across all layers.

**Purpose**: Validate each layer immediately after creation to prevent cascading errors  
**Strategy**: Build → Compile → Test after each major layer completion  
**Benefits**: Faster feedback, easier debugging, confidence in progress

---

## Checkpoint 1: Domain Layer Complete

### Files Created
- [ ] `[Entity].cs` (aggregate root)
- [ ] `[Entity]Id.cs` (typed entity ID) OR attribute applied to aggregate
- [ ] Value objects (e.g., `EmailAddress.cs`)
- [ ] Enumerations (e.g., `[Entity]Status.cs`)
- [ ] Domain events (e.g., `[Entity]CreatedDomainEvent.cs`, `[Entity]UpdatedDomainEvent.cs`, `[Entity]DeletedDomainEvent.cs`)

### Build Validation
- [ ] Run build: `dotnet build src/Modules/[Module]/[Module].Domain`
- [ ] Build succeeds with no errors
- [ ] No compiler warnings related to domain code
- [ ] TypedEntityId source generator runs successfully (if using attribute)

### Code Validation
- [ ] Aggregate factory method compiles and returns `Result<[Entity]>`
- [ ] Value object factory methods compile and return `Result<T>`
- [ ] Enumeration static instances accessible (e.g., `CustomerStatus.Active`)
- [ ] Domain events inherit from `DomainEventBase`
- [ ] All domain types follow naming conventions

### Runtime Validation (Optional)
- [ ] Domain unit tests pass (if created)
- [ ] Aggregate factory method can be invoked without exceptions
- [ ] Value object conversions work (implicit operators, `.Value` property)
- [ ] Enumeration `FromName()` and `FromValue()` methods work

**Commands to Run**:
```powershell
# Build domain project
dotnet build src/Modules/[Module]/[Module].Domain

# Run domain tests (if they exist)
dotnet test tests/Modules/[Module]/[Module].UnitTests --filter "FullyQualifiedName~Domain"
```

**CORRECT Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**WRONG Output**:
```
error CS0246: The type or namespace name 'Result' could not be found
error CS0103: The name 'EmailAddress' does not exist in the current context
```

**Action if Checkpoint Fails**: Fix domain layer errors before proceeding to infrastructure.

---

## Checkpoint 2: Infrastructure Layer Complete

### Files Created
- [ ] `[Entity]TypeConfiguration.cs` (EF Core entity configuration)
- [ ] DbContext updated with `DbSet<[Entity]>` property
- [ ] Repository registered in module startup (via `AddEntityFrameworkRepository<[Entity], TDbContext>()`)
- [ ] EF Core migration created: `dotnet ef migrations add Add[Entity]`

### Build Validation
- [ ] Run build: `dotnet build src/Modules/[Module]/[Module].Infrastructure`
- [ ] Build succeeds with no errors
- [ ] No compiler warnings related to infrastructure code
- [ ] EF Core migration file generated successfully

### Code Validation
- [ ] TypedEntityId configured with `.HasConversion()`
- [ ] Value objects configured with `.HasConversion()` or `.OwnsOne()`
- [ ] Enumerations configured with `.HasConversion(new EnumerationConverter<T>())`
- [ ] AuditState configured with `.OwnsOneAuditState()`
- [ ] Concurrency token configured: `.Property(e => e.ConcurrencyVersion).IsConcurrencyToken()`
- [ ] Domain events ignored: `.Ignore(e => e.DomainEvents)`
- [ ] Repository behaviors chained (logging, audit, domain events, etc.)

### Runtime Validation
- [ ] Apply migration: `dotnet ef database update --project src/Modules/[Module]/[Module].Infrastructure`
- [ ] Migration applies without errors
- [ ] Database table created with correct schema
- [ ] Columns match aggregate properties (primitive types after conversion)
- [ ] Infrastructure integration tests pass (if created)

**Commands to Run**:
```powershell
# Build infrastructure project
dotnet build src/Modules/[Module]/[Module].Infrastructure

# Create migration (from infrastructure project directory)
cd src/Modules/[Module]/[Module].Infrastructure
dotnet ef migrations add Add[Entity] --startup-project ../../../Presentation.Web.Server

# Apply migration
dotnet ef database update --startup-project ../../../Presentation.Web.Server

# Run infrastructure tests (if they exist)
dotnet test tests/Modules/[Module]/[Module].IntegrationTests --filter "FullyQualifiedName~Infrastructure"
```

**CORRECT Output**:
```
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
Applying migration '20260114123456_AddCustomer'.
Done.
```

**WRONG Output**:
```
error CS1061: 'EntityTypeBuilder<Customer>' does not contain a definition for 'HasConversion'
The entity type 'Customer' requires a primary key to be defined
Unable to resolve service for type 'CoreModuleDbContext'
```

**Action if Checkpoint Fails**: Fix infrastructure errors, remove bad migration (`dotnet ef migrations remove`), recreate migration after fixes.

---

## Checkpoint 3: Application Layer Complete

### Files Created
- [ ] `[Entity]CreateCommand.cs` + handler + validator
- [ ] `[Entity]UpdateCommand.cs` + handler + validator
- [ ] `[Entity]DeleteCommand.cs` + handler
- [ ] `[Entity]FindOneQuery.cs` + handler
- [ ] `[Entity]FindAllQuery.cs` + handler
- [ ] `[Entity]Model.cs` (DTO)
- [ ] Commands/queries registered via `AddRequesterHandlers<T>()`

### Build Validation
- [ ] Run build: `dotnet build src/Modules/[Module]/[Module].Application`
- [ ] Build succeeds with no errors
- [ ] No compiler warnings related to application code

### Code Validation
- [ ] Commands/queries inherit from `RequestBase<Result<T>>`
- [ ] Handlers inherit from `RequestHandlerBase<TRequest, TResponse>`
- [ ] Validators inherit from `AbstractValidator<T>` (FluentValidation)
- [ ] Handlers inject `IGenericRepository<[Entity]>` and `IMapper`
- [ ] Handlers use `.Create().Ensure().Bind().Tap()` pattern for create
- [ ] Handlers use `.Change().Ensure().Set().Register().Apply()` pattern for update
- [ ] Handlers return `Result<[Entity]Model>` or `Result<Unit>`
- [ ] Retry and Timeout attributes applied: `[Retry(2)]`, `[Timeout(30)]` (commented in templates)

### Runtime Validation
- [ ] Application unit tests pass (if created)
- [ ] Validators execute and catch invalid data
- [ ] Handlers can instantiate and execute without runtime errors

**Commands to Run**:
```powershell
# Build application project
dotnet build src/Modules/[Module]/[Module].Application

# Run application tests (if they exist)
dotnet test tests/Modules/[Module]/[Module].UnitTests --filter "FullyQualifiedName~Application"
```

**CORRECT Output**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Test Run Successful.
```

**WRONG Output**:
```
error CS0246: The type or namespace name 'RequestBase' could not be found
error CS0311: The type 'Customer' cannot be used as type parameter 'T' in the generic type
error CS1061: 'Result<Customer>' does not contain a definition for 'Value'
```

**Action if Checkpoint Fails**: Fix application layer errors before proceeding to mapping and presentation.

---

## Checkpoint 4: Mapping Layer Complete

### Files Created
- [ ] `[Module]MapperRegister.cs` updated with new aggregate mappings
- [ ] Mappings for: `[Entity] → [Entity]Model` and `[Entity]Model → [Entity]`
- [ ] Value object mappings (domain ↔ primitive)
- [ ] Enumeration mappings (domain ↔ string)

### Build Validation
- [ ] Run build: `dotnet build src/Modules/[Module]/[Module].Presentation`
- [ ] Build succeeds with no errors
- [ ] No compiler warnings related to mapping code

### Code Validation
- [ ] MapperRegister implements `IRegister`
- [ ] `Register(TypeAdapterConfig config)` method contains all mappings
- [ ] TypedEntityId mapped: `.Map(dest => dest.Id, src => src.Id.Value)`
- [ ] Value objects mapped: `.Map(dest => dest.Email, src => src.Email.Value)`
- [ ] Enumerations mapped: `.Map(dest => dest.Status, src => src.Status.Name)`
- [ ] DTO → Domain uses `ConstructUsing()` with factory method
- [ ] Factory Result unwrapped: `.Value` or `.GetValueOrDefault()`

### Runtime Validation
- [ ] Mapping unit tests pass (if created)
- [ ] Can map aggregate → DTO without exceptions
- [ ] Can map DTO → aggregate without exceptions
- [ ] TypedEntityId converts to Guid correctly
- [ ] Value objects convert to primitives correctly
- [ ] Enumerations convert to strings correctly

**Commands to Run**:
```powershell
# Build presentation project
dotnet build src/Modules/[Module]/[Module].Presentation

# Run mapping tests (if they exist)
dotnet test tests/Modules/[Module]/[Module].UnitTests --filter "FullyQualifiedName~Mapping"
```

**CORRECT Output**:
```
Build succeeded.
Test Run Successful.
Total tests: 5
     Passed: 5
```

**WRONG Output**:
```
error CS1503: Argument 2: cannot convert from 'Customer' to 'CustomerModel'
System.InvalidOperationException: Missing map Customer -> CustomerModel
```

**Action if Checkpoint Fails**: Fix mapping errors (add missing mappings, correct type conversions).

---

## Checkpoint 5: Presentation Layer Complete

### Files Created
- [ ] `[Entity]Endpoints.cs` (minimal API endpoints)
- [ ] Endpoints registered in module startup
- [ ] All CRUD endpoints implemented (GET all, GET by id, POST, PUT, DELETE)

### Build Validation
- [ ] Run build: `dotnet build src/Modules/[Module]/[Module].Presentation`
- [ ] Build succeeds with no errors
- [ ] No compiler warnings related to presentation code

### Code Validation
- [ ] Endpoints inherit from `EndpointsBase`
- [ ] Constructor injects `IRequester`
- [ ] `Register(IEndpointRouteBuilder app)` method maps all endpoints
- [ ] Route group configured: `app.MapGroup("api/[module]/[entities]")`
- [ ] All endpoints use `requester.SendAsync()` to delegate to Application layer
- [ ] Result pattern handled via `.Match()` in all endpoints
- [ ] Correct HTTP status codes returned (200, 201, 204, 400, 404, 409)
- [ ] OpenAPI documentation applied (`.WithName()`, `.WithSummary()`, `.Produces<T>()`)

### Runtime Validation
- [ ] Run application: `dotnet run --project src/Presentation.Web.Server`
- [ ] Application starts without errors
- [ ] Swagger UI accessible: `https://localhost:5001/swagger`
- [ ] Endpoints visible in Swagger under correct tag
- [ ] Can invoke GET all endpoint from Swagger (returns empty array or data)
- [ ] Can invoke POST endpoint from Swagger (creates entity)
- [ ] Can invoke GET by id endpoint from Swagger (returns created entity)
- [ ] Can invoke PUT endpoint from Swagger (updates entity)
- [ ] Can invoke DELETE endpoint from Swagger (deletes entity)

**Commands to Run**:
```powershell
# Build presentation project
dotnet build src/Modules/[Module]/[Module].Presentation

# Run application
dotnet run --project src/Presentation.Web.Server

# In browser, navigate to:
# https://localhost:5001/swagger
```

**CORRECT Output**:
```
Build succeeded.
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
Application started. Press Ctrl+C to shut down.
```

**Swagger UI Shows**:
- GET /api/[module]/[entities]
- GET /api/[module]/[entities]/{id}
- POST /api/[module]/[entities]
- PUT /api/[module]/[entities]/{id}
- DELETE /api/[module]/[entities]/{id}

**WRONG Output**:
```
error CS1061: 'IResult' does not contain a definition for 'Match'
InvalidOperationException: No service for type 'IRequester' has been registered
AmbiguousMatchException: The request matched multiple endpoints
```

**Action if Checkpoint Fails**: Fix presentation errors (endpoint registration, DI configuration, Result handling).

---

## Checkpoint 6: Integration Tests Pass

### Tests to Run
- [ ] Run all unit tests: `dotnet test tests/Modules/[Module]/[Module].UnitTests`
- [ ] Run all integration tests: `dotnet test tests/Modules/[Module]/[Module].IntegrationTests`
- [ ] Run endpoint tests specifically: `dotnet test --filter "FullyQualifiedName~[Entity]EndpointTests"`

### Validation
- [ ] All unit tests pass (green)
- [ ] All integration tests pass (green)
- [ ] Endpoint tests cover CRUD operations (GET, POST, PUT, DELETE)
- [ ] Endpoint tests validate status codes (200, 201, 204, 400, 404)
- [ ] Endpoint tests validate response bodies (correct data returned)
- [ ] Endpoint tests validate error scenarios (invalid data returns 400)

**Commands to Run**:
```powershell
# Run all unit tests
dotnet test tests/Modules/[Module]/[Module].UnitTests

# Run all integration tests
dotnet test tests/Modules/[Module]/[Module].IntegrationTests

# Run specific endpoint tests
dotnet test tests/Modules/[Module]/[Module].IntegrationTests --filter "FullyQualifiedName~CustomerEndpointTests"
```

**CORRECT Output**:
```
Test Run Successful.
Total tests: 25
     Passed: 25
 Total time: 3.5 seconds
```

**WRONG Output**:
```
Test Run Failed.
Total tests: 25
     Passed: 20
     Failed: 5
```

**Action if Checkpoint Fails**: Investigate test failures, fix underlying issues (handlers, mapping, endpoints).

---

## Checkpoint 7: End-to-End Manual Testing

### Manual Test Scenarios
- [ ] Use Swagger UI or Postman to test full CRUD workflow
- [ ] **Create**: POST new entity with valid data → 201 Created, Location header present
- [ ] **Read One**: GET by id → 200 OK, correct entity returned
- [ ] **Read All**: GET all → 200 OK, collection contains created entity
- [ ] **Update**: PUT with valid data → 200 OK, entity updated
- [ ] **Delete**: DELETE by id → 204 No Content
- [ ] **Validation**: POST with invalid data → 400 Bad Request with validation errors
- [ ] **Not Found**: GET non-existent id → 404 Not Found
- [ ] **Concurrency**: Update same entity twice → Second update returns 409 Conflict (if concurrency enabled)

### Data Validation
- [ ] Check database to confirm entity persisted
- [ ] Verify TypedEntityId stored as Guid in database
- [ ] Verify Value Objects stored as primitives in database
- [ ] Verify Enumerations stored as int in database
- [ ] Verify AuditState columns populated (CreatedDate, UpdatedDate, etc.)
- [ ] Verify ConcurrencyVersion incremented on updates

**Commands to Run**:
```powershell
# Start application
dotnet run --project src/Presentation.Web.Server

# In another terminal, query database (example with SQL Server)
sqlcmd -S localhost -d GettingStartedDb -Q "SELECT * FROM [Core].[Customers]"
```

**CORRECT Behavior**:
- All HTTP requests return expected status codes
- Response bodies contain correct data
- Database reflects changes after POST/PUT/DELETE
- Validation errors returned with descriptive messages

**WRONG Behavior**:
- 500 Internal Server Error on valid requests
- 200 OK but database not updated
- Validation errors not returned
- Incorrect data types in database

**Action if Checkpoint Fails**: Debug application (check logs, breakpoints), verify DI registration, check EF Core configuration.

---

## Checkpoint 8: Code Quality and Standards

### Code Quality Validation
- [ ] Run code formatter: `dotnet format src/Modules/[Module]`
- [ ] No formatting violations
- [ ] No StyleCop or analyzer warnings
- [ ] All files have proper namespaces
- [ ] All files follow naming conventions

### Architecture Validation
- [ ] Domain layer has NO references to outer layers
- [ ] Application layer references ONLY Domain
- [ ] Infrastructure references Domain and Application
- [ ] Presentation references Application and Domain (for DTOs)
- [ ] No circular dependencies

### Documentation Validation
- [ ] XML comments on public classes and methods (optional)
- [ ] README updated with new aggregate (if module README exists)
- [ ] ADR created if architectural decision made (optional)

**Commands to Run**:
```powershell
# Format code
dotnet format src/Modules/[Module]

# Build entire solution to check for warnings
dotnet build

# Run architecture tests (if they exist)
dotnet test tests/ArchitectureTests
```

**CORRECT Output**:
```
Formatting code files...
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**WRONG Output**:
```
warning CS1591: Missing XML comment for publicly visible type or member
warning IDE0005: Using directive is unnecessary
error CS0246: Circular dependency detected
```

**Action if Checkpoint Fails**: Fix warnings, remove unnecessary usings, resolve circular dependencies.

---

## Final Checkpoint: Aggregate Implementation Complete

### Completion Criteria
- [ ] All 8 checkpoints above passed
- [ ] Domain layer compiles and tests pass
- [ ] Infrastructure layer compiles, migrations applied, tests pass
- [ ] Application layer compiles and tests pass
- [ ] Mapping layer compiles and tests pass
- [ ] Presentation layer compiles and endpoints functional
- [ ] Integration tests pass (unit + integration)
- [ ] End-to-end manual testing successful
- [ ] Code quality standards met
- [ ] No compiler warnings or errors
- [ ] Application runs without exceptions

### Documentation Complete
- [ ] All layer files created (Domain, Infrastructure, Application, Mapping, Presentation)
- [ ] EF Core migration created and applied
- [ ] Tests created (unit + integration) and passing
- [ ] Endpoints documented in Swagger UI
- [ ] Module README updated (if applicable)

### Deliverables
- [ ] Fully functional CRUD API for new aggregate
- [ ] Database schema updated with new table
- [ ] Tests validate correctness of implementation
- [ ] Code follows project conventions and standards
- [ ] Ready for code review and merge

---

## Troubleshooting Common Build Failures

### Problem: Build Errors in Domain Layer
**Symptoms**: `error CS0246: The type or namespace name 'Result' could not be found`

**Solution**:
- Verify `BridgingIT.DevKit.Domain` package referenced
- Add `using BridgingIT.DevKit.Domain;` to files
- Check TypedEntityId attribute syntax: `[TypedEntityId<Guid>]`

---

### Problem: EF Core Migration Fails
**Symptoms**: `Unable to resolve service for type 'CoreModuleDbContext'`

**Solution**:
- Verify DbContext registered in module startup: `services.AddSqlServerDbContext<TDbContext>()`
- Ensure `--startup-project` parameter points to Presentation.Web.Server
- Check connection string in appsettings.json

---

### Problem: Handler Dependency Injection Fails
**Symptoms**: `InvalidOperationException: No service for type 'IGenericRepository<Customer>' has been registered`

**Solution**:
- Verify repository registered: `services.AddEntityFrameworkRepository<Customer, CoreModuleDbContext>()`
- Check repository behaviors chained correctly
- Ensure module registered in Program.cs: `services.AddCoreModule(configuration)`

---

### Problem: Mapping Errors at Runtime
**Symptoms**: `InvalidOperationException: Missing map Customer -> CustomerModel`

**Solution**:
- Verify MapperRegister registered: `services.AddMapping().WithMapster<CoreModuleMapperRegister>()`
- Check `Register(TypeAdapterConfig config)` method contains mappings
- Test mappings with unit tests before integration

---

### Problem: Endpoint Not Found (404)
**Symptoms**: `Cannot GET /api/core/customers`

**Solution**:
- Verify endpoints registered: `services.AddScoped<CustomerEndpoints>()`
- Check `app.MapEndpoints()` called in Program.cs
- Verify route group path correct: `api/[module]/[entities]`

---

## References

- **Build Tasks**: `.vscode/tasks.json` (Solution build, test, format)
- **EF Core Migrations**: [Microsoft Docs](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- **bITdevKit Module Setup**: [DevKit Docs](https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs)

---

**Next Checklist**: quality-gates.md (Final Quality Gates)
