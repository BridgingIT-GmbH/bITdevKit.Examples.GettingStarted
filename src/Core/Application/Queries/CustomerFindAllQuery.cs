﻿// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Core.Application;

using System.Collections.Generic;
using BridgingIT.DevKit.Application.Queries;
using BridgingIT.DevKit.Examples.GettingStarted.Core.Domain.Model;

public class CustomerFindAllQuery
    : QueryRequestBase<IEnumerable<Customer>>
{
}