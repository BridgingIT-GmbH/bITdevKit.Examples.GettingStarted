using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.CreateSequence<int>(
                name: "CustomerNumbers",
                schema: "core",
                startValue: 100000L);

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

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "date", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditState_CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuditState_CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AuditState_CreatedDescription = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AuditState_UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuditState_UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AuditState_UpdatedDescription = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AuditState_UpdatedReasons = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    AuditState_Deactivated = table.Column<bool>(type: "bit", nullable: true),
                    AuditState_DeactivatedReasons = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: true),
                    AuditState_DeactivatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuditState_DeactivatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AuditState_DeactivatedDescription = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AuditState_Deleted = table.Column<bool>(type: "bit", nullable: true),
                    AuditState_DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AuditState_DeletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AuditState_DeletedReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    AuditState_DeletedDescription = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
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

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "core");

            migrationBuilder.DropSequence(
                name: "CustomerNumbers",
                schema: "core");
        }
    }
}
