// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Presentation;

using BridgingIT.DevKit.Examples.GettingStarted.Domain.Model;
using Mapster;

public class MapperRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<Customer, CustomerModel>()
            .Map(d => d.Email, s => s.Email.Value);
    }
}