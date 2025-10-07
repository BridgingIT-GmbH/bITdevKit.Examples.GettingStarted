using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CreateOutboxDomainEventsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "__Outbox_DomainEvents",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Outbox_DomainEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_EventId",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_ProcessedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "ProcessedDate");

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_Type",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__Outbox_DomainEvents",
                schema: "core");
        }
    }
}
