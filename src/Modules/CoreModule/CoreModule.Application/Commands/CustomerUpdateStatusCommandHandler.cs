// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Handler for <see cref="CustomerUpdateStatusCommand"/>. Loads the customer, changes status, persists and returns updated DTO.
/// </summary>
[HandlerRetry(2, 100)]
[HandlerTimeout(500)]
public class CustomerUpdateStatusCommandHandler(
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerUpdateStatusCommand, CustomerModel>
{
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerUpdateStatusCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            // Load existing entity
            await repository.FindOneResultAsync(CustomerId.Create(request.CustomerId), cancellationToken: cancellationToken)

            // Change status (idempotent if same)
            .Tap(e => e.ChangeStatus(request.Status))
            // Persist
            .BindAsync(async (customer, ct) =>
                await repository.UpdateResultAsync(customer, ct), cancellationToken)
            // Audit
            .Tap(_ => Console.WriteLine("AUDIT"))
            // Map
            .Map(mapper.Map<Customer, CustomerModel>);
}
