// ============================================================================
// TEMPLATE: Domain Events for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Provides three standard domain event classes for tracking lifecycle changes
//   to an aggregate: Created, Updated, and Deleted.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all [Module] and [Entity] placeholders
//   2. Place in: src/Modules/[Module]/[Module].Domain/Events/
//   3. Name files: [Entity]CreatedDomainEvent.cs, [Entity]UpdatedDomainEvent.cs, [Entity]DeletedDomainEvent.cs
//   4. Events are registered in aggregate methods using: this.DomainEvents.Register(new [Entity]CreatedDomainEvent(this))
//   5. Events are published by repository behaviors or handlers after persistence
//
// RELATED PATTERNS:
//   - DomainEventBase: bITdevKit base class for all domain events
//   - DomainEventHandlerBase<T>: Base class for implementing event handlers
//   - Outbox Pattern: Events are persisted and published reliably
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Events;

// ============================================================================
// [Entity]CreatedDomainEvent
// ============================================================================

/// <summary>
/// Domain event that is raised whenever a new <see cref="[Entity]"/> aggregate has been created.
/// </summary>
/// <remarks>
/// Follows the Domain Events pattern in DDD:
/// - Published by the <see cref="[Entity]"/> aggregate in its Create factory method.
/// - Consumed by one or more <see cref="DomainEventHandlerBase{TEvent}"/>
///   implementations to trigger side effects such as creating projections, sending notifications or audit logging.
/// </remarks>
[ExcludeFromCodeCoverage]
public partial class [Entity]CreatedDomainEvent([Entity] model) : DomainEventBase
{
    /// <summary>
    /// Gets the created <see cref="[Entity]"/> aggregate instance that triggered this event.
    /// </summary>
    public [Entity] Model { get; private set; } = model;
}

// ============================================================================
// [Entity]UpdatedDomainEvent
// ============================================================================

/// <summary>
/// Domain event that is raised whenever an existing <see cref="[Entity]"/> aggregate has been updated.
/// </summary>
/// <remarks>
/// Follows the Domain Events pattern in DDD:
/// - Published by the <see cref="[Entity]"/> aggregate in change methods (e.g., ChangeName, ChangeStatus).
/// - Consumed by one or more <see cref="DomainEventHandlerBase{TEvent}"/>
///   implementations to trigger side effects such as updating projections, sending notifications or audit logging.
/// </remarks>
[ExcludeFromCodeCoverage]
public partial class [Entity]UpdatedDomainEvent([Entity] model) : DomainEventBase
{
    /// <summary>
    /// Gets the updated <see cref="[Entity]"/> aggregate instance that triggered this event.
    /// </summary>
    public [Entity] Model { get; private set; } = model;
}

// ============================================================================
// [Entity]DeletedDomainEvent
// ============================================================================

/// <summary>
/// Domain event that is raised whenever an existing <see cref="[Entity]"/> aggregate has been deleted.
/// </summary>
/// <remarks>
/// Follows the Domain Events pattern in DDD:
/// - Published by delete command handlers before removing the aggregate from persistence.
/// - Consumed by one or more <see cref="DomainEventHandlerBase{TEvent}"/>
///   implementations to trigger side effects such as cleanup operations, updating projections or audit logging.
/// </remarks>
[ExcludeFromCodeCoverage]
public partial class [Entity]DeletedDomainEvent([Entity] model) : DomainEventBase
{
    /// <summary>
    /// Gets the deleted <see cref="[Entity]"/> aggregate instance that triggered this event.
    /// </summary>
    public [Entity] Model { get; private set; } = model;
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. PRIMARY CONSTRUCTOR SYNTAX (C# 12+):
//    - Concise: public partial class [Entity]CreatedDomainEvent([Entity] model) : DomainEventBase
//    - No explicit constructor body needed
//
// 2. IMMUTABLE EVENT DATA:
//    - Property: public [Entity] Model { get; private set; } = model;
//    - External code can read but not modify the aggregate reference
//
// 3. DOMAIN EVENT BASE CLASS:
//    - Inherits from DomainEventBase (bITdevKit)
//    - Provides: EventId, Timestamp, AggregateId, metadata
//
// 4. EXCLUDE FROM CODE COVERAGE:
//    - [ExcludeFromCodeCoverage] attribute
//    - Events are simple DTOs with minimal logic
//
// 5. PARTIAL CLASS:
//    - Allows future extension (source generators, extensions)
//
// USAGE EXAMPLES:
//
//   // In aggregate Create factory method:
//   var entity = new [Entity](...);
//   entity.DomainEvents.Register(new [Entity]CreatedDomainEvent(entity));
//   return Result<[Entity]>.Success(entity);
//
//   // In aggregate Change method:
//   public Result<[Entity]> ChangeName(string newName)
//   {
//       return this.Change()
//           .Ensure(() => !string.IsNullOrWhiteSpace(newName), "Name cannot be empty")
//           .Set(() => this.Name = newName)
//           .Register(new [Entity]UpdatedDomainEvent(this))
//           .Apply();
//   }
//
//   // In delete command handler:
//   entity.DomainEvents.Register(new [Entity]DeletedDomainEvent(entity));
//   await repository.DeleteResultAsync(entity, cancellationToken);
//
// ============================================================================
