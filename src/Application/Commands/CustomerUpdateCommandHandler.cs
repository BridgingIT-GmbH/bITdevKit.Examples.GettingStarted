// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Application;

using BridgingIT.DevKit.Application.Commands;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CustomerUpdateCommandHandler
    : CommandHandlerBase<CustomerUpdateCommand, Result<Customer>>
{
    private readonly IGenericRepository<Customer> repository;

    public CustomerUpdateCommandHandler(
        ILoggerFactory loggerFactory,
        IGenericRepository<Customer> repository)
        : base(loggerFactory)
    {
        this.repository = repository;
    }

    public override async Task<CommandResponse<Result<Customer>>> Process(
        CustomerUpdateCommand command, CancellationToken cancellationToken)
    {
        var customerResult = await this.repository.FindOneResultAsync(
            CustomerId.Create(command.Id), cancellationToken: cancellationToken);
        var customer = customerResult.Value;
        if (customerResult.IsFailure)
        {
            return CommandResponse.For(customerResult);
        }

        Check.Throw(new IBusinessRule[] { /*no rules yet*/ });

        customer.ChangeName(command.FirstName, command.LastName);

        await this.repository.UpsertAsync(customer, cancellationToken).AnyContext();

        return CommandResponse.Success(customer);
    }
}