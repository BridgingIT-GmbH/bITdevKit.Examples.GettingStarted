// ============================================================================
// TEMPLATE: Update Command for [Entity] Aggregate
// ============================================================================
// PURPOSE:
//   Defines a command for updating an existing aggregate instance with validation rules.
//   Requires concurrency version for optimistic concurrency control.
//
// PLACEHOLDERS TO REPLACE:
//   [Module]       - Module name (e.g., CoreModule)
//   [Entity]       - Aggregate root name (e.g., Customer, Product, Order)
//   [Property]     - Property names to validate (e.g., FirstName, LastName, Email)
//
// USAGE:
//   1. Replace all placeholders with actual values
//   2. Place in: src/Modules/[Module]/[Module].Application/Commands/
//   3. File name: [Entity]UpdateCommand.cs
//   4. Add/remove validation rules based on your aggregate's properties
//   5. Invoked from Presentation layer: await requester.SendAsync(new [Entity]UpdateCommand(model))
//
// RELATED PATTERNS:
//   - RequestBase<T>: bITdevKit base class for CQRS commands/queries
//   - AbstractValidator<T>: FluentValidation for input validation
//   - [Entity]UpdateCommandHandler: Processes this command
//   - Optimistic Concurrency: ConcurrencyVersion prevents conflicting updates
// ============================================================================

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.[Module].Domain.Model;

/// <summary>
/// Command to update an existing <see cref="[Entity]"/> Aggregate.
/// </summary>
/// <remarks>
/// This command follows the CQRS pattern:
/// - Represents user intent to update an existing aggregate
/// - Contains DTO model with all modified data
/// - Requires valid ID and concurrency version (optimistic concurrency)
/// - Validation is performed via FluentValidation rules
/// - Processed by <see cref="[Entity]UpdateCommandHandler"/>
/// </remarks>
public class [Entity]UpdateCommand([Entity]Model model) : RequestBase<[Entity]Model>
{
    /// <summary>
    /// Gets or sets the Model (<see cref="[Entity]Model"/>) that contains data for the Aggregate to update.
    /// </summary>
    public [Entity]Model Model { get; set; } = model;

    /// <summary>
    /// Validation rules for <see cref="[Entity]UpdateCommand"/> using FluentValidation.
    /// Executes before the handler via ValidationPipelineBehavior.
    /// </summary>
    public class Validator : AbstractValidator<[Entity]UpdateCommand>
    {
        public Validator()
        {
            // RULE: Model must not be null
            this.RuleFor(c => c.Model)
                .NotNull()
                .WithMessage("Model is required.");

            // RULE: ID must be a valid non-empty GUID (entity must exist)
            this.RuleFor(c => c.Model.Id)
                .MustNotBeDefaultOrEmptyGuid()
                .WithMessage("Invalid or missing entity ID.");

            // RULE: Concurrency version is required for optimistic concurrency control
            // (Prevents lost updates when multiple users edit the same entity)
            // this.RuleFor(c => c.Model.ConcurrencyVersion)
            //     .NotNull().NotEmpty()
            //     .WithMessage("Concurrency version is required for update operations.");

            // ================================================================
            // ADD PROPERTY-SPECIFIC VALIDATION RULES BELOW
            // ================================================================
            // Example validation rules (customize based on your aggregate):

            // this.RuleFor(c => c.Model.[Property])
            //     .NotNull().NotEmpty()
            //     .WithMessage("[Property] must not be empty.");
            //
            // this.RuleFor(c => c.Model.[Property])
            //     .MaximumLength(256)
            //     .WithMessage("[Property] must not exceed 256 characters.");
            //
            // this.RuleFor(c => c.Model.[Property])
            //     .EmailAddress()
            //     .WithMessage("[Property] must be a valid email address.");
            //
            // this.RuleFor(c => c.Model.[Property])
            //     .GreaterThan(0)
            //     .WithMessage("[Property] must be greater than zero.");

            // ================================================================
            // VALIDATION STRATEGY
            // ================================================================
            // FluentValidation rules handle:
            //   - Structural validation (not null, proper format, length limits)
            //   - Data type validation (email format, numeric ranges)
            //   - Basic business rules (required fields, format constraints)
            //
            // Domain layer handles:
            //   - Complex business invariants (aggregate consistency)
            //   - Cross-entity rules (uniqueness checks excluding current entity)
            //   - State transitions (status changes, lifecycle rules)
        }
    }
}

// ============================================================================
// KEY PATTERNS DEMONSTRATED
// ============================================================================
//
// 1. PRIMARY CONSTRUCTOR:
//    - public class [Entity]UpdateCommand([Entity]Model model) : RequestBase<[Entity]Model>
//
// 2. REQUESTBASE<T> INHERITANCE:
//    - T = return type (typically [Entity]Model for update operations)
//    - Returns updated entity after successful persistence
//
// 3. OPTIMISTIC CONCURRENCY:
//    - ConcurrencyVersion field in DTO (Guid as string)
//    - Validated in handler: entity.ConcurrencyVersion must match request.Model.ConcurrencyVersion
//    - EF Core checks on save: throws DbUpdateConcurrencyException if mismatch
//    - Prevents lost updates in concurrent editing scenarios
//
// 4. ID VALIDATION:
//    - MustNotBeDefaultOrEmptyGuid(): Ensures valid entity identifier
//    - Prevents updates to non-existent entities
//
// 5. COMMAND NAMING CONVENTION:
//    - [Entity]UpdateCommand (e.g., CustomerUpdateCommand)
//
// USAGE EXAMPLES:
//
//   // From Presentation layer (Minimal API endpoint):
//   group.MapPut("/{id:guid}",
//       async ([FromServices] IRequester requester,
//              [FromRoute] string id,
//              [FromBody] [Entity]Model model, CancellationToken ct)
//              => (await requester
//                   .SendAsync(new [Entity]UpdateCommand(model), cancellationToken: ct))
//                   .MapHttpOk());
//
//   // From Application layer (another handler/service):
//   var result = await requester.SendAsync(
//       new [Entity]UpdateCommand(model),
//       cancellationToken: cancellationToken);
//
//   if (result.IsSuccess)
//   {
//       var updated[Entity] = result.Value;
//       // ... use the updated entity
//   }
//   else if (result.HasError<ConcurrencyError>())
//   {
//       // Handle optimistic concurrency conflict (409 Conflict)
//       logger.LogWarning("Entity was modified by another user");
//   }
//
// ============================================================================
