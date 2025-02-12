// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Application;

using BridgingIT.DevKit.Application.Queries;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using EnsureThat;
using Microsoft.Extensions.Logging;

public class CustomerFindOneQueryHandler : QueryHandlerBase<CustomerFindOneQuery, Result<Customer>>
{
    private readonly IGenericRepository<Customer> repository;

    public CustomerFindOneQueryHandler(ILoggerFactory loggerFactory, IGenericRepository<Customer> repository)
        : base(loggerFactory)
    {
        EnsureArg.IsNotNull(repository, nameof(repository));

        this.repository = repository;
    }

    public override async Task<QueryResponse<Result<Customer>>> Process(
        CustomerFindOneQuery query, CancellationToken cancellationToken)
    {
        return QueryResponse.For(
            await this.repository.FindOneResultAsync(
                Guid.Parse(query.CustomerId),
                cancellationToken: cancellationToken).AnyContext());
    }
}