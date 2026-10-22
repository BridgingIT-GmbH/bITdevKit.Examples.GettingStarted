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

            migrationBuilder.CreateTable(
                name: "__Outbox_DomainEvents",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConcurrencyVersion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LockedUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProcessingStartedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProcessedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "CustomersAddresses",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Line1 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Line2 = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomersAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomersAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "core",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX___Outbox_DomainEvents_EventId",
                schema: "core",
                table: "__Outbox_DomainEvents",
                column: "EventId");

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

            migrationBuilder.CreateIndex(
                name: "IX_CustomersAddresses_CustomerId",
                schema: "core",
                table: "CustomersAddresses",
                column: "CustomerId");
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

            migrationBuilder.DropTable(
                name: "__Logging_LogEntries",
                schema: "core");

            migrationBuilder.DropTable(
                name: "__Outbox_DomainEvents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "CustomersAddresses",
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
