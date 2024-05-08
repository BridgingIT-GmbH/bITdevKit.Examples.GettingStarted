// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Infrastructure;

using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DinnerEntityTypeConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        ConfigureCustomersTable(builder);
    }

    private static void ConfigureCustomersTable(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd()
            .HasConversion(
                id => id.Value,
                value => CustomerId.Create(value));

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(512);

        builder.OwnsOne(b => b.Email, pb =>
        {
            pb.Property(e => e.Value)
              .HasColumnName(nameof(Customer.Email))
              .IsRequired()
              .HasMaxLength(256);
            pb.HasIndex(nameof(Customer.Email.Value))
              .IsUnique(true);
        });
    }
}