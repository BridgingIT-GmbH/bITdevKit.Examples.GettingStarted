// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Domain.Repositories;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using FluentValidation;

/// <summary>
/// Command to create a new <see cref="Customer"/> aggregate.
/// Implements <see cref="RequestBase{TResponse}"/> to request a <see cref="CustomerModel"/> result.
/// Validates required input (id must be empty, first/last names, email must be provided).
/// </summary>
public class CustomerCreateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    /// <summary>
    /// Gets or sets the DTO (<see cref="CustomerModel"/>) that contains data for the new customer.
    /// Must not be null. The Id must be default/empty since it will be generated.
    /// </summary>
    public CustomerModel Model { get; set; } = model;

    /// <summary>
    /// Validation rules for <see cref="CustomerCreateCommand"/> using FluentValidation.
    /// </summary>
    public class Validator : AbstractValidator<CustomerCreateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();
            this.RuleFor(c => c.Model.Id).MustBeDefaultOrEmptyGuid();

            this.RuleFor(c => c.Model.FirstName)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.LastName)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");

            this.RuleFor(c => c.Model.Email)
                .NotNull().NotEmpty()
                .WithMessage("Must not be empty.");
        }
    }
}

/// <summary>
/// Handler for <see cref="CustomerCreateCommand"/> that performs business validation,
/// enforces rules, persists a new <see cref="Customer"/> entity and maps back to DTO.
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) and timeout (<see cref="HandlerTimeoutAttribute"/>).
/// - Enforces <see cref="EmailShouldBeUniqueRule"/> and other inline domain rules.
/// - Persists via <see cref="IGenericRepository{T}"/>.
/// - Returns the created <see cref="CustomerModel"/>.
/// </remarks>
[HandlerRetry(2, 100)]   // retry 2 times on transient failures, wait 100ms between attempts
[HandlerTimeout(500)]    // timeout of 500ms enforced for handling
public class CustomerCreateCommandHandler(
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerCreateCommand, CustomerModel>
{
    /// <summary>
    /// Handles the <see cref="CustomerCreateCommand"/> asynchronously.
    /// 1. Maps the DTO to a <see cref="Customer"/> aggregate.
    /// 2. Validates domain invariants and rules.
    /// 3. Inserts the entity into the repository if valid.
    /// 4. Maps entity back to <see cref="CustomerModel"/> for response.
    /// </summary>
    /// <param name="request">The incoming create command instance.</param>
    /// <param name="options">Send options provided by the pipeline (e.g. retries).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="Result{CustomerModel}"/> indicating success or failure.</returns>
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerCreateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
        await Result.Success()

            // Map DTO -> domain aggregate
            .Map(mapper.Map<CustomerModel, Customer>(request.Model))

            // Validate domain rules (fail fast if rules broken)
            .UnlessAsync(async (customer, ct) => await Rule
                .Add(RuleSet.IsNotEmpty(customer.FirstName))  // name required
                .Add(RuleSet.IsNotEmpty(customer.LastName))   // name required
                .Add(RuleSet.NotEqual(customer.LastName, "notallowed")) // reject forbidden values
                .Add(new EmailShouldBeUniqueRule(customer.Email, repository)) // email must be unique
                .CheckAsync(cancellationToken),
                cancellationToken: cancellationToken)

            // Persist
            .BindAsync(async (customer, ct) =>
                await repository.InsertResultAsync(customer, cancellationToken),
                cancellationToken: cancellationToken)

            // Side-effects (Auditing, logging, etc.)
            .Tap(_ => Console.WriteLine("AUDIT"))

            // Map persisted entity -> DTO
            .Map(mapper.Map<Customer, CustomerModel>);
}