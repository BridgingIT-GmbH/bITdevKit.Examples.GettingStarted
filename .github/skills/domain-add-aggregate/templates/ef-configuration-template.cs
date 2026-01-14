// ============================================================================
// TEMPLATE: Entity Framework Core Configuration for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Configures EF Core mapping between domain aggregate and database table.
//   Handles typed IDs, value objects, enumerations, and concurrency tokens.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Property]     - Property names (e.g., FirstName, Email, Status)
//   [ValueObject]  - Value object types (e.g., EmailAddress, CustomerNumber)
//   [Enumeration]  - Enumeration types (e.g., CustomerStatus, OrderStatus)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Infrastructure/EntityFramework/Configurations/
//   3. File name: [Entity]TypeConfiguration.cs
//   4. Add/remove property configurations based on your aggregate
//   5. Register in DbContext: modelBuilder.ApplyConfiguration(new [Entity]TypeConfiguration());
//
// RELATED PATTERNS:
//   - IEntityTypeConfiguration<T>: EF Core fluent configuration
//   - Value Object Conversion: Map value objects to primitive database columns
//   - Typed ID Pattern: Strong typing for entity identifiers
//   - Optimistic Concurrency: ConcurrencyVersion prevents lost updates
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Infrastructure;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core type configuration for the <see cref="[Entity]"/> aggregate.
/// Defines table mappings, property conversions and constraints.
/// </summary>
/// <remarks>
/// Configuration responsibilities:
/// - Table name and schema mapping
/// - Primary key configuration
/// - Property column types, lengths, and nullability
/// - Value object conversions (to/from primitives)
/// - Enumeration conversions (to/from int or string)
/// - Typed ID conversions (to/from Guid)
/// - Concurrency token configuration
/// - Relationships and navigation properties (if applicable)
/// - Audit state (CreatedDate, UpdatedDate, etc.)
/// </remarks>
[ExcludeFromCodeCoverage]
public class [Entity]TypeConfiguration : IEntityTypeConfiguration<[Entity]>
{
    /// <summary>
    /// Configures the <see cref="[Entity]"/> entity type for Entity Framework Core.
    /// </summary>
    /// <param name="builder">The entity type builder to configure.</param>
    public void Configure(EntityTypeBuilder<[Entity]> builder)
    {
        // ================================================================
        // TABLE AND PRIMARY KEY CONFIGURATION
        // ================================================================

        // Map to table "[Entities]" (pluralized aggregate name)
        builder.ToTable("[Entities]")
            .HasKey(x => x.Id)
            .IsClustered(false); // Non-clustered PK allows clustered indexes on natural keys if needed

        // ================================================================
        // CONCURRENCY TOKEN (OPTIMISTIC CONCURRENCY)
        // ================================================================

        // Configure ConcurrencyVersion as a concurrency token (Guid)
        // EF Core will check this value before updating to prevent lost updates
        builder.Property(e => e.ConcurrencyVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever(); // Application layer generates Guid, not database

        // ================================================================
        // TYPED ID CONFIGURATION
        // ================================================================

        // Configure [Entity]Id value object → Guid in database
        // [Entity]Id is a typed wrapper around Guid for type safety
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd() // Database generates Guid on insert
            .HasConversion(
                id => id.Value,                     // Convert [Entity]Id → Guid when saving
                value => [Entity]Id.Create(value)); // Convert Guid → [Entity]Id when loading

        // ================================================================
        // VALUE OBJECT CONVERSIONS
        // ================================================================
        // Map value objects to primitive database columns
        // Pattern: .HasConversion(vo => vo.Value, val => ValueObject.Create(val).Value)

        // Example: EmailAddress value object → string column
        // builder.Property(e => e.Email)
        //     .IsRequired()
        //     .HasMaxLength(256)
        //     .HasConversion(
        //         email => email.Value,                      // When saving
        //         value => EmailAddress.Create(value).Value) // When loading
        //     .HasColumnName("Email");

        // Example: Custom business identifier value object → string column
        // builder.Property(e => e.Number)
        //     .IsRequired()
        //     .HasMaxLength(256)
        //     .HasConversion(
        //         number => number.Value,
        //         value => [Entity]Number.Create(value).Value);

        // ================================================================
        // ENUMERATION CONVERSIONS
        // ================================================================
        // Map enumeration objects to int database columns
        // Uses EnumerationConverter from bITdevKit

        // Example: CustomerStatus enumeration → int column
        // builder.Property(e => e.Status)
        //     .IsRequired(false)
        //     .HasConversion(new EnumerationConverter<[Entity]Status>());
        //
        // Alternative manual conversion:
        // builder.Property(e => e.Status)
        //     .IsRequired(false)
        //     .HasConversion(
        //         status => status.Id,                              // When saving
        //         id => Enumeration.FromId<[Entity]Status>(id));   // When loading

        // ================================================================
        // PRIMITIVE PROPERTY CONFIGURATIONS
        // ================================================================
        // Configure simple properties (strings, numbers, dates)

        // Example string properties:
        // builder.Property(e => e.[Property])
        //     .IsRequired()           // NOT NULL constraint
        //     .HasMaxLength(128);     // VARCHAR(128)
        //
        // builder.Property(e => e.[Property])
        //     .IsRequired(false)      // NULL allowed
        //     .HasMaxLength(512);

        // Example date properties:
        // builder.Property(e => e.DateOfBirth)
        //     .IsRequired(false)
        //     .HasConversion<DateOnlyConverter, DateOnlyComparer>() // DateOnly support
        //     .HasColumnType("date");

        // Example numeric properties:
        // builder.Property(e => e.Quantity)
        //     .IsRequired()
        //     .HasColumnType("int");
        //
        // builder.Property(e => e.Price)
        //     .IsRequired()
        //     .HasColumnType("decimal(18,2)");

        // Example boolean properties:
        // builder.Property(e => e.IsActive)
        //     .IsRequired()
        //     .HasDefaultValue(true);

        // ================================================================
        // DOMAIN EVENTS (IGNORE - NOT PERSISTED)
        // ================================================================

        // Domain events are in-memory only, not persisted to database
        // Automatically ignored by convention, but explicit is clearer
        builder.Ignore(e => e.DomainEvents);

        // ================================================================
        // AUDIT STATE (CREATED/UPDATED DATES)
        // ================================================================

        // Configure audit properties using bITdevKit extension method
        // Maps: CreatedDate, CreatedBy, UpdatedDate, UpdatedBy, DeletedDate, DeletedBy, IsDeleted
        builder.OwnsOneAuditState();
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. IENTITYTYPECONFIGURATION<T>:
//    - Separates EF Core configuration from domain entities
//    - Keeps domain entities persistence-ignorant
//    - Registered via: modelBuilder.ApplyConfiguration(new [Entity]TypeConfiguration())
//
// 2. TYPED ID CONVERSION:
//    - [Entity]Id wraps Guid for type safety
//    - Database stores as Guid (uniqueidentifier in SQL Server)
//    - .HasConversion() maps between typed ID and primitive
//
// 3. VALUE OBJECT CONVERSION:
//    - Value objects (e.g., EmailAddress) stored as primitives
//    - .HasConversion() maps: vo => vo.Value (save), val => VO.Create(val).Value (load)
//    - Preserves domain invariants (validation in Create factory)
//
// 4. ENUMERATION CONVERSION:
//    - Enumeration classes stored as int (or string if preferred)
//    - EnumerationConverter<T> handles mapping automatically
//    - Alternative: Manual conversion with Enumeration.FromId<T>()
//
// 5. OPTIMISTIC CONCURRENCY:
//    - ConcurrencyVersion marked as .IsConcurrencyToken()
//    - EF Core checks value before updating
//    - Mismatch throws DbUpdateConcurrencyException
//
// 6. AUDIT STATE:
//    - .OwnsOneAuditState() configures owned type for audit fields
//    - Automatically populated by repository behaviors
//    - Includes: CreatedDate, UpdatedDate, CreatedBy, UpdatedBy, soft delete fields
//
// ============================================================================
// VALUE OBJECT PATTERN EXAMPLES
// ============================================================================
//
// Simple value object (single property):
//   public class EmailAddress : ValueObject
//   {
//       public string Value { get; private set; }
//       private EmailAddress(string value) => Value = value;
//       public static Result<EmailAddress> Create(string value)
//       {
//           if (string.IsNullOrWhiteSpace(value)) return Result.Failure<EmailAddress>("Email required");
//           if (!value.Contains('@')) return Result.Failure<EmailAddress>("Invalid email format");
//           return Result.Success(new EmailAddress(value));
//       }
//   }
//
// EF Core configuration:
//   builder.Property(e => e.Email)
//       .HasConversion(
//           email => email.Value,
//           value => EmailAddress.Create(value).Value); // NOTE: Assumes valid data from DB
//
// ============================================================================
// ENUMERATION PATTERN EXAMPLES
// ============================================================================
//
// Enumeration class:
//   public class [Entity]Status : Enumeration
//   {
//       public static readonly [Entity]Status Active = new(1, "Active", true, "Entity is active");
//       public static readonly [Entity]Status Inactive = new(2, "Inactive", false, "Entity is inactive");
//
//       private [Entity]Status(int id, string value, bool enabled, string description)
//           : base(id, value) { Enabled = enabled; Description = description; }
//
//       public bool Enabled { get; }
//       public string Description { get; }
//   }
//
// EF Core configuration:
//   builder.Property(e => e.Status)
//       .HasConversion(new EnumerationConverter<[Entity]Status>());
//
// Database stores: 1 (Active), 2 (Inactive)
//
// ============================================================================
// CHILD COLLECTIONS (OWNED ENTITIES)
// ============================================================================
//
// For aggregates with child collections (e.g., Order with OrderItems):
//
//   builder.OwnsMany(e => e.Items, itemBuilder =>
//   {
//       itemBuilder.ToTable("[Entity]Items");
//       itemBuilder.WithOwner().HasForeignKey("[Entity]Id");
//
//       itemBuilder.Property(i => i.Id)
//           .ValueGeneratedOnAdd()
//           .HasConversion(
//               id => id.Value,
//               value => [Entity]ItemId.Create(value));
//
//       itemBuilder.HasKey(i => i.Id);
//
//       itemBuilder.Property(i => i.Name)
//           .IsRequired()
//           .HasMaxLength(256);
//
//       itemBuilder.Property(i => i.Quantity)
//           .IsRequired();
//   });
//
// ============================================================================
// RELATIONSHIPS (AGGREGATE REFERENCES)
// ============================================================================
//
// For aggregates that reference other aggregates (via ID only, not navigation property):
//
//   // Store only the ID, not the full entity (aggregate boundaries)
//   builder.Property(e => e.ParentId)
//       .IsRequired(false)
//       .HasConversion(
//           id => id.Value,
//           value => ParentId.Create(value));
//
//   // No navigation property configured (maintains aggregate boundary)
//
// ============================================================================
