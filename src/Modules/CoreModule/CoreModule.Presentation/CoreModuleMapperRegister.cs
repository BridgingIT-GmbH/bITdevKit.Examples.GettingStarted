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
        config.ForType<Customer, CustomerModel>() // Map concurrency token (Guid in domain -> string in DTO)
            .Map(dest => dest.ConcurrencyVersion, src => src.ConcurrencyVersion.ToString())
            .IgnoreNullValues(true); // don't overwrite existing values with null

        // CustomerModel -> Customer
        config.ForType<CustomerModel, Customer>() // Convert string back to Guid for concurrency token
            .Map(dest => dest.ConcurrencyVersion,
                 src => src.ConcurrencyVersion != null ? Guid.Parse(src.ConcurrencyVersion) : Guid.Empty)
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
        RegisterConverter<Domain.Model.CustomerStatus>(config);
    }

    /// <summary>
    /// Registers a generic mapping configuration for <see cref="Enumeration"/> types.
    /// Maps an enumeration object to its underlying <c>Id</c> and vice versa.
    /// </summary>
    /// <typeparam name="T">The specific <see cref="Enumeration"/> type.</typeparam>
    /// <param name="config">The <see cref="TypeAdapterConfig"/> to register mappings into.</param>
    private static void RegisterConverter<T>(TypeAdapterConfig config)
       where T : Enumeration
    {
        // Enumeration -> int (store Id for transport/persistence)
        config.NewConfig<T, int>()
            .MapWith(src => src.Id);

        // int -> Enumeration (reconstruct from Id)
        config.NewConfig<int, T>()
            .MapWith(src => Enumeration.GetAll<T>().FirstOrDefault(x => x.Id == src));
    }
}