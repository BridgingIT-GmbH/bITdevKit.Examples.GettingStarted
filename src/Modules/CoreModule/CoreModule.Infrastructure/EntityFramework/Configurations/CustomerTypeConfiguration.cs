// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

/// <summary>
/// Entity Framework Core type configuration for the <see cref="Customer"/> aggregate.
/// Defines table mappings, property conversions and constraints.
/// </summary>
[ExcludeFromCodeCoverage]
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

        // Configure CustomerId value object -> Guid in database
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd() // Guid is generated on insert
            .HasConversion(
                id => id.Value,                     // convert to Guid when saving
                value => CustomerId.Create(value)); // convert back to CustomerId when loading

        // First name is required with max length 128
        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        // Map CustomerNumber value object -> string in database
        builder.Property(d => d.Number)
            .IsRequired()
            .HasConversion(
                number => number.Value,                      // when saving
                value => CustomerNumber.Create(value).Value) // when loading
            .HasMaxLength(256);

        // Last name is required with max length 512
        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(512);


        // Map EmailAddress value object -> string in database
        builder.Property(x => x.Email)
            .IsRequired()
            .HasConversion(
                email => email.Value,                      // when saving
                value => EmailAddress.Create(value).Value) // when loading
            .HasMaxLength(256);

        builder.Property(d => d.DateOfBirth)
            .IsRequired(false)
            .HasConversion<DateOnlyConverter, DateOnlyComparer>()
            .HasColumnType("date");

        // Map CustomerStatus enumeration -> int in database using enumeration converter
        builder.Property(x => x.Status)
            .IsRequired(false)
            .HasConversion(new EnumerationConverter<CustomerStatus>());

        // Map owned Address collection to separate table
        builder.OwnsMany(c => c.Addresses, ab =>
        {
            ab.ToTable("CustomersAddresses");
            ab.WithOwner().HasForeignKey("CustomerId");

            // Configure AddressId as primary key with conversion
            ab.Property(a => a.Id)
                .ValueGeneratedOnAdd()
                .HasConversion(
                    id => id.Value,
                    value => AddressId.Create(value));

            ab.HasKey(a => a.Id);

            // Configure address properties
            ab.Property(a => a.Name)
                .IsRequired(true)
                .HasMaxLength(256);

            ab.Property(a => a.Line1)
                .IsRequired()
                .HasMaxLength(256);

            ab.Property(a => a.Line2)
                .IsRequired(false)
                .HasMaxLength(256);

            ab.Property(a => a.PostalCode)
                .IsRequired(false)
                .HasMaxLength(20);

            ab.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100);

            ab.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100);

            ab.Property(a => a.IsPrimary)
                .IsRequired();
        });

        // Map auditing properties (e.g. CreatedDate, UpdatedDate) via shared extension method
        builder.OwnsOneAuditState();
    }
}