// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation;

using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Domain.Model;
using BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Models;
using Mapster;

public class MapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<Customer, CustomerModel>()
            .Map(d => d.Email, s => s.Email.Value);
    }
}