// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handler for <see cref="CustomerCreateCommand"/> that performs business validation,
/// enforces rules, persists a new <see cref="Customer"/> entity, logs steps, and maps back to DTO.
/// </summary>
//[HandlerRetry(2, 100)]
//[HandlerTimeout(500)]
public class CustomerCreateCommandHandler(
    ILogger<CustomerCreateCommandHandler> logger,
    IMapper mapper,
    IGenericRepository<Customer> repository,
    ISequenceNumberGenerator numberGenerator,
    TimeProvider timeProvider)
    : RequestHandlerBase<CustomerCreateCommand, CustomerModel>
{
    /// <summary>
    /// Handles the <see cref="CustomerCreateCommand"/>. Steps:
    /// 1. Map DTO to <see cref="Customer"/> aggregate.
    /// 2. Validate inline rules (basic invariants, e.g., names not empty).
    /// 3. Persist changes via repository update.
    /// 4. Perform audit/logging side-effects.
    /// 5. Map created domain aggregate to <see cref="CustomerModel"/>.
    /// </summary>
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerCreateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
            await Result<CustomerModel>
                // STEP 1 — Create initial context
                .Bind<CustomerCreateContext>(() => new(request.Model))
                .Log(logger, "Context created {@Context}", r => [r.Value])
                .Ensure((ctx) => ctx.Model.FirstName != ctx.Model.LastName,
                    new ValidationError("Firstname cannot be same as lastname", "Firstname"))

                // STEP 2 — Validate model
                .UnlessAsync(async (ctx, ct) => await Rule
                    .Add(RuleSet.IsNotEmpty(ctx.Model.FirstName))  // name required
                    .Add(RuleSet.IsNotEmpty(ctx.Model.LastName))   // name required
                    .Add(RuleSet.NotEqual(ctx.Model.LastName, "notallowed")) // reject forbidden values
                    .Add(new EmailShouldBeUniqueRule(ctx.Model.Email, repository)) // email must be unique
                    .CheckAsync(cancellationToken), cancellationToken: cancellationToken)

                // STEP 3 — Generate sequence number
                .BindResultAsync(this.GenerateSequenceAsync, this.CaptureNumber, cancellationToken)
                //.BindAsync(async (ctx, ct) => await numberGenerator.GetNextAsync(CodeModuleConstants.CustomerNumberSequenceName, "core", ct))

                .Log(logger, "Customer number created{@Number}", r => [r.Value.Number])

                // STEP 4 — Create aggregate
                .Bind(this.CreateEntity)

                // STEP 6 — Save aggregate to repository
                .BindResultAsync(this.PersistEntityAsync, this.CapturePersistedEntity, cancellationToken)

                // STEP 7 — Side effects (audit/logging)
                .Log(logger, "AUDIT - Customer {Id} created for {Email}", r => [r.Value.Entity.Id, r.Value.Entity.Email.Value])

                // STEP 8 — Map domain → DTO
                .Map(this.ToModel)
                .Log(logger, "Mapped to {@Model}", r => [r.Value]);

    private async Task<Result<long>> GenerateSequenceAsync(CustomerCreateContext ctx, CancellationToken ct)
    {
        var r = await numberGenerator.GetNextAsync(CodeModuleConstants.CustomerNumberSequenceName, "core", ct); //.Value;
        return r;
    }

    private CustomerCreateContext CaptureNumber(CustomerCreateContext ctx, long seq)
    {
        ctx.Number = CustomerNumber.Create(timeProvider.GetUtcNow().UtcDateTime, seq);
        return ctx;
    }

    private CustomerCreateContext CreateEntity(CustomerCreateContext ctx)
    {
        ctx.Entity = Customer.Create(
            ctx.Model.FirstName,
            ctx.Model.LastName,
            ctx.Model.Email,
            ctx.Number);
        return ctx;
    }

    private async Task<Result<Customer>> PersistEntityAsync(CustomerCreateContext ctx, CancellationToken ct)
    {
        return await repository.InsertResultAsync(ctx.Entity, ct).AnyContext();
    }

    private CustomerCreateContext CapturePersistedEntity(CustomerCreateContext ctx, Customer entity)
    {
        ctx.Entity = entity;
        return ctx;
    }

    private CustomerModel ToModel(CustomerCreateContext ctx)
    {
        return mapper.Map<Customer, CustomerModel>(ctx.Entity);
    }

    private class CustomerCreateContext(CustomerModel model)
    {
        public CustomerModel Model { get; init; } = model;

        public CustomerNumber Number { get; set; }

        public Customer Entity { get; set; }
    }
}