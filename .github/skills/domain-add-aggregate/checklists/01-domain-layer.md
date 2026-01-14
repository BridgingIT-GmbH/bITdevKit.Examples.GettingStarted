# Domain Layer Checklist

This checklist ensures your domain layer implementation follows DDD principles, bITdevKit patterns, and Clean Architecture boundaries.

## Aggregate Root

### Structure
- [ ] Inherits from `AuditableAggregateRoot<TId>` (or `AggregateRoot<TId>`)
- [ ] Applies `[TypedEntityId<Guid>]` attribute for typed ID generation
- [ ] Has private parameterless constructor (for EF Core)
- [ ] Has private parameterized constructor (for factory method)
- [ ] Properties have private setters (encapsulation)
- [ ] No public fields (encapsulation)

### Factory Method
- [ ] Static `Create()` factory method exists
- [ ] Returns `Result<TAggregate>` (not throws exceptions)
- [ ] Validates all required properties
- [ ] Creates/validates value objects before constructing aggregate
- [ ] Registers `[Entity]CreatedDomainEvent` before returning
- [ ] Uses Result chaining (`.Bind()`, `.Ensure()`, `.Map()`, `.Tap()`)

### Change Methods
- [ ] Public change methods return `Result<TAggregate>` (not void)
- [ ] Follow pattern: `this.Change().Ensure().Set().Register().Apply()`
- [ ] Validate business rules before applying changes
- [ ] Register `[Entity]UpdatedDomainEvent` after changes
- [ ] Do not expose setters (only change methods modify state)
- [ ] Method names follow convention: `Change[Property]`, `Add[Child]`, `Remove[Child]`

### Business Rules & Invariants
- [ ] Required fields validated in factory and change methods
- [ ] Format constraints enforced (e.g., length limits, patterns)
- [ ] Business rules checked using `RuleSet` or custom `Rule` classes
- [ ] Cross-property validation (e.g., "firstname cannot equal lastname")
- [ ] State transition rules enforced (e.g., status changes)
- [ ] Collection invariants maintained (e.g., "only one primary address")

### Child Entities (if applicable)
- [ ] Child collection has private backing field (`private readonly List<T>`)
- [ ] Public property exposes `IReadOnlyCollection<T>` (prevents external modification)
- [ ] Add method: `Add[Child]()` returns `Result<TAggregate>`
- [ ] Remove method: `Remove[Child]()` returns `Result<TAggregate>`
- [ ] Change method: `Change[Child]()` returns `Result<TAggregate>` (if needed)
- [ ] Child entities created via their own `Create()` factory
- [ ] Collection changes register `[Entity]UpdatedDomainEvent`

### Domain Events
- [ ] `DomainEvents` property inherited from `AggregateRoot<TId>`
- [ ] Created event registered in factory method
- [ ] Updated event registered in all change methods
- [ ] Deleted event registered in delete handlers (not aggregate)
- [ ] Events do not mutate aggregate state
- [ ] Events are ignored in EF Core configuration

### Concurrency
- [ ] `ConcurrencyVersion` property inherited from `AggregateRoot<TId>`
- [ ] No manual management of concurrency version (EF Core handles it)
- [ ] Change methods do not modify `ConcurrencyVersion` directly

## Value Objects

### Structure
- [ ] Inherits from `ValueObject` base class
- [ ] Properties have private setters (immutability)
- [ ] Private constructor (forces use of factory)
- [ ] Public static `Create()` factory method
- [ ] Returns `Result<TValueObject>` with validation

### Factory Method
- [ ] Validates all properties (null checks, format, ranges)
- [ ] Normalizes values (e.g., `ToLowerInvariant()` for emails)
- [ ] Returns specific validation errors (not generic messages)
- [ ] Uses Result chaining for multiple validations

### Equality
- [ ] Implements `GetAtomicValues()` returning properties for equality
- [ ] Two instances with same values are equal (`==`, `.Equals()`)
- [ ] `GetHashCode()` consistent with equality

### Conversions (optional)
- [ ] Implicit operator to primitive type (e.g., `string`) if appropriate
- [ ] Explicit operator from primitive if appropriate
- [ ] `ToString()` override returns meaningful representation

### Examples Checklist
- [ ] Single-property value objects (e.g., EmailAddress, PhoneNumber)
- [ ] Multi-property value objects (e.g., Money with amount + currency)
- [ ] Formatted value objects (e.g., CustomerNumber with prefix-year-sequence)

## Enumerations

### Structure
- [ ] Inherits from `Enumeration` base class
- [ ] Static readonly instances for each value (e.g., `CustomerStatus.Active`)
- [ ] Private constructor (prevents external instantiation)
- [ ] Additional properties beyond Id/Value (e.g., `Enabled`, `Description`)

### Instances
- [ ] All valid values defined as static readonly fields
- [ ] Consistent ID numbering (sequential: 1, 2, 3...)
- [ ] Meaningful Value names (e.g., "Active", "Inactive")

### Methods (if needed)
- [ ] Custom methods for business logic (e.g., `CanTransitionTo()`)
- [ ] No static mutable state (enumerations are immutable)

## Domain Events

### Structure
- [ ] Inherits from `DomainEventBase`
- [ ] Uses primary constructor syntax (C# 12+)
- [ ] Marked as `partial` class (allows source generator extensions)
- [ ] Marked as `[ExcludeFromCodeCoverage]` (simple DTOs)

### Properties
- [ ] `Model` property holds reference to aggregate
- [ ] Property has `private set` (immutability)
- [ ] No additional mutable state

### Naming Convention
- [ ] Follows pattern: `[Entity][PastTenseAction]DomainEvent`
- [ ] Examples: `CustomerCreatedDomainEvent`, `OrderShippedDomainEvent`

## Business Rules

### Custom Rules (if needed)
- [ ] Implements `IBusinessRule` or inherits from `RuleBase`
- [ ] Async rules return `Task<bool>`
- [ ] Rules return `ValidationError` on failure
- [ ] Rules are testable in isolation

### Rule Placement
- [ ] Simple rules: Use `RuleSet.IsNotEmpty()`, `RuleSet.NotEqual()` inline
- [ ] Complex rules: Create custom rule class (e.g., `EmailShouldBeUniqueRule`)
- [ ] Cross-aggregate rules: Implement as separate rule classes with repository access

## File Organization

### Folder Structure
- [ ] Aggregate roots in: `Domain/Model/[Entity]Aggregate/[Entity].cs`
- [ ] Value objects in: `Domain/Model/[ValueObject].cs`
- [ ] Enumerations in: `Domain/Model/[Entity]Aggregate/[Entity][Enumeration].cs`
- [ ] Domain events in: `Domain/Events/[Entity][Action]DomainEvent.cs`
- [ ] Rules in: `Domain/Rules/[RuleName]Rule.cs`

### Naming Conventions
- [ ] Aggregate root: `[Entity]` (e.g., `Customer`, `Order`)
- [ ] Typed ID: `[Entity]Id` (e.g., `CustomerId`, `OrderId`)
- [ ] Value object: Descriptive name (e.g., `EmailAddress`, `Money`)
- [ ] Enumeration: `[Entity][Concept]` (e.g., `CustomerStatus`, `OrderStatus`)
- [ ] Domain event: `[Entity][PastTenseAction]DomainEvent`

## Clean Architecture Boundaries

### Dependencies
- [ ] Domain layer has NO references to:
  - Infrastructure layer
  - Application layer
  - Presentation layer
  - External libraries (except bITdevKit abstractions)
- [ ] Only references:
  - bITdevKit.Domain
  - System libraries (no third-party dependencies)

### Persistence Ignorance
- [ ] No EF Core attributes on domain entities
- [ ] No `DbContext` references
- [ ] No database-specific code
- [ ] No repository implementations (only abstractions if needed)

### No Infrastructure Concerns
- [ ] No HTTP/API concerns
- [ ] No serialization attributes (e.g., `[JsonProperty]`)
- [ ] No logging infrastructure (only abstractions if absolutely needed)
- [ ] No caching logic

## Testing

### Unit Tests
- [ ] Factory method tests (valid data → success)
- [ ] Factory method tests (invalid data → failure with correct errors)
- [ ] Change method tests (valid changes → success)
- [ ] Change method tests (invalid changes → failure)
- [ ] Business rule tests (rules pass/fail correctly)
- [ ] Value object equality tests
- [ ] Enumeration tests (GetAll, FromId, FromValue)

### Test Coverage
- [ ] Factory methods: 100% coverage
- [ ] Change methods: 100% coverage
- [ ] Business rules: 100% coverage
- [ ] Value object validation: 100% coverage

## Common Anti-Patterns to Avoid

### Anemic Domain Model
- [ ] WRONG: Public setters with no validation
- [ ] WRONG: All logic in services, entities are just data containers
- [ ] CORRECT: Rich domain model with behavior and validation

### Primitive Obsession
- [ ] WRONG: Using `string` for email, phone, IDs everywhere
- [ ] CORRECT: Value objects for domain concepts

### Breaking Encapsulation
- [ ] WRONG: Public setters allowing invalid state
- [ ] WRONG: Exposing mutable collections (List<T>)
- [ ] CORRECT: Private setters, change methods, IReadOnlyCollection<T>

### Exception-Driven Validation
- [ ] WRONG: Throwing exceptions for validation errors
- [ ] CORRECT: Returning Result<T> with validation errors

### Aggregate Boundary Violations
- [ ] WRONG: Direct access to child entity from outside aggregate
- [ ] WRONG: Repository for child entities
- [ ] CORRECT: All child access through aggregate root

## Code Quality

### Readability
- [ ] XML documentation on all public members
- [ ] Meaningful variable/parameter names
- [ ] Methods focused on single responsibility
- [ ] Complex logic extracted to private methods

### Maintainability
- [ ] No duplicated validation logic
- [ ] Consistent naming conventions
- [ ] Clear separation of concerns
- [ ] Easy to add new properties/behavior

### Performance
- [ ] No N+1 query patterns in domain logic
- [ ] No unnecessary object allocations in hot paths
- [ ] Value objects cached if frequently used (e.g., enumeration instances)

## Final Review

### Before Committing
- [ ] All tests pass
- [ ] No compiler warnings
- [ ] Code follows .editorconfig rules
- [ ] No TODO comments remain
- [ ] Domain logic is pure (no infrastructure concerns)
- [ ] All business rules enforced in domain layer
- [ ] Factory and change methods use Result pattern
- [ ] Domain events registered appropriately

### Architecture Validation
- [ ] No circular dependencies
- [ ] No references to outer layers
- [ ] Aggregate boundaries respected
- [ ] Value objects properly implemented
- [ ] Enumerations used instead of magic strings/ints
