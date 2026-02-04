---
name: domain-add-aggregate
description: Adds new Domain Aggregates using Clean Architecture and DDD principles with full CRUD scaffolding across all layers
---

# Domain Add Aggregate

## Overview

This skill guides you through creating a complete Domain Aggregate with full CRUD operations following Clean Architecture, Domain-Driven Design (DDD), and bITdevKit patterns. It scaffolds code across all five layers: Domain, Infrastructure, Application, Mapping, and Presentation.

**What You'll Create**:

- Domain Aggregate with factory methods and change methods
- Value Objects and Enumerations (if needed)
- Domain Events (Created/Updated/Deleted)
- EF Core configuration and migration
- CQRS Commands and Queries with handlers
- Mapster DTO mappings
- Minimal API endpoints

**Time Estimate**: 30-60 minutes for complete aggregate with CRUD operations.

## Prerequisites

### Knowledge Requirements

- **Domain-Driven Design (DDD)**: Understand aggregates, value objects, domain events
- **Clean Architecture**: Understand layer boundaries (Domain → Application → Infrastructure → Presentation)
- **bITdevKit**: Familiarity with base classes (AuditableAggregateRoot, RequestHandlerBase, etc.)
- **Result Pattern**: Understand `Result<T>` for error handling
- **CQRS**: Understand Commands vs Queries

### Technical Requirements

- **.NET 10 SDK** installed (or .NET 8+ compatible)
- **Project uses bITdevKit framework**
- **Module structure**: `src/Modules/[Module]/[Module].[Layer]/`
- **EF Core** for data persistence
- **Mapster** for object mapping

### Project Structure

This skill assumes:
```
src/Modules/[Module]/
├── [Module].Domain/
│   ├── Model/
│   │   └── [Entity]Aggregate/
│   └── Events/
├── [Module].Application/
│   ├── Commands/
│   └── Queries/
├── [Module].Infrastructure/
│   └── EntityFramework/
│       └── Configurations/
├── [Module].Presentation/
│   ├── Web/
│   │   └── Endpoints/
│   └── [Module]MapperRegister.cs
```

## Core Rules

1. **ALWAYS plan before coding**: Create a checklist of properties, value objects, and operations
2. **ALWAYS work layer-by-layer**: Domain → Infrastructure → Application → Mapping → Presentation
3. **NEVER reference outer layers from inner layers**: Domain cannot reference Application/Infrastructure/Presentation
4. **ALWAYS use factory methods returning `Result<T>`**: No public constructors on aggregates
5. **ALWAYS use private setters**: Properties are immutable from outside the aggregate
6. **ALWAYS register domain events**: In factory methods and change methods
7. **ALWAYS compile after each layer**: Incremental validation prevents cascading errors
8. **ALWAYS use Mapster for mapping**: No manual DTO mapping in handlers
9. **ALWAYS use typed entity IDs**: Apply `[TypedEntityId<Guid>]` attribute
10. **NEVER skip EF configuration**: Fluent API configuration is required for all aggregates

## When to Use This Skill

**Use this skill when**:

- Adding a new business entity (Product, Order, Invoice, etc.)
- Entity has business rules and behavior (not just data)
- Entity is the root of an aggregate (controls its lifecycle)
- You need full CRUD operations across all layers

**DO NOT use this skill when**:

- Adding a simple DTO (no business logic)
- Creating a read-only projection
- Entity is not an aggregate root (create as child entity instead)
- Refactoring existing aggregates (use refactoring workflows instead)

## Workflows

### Phase 1: Planning

#### Step 1: Gather Requirements

Answer these questions:

**Entity Name**: What is the aggregate called? (e.g., Product, Order, Invoice)
**Properties**: What data does it hold? (Name, Description, Price, Status, etc.)
**Value Objects**: Any complex types? (Email, Address, Money, etc.)
**Enumerations**: Any status/type fields? (OrderStatus, ProductCategory, etc.)
**Business Rules**: What validations? (Price > 0, Email format, Name required, etc.)
**Lifecycle Operations**: What changes occur? (Create, Update status, Change price, etc.)

**Example**:
```
Entity: Product
Properties: Name (string), Description (string), Price (decimal), SKU (string), Status (enum)
Value Objects: Money (for Price)
Enumerations: ProductStatus (Draft, Active, Retired)
Business Rules: Name required, Price > 0, SKU unique
Operations: Create, ChangeName, ChangeDescription, ChangePrice, ChangeStatus
```

#### Step 2: Create Todo Checklist

Based on requirements, create checklist:

```
[ ] Domain Layer
    [ ] Create Product aggregate
    [ ] Create Money value object (if needed)
    [ ] Create ProductStatus enumeration
    [ ] Create ProductCreatedDomainEvent
    [ ] Create ProductUpdatedDomainEvent
    [ ] Create ProductDeletedDomainEvent
[ ] Infrastructure Layer
    [ ] Create ProductTypeConfiguration (EF Core)
    [ ] Add DbSet<Product> to ModuleDbContext
    [ ] Generate and apply migration
[ ] Application Layer
    [ ] Create ProductCreateCommand + Handler
    [ ] Create ProductUpdateCommand + Handler
    [ ] Create ProductDeleteCommand + Handler
    [ ] Create ProductFindOneQuery + Handler
    [ ] Create ProductFindAllQuery + Handler
[ ] Mapping Layer
    [ ] Create ProductModel DTO
    [ ] Configure Product → ProductModel mapping
    [ ] Configure value object mappings
[ ] Presentation Layer
    [ ] Create ProductEndpoints (POST, GET, PUT, DELETE)
    [ ] Register endpoints in module
[ ] Validation
    [ ] Run build and fix errors
    [ ] Test endpoints via Swagger
```

### Phase 2: Domain Layer

#### Step 3: Create Aggregate Root Class

**File**: `src/Modules/[Module]/[Module].Domain/Model/[Entity]Aggregate/[Entity].cs`

Use template: [templates/aggregate-template.cs]

**Key Patterns**:

- Inherit from `AuditableAggregateRoot<[Entity]Id>`
- Apply `[TypedEntityId<Guid>]` attribute
- Private parameterless constructor (for EF Core)
- Private parameterized constructor (for factory)
- Public properties with private setters
- Factory method `Create()` returning `Result<[Entity]>`
- Change methods using `this.Change()` builder

**Example**:
```csharp
[TypedEntityId<Guid>]
public class Product : AuditableAggregateRoot<ProductId>, IConcurrency
{
    private Product() { }

    private Product(string name, string description, decimal price, string sku)
    {
        this.Name = name;
        this.Description = description;
        this.Price = price;
        this.SKU = sku;
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public string SKU { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;
    public Guid ConcurrencyVersion { get; set; }

    public static Result<Product> Create(string name, string description, decimal price, string sku)
    {
        return Result<Product>.Success()
            .Ensure(_ => !string.IsNullOrWhiteSpace(name), new ValidationError("Name is required"))
            .Ensure(_ => price > 0, new ValidationError("Price must be greater than zero"))
            .Ensure(_ => !string.IsNullOrWhiteSpace(sku), new ValidationError("SKU is required"))
            .Bind(_ => new Product(name, description, price, sku))
            .Tap(e => e.DomainEvents
                .Register(new ProductCreatedDomainEvent(e))
                .Register(new EntityCreatedDomainEvent<Product>(e)));
    }

    public Result<Product> ChangeName(string name)
    {
        return this.Change()
            .Ensure(_ => !string.IsNullOrWhiteSpace(name), "Name is required")
            .Set(e => e.Name, name)
            .Register(e => new ProductUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Product> ChangePrice(decimal price)
    {
        return this.Change()
            .Ensure(_ => price > 0, "Price must be greater than zero")
            .Set(e => e.Price, price)
            .Register(e => new ProductUpdatedDomainEvent(e))
            .Apply();
    }

    public Result<Product> ChangeStatus(ProductStatus status)
    {
        return this.Change()
            .When(_ => status != null)
            .Set(e => e.Status, status)
            .Register(e => new ProductUpdatedDomainEvent(e))
            .Apply();
    }
}
```

#### Step 4: Create Value Objects (if needed)

**File**: `src/Modules/[Module]/[Module].Domain/Model/[ValueObject].cs`

Use template: [templates/value-object-template.cs]

**Example (EmailAddress-like pattern)**:
```csharp
public class Money : ValueObject
{
    private Money() { }

    private Money(decimal amount, string currency)
    {
        this.Amount = amount;
        this.Currency = currency;
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        return Result<Money>.Success()
            .Ensure(_ => amount >= 0, new ValidationError("Amount cannot be negative"))
            .Ensure(_ => !string.IsNullOrWhiteSpace(currency), new ValidationError("Currency is required"))
            .Bind(_ => new Money(amount, currency));
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return this.Amount;
        yield return this.Currency;
    }
}
```

**When to skip**: If no complex types needed, skip this step.

#### Step 5: Create Enumeration (if needed)

**File**: `src/Modules/[Module]/[Module].Domain/Model/[Entity]Aggregate/[Entity]Status.cs`

Use template: [templates/enumeration-template.cs]

**Example**:
```csharp
public partial class ProductStatus : Enumeration
{
    public static readonly ProductStatus Draft = new(1, nameof(Draft), true, "Product is in draft");
    public static readonly ProductStatus Active = new(2, nameof(Active), true, "Product is active");
    public static readonly ProductStatus Retired = new(3, nameof(Retired), true, "Product is retired");

    public bool Enabled { get; }
    public string Description { get; }
}
```

**When to skip**: If no status/type field needed, skip this step.

#### Step 6: Create Domain Events

**Files**:

- `src/Modules/[Module]/[Module].Domain/Events/[Entity]CreatedDomainEvent.cs`
- `src/Modules/[Module]/[Module].Domain/Events/[Entity]UpdatedDomainEvent.cs`
- `src/Modules/[Module]/[Module].Domain/Events/[Entity]DeletedDomainEvent.cs`

Use template: [templates/domain-events-template.cs]

**Example**:
```csharp
public partial class ProductCreatedDomainEvent(Product model) : DomainEventBase
{
    public Product Model { get; private set; } = model;
}

public partial class ProductUpdatedDomainEvent(Product model) : DomainEventBase
{
    public Product Model { get; private set; } = model;
}

public partial class ProductDeletedDomainEvent(ProductId id) : DomainEventBase
{
    public ProductId Id { get; private set; } = id;
}
```

#### Step 7: Compile & Verify Domain Layer

```bash
dotnet build src/Modules/[Module]/[Module].Domain/
```

**Expected**: Build succeeds with no errors.

**If errors occur**:

- Check namespace declarations
- Verify bITdevKit using statements
- Confirm all required base classes are imported

**Validate using checklist**: [checklists/01-domain-layer.md]

### Phase 3: Infrastructure Layer

#### Step 8: Create EF Core Type Configuration

**File**: `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/Configurations/[Entity]TypeConfiguration.cs`

Use template: [templates/ef-configuration-template.cs]

**Key Patterns**:

- Implement `IEntityTypeConfiguration<[Entity]>`
- Configure table name: `builder.ToTable("[Entities]")`
- Configure primary key: `builder.HasKey(e => e.Id)`
- Configure typed ID conversion: `builder.Property(e => e.Id).ValueGeneratedOnAdd().HasConversion(...)`
- Configure value objects: `builder.OwnsOne(e => e.Money, ...)` or `builder.Property(e => e.Email).HasConversion(...)`
- Configure enumerations: `builder.Property(e => e.Status).HasConversion(...)`
- Ignore domain events: `builder.Ignore(e => e.DomainEvents)`
- Configure concurrency: `builder.Property(e => e.ConcurrencyVersion).IsConcurrencyToken()`

**Example**:
```csharp
public class ProductTypeConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd()
            .HasConversion(
                id => id.Value,
                value => ProductId.Create(value));

        builder.Property(e => e.Name).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Description).HasMaxLength(1024);
        builder.Property(e => e.Price).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SKU).IsRequired().HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion(
                s => s.Id,
                id => Enumeration.FromId<ProductStatus>(id));

        builder.Property(e => e.ConcurrencyVersion).IsConcurrencyToken();

        builder.Ignore(e => e.DomainEvents);
    }
}
```

#### Step 9: Add DbSet to Module DbContext

**File**: `src/Modules/[Module]/[Module].Infrastructure/EntityFramework/[Module]DbContext.cs`

Add DbSet property:
```csharp
public DbSet<Product> Products => this.Set<Product>();
```

#### Step 10: Generate EF Core Migration

```bash
# From solution root
dotnet ef migrations add Add[Entity] --project src/Modules/[Module]/[Module].Infrastructure --startup-project src/Presentation.Web.Server --context [Module]DbContext
```

**Example**:
```bash
dotnet ef migrations add AddProduct --project src/Modules/CoreModule/CoreModule.Infrastructure --startup-project src/Presentation.Web.Server --context CoreModuleDbContext
```

**Review migration file**: Verify it creates correct table and columns.

#### Step 11: Compile & Verify Infrastructure Layer

```bash
dotnet build src/Modules/[Module]/[Module].Infrastructure/
```

**Expected**: Build succeeds with no errors.

**Validate using checklist**: [checklists/02-infrastructure-layer.md]

### Phase 4: Application Layer

#### Step 12: Create Command (Create Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]CreateCommand.cs`

Use template: [templates/command-create-template.cs]

**Example**:
```csharp
public class ProductCreateCommand : IRequest<Result<ProductModel>>
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public string SKU { get; init; }
}

public class ProductCreateCommandValidator : AbstractValidator<ProductCreateCommand>
{
    public ProductCreateCommandValidator()
    {
        this.RuleFor(c => c.Name).NotEmpty().MaximumLength(256);
        this.RuleFor(c => c.Price).GreaterThan(0);
        this.RuleFor(c => c.SKU).NotEmpty().MaximumLength(50);
    }
}
```

#### Step 13: Create Command Handler (Create Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]CreateCommandHandler.cs`

Use template: [templates/command-create-handler-template.cs]

**Key Patterns**:

- Inherit from `RequestHandlerBase<TRequest, TResponse>`
- Inject `IGenericRepository<[Entity]>` and `IMapper`
- Apply `[Retry]` and `[Timeout]` attributes
- Use `Result<T>` pattern throughout
- Call aggregate factory method: `[Entity].Create(...)`
- Insert via repository: `await repository.InsertAsync(...)`
- Map to DTO: `mapper.Map<[Entity], [Entity]Model>(...)`

**Example**:
```csharp
[Retry(2)]
[Timeout(30)]
public class ProductCreateCommandHandler(IGenericRepository<Product> repository, IMapper mapper)
    : RequestHandlerBase<ProductCreateCommand, Result<ProductModel>>
{
    public override async Task<Result<ProductModel>> Process(
        ProductCreateCommand request,
        CancellationToken cancellationToken)
    {
        var productResult = Product.Create(
            request.Name,
            request.Description,
            request.Price,
            request.SKU);

        if (productResult.IsFailure)
        {
            return Result<ProductModel>.Failure().WithMessages(productResult.Messages);
        }

        var product = await repository.InsertAsync(productResult.Value, cancellationToken);

        return mapper.Map<Product, ProductModel>(product);
    }
}
```

#### Step 14: Create Command (Update Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]UpdateCommand.cs`

Use template: [templates/command-update-template.cs]

**Example**:
```csharp
public class ProductUpdateCommand : IRequest<Result<ProductModel>>
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
}

public class ProductUpdateCommandValidator : AbstractValidator<ProductUpdateCommand>
{
    public ProductUpdateCommandValidator()
    {
        this.RuleFor(c => c.Id).NotEmpty();
        this.RuleFor(c => c.Name).NotEmpty().MaximumLength(256);
        this.RuleFor(c => c.Price).GreaterThan(0);
    }
}
```

#### Step 15: Create Command Handler (Update Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]UpdateCommandHandler.cs`

Use template: [templates/command-update-handler-template.cs]

**Key Patterns**:

- Find entity: `await repository.FindOneAsync(id, ...)`
- Check if exists: Return `Result.Failure()` if not found
- Call change methods: `entity.ChangeName(...).ChangePrice(...)`
- Update via repository: `await repository.UpdateAsync(...)`

**Example**:
```csharp
[Retry(2)]
[Timeout(30)]
public class ProductUpdateCommandHandler(IGenericRepository<Product> repository, IMapper mapper)
    : RequestHandlerBase<ProductUpdateCommand, Result<ProductModel>>
{
    public override async Task<Result<ProductModel>> Process(
        ProductUpdateCommand request,
        CancellationToken cancellationToken)
    {
        var product = await repository.FindOneAsync(
            ProductId.Create(request.Id),
            cancellationToken: cancellationToken);

        if (product == null)
        {
            return Result<ProductModel>.Failure($"Product with ID {request.Id} not found");
        }

        var nameResult = product.ChangeName(request.Name);
        if (nameResult.IsFailure)
        {
            return Result<ProductModel>.Failure().WithMessages(nameResult.Messages);
        }

        var priceResult = product.ChangePrice(request.Price);
        if (priceResult.IsFailure)
        {
            return Result<ProductModel>.Failure().WithMessages(priceResult.Messages);
        }

        await repository.UpdateAsync(product, cancellationToken);

        return mapper.Map<Product, ProductModel>(product);
    }
}
```

#### Step 16: Create Command (Delete Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]DeleteCommand.cs`

Use template: [templates/command-delete-template.cs]

**Example**:
```csharp
public class ProductDeleteCommand : IRequest<Result>
{
    public Guid Id { get; init; }
}

public class ProductDeleteCommandValidator : AbstractValidator<ProductDeleteCommand>
{
    public ProductDeleteCommandValidator()
    {
        this.RuleFor(c => c.Id).NotEmpty();
    }
}
```

#### Step 17: Create Command Handler (Delete Operation)

**File**: `src/Modules/[Module]/[Module].Application/Commands/[Entity]DeleteCommandHandler.cs`

Use template: [templates/command-delete-handler-template.cs]

**Example**:
```csharp
[Retry(2)]
[Timeout(30)]
public class ProductDeleteCommandHandler(IGenericRepository<Product> repository)
    : RequestHandlerBase<ProductDeleteCommand, Result>
{
    public override async Task<Result> Process(
        ProductDeleteCommand request,
        CancellationToken cancellationToken)
    {
        var product = await repository.FindOneAsync(
            ProductId.Create(request.Id),
            cancellationToken: cancellationToken);

        if (product == null)
        {
            return Result.Failure($"Product with ID {request.Id} not found");
        }

        product.DomainEvents.Register(new ProductDeletedDomainEvent(product.Id));

        await repository.DeleteAsync(product, cancellationToken);

        return Result.Success();
    }
}
```

#### Step 18: Create Query (FindOne Operation)

**File**: `src/Modules/[Module]/[Module].Application/Queries/[Entity]FindOneQuery.cs`

Use template: [templates/query-findone-template.cs]

**Example**:
```csharp
public class ProductFindOneQuery : IRequest<Result<ProductModel>>
{
    public Guid Id { get; init; }
}

public class ProductFindOneQueryValidator : AbstractValidator<ProductFindOneQuery>
{
    public ProductFindOneQueryValidator()
    {
        this.RuleFor(q => q.Id).NotEmpty();
    }
}
```

#### Step 19: Create Query Handler (FindOne Operation)

**File**: `src/Modules/[Module]/[Module].Application/Queries/[Entity]FindOneQueryHandler.cs`

Use template: [templates/query-findone-handler-template.cs]

**Example**:
```csharp
[Timeout(30)]
public class ProductFindOneQueryHandler(IGenericRepository<Product> repository, IMapper mapper)
    : RequestHandlerBase<ProductFindOneQuery, Result<ProductModel>>
{
    public override async Task<Result<ProductModel>> Process(
        ProductFindOneQuery request,
        CancellationToken cancellationToken)
    {
        var product = await repository.FindOneAsync(
            ProductId.Create(request.Id),
            cancellationToken: cancellationToken);

        if (product == null)
        {
            return Result<ProductModel>.Failure($"Product with ID {request.Id} not found");
        }

        return mapper.Map<Product, ProductModel>(product);
    }
}
```

#### Step 20: Create Query (FindAll Operation)

**File**: `src/Modules/[Module]/[Module].Application/Queries/[Entity]FindAllQuery.cs`

Use template: [templates/query-findall-template.cs]

**Example**:
```csharp
public class ProductFindAllQuery : IRequest<Result<IEnumerable<ProductModel>>>
{
    // Optional: Add filtering/sorting parameters
}

public class ProductFindAllQueryValidator : AbstractValidator<ProductFindAllQuery>
{
    public ProductFindAllQueryValidator()
    {
        // No validation needed for basic find all
    }
}
```

#### Step 21: Create Query Handler (FindAll Operation)

**File**: `src/Modules/[Module]/[Module].Application/Queries/[Entity]FindAllQueryHandler.cs`

Use template: [templates/query-findall-handler-template.cs]

**Example**:
```csharp
[Timeout(30)]
public class ProductFindAllQueryHandler(IGenericRepository<Product> repository, IMapper mapper)
    : RequestHandlerBase<ProductFindAllQuery, Result<IEnumerable<ProductModel>>>
{
    public override async Task<Result<IEnumerable<ProductModel>>> Process(
        ProductFindAllQuery request,
        CancellationToken cancellationToken)
    {
        var products = await repository.FindAllAsync(cancellationToken: cancellationToken);

        return Result<IEnumerable<ProductModel>>.Success(
            products.Select(p => mapper.Map<Product, ProductModel>(p)));
    }
}
```

#### Step 22: Compile & Verify Application Layer

```bash
dotnet build src/Modules/[Module]/[Module].Application/
```

**Expected**: Build succeeds with no errors.

**Validate using checklist**: [checklists/03-application-layer.md]

### Phase 5: Mapping Layer

#### Step 23: Create DTO Model

**File**: `src/Modules/[Module]/[Module].Presentation/[Entity]Model.cs`

Use template: [templates/model-template.cs]

**Example**:
```csharp
public class ProductModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string SKU { get; set; }
    public string Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

#### Step 24: Update Mapper Register

**File**: `src/Modules/[Module]/[Module].Presentation/[Module]MapperRegister.cs`

Use template: [templates/mapper-register-template.cs]

Add mapping configurations:
```csharp
public class CoreModuleMapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Existing mappings...

        // Product mappings
        config.ForType<Product, ProductModel>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Status, src => src.Status.Name);

        // Value object conversions (if applicable)
        config.NewConfig<Money, decimal>()
            .MapWith(src => src.Amount);
    }
}
```

#### Step 25: Compile & Verify Mapping Layer

```bash
dotnet build src/Modules/[Module]/[Module].Presentation/
```

**Expected**: Build succeeds with no errors.

**Validate using checklist**: [checklists/04-mapping-layer.md]

### Phase 6: Presentation Layer

#### Step 26: Create API Endpoints

**File**: `src/Modules/[Module]/[Module].Presentation/Web/Endpoints/[Entity]Endpoints.cs`

Use template: [templates/endpoint-template.cs]

**Example**:
```csharp
public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("api/core/products", CreateProduct)
            .WithName("CreateProduct")
            .WithTags("Products")
            .Produces<ProductModel>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        app.MapGet("api/core/products/{id:guid}", GetProduct)
            .WithName("GetProduct")
            .WithTags("Products")
            .Produces<ProductModel>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        app.MapGet("api/core/products", GetAllProducts)
            .WithName("GetAllProducts")
            .WithTags("Products")
            .Produces<IEnumerable<ProductModel>>(StatusCodes.Status200OK);

        app.MapPut("api/core/products/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithTags("Products")
            .Produces<ProductModel>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        app.MapDelete("api/core/products/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithTags("Products")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateProduct(
        [FromServices] IRequester requester,
        [FromBody] ProductCreateCommand command)
    {
        var result = await requester.RequestAsync(command);
        return result.IsSuccess
            ? Results.Created($"/api/core/products/{result.Value.Id}", result.Value)
            : Results.BadRequest(result.ToProblemDetails());
    }

    private static async Task<IResult> GetProduct(
        [FromServices] IRequester requester,
        Guid id)
    {
        var result = await requester.RequestAsync(new ProductFindOneQuery { Id = id });
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.ToProblemDetails());
    }

    private static async Task<IResult> GetAllProducts(
        [FromServices] IRequester requester)
    {
        var result = await requester.RequestAsync(new ProductFindAllQuery());
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(result.ToProblemDetails());
    }

    private static async Task<IResult> UpdateProduct(
        [FromServices] IRequester requester,
        Guid id,
        [FromBody] ProductUpdateCommand command)
    {
        var commandWithId = command with { Id = id };
        var result = await requester.RequestAsync(commandWithId);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(result.ToProblemDetails());
    }

    private static async Task<IResult> DeleteProduct(
        [FromServices] IRequester requester,
        Guid id)
    {
        var result = await requester.RequestAsync(new ProductDeleteCommand { Id = id });
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(result.ToProblemDetails());
    }
}
```

#### Step 27: Register Endpoints in Module

**File**: `src/Modules/[Module]/[Module].Presentation/[Module]Module.cs`

Add endpoint registration:
```csharp
app.MapProductEndpoints();
```

#### Step 28: Compile & Verify Presentation Layer

```bash
dotnet build src/Modules/[Module]/[Module].Presentation/
```

**Expected**: Build succeeds with no errors.

**Validate using checklist**: [checklists/05-presentation-layer.md]

### Phase 7: Final Validation

#### Step 29: Build Entire Solution

```bash
dotnet build
```

**Expected**: Build succeeds with no errors.

**If errors occur**:

- Review error messages carefully
- Common issues: missing using statements, typos in class/property names, incorrect namespaces
- Use `dotnet build --verbosity detailed` for more information

#### Step 30: Apply Migrations (if not already applied)

```bash
dotnet ef database update --project src/Modules/[Module]/[Module].Infrastructure --startup-project src/Presentation.Web.Server --context [Module]DbContext
```

**Expected**: Database updated with new table.

#### Step 31: Run Application and Test with Swagger

```bash
dotnet run --project src/Presentation.Web.Server
```

Navigate to: `https://localhost:5001/swagger`

**Test Endpoints**:

1. **POST /api/core/products**: Create new product
2. **GET /api/core/products**: List all products
3. **GET /api/core/products/{id}**: Get single product
4. **PUT /api/core/products/{id}**: Update product
5. **DELETE /api/core/products/{id}**: Delete product

**Validate using checklist**: [checklists/quality-gates.md]

#### Step 32: Mark Complete

Congratulations! You've successfully created a complete Domain Aggregate with full CRUD operations across all five layers.

**Created Files Summary** (for Product example):
```
Domain Layer (7 files):
- Product.cs (aggregate)
- Money.cs (value object, if created)
- ProductStatus.cs (enumeration)
- ProductCreatedDomainEvent.cs
- ProductUpdatedDomainEvent.cs
- ProductDeletedDomainEvent.cs

Infrastructure Layer (2 files):
- ProductTypeConfiguration.cs
- Add[Entity] migration

Application Layer (10 files):
- ProductCreateCommand.cs + ProductCreateCommandHandler.cs
- ProductUpdateCommand.cs + ProductUpdateCommandHandler.cs
- ProductDeleteCommand.cs + ProductDeleteCommandHandler.cs
- ProductFindOneQuery.cs + ProductFindOneQueryHandler.cs
- ProductFindAllQuery.cs + ProductFindAllQueryHandler.cs

Presentation Layer (2 files):
- ProductModel.cs (DTO)
- ProductEndpoints.cs

Total: ~21 files
```

## Examples

### Example 1: Simple Aggregate (Product)

**User**: "Add a Product aggregate with Name, Description, Price, and SKU properties"

**Actions**:

1. Planning: Identify no value objects needed, ProductStatus enumeration needed
2. Domain: Create Product.cs, ProductStatus.cs, 3 domain events
3. Infrastructure: Create ProductTypeConfiguration.cs, generate migration
4. Application: Create 5 command/query pairs with handlers
5. Mapping: Create ProductModel.cs, update MapperRegister
6. Presentation: Create ProductEndpoints.cs
7. Validation: Build, test with Swagger

**Result**: Complete Product aggregate with CRUD operations (~20 files)

### Example 2: Aggregate with Value Object (Customer)

**User**: "Add a Customer aggregate with FirstName, LastName, Email (value object), and Status"

**Actions**:

1. Planning: EmailAddress value object needed, CustomerStatus enumeration needed
2. Domain: Create Customer.cs, EmailAddress.cs, CustomerStatus.cs, 3 domain events
3. Infrastructure: Create CustomerTypeConfiguration.cs (with value object conversion), generate migration
4. Application: Create 5 command/query pairs with handlers
5. Mapping: Create CustomerModel.cs, update MapperRegister (with EmailAddress → string conversion)
6. Presentation: Create CustomerEndpoints.cs
7. Validation: Build, test with Swagger

**Result**: Complete Customer aggregate with value object (~22 files)

## Quality Checklist

Before considering aggregate complete:

- [ ] **Domain Layer**: Aggregate compiles, factory method returns Result<T>, change methods use Change() builder
- [ ] **Infrastructure Layer**: EF configuration complete, migration generated and applied
- [ ] **Application Layer**: Commands/Queries/Handlers compile, validators present, retry/timeout attributes applied
- [ ] **Mapping Layer**: DTO model created, Mapster configurations registered
- [ ] **Presentation Layer**: Endpoints created (POST, GET, PUT, DELETE), registered in module
- [ ] **Build**: Solution builds without errors
- [ ] **Database**: Migration applied successfully
- [ ] **Swagger**: All endpoints visible and testable
- [ ] **Functional Test**: Can create, read, update, delete via Swagger

## Common Pitfalls

**WRONG:**

- Public constructors on aggregates (use factory methods)
- Public setters on properties (use private setters)
- Forgetting to register domain events
- Skipping EF configuration (fluent API required)
- Manual mapping in handlers (use IMapper)
- Missing validators on commands/queries
- Forgetting to compile after each layer

**CORRECT:**

- Private constructors + factory methods returning Result<T>
- Private setters on all properties
- Domain events registered in factory and change methods
- Complete EF configuration with typed ID conversion
- Mapster for all domain ↔ DTO mapping
- FluentValidation validators on all commands/queries
- Incremental compilation and validation

## File Naming Conventions

- **Aggregate**: `[Entity].cs` (e.g., `Product.cs`)
- **Value Object**: `[Name].cs` (e.g., `EmailAddress.cs`)
- **Enumeration**: `[Entity][Type].cs` (e.g., `ProductStatus.cs`)
- **Domain Event**: `[Entity][Action]DomainEvent.cs` (e.g., `ProductCreatedDomainEvent.cs`)
- **EF Configuration**: `[Entity]TypeConfiguration.cs` (e.g., `ProductTypeConfiguration.cs`)
- **Command**: `[Entity][Action]Command.cs` (e.g., `ProductCreateCommand.cs`)
- **Command Handler**: `[Entity][Action]CommandHandler.cs` (e.g., `ProductCreateCommandHandler.cs`)
- **Query**: `[Entity][Action]Query.cs` (e.g., `ProductFindOneQuery.cs`)
- **Query Handler**: `[Entity][Action]QueryHandler.cs` (e.g., `ProductFindOneQueryHandler.cs`)
- **DTO Model**: `[Entity]Model.cs` (e.g., `ProductModel.cs`)
- **Endpoints**: `[Entity]Endpoints.cs` (e.g., `ProductEndpoints.cs`)

## References

### Project Documentation

- **Project Architecture**: `AGENTS.md`
- **Module Structure**: `src/Modules/CoreModule/CoreModule-README.md`
- **ADRs**: `docs/ADR/` (especially ADR-0001, ADR-0003, ADR-0005, ADR-0012)

### bITdevKit Documentation

- **bITdevKit GitHub**: https://github.com/BridgingIT-GmbH/bITdevKit
- **bITdevKit Docs**: https://github.com/BridgingIT-GmbH/bITdevKit/tree/main/docs

### External References

- **Clean Architecture**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
- **Domain-Driven Design**: https://martinfowler.com/bliki/DomainDrivenDesign.html
- **Result Pattern**: https://enterprisecraftsmanship.com/posts/error-handling-exception-or-result/
- **CQRS**: https://martinfowler.com/bliki/CQRS.html

### Templates

- See: `.github/skills/domain-add-aggregate/templates/` for all code templates

### Examples

- See: `.github/skills/domain-add-aggregate/examples/` for complete walkthroughs

### Checklists

- See: `.github/skills/domain-add-aggregate/checklists/` for layer-specific validation
