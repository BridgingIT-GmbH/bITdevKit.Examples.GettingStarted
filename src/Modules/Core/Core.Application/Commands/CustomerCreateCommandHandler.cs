// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Application.Commands;

using System.Threading;
using System.Threading.Tasks;
using BridgingIT.DevKit.Application.Commands;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.Core.Domain.Model;
using Microsoft.Extensions.Logging;

public class CustomerCreateCommandHandler
    : CommandHandlerBase<CustomerCreateCommand, Result<Customer>>
{
    private readonly IGenericRepository<Customer> repository;

    public CustomerCreateCommandHandler(
        ILoggerFactory loggerFactory,
        IGenericRepository<Customer> repository)
        : base(loggerFactory) => this.repository = repository;

    public override async Task<CommandResponse<Result<Customer>>> Process(
        CustomerCreateCommand command, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(command.FirstName, command.LastName, command.Email);
        var result = await this.repository.InsertResultAsync(customer, cancellationToken).AnyContext();

        return CommandResponse.For(result);
    }
}