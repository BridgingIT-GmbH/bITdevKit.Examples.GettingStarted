// Template: Enumeration
// Replace placeholders: [Entity], [Status]
// Example: [Entity] = Product, [Status] = ProductStatus

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Represents the status/type of a <see cref="[Entity]"/> in the domain.
/// Enumerations provide type-safe, rich enums with additional properties.
/// </summary>
[DebuggerDisplay("Id={Id}, Value={Value}")] // Pattern: Shows ID and name in debugger
public partial class [Entity]Status : Enumeration
{
    // Pattern: Static readonly instances define all valid values
    // Format: new(id, name, enabled, description)
    
    /// <summary>Status indicating [description of status 1].</summary>
    public static readonly [Entity]Status Draft = new(1, nameof(Draft), true, "Draft [entity]");

    /// <summary>Status indicating [description of status 2].</summary>
    public static readonly [Entity]Status Active = new(2, nameof(Active), true, "Active [entity]");

    /// <summary>Status indicating [description of status 3].</summary>
    public static readonly [Entity]Status Retired = new(3, nameof(Retired), true, "Retired [entity]");

    // Add more status values as needed:
    // public static readonly [Entity]Status Archived = new(4, nameof(Archived), false, "Archived [entity]");

    /// <summary>
    /// Gets a flag indicating whether the status is enabled/active.
    /// Can be used for soft deletion or disabling statuses.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the human-readable description of this status.
    /// Useful for display in UI or audit logs.
    /// </summary>
    public string Description { get; }
}

// Key Patterns Summary:
// 1. Inherit from Enumeration: bITdevKit base class provides ID, Name, equality
// 2. Static Readonly Instances: All valid values defined at compile-time
// 3. Additional Properties: Enabled, Description (add more as needed)
// 4. Partial Class: Allows extension in separate file if needed
//
// Enumeration Base Class Provides:
// - int Id: Unique numeric identifier (stored in database)
// - string Name: String name (e.g., "Active")
// - Equality: Based on Id (value semantics)
// - Static methods: GetAll(), FromId(), FromName(), etc.
//
// Usage Examples:
// - Assignment: var status = ProductStatus.Active;
// - Comparison: if (product.Status == ProductStatus.Active) { ... }
// - From ID: var status = Enumeration.FromId<ProductStatus>(2); // Returns Active
// - From Name: var status = Enumeration.FromName<ProductStatus>("Active");
// - Get All: var all = Enumeration.GetAll<ProductStatus>(); // Returns all statuses
//
// EF Core Mapping (in TypeConfiguration):
// - Store as ID:
//   builder.Property(e => e.Status)
//       .HasConversion(
//           s => s.Id,                                          // To DB: Store Id
//           id => Enumeration.FromId<[Entity]Status>(id));     // From DB: Reconstruct
//
// Business Rules Using Enumerations:
// - .Ensure(_ => status != [Entity]Status.Retired, "Cannot modify retired [entity]")
// - .When(_ => status == [Entity]Status.Active) // Conditional logic
//
// Additional Properties Examples:
// - public int SortOrder { get; } // For custom ordering
// - public string Color { get; } // For UI display
// - public bool AllowsEdit { get; } // For business rules
//
// Advanced: State Machine Patterns
// public bool CanTransitionTo([Entity]Status targetStatus)
// {
//     return this == Draft && targetStatus == Active
//         || this == Active && targetStatus == Retired;
// }
