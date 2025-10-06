// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Application database context for the CoreModule.
/// Provides access to domain aggregates persisted in the relational database and applies EF Core mappings from the current assembly.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CoreDbContext"/> class.
/// Configured via <see cref="DbContextOptions{AppDbContext}"/> dependency injection.
/// </remarks>
/// <param name="options">The database context options (provider, connection string, etc.).</param>
public class CoreDbContext(DbContextOptions<CoreDbContext> options) : ModuleDbContextBase(options)
{
    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for managing <see cref="Customer"/> entities.
    /// Represents the "Customers" table in the database.
    /// </summary>
    public DbSet<Customer> Customers { get; set; }
}