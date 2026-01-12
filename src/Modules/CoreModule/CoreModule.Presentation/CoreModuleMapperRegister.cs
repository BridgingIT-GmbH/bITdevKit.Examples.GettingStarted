// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

using Mapster;

/// <summary>
/// Registers Mapster type adapters for the CoreModule.
/// Provides conversions between domain models (e.g. <see cref="Customer"/>, <see cref="EmailAddress"/>)
/// and DTOs/view models (e.g. <see cref="CustomerModel"/>).
/// </summary>
public class CoreModuleMapperRegister : IRegister
{
    /// <summary>
    /// Configures all mappings and type conversions for Mapster.
    /// </summary>
    /// <param name="config">The <see cref="TypeAdapterConfig"/> to register mappings into.</param>
    public void Register(TypeAdapterConfig config)
    {
        // ----------------------------
        // Aggregate ↔ DTO mappings
        // ----------------------------

        // Customer -> CustomerModel
        config.ForType<Customer, CustomerModel>()
            .Map(dest => dest.ConcurrencyVersion, // Map concurrency token (Guid in domain -> string in DTO)
                 src => src.ConcurrencyVersion.ToString())
            .Map(dest => dest.Addresses, // Map address collection
                 src => src.Addresses)
            .IgnoreNullValues(true);

        // CustomerModel -> Customer
        config.ForType<CustomerModel, Customer>()
            .ConstructUsing(src => Customer.Create( // Reconstruct Customer aggregate from model properties
                src.FirstName,
                src.LastName,
                src.Email,
                src.Number).Value)
            .Map(dest => dest.ConcurrencyVersion, // Convert string back to Guid for concurrency token
                 src => src.ConcurrencyVersion != null ? Guid.Parse(src.ConcurrencyVersion) : Guid.Empty)
            .Ignore(dest => dest.Addresses) // Addresses are managed separately via AddAddress, RemoveAddress, ChangeAddress methods
            .IgnoreNullValues(true);

        // Address -> CustomerAddressModel
        config.ForType<Address, CustomerAddressModel>()
            .Map(dest => dest.Id, // Map AddressId -> string
                 src => src.Id.Value.ToString())
            .IgnoreNullValues(true);

        // CustomerAddressModel -> Address
        config.ForType<CustomerAddressModel, Address>()
            .ConstructUsing(src => Address.Create( // Reconstruct Address entity from model properties
                src.Name,
                src.Line1,
                src.Line2,
                src.PostalCode,
                src.City,
                src.Country,
                src.IsPrimary).Value)
            .IgnoreNullValues(true);

        // ----------------------------
        // Value object conversions
        // ----------------------------

        // Map EmailAddress -> string (for persistence/DTO output)
        config.NewConfig<EmailAddress, string>()
            .MapWith(src => src.Value);

        // Map string -> EmailAddress (for reconstructing value object on input)
        config.NewConfig<string, EmailAddress>()
            .MapWith(src => EmailAddress.Create(src).Value);

        // Map EmailAddress -> string (for persistence/DTO output)
        config.NewConfig<CustomerNumber, string>()
            .MapWith(src => src.Value);

        // Map string -> EmailAddress (for reconstructing value object on input)
        config.NewConfig<string, CustomerNumber>()
            .MapWith(src => CustomerNumber.Create(src).Value);

        // ----------------------------
        // Enumeration conversions
        // ----------------------------
        RegisterConverter<CustomerStatus>(config);
    }

    /// <summary>
    /// Registers a generic mapping configuration for <see cref="Enumeration"/> types.
    /// Maps an enumeration object to its underlying <c>Value</c> (string name) and vice versa.
    /// </summary>
    /// <typeparam name="T">The specific <see cref="Enumeration"/> type.</typeparam>
    /// <param name="config">The <see cref="TypeAdapterConfig"/> to register mappings into.</param>
    private static void RegisterConverter<T>(TypeAdapterConfig config)
       where T : Enumeration
    {
        // Enumeration -> string (store Value for transport/DTO output)
        config.NewConfig<T, string>()
            .MapWith(src => src.Value);

        // string -> Enumeration (reconstruct from Value)
        config.NewConfig<string, T>()
            .MapWith(src => Enumeration.GetAll<T>().FirstOrDefault(x => x.Value == src));
    }
}