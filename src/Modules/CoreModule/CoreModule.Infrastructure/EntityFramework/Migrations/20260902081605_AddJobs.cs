using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Infrastructure.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "__Jobs_AcceptedEvents",
                schema: "core",
                columns: table => new
                {
                    AcceptedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SerializedData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_AcceptedEvents", x => x.AcceptedEventId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_Batches",
                schema: "core",
                columns: table => new
                {
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalBatchId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionPolicy = table.Column<int>(type: "int", nullable: false),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CausationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AcceptedCount = table.Column<int>(type: "int", nullable: false),
                    SucceededCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    CancelledCount = table.Column<int>(type: "int", nullable: false),
                    ArchivedCount = table.Column<int>(type: "int", nullable: false),
                    CancellationRequestedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_Batches", x => x.BatchId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_BatchHistory",
                schema: "core",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalBatchId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BatchStatus = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SchedulerInstanceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_BatchHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_BatchOccurrences",
                schema: "core",
                columns: table => new
                {
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildStatus = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_BatchOccurrences", x => new { x.BatchId, x.OccurrenceId });
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_ExecutionHistory",
                schema: "core",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TriggerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OccurrenceStatus = table.Column<int>(type: "int", nullable: true),
                    ExecutionStatus = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_ExecutionHistory", x => x.HistoryId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_Executions",
                schema: "core",
                columns: table => new
                {
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TriggerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_Executions", x => x.ExecutionId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_Leases",
                schema: "core",
                columns: table => new
                {
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchedulerInstanceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OwnershipToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AcquiredUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RenewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RenewalCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_Leases", x => x.OccurrenceId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_OccurrenceDependencies",
                schema: "core",
                columns: table => new
                {
                    DependencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DependentOccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrerequisiteOccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredStatuses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailurePolicy = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_OccurrenceDependencies", x => x.DependencyId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_Occurrences",
                schema: "core",
                columns: table => new
                {
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TriggerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SerializedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SerializedProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CausationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ResumeStatus = table.Column<int>(type: "int", nullable: true),
                    BlockedReason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_Occurrences", x => x.OccurrenceId);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_RuntimeStates",
                schema: "core",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    Paused = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_RuntimeStates", x => x.JobName);
                });

            migrationBuilder.CreateTable(
                name: "__Jobs_TriggerRuntimeStates",
                schema: "core",
                columns: table => new
                {
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TriggerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActivatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DueUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastMaterializedScheduledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HasMaterializedOccurrence = table.Column<bool>(type: "bit", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: true),
                    Paused = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAcceptedEventUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAcceptedEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___Jobs_TriggerRuntimeStates", x => new { x.JobName, x.TriggerName });
                });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_AcceptedEvents_Source_AcceptedUtc_AcceptedEventId",
                schema: "core",
                table: "__Jobs_AcceptedEvents",
                columns: new[] { "Source", "AcceptedUtc", "AcceptedEventId" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_AcceptedEvents_Source_IdempotencyKey",
                schema: "core",
                table: "__Jobs_AcceptedEvents",
                columns: new[] { "Source", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Batches_ArchivedDate",
                schema: "core",
                table: "__Jobs_Batches",
                column: "ArchivedDate");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Batches_CorrelationId",
                schema: "core",
                table: "__Jobs_Batches",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Batches_ExternalBatchId",
                schema: "core",
                table: "__Jobs_Batches",
                column: "ExternalBatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Batches_IdempotencyKey",
                schema: "core",
                table: "__Jobs_Batches",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Batches_Status_CreatedDate",
                schema: "core",
                table: "__Jobs_Batches",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_BatchHistory_BatchId_RecordedAt",
                schema: "core",
                table: "__Jobs_BatchHistory",
                columns: new[] { "BatchId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_BatchHistory_EventName_RecordedAt",
                schema: "core",
                table: "__Jobs_BatchHistory",
                columns: new[] { "EventName", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_BatchOccurrences_BatchId_Sequence",
                schema: "core",
                table: "__Jobs_BatchOccurrences",
                columns: new[] { "BatchId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_BatchOccurrences_OccurrenceId",
                schema: "core",
                table: "__Jobs_BatchOccurrences",
                column: "OccurrenceId");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_ExecutionHistory_EventName_RecordedAt",
                schema: "core",
                table: "__Jobs_ExecutionHistory",
                columns: new[] { "EventName", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_ExecutionHistory_ExecutionId_RecordedAt",
                schema: "core",
                table: "__Jobs_ExecutionHistory",
                columns: new[] { "ExecutionId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_ExecutionHistory_OccurrenceId_RecordedAt",
                schema: "core",
                table: "__Jobs_ExecutionHistory",
                columns: new[] { "OccurrenceId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Executions_JobName_TriggerName_StartedUtc",
                schema: "core",
                table: "__Jobs_Executions",
                columns: new[] { "JobName", "TriggerName", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Executions_OccurrenceId_AttemptNumber",
                schema: "core",
                table: "__Jobs_Executions",
                columns: new[] { "OccurrenceId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Executions_Status_StartedUtc",
                schema: "core",
                table: "__Jobs_Executions",
                columns: new[] { "Status", "StartedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Leases_ExpiresUtc",
                schema: "core",
                table: "__Jobs_Leases",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Leases_SchedulerInstanceId_ExpiresUtc",
                schema: "core",
                table: "__Jobs_Leases",
                columns: new[] { "SchedulerInstanceId", "ExpiresUtc" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_OccurrenceDependencies_DependentOccurrenceId_Status",
                schema: "core",
                table: "__Jobs_OccurrenceDependencies",
                columns: new[] { "DependentOccurrenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_OccurrenceDependencies_PrerequisiteOccurrenceId_Status",
                schema: "core",
                table: "__Jobs_OccurrenceDependencies",
                columns: new[] { "PrerequisiteOccurrenceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Occurrences_CorrelationId",
                schema: "core",
                table: "__Jobs_Occurrences",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Occurrences_IdempotencyKey",
                schema: "core",
                table: "__Jobs_Occurrences",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Occurrences_JobName_TriggerName_DueUtc",
                schema: "core",
                table: "__Jobs_Occurrences",
                columns: new[] { "JobName", "TriggerName", "DueUtc" });

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Occurrences_OccurrenceKey",
                schema: "core",
                table: "__Jobs_Occurrences",
                column: "OccurrenceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX___Jobs_Occurrences_Status_DueUtc",
                schema: "core",
                table: "__Jobs_Occurrences",
                columns: new[] { "Status", "DueUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__Jobs_AcceptedEvents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_Batches",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_BatchHistory",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_BatchOccurrences",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_ExecutionHistory",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_Executions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_Leases",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_OccurrenceDependencies",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_Occurrences",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_RuntimeStates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Jobs_TriggerRuntimeStates",
                schema: "core");

        }
    }
}
