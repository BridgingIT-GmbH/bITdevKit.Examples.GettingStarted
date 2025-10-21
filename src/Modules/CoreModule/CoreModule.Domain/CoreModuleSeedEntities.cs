// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain;

using System.Diagnostics.CodeAnalysis;
using BridgingIT.DevKit.Common;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;

/// <summary>
/// The CoreSeedEntities class provides methods to create and manage seed data for core domain entities such as Customers in the Domain.
/// </summary>
[ExcludeFromCodeCoverage]
public static class CoreModuleSeedEntities
{
    /// <summary>
    /// Creates an array of pre-defined Customer objects with their associated informations.
    /// </summary>
    public static Customer[] CreateCustomer() => [
        ..new[]
            {
                Customer.Create("John", "Doe", "john.doe@example.com", CustomerNumber.Create(DateTime.Now, 100000)),
                Customer.Create("Mary", "Jane", "mary.jane@example.com", CustomerNumber.Create(DateTime.Now, 100001))
         }.ForEach(e => e.Id = GuidGenerator.Create($"Customer_{e.Email.Value}"))];
}