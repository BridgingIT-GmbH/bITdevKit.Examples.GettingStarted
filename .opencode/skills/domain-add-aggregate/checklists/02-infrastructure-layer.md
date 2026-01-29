# Infrastructure Layer Checklist

This checklist ensures your infrastructure layer properly implements persistence, integrates with EF Core, and maintains Clean Architecture boundaries.

## EF Core DbContext Configuration

### DbContext Class
- [ ] Inherits from `ModuleDbContextBase` or `DbContext`
- [ ] DbSet properties for aggregate roots only (not child entities)
- [ ] Constructor accepts `DbContextOptions<TDbContext>`
- [ ] Override `OnModelCreating()` to apply configurations
- [ ] Applies all type configurations via `ApplyConfiguration()`

### Example Structure
```csharp
public class CoreDbContext : ModuleDbContextBase
{
    public DbSet<Customer> Customers { get; set; }
    // No DbSet for Address (owned by Customer)
    
    public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new CustomerTypeConfiguration());
    }
}
```

## Entity Type Configuration

### Configuration Class
- [ ] Implements `IEntityTypeConfiguration<TEntity>`
- [ ] Located in `Infrastructure/EntityFramework/Configurations/`
- [ ] File name: `[Entity]TypeConfiguration.cs`
- [ ] Marked with `[ExcludeFromCodeCoverage]` attribute

### Table Mapping
- [ ] Table name configured: `.ToTable("[Entities]")`
- [ ] Primary key configured: `.HasKey(x => x.Id)`
- [ ] Clustered/non-clustered index specified if needed

### Primary Key (Typed ID)
- [ ] ID property configured with conversion
- [ ] Conversion: `id => id.Value` (to Guid)
- [ ] Conversion: `value => [Entity]Id.Create(value)` (from Guid)
- [ ] Value generation: `.ValueGeneratedOnAdd()` or `.ValueGeneratedNever()`

### Concurrency Token
- [ ] `ConcurrencyVersion` marked as concurrency token
- [ ] Configuration: `.IsConcurrencyToken()`
- [ ] Value generation: `.ValueGeneratedNever()` (app manages it)

### Value Object Conversions
- [ ] Each value object property has `.HasConversion()`
- [ ] To database: `vo => vo.Value`
- [ ] From database: `value => ValueObject.Create(value).Value`
- [ ] Max length specified: `.HasMaxLength(256)`
- [ ] Required/optional: `.IsRequired()` or `.IsRequired(false)`

### Enumeration Conversions
- [ ] Enumeration properties use `EnumerationConverter<T>`
- [ ] Configuration: `.HasConversion(new EnumerationConverter<[Entity]Status>())`
- [ ] Alternative: Manual conversion with `.HasConversion(s => s.Id, id => Enumeration.FromId<T>(id))`

### Primitive Properties
- [ ] All string properties have `.HasMaxLength()` specified
- [ ] Required properties marked: `.IsRequired()`
- [ ] Optional properties marked: `.IsRequired(false)`
- [ ] Column types specified for special cases: `.HasColumnType("date")`, `.HasColumnType("decimal(18,2)")`

### Date Handling
- [ ] DateOnly properties use `DateOnlyConverter` and `DateOnlyComparer`
- [ ] Configuration: `.HasConversion<DateOnlyConverter, DateOnlyComparer>()`
- [ ] Column type: `.HasColumnType("date")`

### Domain Events (Ignored)
- [ ] `DomainEvents` property explicitly ignored
- [ ] Configuration: `builder.Ignore(e => e.DomainEvents)`

### Audit State
- [ ] Audit properties configured via `.OwnsOneAuditState()`
- [ ] Includes: CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, IsDeleted, DeletedDate, DeletedBy

### Owned Entities (Child Collections)
- [ ] Child collections configured with `.OwnsMany()`
- [ ] Mapped to separate table: `.ToTable("[Entity][Children]")`
- [ ] Foreign key specified: `.WithOwner().HasForeignKey("[Entity]Id")`
- [ ] Child ID conversion configured (same pattern as parent)
- [ ] Child properties configured (max length, required, etc.)

### Example Owned Entity Configuration
```csharp
builder.OwnsMany(c => c.Addresses, ab =>
{
    ab.ToTable("CustomersAddresses");
    ab.WithOwner().HasForeignKey("CustomerId");
    
    ab.Property(a => a.Id)
        .ValueGeneratedOnAdd()
        .HasConversion(
            id => id.Value,
            value => AddressId.Create(value));
    
    ab.HasKey(a => a.Id);
    
    ab.Property(a => a.Line1).IsRequired().HasMaxLength(256);
    ab.Property(a => a.City).IsRequired().HasMaxLength(100);
});
```

## Repository Registration

### Service Registration
- [ ] Repository registered in module's `AddInfrastructure()` method
- [ ] Uses bITdevKit extension: `.AddEntityFrameworkRepository<TEntity, TDbContext>()`
- [ ] Decorator behaviors chained as needed

### Example Registration
```csharp
services
    .AddEntityFrameworkRepository<Customer, CoreDbContext>()
    .WithBehavior<RepositoryLoggingBehavior<Customer>>()
    .WithBehavior<RepositoryDomainEventBehavior<Customer>>()
    .WithBehavior<RepositoryDomainEventPublisherBehavior<Customer>>()
    .WithBehavior((innerRepository, sp) =>
        new RepositoryAuditStateBehavior<Customer>(
            innerRepository,
            sp.GetRequiredService<ICurrentUserService>()));
```

### Repository Behaviors
- [ ] Logging behavior added (logs repository operations)
- [ ] Domain event behavior added (publishes events after persistence)
- [ ] Audit state behavior added (sets CreatedBy, UpdatedBy, etc.)
- [ ] Order matters: Logging → Events → Audit → Inner

## EF Core Migrations

### Migration Creation
- [ ] Migrations created via: `dotnet ef migrations add [MigrationName] --project [Infrastructure] --startup-project [Server]`
- [ ] Migration names descriptive: `Initial`, `AddCustomerStatus`, `AddAddresses`
- [ ] Migrations reviewed before applying (verify SQL is correct)

### Migration Application
- [ ] Applied via: `dotnet ef database update --project [Infrastructure] --startup-project [Server]`
- [ ] Or automatic on startup (if configured): `context.Database.Migrate()`
- [ ] Rollback tested: `dotnet ef database update [PreviousMigration]`

### Migration Best Practices
- [ ] One logical change per migration
- [ ] Reviewed generated SQL (especially for data migrations)
- [ ] Tested on clean database (drop and recreate)
- [ ] Tested upgrade path from previous version

## Connection String Configuration

### Configuration Source
- [ ] Connection string in module configuration: `appsettings.json` or environment variable
- [ ] Module reads: `moduleOptions.ConnectionStrings.GetValue("Default")`
- [ ] DbContext registered with: `.AddSqlServerDbContext<TDbContext>(connectionString)`

### Example Configuration
```json
{
  "Modules": {
    "CoreModule": {
      "ConnectionStrings": {
        "Default": "Server=localhost;Database=GettingStarted;Trusted_Connection=true;"
      }
    }
  }
}
```

### Security
- [ ] Connection strings not hardcoded
- [ ] Secrets stored in: User Secrets (dev), Azure Key Vault (prod), or environment variables
- [ ] No credentials in source control

## Startup Tasks (Optional)

### Database Initialization
- [ ] Startup task created if database seeding needed
- [ ] Implements `IStartupTask` or `IModuleStartupTask`
- [ ] Ensures database created: `await context.Database.EnsureCreatedAsync()`
- [ ] Or migrates: `await context.Database.MigrateAsync()`

### Data Seeding
- [ ] Seed data added via startup task (not in migrations for dev data)
- [ ] Idempotent seeding (check if data exists before inserting)
- [ ] Only seed reference data, not user data

### Example Startup Task
```csharp
public class CoreModuleDatabaseStartupTask : IModuleStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await using var context = serviceProvider.GetRequiredService<CoreDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
        
        if (!await context.Customers.AnyAsync(cancellationToken))
        {
            // Seed reference data
        }
    }
}
```

## Background Jobs (Optional)

### Job Implementation
- [ ] Job class implements `IJob` or inherits from `JobBase`
- [ ] Located in `Infrastructure/Jobs/`
- [ ] Uses repository abstractions (not DbContext directly)

### Job Registration
- [ ] Registered in module's `AddInfrastructure()` method
- [ ] Uses bITdevKit Quartz integration: `.AddJob<TJob>(schedule)`

### Example Job Registration
```csharp
services.AddJob<CustomerExportJob>(
    jobOptions =>
    {
        jobOptions.CronSchedule = "0 0 2 * * ?"; // Daily at 2 AM
        jobOptions.Enabled = moduleOptions.Jobs.CustomerExport.Enabled;
    });
```

## Clean Architecture Boundaries

### Dependencies
- [ ] Infrastructure references:
  - Domain layer (for entities, value objects, aggregates)
  - Application layer (for abstractions like repositories, if defined there)
  - bITdevKit.Infrastructure
  - EF Core packages
  - Third-party integrations (email, storage, etc.)
- [ ] Infrastructure does NOT reference:
  - Presentation layer

### Abstractions
- [ ] Repository abstractions defined in Application or Domain (if custom needed)
- [ ] Infrastructure implements abstractions from Application/Domain
- [ ] No infrastructure types exposed to Application/Domain

## File Organization

### Folder Structure
- [ ] `Infrastructure/EntityFramework/Configurations/` - EF Core configurations
- [ ] `Infrastructure/EntityFramework/[Module]DbContext.cs` - DbContext
- [ ] `Infrastructure/Jobs/` - Background jobs (if applicable)
- [ ] `Infrastructure/StartupTasks/` - Startup tasks (if applicable)

### Naming Conventions
- [ ] DbContext: `[Module]DbContext` (e.g., `CoreDbContext`)
- [ ] Configuration: `[Entity]TypeConfiguration` (e.g., `CustomerTypeConfiguration`)
- [ ] Job: `[Entity][Action]Job` (e.g., `CustomerExportJob`)
- [ ] Startup task: `[Module]DatabaseStartupTask`

## Testing

### Integration Tests
- [ ] Test DbContext configuration (migrations apply successfully)
- [ ] Test repository operations (insert, update, delete, find)
- [ ] Test value object conversions (save and retrieve correctly)
- [ ] Test enumeration conversions (save and retrieve correctly)
- [ ] Test concurrency token behavior (concurrent updates fail correctly)
- [ ] Test owned entity persistence (child collections save/load correctly)

### Test Database
- [ ] Use in-memory database or test container for tests
- [ ] Each test uses isolated database instance (or transaction rollback)
- [ ] Test data cleaned up after each test

### Example Integration Test
```csharp
[Fact]
public async Task Repository_InsertCustomer_ShouldPersist()
{
    // Arrange
    await using var context = CreateDbContext();
    var repository = new EntityFrameworkRepository<Customer>(context);
    var customer = Customer.Create("John", "Doe", "john@example.com", "CUS-001").Value;
    
    // Act
    await repository.InsertAsync(customer);
    await context.SaveChangesAsync();
    
    // Assert
    var loaded = await repository.FindOneAsync(customer.Id);
    loaded.ShouldNotBeNull();
    loaded.FirstName.ShouldBe("John");
    loaded.Email.Value.ShouldBe("john@example.com");
}
```

## Common Anti-Patterns to Avoid

### Leaking Infrastructure to Domain
- [ ] WRONG: EF Core attributes on domain entities
- [ ] WRONG: DbContext references in domain
- [ ] CORRECT: Configuration in separate classes

### Anemic Repositories
- [ ] WRONG: Repository with 50+ methods for every query
- [ ] CORRECT: Generic repository + specifications for queries

### Ignoring Concurrency
- [ ] WRONG: No concurrency token configured
- [ ] CORRECT: ConcurrencyVersion configured as concurrency token

### Hardcoded Connection Strings
- [ ] WRONG: Connection string in code
- [ ] CORRECT: Connection string in configuration

### Missing Conversions
- [ ] WRONG: Value objects saved as complex types (serialized JSON)
- [ ] CORRECT: Value objects converted to primitives with `.HasConversion()`

## Code Quality

### Readability
- [ ] Configuration methods focused and readable
- [ ] Complex configurations extracted to separate methods
- [ ] Comments explain "why", not "what"

### Maintainability
- [ ] Consistent naming conventions
- [ ] Configuration patterns consistent across entities
- [ ] Easy to add new entities (copy-paste-modify pattern)

### Performance
- [ ] Indexes defined for frequently queried columns
- [ ] No lazy loading (use explicit includes or specifications)
- [ ] Queries optimized (no N+1 patterns)

## Final Review

### Before Committing
- [ ] All tests pass (unit + integration)
- [ ] Migrations apply successfully
- [ ] No compiler warnings
- [ ] Code follows .editorconfig rules
- [ ] Connection strings not hardcoded

### Database Validation
- [ ] Database schema matches domain model
- [ ] Migrations apply cleanly on empty database
- [ ] All value objects/enumerations convert correctly
- [ ] Concurrency tokens configured
- [ ] Foreign keys correct for owned entities

### Architecture Validation
- [ ] No domain references to infrastructure
- [ ] Repository abstractions respected
- [ ] Clean separation between persistence and domain
