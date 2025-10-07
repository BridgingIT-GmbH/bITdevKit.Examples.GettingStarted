// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;

public partial class CoreModuleCustomerEndpoints
{
    /// <summary>
    /// Request body for changing customer status.
    /// </summary>
    /// <param name="StatusId">Enumeration Id of target status.</param>
    private sealed record CustomerUpdateStatusRequestModel(int StatusId);
}