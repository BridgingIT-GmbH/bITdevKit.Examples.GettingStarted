# Quality Gates Checklist

This checklist provides comprehensive final validation before considering the aggregate implementation complete and ready for code review.

**Purpose**: Ensure production-ready quality across all dimensions  
**Timing**: Run after all layers implemented and build checkpoints passed  
**Outcome**: High confidence in correctness, maintainability, and adherence to standards

---

## Gate 1: Functional Correctness

### CRUD Operations Work End-to-End
- [ ] **Create**: Can create new entities with valid data via API
- [ ] **Read One**: Can retrieve individual entity by ID via API
- [ ] **Read All**: Can retrieve collection of entities via API
- [ ] **Update**: Can modify existing entities via API
- [ ] **Delete**: Can remove entities via API
- [ ] All operations persist changes to database correctly
- [ ] All operations return correct HTTP status codes

### Business Rules Enforced
- [ ] Aggregate factory method validates inputs and returns Failure for invalid data
- [ ] Value object factory methods validate inputs and return Failure for invalid data
- [ ] Change methods enforce invariants (e.g., cannot set status to invalid value)
- [ ] Domain events registered on create, update, delete operations
- [ ] Concurrency handled correctly (optimistic concurrency via ConcurrencyVersion)

### Error Handling Works Correctly
- [ ] Invalid data returns 400 Bad Request with validation messages
- [ ] Non-existent entities return 404 Not Found
- [ ] Concurrency conflicts return 409 Conflict (if applicable)
- [ ] Unhandled errors return 500 Internal Server Error with sanitized messages
- [ ] Error messages do not leak sensitive information or stack traces

---

## Gate 2: Code Quality

### Naming Conventions Followed
- [ ] Aggregate root: `[Entity].cs` (PascalCase, singular)
- [ ] Typed entity ID: `[Entity]Id.cs` or `[TypedEntityId<Guid>]` attribute
- [ ] Value objects: Descriptive singular names (e.g., `EmailAddress`)
- [ ] Enumerations: `[Entity]Status` or similar, with static instances
- [ ] Commands: `[Entity][Action]Command` (e.g., `CustomerCreateCommand`)
- [ ] Queries: `[Entity][Action]Query` (e.g., `CustomerFindOneQuery`)
- [ ] Handlers: `[Entity][Command|Query]Handler`
- [ ] Validators: `[Entity][Command]Validator`
- [ ] DTOs: `[Entity]Model`
- [ ] Endpoints: `[Entity]Endpoints`
- [ ] EF Configuration: `[Entity]TypeConfiguration`

### Folder Structure Correct
- [ ] Domain layer files in `src/Modules/[Module]/[Module].Domain/Model/[Entity]Aggregate/`
- [ ] Application layer files in `src/Modules/[Module]/[Module].Application/Commands/` and `.../Queries/`
- [ ] Infrastructure layer files in `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/Configurations/`
- [ ] Presentation layer files in `src/Modules/[Module]/[Module].Presentation/Web/Endpoints/` and `[Module]MapperRegister.cs`
- [ ] Test files mirror source structure under `tests/Modules/[Module]/`

### Code Style Consistent
- [ ] File-scoped namespaces used (`namespace X.Y.Z;` not `namespace X.Y.Z { }`)
- [ ] Primary constructors used where appropriate
- [ ] `var` used when type is obvious
- [ ] Null-conditional operators used (`?.`, `??`)
- [ ] String interpolation used over concatenation
- [ ] LINQ used appropriately (readable, efficient)
- [ ] No unused `using` directives
- [ ] No commented-out code blocks

### No Code Smells
- [ ] No duplicate code (DRY principle followed)
- [ ] No magic numbers or strings (use constants or enumerations)
- [ ] No long methods (< 50 lines per method)
- [ ] No deep nesting (< 4 levels)
- [ ] No primitive obsession (value objects used appropriately)
- [ ] No anemic domain model (behavior in domain, not scattered)
- [ ] No fat endpoints (logic in handlers, not endpoints)

---

## Gate 3: Architecture Compliance

### Layer Boundaries Respected
- [ ] **Domain** has NO references to Application, Infrastructure, or Presentation
- [ ] **Application** references ONLY Domain
- [ ] **Infrastructure** references Domain and Application
- [ ] **Presentation** references Application and Domain (for DTOs only)
- [ ] No circular dependencies between projects

### Dependency Injection Correct
- [ ] All dependencies injected via constructor (no `new` for services)
- [ ] Repository abstraction used (no direct DbContext access in Application)
- [ ] IRequester used in endpoints (no direct handler invocation)
- [ ] IMapper used in handlers (no manual mapping logic)
- [ ] Service lifetimes appropriate (Scoped for repositories, handlers)

### Pattern Adherence
- [ ] **Result Pattern**: All domain factories and change methods return `Result<T>`
- [ ] **CQRS**: Commands and Queries separated, handlers implement `IRequestHandler`
- [ ] **Repository Pattern**: `IGenericRepository<T>` used for data access
- [ ] **Mediator Pattern**: `IRequester` used to send commands/queries
- [ ] **Specification Pattern**: Specifications used for complex queries (optional)
- [ ] **Outbox Pattern**: Domain events published via repository behaviors

### Domain-Driven Design Principles
- [ ] Aggregate boundary clear (root entity, child entities, value objects)
- [ ] Aggregate root enforces invariants
- [ ] Entities have identity (TypedEntityId)
- [ ] Value objects immutable and equality based on values
- [ ] Enumerations encapsulate related constants with behavior
- [ ] Domain events capture significant domain occurrences
- [ ] Ubiquitous language used in naming (business terms, not technical jargon)

---

## Gate 4: Testing Coverage

### Unit Tests Exist and Pass
- [ ] Domain layer: Aggregate factory, change methods, value objects, enumerations tested
- [ ] Application layer: Command/query handlers tested with mocks
- [ ] Application layer: Validators tested with valid/invalid inputs
- [ ] Mapping layer: Aggregate ↔ DTO mappings tested
- [ ] All unit tests pass (green)
- [ ] Unit tests run quickly (< 5 seconds total)

### Integration Tests Exist and Pass
- [ ] Infrastructure layer: EF Core configuration tested (add, retrieve, update, delete)
- [ ] Presentation layer: Endpoint tests cover CRUD operations
- [ ] Endpoint tests validate HTTP status codes (200, 201, 204, 400, 404, 409)
- [ ] Endpoint tests validate response bodies (correct data returned)
- [ ] Endpoint tests validate error scenarios (invalid data, not found, etc.)
- [ ] All integration tests pass (green)
- [ ] Integration tests run reasonably fast (< 30 seconds total)

### Code Coverage Acceptable
- [ ] Overall coverage ≥ 70%
- [ ] Domain layer coverage ≥ 80% (critical business logic)
- [ ] Application layer coverage ≥ 70% (handlers, validators)
- [ ] Infrastructure layer coverage ≥ 50% (mostly integration tests)
- [ ] Coverage report generated: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Coverage gaps reviewed and justified (e.g., trivial getters/setters)

### Test Quality High
- [ ] Tests follow Arrange-Act-Assert pattern
- [ ] Tests have descriptive names (`Method_Scenario_ExpectedOutcome`)
- [ ] Tests are isolated (no shared state)
- [ ] Tests are deterministic (no flaky tests)
- [ ] Tests use appropriate assertions (Shouldly fluent assertions)
- [ ] Mocks used appropriately (external dependencies only, not domain objects)

---

## Gate 5: Database and Persistence

### EF Core Configuration Correct
- [ ] Entity type configuration class exists: `[Entity]TypeConfiguration`
- [ ] Configuration applied in DbContext: `modelBuilder.ApplyConfiguration(new [Entity]TypeConfiguration())`
- [ ] Table name configured: `.ToTable("[Entities]", CoreModuleConstants.Schema)`
- [ ] Primary key configured (TypedEntityId with conversion)
- [ ] Value objects configured with `.HasConversion()` or `.OwnsOne()`
- [ ] Enumerations configured with `.HasConversion(new EnumerationConverter<T>())`
- [ ] AuditState configured with `.OwnsOneAuditState()`
- [ ] Concurrency token configured: `.Property(e => e.ConcurrencyVersion).IsConcurrencyToken()`
- [ ] Domain events ignored: `.Ignore(e => e.DomainEvents)`
- [ ] Navigation properties configured (if applicable)

### Migrations Created and Applied
- [ ] EF Core migration created: `dotnet ef migrations add Add[Entity]`
- [ ] Migration file reviewed (Up/Down methods correct)
- [ ] Migration applied to database: `dotnet ef database update`
- [ ] Database table exists with correct schema
- [ ] Database columns match aggregate properties (after conversions)
- [ ] Indexes created where appropriate (optional but recommended)

### Data Integrity Validated
- [ ] Can insert entity and retrieve it from database
- [ ] Can update entity and changes persist
- [ ] Can delete entity and it's removed from database
- [ ] TypedEntityId stored as Guid (not as complex type)
- [ ] Value objects stored as primitives (e.g., EmailAddress → nvarchar)
- [ ] Enumerations stored as int (not string, unless configured otherwise)
- [ ] AuditState columns populated (CreatedDate, CreatedBy, UpdatedDate, UpdatedBy)
- [ ] ConcurrencyVersion increments on updates

---

## Gate 6: API and Documentation

### Endpoints Registered and Functional
- [ ] All CRUD endpoints registered in `[Entity]Endpoints.cs`
- [ ] Endpoints inherit from `EndpointsBase`
- [ ] Route group configured: `app.MapGroup("api/[module]/[entities]")`
- [ ] Endpoints use IRequester to delegate to Application layer
- [ ] Endpoints handle Result pattern correctly via `.Match()`
- [ ] Endpoints return correct HTTP status codes
- [ ] Endpoints registered in DI container and discovered at startup

### OpenAPI/Swagger Documentation Complete
- [ ] Each endpoint has unique operation ID: `.WithName("GetAllCustomers")`
- [ ] Each endpoint has summary: `.WithSummary("Get all customers")`
- [ ] Complex endpoints have description: `.WithDescription("...")`
- [ ] Response types documented: `.Produces<CustomerModel>(200)`
- [ ] Error responses documented: `.ProducesProblem(400)`, `.ProducesValidationProblem()`
- [ ] Tags applied: `.WithTags("Customers")`
- [ ] Swagger UI shows all endpoints under correct tag
- [ ] Swagger UI allows testing endpoints (Try it out)

### API Usability
- [ ] Endpoints follow RESTful conventions
- [ ] Route paths intuitive (plural entity names, hierarchical if needed)
- [ ] Request/response payloads clear (DTO models with meaningful property names)
- [ ] Validation errors return structured messages (ProblemDetails format)
- [ ] Location header set on 201 Created responses
- [ ] Consistent error response format across all endpoints

---

## Gate 7: Security and Validation

### Input Validation Enforced
- [ ] FluentValidation rules defined for all commands
- [ ] Validators registered and invoked automatically via ValidationPipelineBehavior
- [ ] Null/empty checks for required properties
- [ ] Format validation for emails, phone numbers, etc.
- [ ] Range validation for numeric properties
- [ ] Business rule validation (e.g., status transitions valid)
- [ ] Validation errors return 400 Bad Request with descriptive messages

### Domain Validation Enforced
- [ ] Aggregate factory methods enforce invariants (guard clauses)
- [ ] Value object factory methods enforce constraints
- [ ] Change methods validate inputs before applying changes
- [ ] Enumerations prevent invalid values (static instances only)
- [ ] Business rules encapsulated in domain layer (not leaked to Application)

### Security Best Practices
- [ ] No SQL injection risk (using EF Core parameterized queries)
- [ ] No sensitive data logged or exposed in error messages
- [ ] No stack traces exposed to API consumers
- [ ] Authorization attributes applied to endpoints (if authentication enabled)
- [ ] HTTPS enforced (configured in Program.cs)
- [ ] CORS policy configured appropriately (if needed)

### Authentication/Authorization (if applicable)
- [ ] Endpoints protected with `.RequireAuthorization()` where needed
- [ ] Public endpoints explicitly marked with `.AllowAnonymous()`
- [ ] Role-based or policy-based authorization configured
- [ ] JWT bearer tokens validated correctly
- [ ] 401 Unauthorized returned for unauthenticated requests
- [ ] 403 Forbidden returned for unauthorized requests

---

## Gate 8: Observability and Logging

### Logging Configured
- [ ] Serilog configured in module startup
- [ ] Structured logging used (not string concatenation)
- [ ] Correlation ID propagated through requests
- [ ] Log levels appropriate (Information, Warning, Error)
- [ ] No excessive logging (not logging every method entry/exit)
- [ ] No sensitive data logged (PII, secrets, passwords)

### Repository Behaviors Applied
- [ ] Logging behavior applied: `.WithBehavior<RepositoryLoggingBehavior<TEntity>>()`
- [ ] Audit behavior applied: `.WithBehavior<RepositoryAuditStateBehavior<TEntity>>()`
- [ ] Domain events behavior applied: `.WithBehavior<RepositoryDomainEventBehavior<TEntity>>()`
- [ ] Behaviors execute in correct order (logging → audit → domain events)
- [ ] Behaviors log relevant information without excessive verbosity

### Error Handling and Tracing
- [ ] Global exception middleware configured (ProblemDetails for errors)
- [ ] Unhandled exceptions logged with full stack traces (server-side only)
- [ ] Errors return structured ProblemDetails responses to clients
- [ ] Correlation ID included in logs and error responses (for tracing)
- [ ] Application Insights or similar APM configured (optional)

---

## Gate 9: Performance and Scalability

### Query Performance
- [ ] No N+1 query issues (use `.Include()` for related entities)
- [ ] Appropriate indexes created on frequently queried columns (optional)
- [ ] Pagination implemented for list endpoints (avoid returning unbounded collections)
- [ ] Specifications used for complex queries (reusable, testable)
- [ ] No unnecessary data fetched (project to DTOs early if possible)

### Command Performance
- [ ] Retry behavior configured appropriately: `[Retry(2)]`
- [ ] Timeout behavior configured appropriately: `[Timeout(30)]`
- [ ] Database transactions scoped correctly (implicit via EF Core SaveChanges)
- [ ] No long-running operations in request handlers (consider background jobs)

### Scalability Considerations
- [ ] Stateless handlers (no static mutable state)
- [ ] Thread-safe code (no shared mutable state without synchronization)
- [ ] DbContext scoped per request (not singleton)
- [ ] Repository scoped per request (not singleton)
- [ ] Handlers scoped per request (not singleton)
- [ ] Consider caching for frequently accessed read-only data (optional)

---

## Gate 10: Maintainability and Documentation

### Code Documentation
- [ ] Complex business rules have explanatory comments
- [ ] XML comments on public classes and methods (optional but recommended)
- [ ] No misleading or outdated comments
- [ ] TODOs addressed or tracked (no lingering TODO comments)

### Project Documentation
- [ ] Module README updated with new aggregate (if applicable)
- [ ] ADR created for significant architectural decisions (optional)
- [ ] API documentation generated and accessible (Swagger)
- [ ] Migration notes added (if database schema changes significant)

### Code Readability
- [ ] Code is self-explanatory (intent clear from naming and structure)
- [ ] Minimal cyclomatic complexity (no overly complex methods)
- [ ] Consistent formatting (dotnet format applied)
- [ ] Logical grouping of related code (commands together, queries together)
- [ ] No "clever" code (favor clarity over brevity)

### Extensibility
- [ ] Easy to add new fields to aggregate (add to aggregate, DTO, mapping, migration)
- [ ] Easy to add new commands/queries (follow existing pattern)
- [ ] Easy to add new endpoints (add to Endpoints class)
- [ ] Easy to add new business rules (add to domain layer, enforce in factory/change methods)
- [ ] Easy to add new validation rules (add to FluentValidation validator)

---

## Gate 11: Build and Deployment

### Build Succeeds
- [ ] `dotnet build` succeeds for entire solution
- [ ] No compiler warnings related to new code
- [ ] No analyzer warnings (StyleCop, FxCop, etc.)
- [ ] No nullable reference warnings (if nullability enabled)

### Tests Succeed
- [ ] All unit tests pass: `dotnet test tests/Modules/[Module]/[Module].UnitTests`
- [ ] All integration tests pass: `dotnet test tests/Modules/[Module]/[Module].IntegrationTests`
- [ ] All architecture tests pass: `dotnet test tests/ArchitectureTests` (if applicable)
- [ ] Test execution time reasonable (< 1 minute total)

### Application Runs
- [ ] `dotnet run --project src/Presentation.Web.Server` starts without errors
- [ ] Application responds to health check endpoint (if configured)
- [ ] Application accessible via browser (Swagger UI loads)
- [ ] No errors in console output during startup

### Deployment Ready
- [ ] Docker build succeeds (if using Docker): `docker build -t app .`
- [ ] Docker run succeeds (if using Docker): `docker run -p 5000:5000 app`
- [ ] Environment-specific configuration handled (appsettings.Development.json, appsettings.Production.json)
- [ ] Database migration can be applied in production (via startup task or manual command)
- [ ] Secrets not hardcoded (use User Secrets, Azure Key Vault, etc.)

---

## Gate 12: Final Review Checklist

### Completeness
- [ ] All required files created (Domain, Infrastructure, Application, Mapping, Presentation)
- [ ] All CRUD operations implemented (Create, Read, Update, Delete)
- [ ] All tests written and passing (unit + integration)
- [ ] All documentation updated (README, ADR, Swagger)

### Correctness
- [ ] Business rules correctly implemented
- [ ] Data persisted correctly to database
- [ ] API returns correct status codes and payloads
- [ ] Error scenarios handled gracefully

### Quality
- [ ] Code follows project conventions and standards
- [ ] No code smells or anti-patterns
- [ ] Test coverage meets targets (≥ 70% overall)
- [ ] No compiler warnings or analyzer issues

### Maintainability
- [ ] Code is readable and self-explanatory
- [ ] Code is well-organized and follows clear structure
- [ ] Code is extensible (easy to add new features)
- [ ] Documentation sufficient for future developers

### Production Readiness
- [ ] Security validated (authentication, authorization, input validation)
- [ ] Performance acceptable (no obvious bottlenecks)
- [ ] Observability configured (logging, tracing)
- [ ] Deployment ready (builds, runs, migrates)

---

## Sign-Off

Once all quality gates pass:

- [ ] Developer self-review complete (all items checked)
- [ ] Code formatted: `dotnet format`
- [ ] Changes committed to feature branch
- [ ] Pull request created with descriptive title and summary
- [ ] PR includes references to related issues/work items
- [ ] PR ready for peer review

---

## Post-Review Actions

After code review approval:

- [ ] Address all review comments
- [ ] Merge to main/develop branch
- [ ] Verify CI/CD pipeline passes
- [ ] Verify deployment to staging environment successful
- [ ] Perform smoke tests in staging
- [ ] Schedule production deployment
- [ ] Monitor application logs post-deployment
- [ ] Update project documentation (release notes, changelog)

---

## References

- **Project Coding Standards**: `.editorconfig`, `AGENTS.md`
- **Architecture Documentation**: `docs/ADR/README.md`
- **Testing Strategy**: `.github/skills/domain-add-aggregate/checklists/06-tests.md`
- **Build Checkpoints**: `.github/skills/domain-add-aggregate/checklists/build-checkpoints.md`

---

**Congratulations!** If all quality gates pass, your aggregate implementation is complete, high-quality, and ready for production use.
