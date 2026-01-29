// ============================================================================
// TEMPLATE: Mapster Mapping Configuration for [Module]
// ============================================================================
// PURPOSE:
//   Registers Mapster type adapters for mapping between domain entities and DTOs.
//   Handles value objects, enumerations, and aggregate conversions.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [ValueObject]  - Value object types (e.g., EmailAddress, CustomerNumber)
//   [Enumeration]  - Enumeration types (e.g., CustomerStatus, OrderStatus)
//   [Property]     - Property names for custom mappings
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Presentation/
//   3. File name: [Module]MapperRegister.cs
//   4. Register in Presentation module: services.AddMapping().WithMapster<[Module]MapperRegister>()
//   5. Add mappings for all aggregates and value objects in the module
//
// RELATED PATTERNS:
//   - IRegister: Mapster registration interface
//   - TypeAdapterConfig: Mapster configuration container
//   - Value Object Mapping: Bidirectional conversion with primitives
//   - DTO Pattern: Separate domain from external representations
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Presentation;

using Mapster;

/// <summary>
/// Registers Mapster type adapters for the [Module].
/// Provides conversions between domain models (e.g. <see cref="[Entity]"/>, <see cref="[ValueObject]"/>)
/// and DTOs/view models (e.g. <see cref="[Entity]Model"/>).
/// </summary>
/// <remarks>
/// Mapping responsibilities:
/// - Aggregate ↔ DTO bidirectional mappings
/// - Value objects ↔ primitives (string, int, etc.)
/// - Enumerations ↔ strings (for API representation)
/// - Child collections ↔ child DTOs
/// - Concurrency version Guid ↔ string
///
/// Mapster conventions:
/// - Same property names map automatically (convention over configuration)
/// - Custom mappings use .Map() for forward, .ConstructUsing() for reverse
/// - Ignore properties that should not be mapped with .Ignore()
/// </remarks>
public class [Module]MapperRegister : IRegister
{
    /// <summary>
    /// Configures all mappings and type conversions for Mapster.
    /// </summary>
    /// <param name="config">The <see cref="TypeAdapterConfig"/> to register mappings into.</param>
    public void Register(TypeAdapterConfig config)
    {
        // ================================================================
        // AGGREGATE ↔ DTO MAPPINGS
        // ================================================================

        // ----------------------------
        // [Entity] → [Entity]Model (Domain → DTO)
        // ----------------------------
        config.ForType<[Entity], [Entity]Model>()
            // Map typed ID to string
            .Map(dest => dest.Id, src => src.Id.Value.ToString())
            // Map concurrency token Guid → string
            .Map(dest => dest.ConcurrencyVersion, src => src.ConcurrencyVersion.ToString())
            // Map value objects to primitives (if applicable)
            // .Map(dest => dest.[Property], src => src.[ValueObject].Value)
            // Map enumerations to strings (if applicable)
            // .Map(dest => dest.Status, src => src.Status.Value)
            // Ignore null values during mapping
            .IgnoreNullValues(true);

        // ----------------------------
        // [Entity]Model → [Entity] (DTO → Domain)
        // ----------------------------
        config.ForType<[Entity]Model, [Entity]>()
            // Reconstruct [Entity] aggregate from DTO using Create factory
            .ConstructUsing(src => [Entity].Create(
                // Add parameters matching your aggregate's Create factory method:
                // src.[Property1],
                // src.[Property2],
                // src.[Property3]
            ).Value) // NOTE: Assumes valid data from DTO (validation happens in command validators)
            // Map string → Guid for concurrency version
            .Map(dest => dest.ConcurrencyVersion,
                 src => src.ConcurrencyVersion != null ? Guid.Parse(src.ConcurrencyVersion) : Guid.Empty)
            .IgnoreNullValues(true);

        // ================================================================
        // CHILD COLLECTION MAPPINGS (if applicable)
        // ================================================================
        // If your aggregate has child entities/collections, map them here

        // Example: [Entity]Item ↔ [Entity]ItemModel
        // config.ForType<[Entity]Item, [Entity]ItemModel>()
        //     .Map(dest => dest.Id, src => src.Id.Value.ToString())
        //     .IgnoreNullValues(true);
        //
        // config.ForType<[Entity]ItemModel, [Entity]Item>()
        //     .ConstructUsing(src => [Entity]Item.Create(
        //         src.Name,
        //         src.Quantity).Value);

        // ================================================================
        // VALUE OBJECT CONVERSIONS
        // ================================================================
        // Bidirectional mapping between value objects and primitives

        // Example: EmailAddress ↔ string
        // config.NewConfig<[ValueObject], string>()
        //     .MapWith(src => src.Value);
        //
        // config.NewConfig<string, [ValueObject]>()
        //     .MapWith(src => [ValueObject].Create(src).Value);

        // ================================================================
        // ENUMERATION CONVERSIONS
        // ================================================================
        // Bidirectional mapping between enumerations and strings

        // Example: [Entity]Status ↔ string
        // RegisterEnumerationConverter<[Entity]Status>(config);
    }

    /// <summary>
    /// Registers a generic mapping configuration for <see cref="Enumeration"/> types.
    /// Maps an enumeration object to its underlying <c>Value</c> (string name) and vice versa.
    /// </summary>
    /// <typeparam name="T">The specific <see cref="Enumeration"/> type.</typeparam>
    /// <param name="config">The <see cref="TypeAdapterConfig"/> to register mappings into.</param>
    private static void RegisterEnumerationConverter<T>(TypeAdapterConfig config)
       where T : Enumeration
    {
        // Enumeration → string (store Value for transport/DTO output)
        config.NewConfig<T, string>()
            .MapWith(src => src.Value);

        // string → Enumeration (reconstruct from Value)
        config.NewConfig<string, T>()
            .MapWith(src => Enumeration.GetAll<T>().FirstOrDefault(x => x.Value == src));
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. IREGISTER INTERFACE:
//    - Mapster discovers IRegister implementations via AddMapping().WithMapster<T>()
//    - Centralizes all mapping configuration for a module
//    - Keeps mapping logic separate from domain and application layers
//
// 2. FORTYPE<TSOURCE, TDEST>():
//    - Configures mapping from source type → destination type
//    - Use .Map() for custom property mappings
//    - Use .ConstructUsing() for custom object construction
//
// 3. CONSTRUCTUSING WITH FACTORY:
//    - Reconstruct domain aggregates using Create factory method
//    - Ensures domain invariants are enforced during mapping
//    - Example: .ConstructUsing(src => [Entity].Create(src.Name, src.Email).Value)
//
// 4. VALUE OBJECT MAPPING:
//    - NewConfig<ValueObject, string>(): Map value object → primitive
//    - NewConfig<string, ValueObject>(): Map primitive → value object
//    - Uses implicit operators or .Value property
//
// 5. ENUMERATION MAPPING:
//    - RegisterEnumerationConverter<T>(): Generic helper for enumerations
//    - Maps enumeration ↔ string for API representation
//    - Alternative: Map to int (enumeration.Id) for compact representation
//
// 6. IGNORENULLVALUES(TRUE):
//    - Prevents overwriting destination properties with null source values
//    - Useful for partial updates
//
// 7. IGNORE():
//    - Skip properties that should not be mapped
//    - Example: .Ignore(dest => dest.DomainEvents)
//
// ============================================================================
// MAPSTER USAGE EXAMPLES
// ============================================================================
//
// Mapping in handlers:
//   // Domain → DTO (single entity)
//   var dto = mapper.Map<[Entity], [Entity]Model>(domainEntity);
//
//   // Domain → DTO (collection)
//   var dtos = mapper.Map<[Entity], [Entity]Model>(domainEntities);
//
//   // DTO → Domain (via factory, configured in ConstructUsing)
//   var domainEntity = mapper.Map<[Entity]Model, [Entity]>(dto);
//
// Result pattern integration:
//   // Map inside Result<T>
//   return await repository
//       .FindOneResultAsync(id, cancellationToken)
//       .MapResult<[Entity], [Entity]Model>(mapper);
//
// ============================================================================
// ADVANCED MAPPING PATTERNS
// ============================================================================
//
// 1. CONDITIONAL MAPPING:
//    - Use .When() to apply mappings conditionally
//
//    config.ForType<[Entity], [Entity]Model>()
//        .Map(dest => dest.StatusName,
//             src => src.Status != null ? src.Status.Value : null)
//        .When(src => src.Status != null);
//
// 2. NESTED MAPPINGS:
//    - Mapster automatically maps nested objects if configurations exist
//
//    config.ForType<[Entity], [Entity]Model>()
//        .Map(dest => dest.Items, src => src.Items); // Uses [Entity]Item → [Entity]ItemModel config
//
// 3. FLATTENING:
//    - Map nested properties to flat DTO
//
//    config.ForType<[Entity], [Entity]Model>()
//        .Map(dest => dest.AddressCity, src => src.Address.City)
//        .Map(dest => dest.AddressCountry, src => src.Address.Country);
//
// 4. AFTER MAPPING HOOK:
//    - Execute custom logic after mapping
//
//    config.ForType<[Entity], [Entity]Model>()
//        .AfterMapping((src, dest) =>
//        {
//            // Custom post-mapping logic
//            dest.FullName = $"{dest.FirstName} {dest.LastName}";
//        });
//
// 5. MULTIPLE DTO MAPPINGS:
//    - Configure mappings to different DTOs for different use cases
//
//    // [Entity] → [Entity]SummaryModel (minimal data)
//    config.ForType<[Entity], [Entity]SummaryModel>()
//        .Map(dest => dest.Id, src => src.Id.Value.ToString())
//        .Map(dest => dest.Name, src => src.Name);
//
//    // [Entity] → [Entity]DetailModel (full data)
//    config.ForType<[Entity], [Entity]DetailModel>()
//        .Map(dest => dest.Id, src => src.Id.Value.ToString())
//        .Map(dest => dest.Items, src => src.Items);
//
// ============================================================================
// CHILD COLLECTION MAPPING EXAMPLE
// ============================================================================
//
// For aggregates with owned collections (e.g., Customer with Addresses):
//
//   // Address → CustomerAddressModel
//   config.ForType<Address, CustomerAddressModel>()
//       .Map(dest => dest.Id, src => src.Id.Value.ToString())
//       .IgnoreNullValues(true);
//
//   // CustomerAddressModel → Address
//   config.ForType<CustomerAddressModel, Address>()
//       .ConstructUsing(src => Address.Create(
//           src.Name,
//           src.Line1,
//           src.Line2,
//           src.PostalCode,
//           src.City,
//           src.Country,
//           src.IsPrimary).Value)
//       .IgnoreNullValues(true);
//
//   // Customer → CustomerModel (includes addresses)
//   config.ForType<Customer, CustomerModel>()
//       .Map(dest => dest.Addresses, src => src.Addresses); // Automatic collection mapping
//
//   // CustomerModel → Customer (addresses handled separately in handler)
//   config.ForType<CustomerModel, Customer>()
//       .Ignore(dest => dest.Addresses); // Addresses managed via AddAddress, RemoveAddress methods
//
// ============================================================================
