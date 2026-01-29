// ============================================================================
// TEMPLATE: DTO Model for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines a data transfer object (DTO) for exposing the aggregate to external consumers.
//   Used in Application and Presentation layers; never in Domain layer.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Property]     - Property names (e.g., FirstName, LastName, Email, Status)
//   [PropertyType] - Property types (e.g., string, int, DateOnly?, bool)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Models/
//   3. File name: [Entity]Model.cs
//   4. Add/remove properties based on your aggregate's public state
//   5. Use primitive types (string, int, bool) or simple structs (DateOnly, Guid as string)
//
// RELATED PATTERNS:
//   - DTO Pattern: Data transfer objects for cross-layer communication
//   - Mapster: Mapping between domain entities and DTOs
//   - API Contracts: Models define the public API surface
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

/// <summary>
/// Data transfer object (DTO) representing a <see cref="Domain.Model.[Entity]"/>.
/// Used by the application and presentation layers to expose Aggregate to clients.
/// </summary>
/// <remarks>
/// This DTO is used for:
/// - API request/response payloads
/// - Mapping domain entities to external representations
/// - Decoupling domain model from API contracts
///
/// Guidelines:
/// - Use primitive types (string, int, bool) for properties
/// - Avoid domain types (value objects, enumerations) - map to primitives
/// - Include XML documentation with examples for API documentation
/// - Add validation attributes only if using DataAnnotations (prefer FluentValidation)
/// </remarks>
public class [Entity]Model
{
    /// <summary>
    /// Gets or sets the unique identifier of the entity.
    /// Empty/null for new entities, populated after creation.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the concurrency version token (as a string Guid).
    /// Used for optimistic concurrency control to prevent conflicting updates.
    /// Must be provided when updating to ensure the entity hasn't been modified by another operation.
    /// </summary>
    /// <example>8f7a9b2c-3d4e-5f6a-7b8c-9d0e1f2a3b4c</example>
    public string ConcurrencyVersion { get; set; }

    // ================================================================
    // ADD AGGREGATE-SPECIFIC PROPERTIES BELOW
    // ================================================================
    // Replace these example properties with your aggregate's actual properties:

    // /// <summary>
    // /// Gets or sets the [property description].
    // /// </summary>
    // /// <example>[example value]</example>
    // public [PropertyType] [Property] { get; set; }

    // Example string property:
    // /// <summary>
    // /// Gets or sets the entity's name.
    // /// </summary>
    // /// <example>John Doe</example>
    // public string Name { get; set; }

    // Example nullable date property:
    // /// <summary>
    // /// Gets or sets the date of birth (optional).
    // /// </summary>
    // /// <example>1990-05-15</example>
    // public DateOnly? DateOfBirth { get; set; }

    // Example enumeration as string:
    // /// <summary>
    // /// Gets or sets the current status as a string value.
    // /// Valid values: "Active", "Inactive", "Pending".
    // /// </summary>
    // /// <example>Active</example>
    // public string Status { get; set; }

    // Example boolean property:
    // /// <summary>
    // /// Gets or sets a value indicating whether this entity is active.
    // /// </summary>
    // /// <example>true</example>
    // public bool IsActive { get; set; }

    // Example numeric property:
    // /// <summary>
    // /// Gets or sets the quantity.
    // /// </summary>
    // /// <example>42</example>
    // public int Quantity { get; set; }

    // Example decimal property:
    // /// <summary>
    // /// Gets or sets the price.
    // /// </summary>
    // /// <example>99.99</example>
    // public decimal Price { get; set; }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. PRIMITIVE PROPERTIES:
//    - Use string, int, bool, decimal instead of domain types
//    - Example: string Status (not CustomerStatus enumeration)
//    - Example: string Email (not EmailAddress value object)
//    - Simplifies serialization and API contracts
//
// 2. GUID AS STRING:
//    - Id and ConcurrencyVersion are strings, not Guid
//    - Easier for JSON serialization and client consumption
//    - Example: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
//
// 3. NULLABLE PROPERTIES:
//    - Use nullable types (?) for optional fields
//    - Example: DateOnly? DateOfBirth, int? Age
//    - Non-nullable properties are implicitly required
//
// 4. XML DOCUMENTATION:
//    - <summary>: Brief description
//    - <example>: Sample value for API documentation (Swagger/OpenAPI)
//    - Helps generate accurate API documentation
//
// 5. NO VALIDATION ATTRIBUTES:
//    - Prefer FluentValidation in Command/Query validators
//    - Cleaner separation of concerns
//    - More flexible validation rules
//
// 6. NO DOMAIN LOGIC:
//    - DTOs are pure data carriers
//    - No methods, no calculated properties (unless for convenience)
//    - Domain logic belongs in aggregate
//
// MAPPING EXAMPLES:
//
// Domain → DTO (Mapster configuration in [Module]MapperRegister):
//   config.ForType<[Entity], [Entity]Model>()
//       .Map(dest => dest.Id, src => src.Id.Value.ToString())
//       .Map(dest => dest.Status, src => src.Status.Value)
//       .Map(dest => dest.Email, src => src.Email.Value);
//
// DTO → Domain (typically via aggregate Create factory):
//   var result = [Entity].Create(
//       model.Property1,
//       model.Property2,
//       model.Property3);
//
// ============================================================================
// ADVANCED DTO PATTERNS
// ============================================================================
//
// 1. NESTED CHILD DTOS:
//    - For aggregate child entities or collections
//    - Example: List<[Entity]ItemModel> Items
//
//    public class [Entity]Model
//    {
//        public string Id { get; set; }
//        public List<[Entity]ItemModel> Items { get; set; }
//    }
//
//    public class [Entity]ItemModel
//    {
//        public string Id { get; set; }
//        public string Name { get; set; }
//    }
//
// 2. MULTIPLE DTO VARIANTS:
//    - Define multiple DTOs for different use cases
//    - [Entity]SummaryModel: Minimal data for list views
//    - [Entity]DetailModel: Full data for single-entity views
//    - [Entity]CreateRequestModel: Data for create operations (no Id)
//    - [Entity]UpdateRequestModel: Data for update operations (with Id)
//
// 3. CALCULATED/CONVENIENCE PROPERTIES:
//    - Read-only properties that combine or format data
//    - Example: public string FullName => $"{FirstName} {LastName}";
//    - Example: public bool IsExpired => ExpirationDate < DateTime.UtcNow;
//
// 4. INHERITANCE FOR SHARED PROPERTIES:
//    - Base DTO class for common properties (Id, ConcurrencyVersion, audit fields)
//
//    public abstract class EntityModelBase
//    {
//        public string Id { get; set; }
//        public string ConcurrencyVersion { get; set; }
//    }
//
//    public class [Entity]Model : EntityModelBase
//    {
//        // Aggregate-specific properties
//    }
//
// ============================================================================
// API DOCUMENTATION BEST PRACTICES
// ============================================================================
//
// 1. EXAMPLE VALUES:
//    - Provide realistic examples in <example> tags
//    - Examples appear in Swagger/OpenAPI documentation
//    - Help developers understand expected formats
//
// 2. VALID VALUES:
//    - Document valid values for enumerations/status fields
//    - Example: /// Valid values: "Lead", "Active", "Retired"
//
// 3. FORMAT HINTS:
//    - Document expected formats for strings
//    - Example: /// Format: CUS-YYYY-NNNNNN (e.g., CUS-2024-100000)
//
// 4. REQUIRED VS OPTIONAL:
//    - Use nullable types (?) for optional fields
//    - Add comments for conditional requirements
//    - Example: /// Required when creating, optional when updating
//
// ============================================================================
