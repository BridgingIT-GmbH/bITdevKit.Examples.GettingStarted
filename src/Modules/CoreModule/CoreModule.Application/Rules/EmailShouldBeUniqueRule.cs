// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Application;

using System.Threading;
using System.Threading.Tasks;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// Business rule that enforces unique customer email addresses across the system.
/// This ensures that no two <see cref="Customer"/> aggregates share the same email.
/// Implements <see cref="AsyncRuleBase"/> so it can be checked asynchronously
/// against the persistence store.
/// </summary>
/// <param name="email">The email address to check for uniqueness.</param>
/// <param name="repository">A generic repository for querying <see cref="Customer"/> entities.</param>
public class EmailShouldBeUniqueRule(string email, IGenericRepository<Customer> repository) : AsyncRuleBase
{
    /// <summary>
    /// The validation/error message returned when this rule fails.
    /// </summary>
    public override string Message { get; } = "Customer Email should not be used already";

    /// <summary>
    /// Executes the business rule asynchronously.
    /// Checks whether the provided email exists in the <see cref="Customer"/> repository.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel query execution.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success (if email is unique) or failure (if the email is already in use).
    /// </returns>
    public override async Task<Result> ExecuteAsync(CancellationToken cancellationToken)
    {
        // Query number of customers with the given email
        return await repository
            .CountResultAsync(e => e.Email == email, cancellationToken)
            .Ensure(e => e == 0, new ValidationError(this.Message));   // fail if count > 0
    }
}