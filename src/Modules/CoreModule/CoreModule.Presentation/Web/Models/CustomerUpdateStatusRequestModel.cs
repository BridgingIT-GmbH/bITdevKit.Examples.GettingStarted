// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Presentation.Web;

/// <summary>
/// Request body for changing customer status.
/// </summary>
public class CustomerUpdateStatusRequestModel
{
    private CustomerUpdateStatusRequestModel()
    {
    }

    public CustomerUpdateStatusRequestModel(int status)
    {
        this.Status = status;
    }

    /// <summary>
    /// Gets or sets the new status value for the customer.
    /// Valid values: 1 = Lead, 2 = Active, 3 = Retired. See CustomerStatus enumeration for details.
    /// </summary>
    /// <example>2</example>
    public int Status { get; set; }
}