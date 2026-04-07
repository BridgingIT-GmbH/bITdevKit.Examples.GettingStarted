// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Command to change a Aggregate status to any valid <see cref="Domain.Model.CustomerStatus"/>.
/// </summary>
/// <summary>
/// Handler for <see cref="CustomerUpdateStatusCommand"/>. Loads the customer, changes status, persists and returns updated DTO.
/// </summary>
//[HandlerRetry(2, 100)]
//[HandlerTimeout(500)]
[Command]
public partial class CustomerUpdateStatusCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerUpdateStatusCommand"/> class.
    /// </summary>
    /// <param name="id">The string representation of the Aggregate's identifier.</param>
    /// <param name="status">Target status value (e.g., "Lead", "Active", "Retired").</param>
    public CustomerUpdateStatusCommand(string id, string status)
    {
        Id = id;
        Status = status;
    }

    /// <summary>Gets or sets the Aggregate id.</summary>
    [ValidateNotEmptyGuid("Invalid guid.")]
    public string Id { get; set; }

    /// <summary>Gets or sets target status value.</summary>
    [ValidateNotEmpty("Invalid status value. Valid values: Lead, Active, Retired.")]
    public string Status { get; set; } // TODO: use CustomerStatus Enumeration instead of string to enforce valid values at compile time

    /// <summary>Validator ensuring valid id and status.</summary>
    [Validate]
    private static void Validate(InlineValidator<CustomerUpdateStatusCommand> validator)
    {
        validator.RuleFor(c => c.Status)
            .Must(value => CustomerStatus.GetAll().Any(s => s.Value == value))
            .WithMessage("Invalid status value. Valid values: Lead, Active, Retired.");
    }

    [Handle]
    private async Task<Result<CustomerModel>> HandleAsync(
        ILogger<CustomerUpdateStatusCommand> logger,
        IMapper mapper,
        IGenericRepository<Customer> repository,
        CancellationToken cancellationToken) =>
            // STEP 1 - Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(Id), cancellationToken: cancellationToken)

            // STEP 2 - Change status (idempotent if same)
            .Bind(e => e.ChangeStatus(Status))

            // STEP 3 - Update in repository
            .BindAsync(async (e, ct) =>
                await repository.UpdateResultAsync(e, ct), cancellationToken)

            // STEP 4 — Side effects (audit/logging)
            .Log(logger, "AUDIT - Customer {Id} status updated for {Email}", r => [r.Value.Id, r.Value.Email.Value])

            // STEP 5 — Map updated Aggregate -> Model
            .MapResult<Customer, CustomerModel>(mapper);
}
