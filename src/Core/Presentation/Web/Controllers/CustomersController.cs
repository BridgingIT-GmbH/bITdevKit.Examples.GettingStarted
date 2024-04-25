// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Ddd.Examples.GettingStarted.Presentation.Web.Controllers;

using BridgingIT.DevKit.Examples.GettingStarted.Core.Application;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Presentation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator mediator;

    public CustomersController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerModel>>> GetAsync()
    {
        var query = new CustomerFindAllQuery();
        var result = await this.mediator.Send(query);

        return this.Ok(result?.Result?.Select(e =>
            new CustomerModel
            {
                Id = e.Id.ToString(),
                FirstName = e.FirstName,
                LastName = e.LastName
            }));
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync([FromBody] CustomerModel model)
    {
        if (model is null)
        {
            return this.BadRequest();
        }

        var command = new CustomerCreateCommand()
        {
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await this.mediator.Send(command);

        return this.Created($"/api/customers/{result.Result.Id}", null);
    }
}