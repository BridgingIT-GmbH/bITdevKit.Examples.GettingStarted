using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOutboxPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_ProcessedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_Type",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyVersion",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedUntil",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessingStartedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_ArchivedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                columns: new[] { "IsArchived", "ArchivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_ProcessedDate_LockedUntil_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                columns: new[] { "IsArchived", "ProcessedDate", "LockedUntil", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_Type_ProcessedDate_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                columns: new[] { "IsArchived", "Type", "ProcessedDate", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_ArchivedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_ProcessedDate_LockedUntil_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropIndex(
                name: "IX___Outbox_DomainEvents_IsArchived_Type_ProcessedDate_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "ArchivedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "ConcurrencyVersion",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "LastError",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedDate",
                schema: "core",
                table: "__Outbox_DomainEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                schema: "core",
                table: "__Outbox_DomainEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_CreatedDate",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "CreatedDate");

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
    }
}
