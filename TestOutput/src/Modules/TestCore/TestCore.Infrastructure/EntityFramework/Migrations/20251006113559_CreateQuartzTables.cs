using BridgingIT.DevKit.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestOutput.Modules.TestCore.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateQuartzTables : Migration
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
