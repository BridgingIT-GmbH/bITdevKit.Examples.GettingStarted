// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace TestOutput.Modules.TestCore.Domain;

using System.Diagnostics.CodeAnalysis;
using BridgingIT.DevKit.Common;
using TestOutput.Modules.TestCore.Domain.Model;

/// <summary>
/// The CoreSeedEntities class provides methods to create and manage seed data for core domain entities such as Customers in the Domain.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CoreSeedEntities
{
    /// <summary>
    /// Creates an array of pre-defined Customer objects with their associated informations.
    /// </summary>
    public static Customer[] CreateCustomer() => [
        ..new[]
            {
                Customer.Create("John", "Doe", "john.doe@example.com"),
                Customer.Create("Mary", "Jane", "mary.jane@example.com")
         }.ForEach(e => e.Id = GuidGenerator.Create($"Customer_{e.Email.Value}"))];
}