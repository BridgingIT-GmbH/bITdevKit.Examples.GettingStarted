using BridgingIT.DevKit.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddQuartzTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SqlServerJobStoreMigrationHelper.CreateQuartzTables(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            SqlServerJobStoreMigrationHelper.DropQuartzTables(migrationBuilder);
        }
    }
}
