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
/// Command to update an existing <see cref="Customer"/> aggregate.
/// Contains a <see cref="CustomerModel"/> with updated properties.
/// Expects the provided Id to already exist.
/// </summary>
public class CustomerUpdateCommand(CustomerModel model) : RequestBase<CustomerModel>
{
    /// <summary>
    /// Gets or sets the incoming DTO (<see cref="CustomerModel"/>) representing the updated state of a customer.
    /// Must include a valid non-empty Id.
    /// </summary>
    public CustomerModel Model { get; set; } = model;

    /// <summary>
    /// Validation rules for <see cref="CustomerUpdateCommand"/>.
    /// </summary>
    public class Validator : AbstractValidator<CustomerUpdateCommand>
    {
        public Validator()
        {
            this.RuleFor(c => c.Model).NotNull();

            // Id must be set to a non-empty Guid (unlike CreateCommand where it's empty)
            this.RuleFor(c => c.Model.Id).MustNotBeDefaultOrEmptyGuid();

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
/// Handler for <see cref="CustomerUpdateCommand"/>.
/// Maps DTO → domain, checks business rules, updates the entity in the repository,
/// and maps back to <see cref="CustomerModel"/> for returning to the client.
/// </summary>
/// <remarks>
/// - Configured with retry (<see cref="HandlerRetryAttribute"/>) and timeout (<see cref="HandlerTimeoutAttribute"/>).
/// - Rule validation similar to <see cref="CustomerCreateCommandHandler"/>, but must consider
///   excluding the current customer when checking uniqueness (TODO noted).
/// </remarks>
[HandlerRetry(2, 100)]   // retry on transient failures
[HandlerTimeout(500)]    // max execution 500ms
public class CustomerUpdateCommandHandler(
    IMapper mapper,
    IGenericRepository<Customer> repository)
    : RequestHandlerBase<CustomerUpdateCommand, CustomerModel>
{
    /// <summary>
    /// Handles the <see cref="CustomerUpdateCommand"/>. Steps:
    /// 1. Map DTO to <see cref="Customer"/> aggregate.
    /// 2. Validate inline rules (basic invariants, e.g., names not empty).
    /// 3. Persist changes via repository update.
    /// 4. Perform audit/logging side-effects.
    /// 5. Map updated domain aggregate back to <see cref="CustomerModel"/>.
    /// </summary>
    protected override async Task<Result<CustomerModel>> HandleAsync(
        CustomerUpdateCommand request,
        SendOptions options,
        CancellationToken cancellationToken) =>
        await Result.Success()

            // Map from DTO -> domain entity
            .Map(mapper.Map<CustomerModel, Customer>(request.Model))

            // Run business rules
            .UnlessAsync(async (customer, ct) => await Rule
                .Add(RuleSet.IsNotEmpty(customer.FirstName))
                .Add(RuleSet.IsNotEmpty(customer.LastName))
                .Add(RuleSet.NotEqual(customer.LastName, "notallowed"))

                // TODO: Check unique email excluding the current entity (currently disabled)
                //.Add(new EmailShouldBeUniqueRule(customer.Email, repository))

                .CheckAsync(cancellationToken),
                cancellationToken: cancellationToken)

            // Update in repository
            .BindAsync(async (customer, ct) =>
                await repository.UpdateResultAsync(customer, ct),
                cancellationToken)

            // Side-effect (audit or logging)
            .Tap(_ => Console.WriteLine("AUDIT"))

            // Map domain entity -> DTO result
            .Map(mapper.Map<Customer, CustomerModel>);
}