// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="CustomerUpdateStatusCommand"/>. Loads the customer, changes status, persists and returns updated DTO.
/// </summary>
//[HandlerRetry(2, 100)]
//[HandlerTimeout(500)]
public class CustomerUpdateStatusCommandHandler(
    ILogger<CustomerUpdateStatusCommandHandler> logger,
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerUpdateStatusCommand, CustomerModel>
{
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerUpdateStatusCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // STEP 1 - Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(request.Id), cancellationToken: cancellationToken)

            // STEP 2 - Change status (idempotent if same)
            .Bind(e => e.ChangeStatus(request.Status))

            // STEP 3 - Update in repository
            .BindAsync(async (e, ct) =>
                await repository.UpdateResultAsync(e, ct), cancellationToken)

            // STEP 4 — Side effects (audit/logging)
            .Log(logger, "AUDIT - Customer {Id} status updated for {Email}", r => [r.Value.Id, r.Value.Email.Value])

            // STEP 5 — Map updated Aggregate -> Model
            .MapResult<Customer, CustomerModel>(mapper);
}
