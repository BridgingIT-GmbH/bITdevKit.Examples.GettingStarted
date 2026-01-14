// ============================================================================
// TEMPLATE: Delete Command for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines a command for deleting an existing aggregate instance by ID.
//   Returns Unit (void equivalent) on success.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Commands/
//   3. File name: [Entity]DeleteCommand.cs
//   4. Invoked from Presentation layer: await requester.SendAsync(new [Entity]DeleteCommand(id))
//
// RELATED PATTERNS:
//   - RequestBase<Unit>: bITdevKit base class for commands returning no data
//   - Unit: Represents "void" in functional programming (successful completion with no value)
//   - AbstractValidator<T>: FluentValidation for input validation
//   - [Entity]DeleteCommandHandler: Processes this command
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Command to delete an existing <see cref="[Entity]"/> Aggregate by its unique identifier.
/// </summary>
/// <param name="id">The string representation of the Aggregate's identifier.</param>
/// <remarks>
/// This command follows the CQRS pattern:
/// - Represents user intent to permanently delete an aggregate
/// - Contains only the entity ID
/// - Returns Unit (void equivalent) on success
/// - Validation is performed via FluentValidation rules
/// - Processed by <see cref="[Entity]DeleteCommandHandler"/>
///
/// IMPORTANT: Deletion is permanent. Consider soft-delete patterns for audit requirements.
/// </remarks>
public class [Entity]DeleteCommand(string id) : RequestBase<Unit>
{
    /// <summary>
    /// Gets the Aggregate id to delete.
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// Validation rules for <see cref="[Entity]DeleteCommand"/> using FluentValidation.
    /// Executes before the handler via ValidationPipelineBehavior.
    /// </summary>
    public class Validator : AbstractValidator<[Entity]DeleteCommand>
    {
        public Validator()
        {
            // RULE: ID must be a valid non-empty GUID
            this.RuleFor(c => c.Id)
                .MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid or missing entity ID.");

            // ================================================================
            // ADD ADDITIONAL VALIDATION RULES IF NEEDED
            // ================================================================
            // Example: Prevent deletion based on business rules
            // (Note: Most complex deletion rules should be in the handler or domain layer)

            // this.RuleFor(c => c.Id)
            //     .Must(id => !IsProtectedEntity(id))
            //     .WithMessage("Cannot delete protected entities.");
        }
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. PRIMARY CONSTRUCTOR WITH PARAMETER:
//    - public class [Entity]DeleteCommand(string id) : RequestBase<Unit>
//    - Concise: parameter becomes a property automatically
//
// 2. REQUESTBASE<UNIT>:
//    - Unit = functional "void" (successful completion with no return value)
//    - Represents successful deletion without returning entity data
//    - Alternative: Return deleted entity if needed (change to RequestBase<[Entity]Model>)
//
// 3. IMMUTABLE COMMAND:
//    - public string Id { get; } = id; (getter only)
//    - Commands should be immutable after construction
//    - Prevents accidental modification in pipeline
//
// 4. MINIMAL VALIDATION:
//    - Delete commands typically only validate ID format
//    - Complex business rules (e.g., "can this entity be deleted?") belong in handler or domain
//
// 5. COMMAND NAMING CONVENTION:
//    - [Entity]DeleteCommand (e.g., CustomerDeleteCommand)
//
// USAGE EXAMPLES:
//
//   // From Presentation layer (Minimal API endpoint):
//   group.MapDelete("/{id:guid}",
//       async ([FromServices] IRequester requester,
//              [FromRoute] string id, CancellationToken ct)
//              => (await requester
//                   .SendAsync(new [Entity]DeleteCommand(id), cancellationToken: ct))
//                   .MapHttpNoContent());
//
//   // From Application layer (another handler/service):
//   var result = await requester.SendAsync(
//       new [Entity]DeleteCommand(entityId),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       logger.LogInformation("[Entity] deleted successfully");
//   }
//   else if (result.HasError<NotFoundError>())
//   {
//       logger.LogWarning("[Entity] not found for deletion");
//   }
//
// ============================================================================
// SOFT DELETE ALTERNATIVE
// ============================================================================
//
// If your domain requires audit trails or "undo" capabilities, consider soft delete:
//
// 1. Add IsDeleted/DeletedDate properties to aggregate
// 2. Change command to [Entity]ArchiveCommand or [Entity]SoftDeleteCommand
// 3. Handler calls: entity.Archive() or entity.MarkAsDeleted()
// 4. Repository: Filter out soft-deleted entities in queries (global query filter in EF Core)
// 5. Return [Entity]Model instead of Unit to confirm soft-delete state
//
// Example:
//   public class [Entity]ArchiveCommand(string id) : RequestBase<[Entity]Model>
//   {
//       public string Id { get; } = id;
//   }
//
//   // Handler:
//   .Bind(entity => entity.Archive())
//   .BindAsync(async (entity, ct) => await repository.UpdateResultAsync(entity, ct))
//
// ============================================================================
