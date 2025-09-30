// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Handler for processing <see cref="CustomerFindOneQuery"/>.
/// Loads a single customer from the repository by ID, audits/logs the operation,
/// and maps the domain entity (<see cref="Customer"/>) to a DTO (<see cref="CustomerModel"/>).
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) for transient failures.
/// - Configured with timeout (<see cref="HandlerTimeoutAttribute"/>) to bound maximum execution time.
/// - Uses <see cref="IGenericRepository{Customer}"/> and domain-specific <see cref="CustomerId"/> value object
///   to perform the lookup.
/// </remarks>
[HandlerRetry(2, 100)]   // retry twice, wait 100ms between retries
[HandlerTimeout(500)]    // timeout after 500ms execution
public class CustomerFindOneQueryHandler(
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerFindOneQuery, CustomerModel>
{
    /// <summary>
    /// Handles the <see cref="CustomerFindOneQuery"/> request. Steps:
    /// 1. Convert the incoming ID string into a <see cref="CustomerId"/> value object.
    /// 2. Query repository for that customer.
    /// 3. Perform audit/logging side-effect.
    /// 4. Map the domain <see cref="Customer"/> into a <see cref="CustomerModel"/> DTO for return.
    /// </summary>
    /// <param name="request">The incoming query containing the Customer ID.</param>
    /// <param name="options">Pipeline send options (retries, context).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{CustomerModel}"/> containing the mapped customer
    /// if found, or an error result if the entity does not exist.
    /// </returns>
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerFindOneQuery request,
        SendOptions options,
        CancellationToken cancellationToken) =>
        await repository.FindOneResultAsync(CustomerId.Create(request.CustomerId), cancellationToken: cancellationToken)

            // Side-effect: audit/logging/telemetry etc.
            .Tap(_ => Console.WriteLine("AUDIT"))

            // Map domain entity -> DTO
            .Map(mapper.Map<Customer, CustomerModel>);
}