// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Infrastructure.EntityFramework;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

public class CoreModuleDbContextTests
{
    private const string Conn =
        //"Server=127.0.0.1,14339;Database=bit_devkit_gettingstarted;User=sa;Password=Abcd1234!;Trusted_Connection=False;TrustServerCertificate=True;MultipleActiveResultSets=true;Encrypt=False;";
        "Server=(localdb)\\MSSQLLocalDB;Database=bit_devkit_gettingstarted;Trusted_Connection=True;MultipleActiveResultSets=true";

    private static DbContextOptions<CoreModuleDbContext> BuildOptions()
    {
        return new DbContextOptionsBuilder<CoreModuleDbContext>()
            .UseSqlServer(
                Conn,
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(1),
                    errorNumbersToAdd: null))
            .Options;
    }

    [Fact] // or ClassFixture ctor
    public async Task EnsureSqlReadyAsync()
    {
        using var con = new SqlConnection(Conn);
        await con.OpenAsync();

        var dbName = "bit_devkit_gettingstarted";
        using var cmd = new SqlCommand($@"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @db)
BEGIN
    DECLARE @sql nvarchar(max) = 'CREATE DATABASE [' + @db + ']';
    EXEC (@sql);
END", con);
        cmd.Parameters.AddWithValue("@db", dbName);
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Can_Query_Empty_Customers_Table()
    {
        await using var ctx = new CoreModuleDbContext(BuildOptions());

        var canConnect = await ctx.Database.CanConnectAsync();
        Assert.True(canConnect);

        // Minimal query; replace with something that matches your current schema
        _ = await ctx.Database.ExecuteSqlRawAsync("SELECT 1");
    }
}