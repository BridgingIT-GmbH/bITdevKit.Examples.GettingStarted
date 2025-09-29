// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core type configuration for the <see cref="Customer"/> aggregate.
/// Defines table mappings, property conversions and constraints.
/// </summary>
public class CustomerTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Map to table "Customers" and configure primary key
        builder.ToTable("Customers")
            .HasKey(x => x.Id)
            .IsClustered(false); // Non-clustered PK allows clustered indexes on natural keys if needed

        // Concurrency token for optimistic concurrency
        builder.Property(e => e.ConcurrencyVersion)
            .IsConcurrencyToken()
            .ValueGeneratedNever(); // EF won't generate this value, it must come from the app layer

        // Configure CustomerId value object → Guid in database
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd() // Guid is generated on insert
            .HasConversion(
                id => id.Value,                 // convert to Guid when saving
                value => CustomerId.Create(value)); // convert back to CustomerId when loading

        // First name is required with max length 128
        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        // Last name is required with max length 512
        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(512);

        // Map EmailAddress value object → string in database
        builder.Property(x => x.Email)
            .IsRequired()
            .HasConversion(
                email => email.Value,               // when saving
                value => EmailAddress.Create(value)) // when loading
            .HasMaxLength(256);

        // Map CustomerStatus enumeration → int in database using custom converter
        builder.Property(x => x.Status)
            .HasConversion(new EnumerationConverter<CustomerStatus>())
            .IsRequired();

        // Map auditing properties (e.g. CreatedAt, ModifiedAt) via shared extension method
        builder.OwnsOneAuditState();
    }
}