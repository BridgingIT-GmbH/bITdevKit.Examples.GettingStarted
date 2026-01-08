// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System.Collections.Generic;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for processing <see cref="CustomerFindAllQuery"/>.
/// Responsible for loading all customers from the repository with optional filters,
/// auditing/logging the operation, and mapping results from domain entities
/// (<see cref="Customer"/>) to application DTOs (<see cref="CustomerModel"/>).
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) for transient failures.
/// - Configured with timeout (<see cref="HandlerTimeoutAttribute"/>) to bound execution.
/// - Uses <see cref="IGenericRepository{Customer}"/> for persistence access.
/// - Uses <see cref="IMapper"/> (Mapster abstraction) for domain → DTO transformations.
/// </remarks>
// [HandlerRetry(2, 100)]   // retry twice with 100ms delay between retries
// [HandlerTimeout(500)]    // enforce max 500ms execution per request
public class CustomerFindAllQueryHandler(
    ILogger<CustomerFindAllQueryHandler> logger,
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerFindAllQuery, IEnumerable<CustomerModel>>()
{
    /// <summary>
    /// Handles the <see cref="CustomerFindAllQuery"/> asynchronously.
    /// </summary>
    /// <param name="request">The incoming query containing filter settings (can be null).</param>
    /// <param name="options">Request send options (retry handling, pipeline context).</param>
    /// <param name="cancellationToken">Cancellation token for async workflow.</param>
    /// <returns>
    /// A Result containing an enumerable of model instances representing all aggregates found, or a failure result in case of an error.
    /// </returns>
    protected override async Task<Result<IEnumerable<CustomerModel>>> HandleAsync(
        CustomerFindAllQuery request,
        SendOptions options,
        CancellationToken cancellationToken)
    {
        // Load all matching customers from repository
        return await repository.FindAllResultAsync(request.Filter, cancellationToken: cancellationToken) // TODO: use paging -> FindAllResultPagedAsync

            // Side effects (audit/logging)
            .Log(logger, "AUDIT - Customers retrieved (count: {Count}, filter: {@Filter})", r => [r.Value.Count(), request.Filter])

            // Map retrieved Aggregates → Models
            .Map(mapper.Map<Customer, CustomerModel>);
    }
}