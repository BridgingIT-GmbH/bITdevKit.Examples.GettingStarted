// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license
namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Infrastructure.EntityFramework;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit.Abstractions;
using Xunit.Sdk;

[CollectionDefinition(nameof(SqlServerTestContainerCollection))]
public class SqlServerTestContainerCollection : ICollectionFixture<SqlServerTestFixture>
{
}

public class SqlServerTestFixture : IAsyncLifetime
{
    private readonly List<string> logs = new();
    private ITestOutputHelper output;
    private MsSqlContainer container;
    private string fallbackConnectionString;
    private string fallbackDatabaseName;
    private bool FallbackUsed => this.fallbackConnectionString != null;

    public DbContextOptions<CoreModuleDbContext> Options { get; private set; }

    public bool Available { get; private set; }

    public string FailureReason { get; private set; }

    public void Attach(ITestOutputHelper testOutput)
    {
        if (testOutput == null)
        {
            return; // ignore null attachment
        }

        this.output = testOutput;
        foreach (var m in this.logs)
        {
            try { this.output.WriteLine(m); } catch { /* ignore after test context disposed */ }
        }
    }

    private void Log(string message)
    {
        var line = $"[Fixture] {DateTime.UtcNow:HH:mm:ss.fff} {message}";
        this.logs.Add(line);
        try { this.output?.WriteLine(line); } catch { /* ignore */ }
    }

    public async Task InitializeAsync()
    {
        this.Log("Initializing SQL fixture...");
        try // Try starting docker test container
        {
            this.Log("Attempting to start SQL Server testcontainer...");
            this.container = new MsSqlBuilder() // https://github.com/testcontainers/testcontainers-dotnet
              .WithImage("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
              .WithCleanUp(true).Build();

            await this.container.StartAsync();
            this.Log("Container started. Building DbContext options...");

            var connectionString = this.container.GetConnectionString() + ";TrustServerCertificate=True;MultipleActiveResultSets=true";
            this.Options = new DbContextOptionsBuilder<CoreModuleDbContext>()
                .UseSqlServer(connectionString, sql => sql
                    .EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), null)
                    .MigrationsAssembly(typeof(CoreModuleDbContext).Assembly.FullName))
                .Options;

            await using var context = new CoreModuleDbContext(this.Options);
            this.Log("Applying EF Core migrations (container)...");
            await context.Database.MigrateAsync();
            this.Available = true;
            this.Log("Migrations applied successfully (container mode). Fixture ready.");
            return;
        }
        catch (Exception ex)
        {
            this.FailureReason = $"Docker container start failed: {ex.Message}";
            this.Log(this.FailureReason);
        }

        try // Fallback: use localdb (developer machine) if Docker not available
        {
            this.Log("Falling back to LocalDB...");
            this.fallbackDatabaseName = $"bit_devkit_gettingstarted_test_{Guid.NewGuid():N}";
            this.fallbackConnectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={this.fallbackDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true";

            this.Options = new DbContextOptionsBuilder<CoreModuleDbContext>()
                .UseSqlServer(this.fallbackConnectionString, sql => sql
                    .EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), null)
                    .MigrationsAssembly(typeof(CoreModuleDbContext).Assembly.FullName))
                .Options;

            await using var context = new CoreModuleDbContext(this.Options);
            this.Log("Applying EF Core migrations (LocalDB fallback)...");
            await context.Database.MigrateAsync();
            this.Available = true;
            this.Log("Migrations applied successfully (LocalDB mode). Fixture ready.");
        }
        catch (Exception ex)
        {
            this.FailureReason += $" | LocalDB fallback failed: {ex.Message}";
            this.Log($"LocalDB fallback failed: {ex.Message}");
            this.Available = false;
        }
    }

    public async Task DisposeAsync()
    {
        this.Log("Disposing SQL fixture...");
        try
        {
            if (this.container != null)
            {
                this.Log("Stopping container...");
                await this.container.StopAsync();
                await this.container.DisposeAsync();
                this.Log("Container disposed.");
            }
        }
        catch (Exception ex)
        {
            this.Log($"Error stopping container: {ex.Message}");
        }

        if (this.FallbackUsed && this.Available)
        {
            try
            {
                this.Log($"Dropping LocalDB database '{this.fallbackDatabaseName}'...");
                await using var con = new Microsoft.Data.SqlClient.SqlConnection("Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;");
                await con.OpenAsync();
                await using var cmd = con.CreateCommand();
                cmd.CommandText = $"IF DB_ID('{this.fallbackDatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{this.fallbackDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{this.fallbackDatabaseName}] END";
                await cmd.ExecuteNonQueryAsync();
                this.Log("LocalDB database dropped.");
            }
            catch (Exception ex)
            {
                this.Log($"Error dropping LocalDB database: {ex.Message}");
            }
        }
        this.Log("SQL fixture disposed.");
    }

    public void SkipIfUnavailable()
    {
        if (!this.Available)
        {
            this.Log("Skipping tests - no SQL available.");
            throw SkipException.ForSkip($"No SQL Server available (Testcontainers + LocalDB failed). Reason: {this.FailureReason}");
        }
    }
}
