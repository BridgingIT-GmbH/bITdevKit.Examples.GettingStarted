using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "__Logging_LogEntries",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SpanId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LogKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: true),
                    ModuleName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThreadId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ShortTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LogEvent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Logging_LogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_CorrelationId",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_IsArchived",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_Level",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_LogKey",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "LogKey");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_ModuleName",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "ModuleName");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_ShortTypeName",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "ShortTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_TimeStamp",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_Logging_LogEntries_TraceId",
                schema: "core",
                table: "__Logging_LogEntries",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__Logging_LogEntries",
                schema: "core");
        }
    }
}
