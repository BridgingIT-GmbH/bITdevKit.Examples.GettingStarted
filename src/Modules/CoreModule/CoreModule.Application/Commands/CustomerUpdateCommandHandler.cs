// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="CustomerUpdateCommand"/>.
/// Maps DTO -> domain, checks business rules, updates the entity in the repository,
/// and maps back to <see cref="CustomerModel"/> for returning to the client.
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) and timeout (<see cref="HandlerTimeoutAttribute"/>).
/// - Rule validation similar to <see cref="CustomerCreateCommandHandler"/>, but must consider
///   excluding the current customer when checking uniqueness (TODO noted).
/// </remarks>
//[HandlerRetry(2, 100)]   // retry on transient failures
//[HandlerTimeout(500)]    // max execution 500ms
public class CustomerUpdateCommandHandler(
    ILogger<CustomerUpdateCommandHandler> logger,
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerUpdateCommand, CustomerModel>
{
    /// <summary>
    /// Handles the <see cref="CustomerUpdateCommand"/>. Steps:
    /// 1. Map DTO to <see cref="Customer"/> aggregate.
    /// 2. Validate inline rules (basic invariants, e.g., names not empty).
    /// 3. Persist changes via repository update.
    /// 4. Perform audit/logging side-effects.
    /// 5. Map updated domain aggregate to <see cref="CustomerModel"/>.
    /// </summary>
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerUpdateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // STEP 1 - Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(request.Model.Id), cancellationToken: cancellationToken)
            //.Unless((e) => e?.AuditState?.IsDeleted() == true, new NotFoundError("Entity already deleted"))

            // STEP 2 — Validate request model
            .UnlessAsync(async (e, ct) => await Rule
                .Add(RuleSet.IsNotEmpty(e.FirstName)) // also validated in domain
                .Add(RuleSet.IsNotEmpty(e.LastName)) // also validated in domain
                .Add(RuleSet.NotEqual(e.LastName, "notallowed")) // also validated in domain
                //.Add(new EmailShouldBeUniqueRule(e.Email, repository)) // TODO: Check unique email excluding the current entity (currently disabled)
                .CheckAsync(cancellationToken), cancellationToken: cancellationToken)

            // STEP 3 - Apply changes to Aggregate from request model
            .Bind(e => this.UpdateAggregate(e, request.Model))

            // STEP 4 — Save updated Aggregate to repository
            .BindAsync(async (e, ct) =>
                await repository.UpdateResultAsync(e, ct), cancellationToken)

            // STEP 5 — Side effects (audit/logging)
            .Log(logger, "AUDIT - Customer {Id} updated for {Email}", r => [r.Value.Id, r.Value.Email.Value])

            // STEP 6 — Map updated Aggregate -> Model
            .MapResult<Customer, CustomerModel>(mapper)
            .Log(logger, "Aggregate mapped to {@Model}", r => [r.Value]);

    /// <summary>
    /// Updates the customer's basic properties (name, email, birth date, status).
    /// </summary>
    /// <param name="customer">The customer aggregate to update.</param>
    /// <param name="model">The customer model containing the updated values.</param>
    /// <returns>The updated customer wrapped in a Result.</returns>
    private Result<Customer> UpdateAggregate(Customer customer, CustomerModel model) =>
        // Setup the customer update chain.
        Result<Customer>.Success(customer)
            .Bind(e => e.ChangeName(model.FirstName, model.LastName))
            .Bind(e => e.ChangeEmail(model.Email))
            .Bind(e => e.ChangeBirthDate(model.DateOfBirth))
            .Bind(e => e.ChangeStatus(model.Status))
            .Bind(e => this.ChangeAddresses(customer, model))
            .Tap(e => e.ConcurrencyVersion = Guid.Parse(model.ConcurrencyVersion)); // set concurrency version for optimistic concurrency check

    /// <summary>
    /// Processes address changes by comparing the specified addresses with existing ones.
    /// Removes Customer addresses not in the specified addresses, adds new addresses and updates existing addresses.
    /// </summary>
    /// <param name="customer">The customer aggregate to update.</param>
    /// <param name="model">The customer model containing the updated values.</param>
    /// <returns>The updated customer wrapped in a Result.</returns>
    private Result<Customer> ChangeAddresses(Customer customer, CustomerModel model)
    {
        // Setup the address update chain.
        return Result<Customer>.Success(customer)
            .When(_ => model.Addresses.SafeAny(), r => r
                .Bind(c => RemoveMissing(c, model.Addresses))
                .Bind(c => AddOrChange(c, model.Addresses))
                .Bind(c => SetPrimary(c, model.Addresses)));

        // Removes addresses from the customer that are not present in the provided address models.
        Result<Customer> RemoveMissing(Customer customer, List<CustomerAddressModel> addressModels)
        {
            var keepIds = addressModels
                .Where(m => !string.IsNullOrWhiteSpace(m.Id))
                .Select(m => AddressId.Create(m.Id)).ToList();
            var removeIds = customer.Addresses.Select(a => a.Id).Except(keepIds).ToList();
            foreach (var id in removeIds)
            {
                var result = customer.RemoveAddress(id);
                if (result.IsFailure) return result;
            }
            return Result<Customer>.Success(customer);
        }

        // Processes the provided address models to add new addresses or update existing ones.
        Result<Customer> AddOrChange(Customer customer, List<CustomerAddressModel> addressModels)
        {
            foreach (var m in addressModels)
            {
                var result = string.IsNullOrWhiteSpace(m.Id)
                    ? customer.AddAddress(m.Name, m.Line1, m.Line2, m.PostalCode, m.City, m.Country)
                    : customer.ChangeAddress(m.Id, m.Name, m.Line1, m.Line2, m.PostalCode, m.City, m.Country);
                if (result.IsFailure) return result;
            }
            return Result<Customer>.Success(customer);
        }

        // Sets the primary address of the customer based on the provided address models.
        Result<Customer> SetPrimary(Customer customer, List<CustomerAddressModel> addressModels) =>
            addressModels.Find(m => m.IsPrimary).Match(
                some: m => customer.SetPrimaryAddress(m.Id),
                none: () => Result<Customer>.Success(customer));
    }
}