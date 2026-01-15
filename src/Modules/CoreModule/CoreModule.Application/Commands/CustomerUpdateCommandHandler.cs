// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="CustomerUpdateCommand"/>.
/// Maps DTO → domain, checks business rules, updates the entity in the repository,
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

            // STEP 3 - Apply changes to Aggregate
            .Bind(e => e.ChangeName(request.Model.FirstName, request.Model.LastName))
            .Bind(e => e.ChangeEmail(request.Model.Email))
            .Bind(e => e.ChangeBirthDate(request.Model.DateOfBirth))
            .Bind(e => e.ChangeStatus(request.Model.Status))
            .Bind(e => this.UpdateAddresses(e, request.Model.Addresses))
            .Tap(e => // set concurrency version for optimistic concurrency check
            {
                e.ConcurrencyVersion = Guid.Parse(request.Model.ConcurrencyVersion);
            })

            // STEP 4 — Save updated Aggregate to repository
            .BindAsync(async (e, ct) =>
                await repository.UpdateResultAsync(e, ct), cancellationToken)

            // STEP 5 — Side effects (audit/logging)
            .Log(logger, "AUDIT - Customer {Id} updated for {Email}", r => [r.Value.Id, r.Value.Email.Value])

            // STEP 6 — Map updated Aggregate → Model
            .MapResult<Customer, CustomerModel>(mapper)
            .Log(logger, "Entity mapped to {@Model}", r => [r.Value]);

    /// <summary>
    /// Processes address changes by comparing the specified addresses with existing ones.
    /// Removes Customer addresses not in the specified addresses, adds new addresses and updates existing addresses.
    /// </summary>
    /// <param name="customer">The customer aggregate to update.</param>
    /// <param name="addressModels">The addresses from the update request.</param>
    /// <returns>The updated customer wrapped in a Result.</returns>
    private Result<Customer> UpdateAddresses(Customer customer, List<CustomerAddressModel> addressModels)
    {
        addressModels ??= [];

        // Extract valid address IDs from request using LINQ fluent style
        var addressIds = addressModels
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => AddressId.Create(m.Id)).ToList();

        // Remove obsolete addresses - find and return first failure if any
        var removeResult = customer.Addresses
            .Select(a => a.Id).Except(addressIds)
            .Select(customer.RemoveAddress).FirstOrDefault(r => r.IsFailure).Unwrap(); // TODO: use .Flatten()

        // Early return on first failure using fluent When extension
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        // Process each address: add new or update existing
        foreach (var addressModel in addressModels)
        {
            var result = ProcessAddress(customer, addressModel);
            if (result.IsFailure)
            {
                return result;
            }
        }

        // Set primary address if specified
        return addressModels
            .Find(m => m.IsPrimary)
            .Match(
                some: m => customer.SetPrimaryAddress(m.Id),
                none: () => Result<Customer>.Success(customer));

        Result<Customer> ProcessAddress(Customer customer, CustomerAddressModel model) =>
            string.IsNullOrWhiteSpace(model.Id)
                ? customer.AddAddress(
                    model.Name, model.Line1, model.Line2,
                    model.PostalCode, model.City, model.Country)
                : customer.Addresses
                    .Find(a => a.Id == AddressId.Create(model.Id))
                    .Match(
                        some: _ => customer.ChangeAddress(
                            model.Id, model.Name, model.Line1, model.Line2,
                            model.PostalCode, model.City, model.Country),
                        none: () => Result<Customer>.Success(customer));
    }
}