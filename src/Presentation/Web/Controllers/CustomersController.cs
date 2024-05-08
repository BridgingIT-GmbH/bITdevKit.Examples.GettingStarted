// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation.Web.Controllers;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Presentation;
using BridgingIT.DevKit.Presentation.Web;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(IMapper mapper, IMediator mediator) : ControllerBase
{
    private readonly IMediator mediator = mediator;
    private readonly IMapper mapper = mapper;

    [HttpGet("{id}", Name = nameof(Get))]
    public async Task<ActionResult<CustomerModel>> Get(string id, CancellationToken cancellationToken)
    {
        var result = (await this.mediator.Send(
            new CustomerFindOneQuery(id), cancellationToken)).Result;
        return result.ToOkActionResult<Customer, CustomerModel>(this.mapper);
    }

    [HttpGet]
    public async Task<ActionResult<ICollection<CustomerModel>>> GetAll(CancellationToken cancellationToken)
    {
        var result = (await this.mediator.Send(
            new CustomerFindAllQuery(), cancellationToken)).Result;
        return result.ToOkActionResult<Customer, CustomerModel>(this.mapper);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerModel>> PostAsync([FromBody] CustomerModel model, CancellationToken cancellationToken)
    {
        var result = (await this.mediator.Send(
            this.mapper.Map<CustomerModel, CustomerCreateCommand>(model), cancellationToken)).Result;
        return result.ToCreatedActionResult<Customer, CustomerModel>(this.mapper, nameof(this.Get), new { id = result.Value?.Id });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerModel>> PutAsync([FromBody] CustomerModel model, CancellationToken cancellationToken)
    {
        var result = (await this.mediator.Send(
            this.mapper.Map<CustomerModel, CustomerUpdateCommand>(model), cancellationToken)).Result;
        return result.ToUpdatedActionResult<Customer, CustomerModel>(this.mapper, nameof(this.Get), new { id = result.Value?.Id });
    }
}