// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Application;

using System.Collections.Generic;
using System.Threading.Tasks;
using BridgingIT.DevKit.Application.Queries;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using Microsoft.Extensions.Logging;

public class CustomerFindAllQueryHandler
    : QueryHandlerBase<CustomerFindAllQuery, IEnumerable<Customer>>
{
    private readonly IGenericRepository<Customer> repository;

    public CustomerFindAllQueryHandler(
        ILoggerFactory loggerFactory,
        IGenericRepository<Customer> repository)
        : base(loggerFactory)
    {
        this.repository = repository;
    }

    public override async Task<QueryResponse<IEnumerable<Customer>>> Process(
        CustomerFindAllQuery query,
        CancellationToken cancellationToken)
    {
        return new QueryResponse<IEnumerable<Customer>>
        {
            Result = await this.repository.FindAllAsync(cancellationToken: cancellationToken).AnyContext()
        };
    }
}