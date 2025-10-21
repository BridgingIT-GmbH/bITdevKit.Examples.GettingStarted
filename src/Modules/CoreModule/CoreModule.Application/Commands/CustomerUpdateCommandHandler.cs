// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Events;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using FluentValidation;

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
            // Map from DTO -> domain entity
            await mapper.MapResult<CustomerModel, Customer>(request.Model)

            // Run some business rules
            .UnlessAsync(async (e, ct) => await Rule
                .Add(RuleSet.IsNotEmpty(e.FirstName))
                .Add(RuleSet.IsNotEmpty(e.LastName))
                .Add(RuleSet.NotEqual(e.LastName, "notallowed"))
                // TODO: Check unique email excluding the current entity (currently disabled)
                //.Add(new EmailShouldBeUniqueRule(customer.Email, repository))

                .CheckAsync(cancellationToken), cancellationToken: cancellationToken)

            // Register domain event
            .Tap(e => e.DomainEvents.Register(new CustomerUpdatedDomainEvent(e)))

            // Update in repository
            .BindAsync(async (e, ct) =>
                await repository.UpdateResultAsync(e, ct), cancellationToken)

            // Side-effect (audit or logging)
            .Tap(_ => Console.WriteLine("AUDIT"))

            // Map domain entity -> DTO result
            .MapResult<Customer, CustomerModel>(mapper);
            //.Map(mapper.Map<Customer, CustomerModel>);
}