// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Application database context for the CoreModule.
/// Provides access to domain aggregates persisted in the relational database and applies EF Core mappings from the current assembly.
/// </summary>
public class CoreDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreDbContext"/> class.
    /// Configured via <see cref="DbContextOptions{AppDbContext}"/> dependency injection.
    /// </summary>
    /// <param name="options">The database context options (provider, connection string, etc.).</param>
    public CoreDbContext(DbContextOptions<CoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for managing <see cref="Customer"/> entities.
    /// Represents the "Customers" table in the database.
    /// </summary>
    public DbSet<Customer> Customers { get; set; }

    /// <summary>
    /// Called by EF Core during model creation.
    /// Applies all <see cref="IEntityTypeConfiguration{TEntity}"/> implementations
    /// found in the same assembly (e.g. <c>CustomerTypeConfiguration</c>).
    /// </summary>
    /// <param name="modelBuilder">The model builder instance used to configure entity mappings.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatically register all IEntityTypeConfiguration<TEntity> mappings
        // in this assembly — this keeps DbContext clean and scalable as the model grows
        modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
    }
}