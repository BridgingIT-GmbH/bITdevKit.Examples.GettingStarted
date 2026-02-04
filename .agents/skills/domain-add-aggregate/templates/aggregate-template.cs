// Template: Domain Aggregate Root
// Replace placeholders: [Entity], [Module], [Property], [PropertyType]
// Example: [Entity] = Product, [Module] = CoreModule, [Property] = Name, [PropertyType] = string

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

using BridgingIT.DevKit.Domain;

/// <summary>
/// Represents a [Entity] aggregate root in the domain.
/// Aggregates encapsulate business logic and maintain invariants.
/// </summary>
[TypedEntityId<Guid>] // Pattern: Generates strongly-typed ID wrapper ([Entity]Id)
public class [Entity] : AuditableAggregateRoot<[Entity]Id>, IConcurrency
{
    // Pattern: Private parameterless constructor for EF Core
    // EF Core requires this to materialize entities from database
    private [Entity]() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="[Entity]"/> class.
    /// Private constructor enforces controlled creation via factory method.
    /// </summary>
    /// <param name="[property]">The [property] of the [entity].</param>
    private [Entity]([PropertyType] [property], [PropertyType2] [property2])
    {
        // Pattern: Direct assignment in private constructor (validation done in factory)
        this.[Property] = [property];
        this.[Property2] = [property2];
    }

    /// <summary>
    /// Gets the [property] of the [entity].
    /// </summary>
    public [PropertyType] [Property] { get; private set; }

    /// <summary>
    /// Gets the [property2] of the [entity].
    /// </summary>
    public [PropertyType2] [Property2] { get; private set; }

    /// <summary>
    /// Gets the current status of the [entity].
    /// </summary>
    public [Entity]Status Status { get; private set; } = [Entity]Status.Draft; // Pattern: Default enumeration value

    /// <summary>
    /// Gets or sets the concurrency version token for optimistic concurrency control.
    /// EF Core uses this for conflict detection during updates.
    /// </summary>
    public Guid ConcurrencyVersion { get; set; }

    /// <summary>
    /// Factory method to create a new <see cref="[Entity]"/> aggregate.
    /// Validates business rules and registers domain events.
    /// </summary>
    /// <param name="[property]">The [property] of the [entity].</param>
    /// <param name="[property2]">The [property2] of the [entity].</param>
    /// <returns>A Result containing the new [Entity] instance or validation errors.</returns>
    public static Result<[Entity]> Create([PropertyType] [property], [PropertyType2] [property2])
    {
        // Pattern: Result chaining with .Ensure() for validation, .Bind() for success, .Tap() for side effects
        return Result<[Entity]>.Success()
            // Pattern: .Ensure() validates business rules (returns Result.Failure if rule fails)
            .Ensure(_ => !string.IsNullOrWhiteSpace([property]), new ValidationError("[Property] is required"))
            .Ensure(_ => [property2] != null, new ValidationError("[Property2] cannot be null"))
            // Add more validation rules here
            // Pattern: .Bind() creates the instance (only called if all Ensure() checks pass)
            .Bind(_ => new [Entity]([property], [property2]))
            // Pattern: .Tap() registers domain events (side effect that doesn't change the Result)
            .Tap(e => e.DomainEvents
                .Register(new [Entity]CreatedDomainEvent(e))         // Aggregate-specific event
                .Register(new EntityCreatedDomainEvent<[Entity]>(e))); // Generic bITdevKit event
    }

    /// <summary>
    /// Changes the [property] of the [entity] if different from current value.
    /// Registers a <see cref="[Entity]UpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="[property]">The new [property] value.</param>
    /// <returns>The current [Entity] instance for method chaining.</returns>
    public Result<[Entity]> Change[Property]([PropertyType] [property])
    {
        // Pattern: this.Change() builder provides fluent API for updates with validation
        return this.Change()
            // Pattern: .Ensure() validates the new value
            .Ensure(_ => !string.IsNullOrWhiteSpace([property]), "[Property] is required")
            // Pattern: .Set() assigns the new value (only if different from current)
            .Set(e => e.[Property], [property])
            // Pattern: .Register() adds domain event (only if value actually changed)
            .Register(e => new [Entity]UpdatedDomainEvent(e))
            // Pattern: .Apply() finalizes the change and returns Result<[Entity]>
            .Apply();
    }

    /// <summary>
    /// Changes the [property2] of the [entity] if different from current value.
    /// Registers a <see cref="[Entity]UpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="[property2]">The new [property2] value.</param>
    /// <returns>The current [Entity] instance for method chaining.</returns>
    public Result<[Entity]> Change[Property2]([PropertyType2] [property2])
    {
        return this.Change()
            .Ensure(_ => [property2] != null, "[Property2] cannot be null")
            .Set(e => e.[Property2], [property2])
            .Register(e => new [Entity]UpdatedDomainEvent(e))
            .Apply();
    }

    /// <summary>
    /// Changes the status of the [entity] if different from current value.
    /// Registers a <see cref="[Entity]UpdatedDomainEvent"/> if changed.
    /// </summary>
    /// <param name="status">The new status.</param>
    /// <returns>The current [Entity] instance for method chaining.</returns>
    public Result<[Entity]> ChangeStatus([Entity]Status status)
    {
        return this.Change()
            // Pattern: .When() conditionally applies change (only if condition true)
            .When(_ => status != null)
            .Set(e => e.Status, status)
            .Register(e => new [Entity]UpdatedDomainEvent(e))
            .Apply();
    }
}

// Key Patterns Summary:
// 1. Typed IDs: [TypedEntityId<Guid>] generates [Entity]Id strongly-typed wrapper
// 2. Private Constructors: Enforce factory method usage
// 3. Private Setters: Immutability from outside aggregate
// 4. Factory Method: Create() returns Result<T> with validation
// 5. Result Chaining: .Ensure().Bind().Tap() pattern
// 6. Change Methods: Use this.Change() builder for updates
// 7. Domain Events: Registered in Create() and Change*() methods
// 8. Concurrency: IConcurrency interface with ConcurrencyVersion property
//
// Validation Patterns:
// - .Ensure(_ => condition, "error message") for simple validation
// - .Ensure(_ => condition, new ValidationError("message")) for typed errors
// - Multiple .Ensure() calls chain together (all must pass)
//
// Change() Builder Methods:
// - .Ensure() - Validate before change
// - .When() - Conditionally apply change
// - .Set() - Assign new value (only if different)
// - .Register() - Add domain event (only if changed)
// - .Apply() - Finalize and return Result<[Entity]>
