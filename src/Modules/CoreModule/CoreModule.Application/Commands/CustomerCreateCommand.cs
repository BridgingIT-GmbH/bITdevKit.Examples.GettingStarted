// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Command to create a new <see cref="Customer"/> Aggregate.
/// </summary>
/// <summary>
/// Handler for <see cref="CustomerCreateCommand"/> that performs business validation,
/// enforces rules, persists a new <see cref="Customer"/> entity, logs steps, and maps back to DTO.
/// </summary>
//[HandlerRetry(2, 100)]
//[HandlerTimeout(500)]
//[HandlerDatabaseTransactionAttribute<CoreDbContext>] TODO: not possible due to CoreDbContext defined in Infrastructure
[Command]
public partial class CustomerCreateCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerCreateCommand"/> class.
    /// </summary>
    /// <param name="model">The model that contains data for the Aggregate to create.</param>
    public CustomerCreateCommand(CustomerModel model)
    {
        Model = model;
    }

    /// <summary>Gets or sets the Model (<see cref="CustomerModel"/>) that contains data for the Aggregate to create.</summary>
    public CustomerModel Model { get; set; }

    /// <summary>Validation rules for <see cref="CustomerCreateCommand"/> using FluentValidation.</summary>
    [Validate]
    private static void Validate(InlineValidator<CustomerCreateCommand> validator)
    {
        validator.RuleFor(c => c.Model).NotNull();
        validator.When(c => c.Model != null, () =>
        {
            validator.RuleFor(c => c.Model.Id).MustBeDefaultOrEmptyGuid()
                .WithMessage(Resources.Validator_InvalidValue);
            validator.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);
            validator.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);
            validator.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty);
            validator.RuleFor(c => c.Model.Addresses) // Address validation rules
                .Must(addresses => addresses.IsNullOrEmpty() || addresses.Count(a => a.IsPrimary) == 1)
                .WithMessage(Resources.Validator_OnePrimaryAddressRequired);
            validator.RuleForEach(c => c.Model.Addresses).ChildRules(address =>
            {
                address.RuleFor(a => a.Line1)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(256).WithMessage(Resources.Validator_MustNotExceed256Characters);
                address.RuleFor(a => a.Line2)
                    .MaximumLength(256).WithMessage(Resources.Validator_MustNotExceed256Characters);
                address.RuleFor(a => a.City)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(100).WithMessage(Resources.Validator_MustNotExceed100Characters);
                address.RuleFor(a => a.Country)
                    .NotEmpty().WithMessage(Resources.Validator_MustNotBeEmpty)
                    .MaximumLength(100).WithMessage(Resources.Validator_MustNotExceed100Characters);
                address.RuleFor(a => a.Name)
                    .MaximumLength(128).WithMessage(Resources.Validator_MustNotExceed128Characters);
                address.RuleFor(a => a.PostalCode)
                    .MaximumLength(20).WithMessage(Resources.Validator_MustNotExceed20Characters);
            });
        });
    }

    /// <summary>
    /// Handles the <see cref="CustomerCreateCommand"/>. Steps:
    /// 1. Map DTO to <see cref="Customer"/> aggregate.
    /// 2. Validate inline rules (basic invariants, e.g., names not empty).
    /// 3. Persist changes via repository update.
    /// 4. Perform audit/logging side-effects.
    /// 5. Map created domain aggregate to <see cref="CustomerModel"/>.
    /// </summary>
    [Handle]
    private async Task<Result<CustomerModel>> HandleAsync(
        ILogger<CustomerCreateCommand> logger,
        IMapper mapper,
        IGenericRepository<Customer> repository,
        ISequenceNumberGenerator numberGenerator,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
            await Result<CustomerModel>
                // STEP 1 — Create initial context
                .Bind<CustomerCreateContext>(() => new(Model))
                .Log(logger, "Context created {@Context}", r => [r.Value])

                // STEP 2 — Validate model
                .UnlessAsync(async (ctx, ct) => await Rule
                    .Add(RuleSet.IsNotEmpty(ctx.Model.FirstName))  // name required
                    .Add(RuleSet.IsNotEmpty(ctx.Model.LastName))   // name required
                    .Add(RuleSet.NotEqual(ctx.Model.LastName, "notallowed")) // reject forbidden values
                    .Add(new EmailShouldBeUniqueRule(ctx.Model.Email, repository)) // email must be unique
                    .CheckAsync(cancellationToken), cancellationToken: cancellationToken)

                // STEP 3 — Generate sequence number
                .BindResultAsync((ctx, ct) => GenerateSequenceAsync(ctx, numberGenerator, ct), CaptureNumber(timeProvider), cancellationToken)
                //.BindAsync(async (ctx, ct) => await numberGenerator.GetNextAsync(CodeModuleConstants.CustomerNumberSequenceName, "core", ct))

                .Log(logger, "Customer number created{@Number}", r => [r.Value.Number])

                // STEP 4 — Create new Aggregate from request model
                .Bind(CreateAggregate)

                // STEP 6 — Save new Aggregate to repository
                .BindResultAsync((ctx, ct) => PersistEntityAsync(ctx, repository, ct), CapturePersistedEntity, cancellationToken)

                // STEP 7 — Side effects (audit/logging)
                .Log(logger, "AUDIT - Customer {Id} created for {Email}", r => [r.Value.Entity.Id, r.Value.Entity.Email.Value])

                // STEP 8 — Map created Aggregate -> Model
                .Map(ctx => ctx.Entity)
                .MapResult<Customer, CustomerModel>(mapper) //.Map(this.ToModel)
                .Log(logger, "Aggregate mapped to {@Model}", r => [r.Value]);

    private static async Task<Result<long>> GenerateSequenceAsync(CustomerCreateContext ctx, ISequenceNumberGenerator numberGenerator, CancellationToken ct)
    {
        var r = await numberGenerator.GetNextAsync(CodeModuleConstants.CustomerNumberSequenceName, "core", ct); //.Value;
        return r;
    }

    private static Func<CustomerCreateContext, long, CustomerCreateContext> CaptureNumber(TimeProvider timeProvider) =>
        (ctx, seq) =>
        {
            ctx.Number = CustomerNumber.Create(timeProvider.GetUtcNow().UtcDateTime, seq).Value;
            return ctx;
        };

    private static Result<CustomerCreateContext> CreateAggregate(CustomerCreateContext ctx)
    {
        return EmailAddress.Create(ctx.Model.Email)
            .Bind(email => Customer
                .Create(ctx.Model.FirstName, ctx.Model.LastName, email, ctx.Number)
                // Apply additional properties
                .When(ctx.Model.Status != null, r => r
                    .Bind(e => e.ChangeStatus(ctx.Model.Status)))
                .When(ctx.Model.DateOfBirth.HasValue, r => r
                    .Bind(e => e.ChangeBirthDate(ctx.Model.DateOfBirth.Value)))
                .When(ctx.Model.Addresses.SafeAny(), r => r
                    .Bind(e =>
                    {
                        foreach (var addressModel in ctx.Model.Addresses)
                        {
                            r.Bind(e => e.AddAddress(addressModel.Name, addressModel.Line1, addressModel.Line2, addressModel.PostalCode, addressModel.City, addressModel.Country));
                        }

                        return r;
                    })))
            .Map(customer =>
            {
                ctx.Entity = customer;
                return ctx;
            });
    }

    private static async Task<Result<Customer>> PersistEntityAsync(CustomerCreateContext ctx, IGenericRepository<Customer> repository, CancellationToken ct)
    {
        return await repository.InsertResultAsync(ctx.Entity, ct).AnyContext();
    }

    private static CustomerCreateContext CapturePersistedEntity(CustomerCreateContext ctx, Customer entity)
    {
        ctx.Entity = entity;
        return ctx;
    }

    private class CustomerCreateContext(CustomerModel model)
    {
        public CustomerModel Model { get; init; } = model;

        public CustomerNumber Number { get; set; }

        public Customer Entity { get; set; }
    }
}
