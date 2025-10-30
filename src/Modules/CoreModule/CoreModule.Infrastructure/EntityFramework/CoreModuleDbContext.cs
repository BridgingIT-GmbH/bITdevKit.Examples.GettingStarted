// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;

/// <summary>
/// Application database context for the CoreModule.
/// Provides access to domain aggregates persisted in the relational database and applies EF Core mappings from the current assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CoreModuleDbContext"/> class.
/// Configured via <see cref="DbContextOptions{AppDbContext}"/> dependency injection.
/// </remarks>
/// <param name="options">The database context options (provider, connection string, etc.).</param>
public class CoreModuleDbContext(DbContextOptions<CoreModuleDbContext> options) : ModuleDbContextBase(options), IOutboxDomainEventContext
{
    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for managing <see cref="Customer"/> entities.
    /// Represents the "Customers" table in the database.
    /// </summary>
    public DbSet<Customer> Customers { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for managing <see cref="OutboxDomainEvent"/> entities.
    /// Represents the "OutboxDomainEvents" table in the database.
    /// </summary>
    public DbSet<OutboxDomainEvent> OutboxDomainEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<int>(CodeModuleConstants.CustomerNumberSequenceName)
            .StartsAt(100000);

        base.OnModelCreating(modelBuilder);
    }
}