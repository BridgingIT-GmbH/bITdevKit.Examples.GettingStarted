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

    public CustomerUpdateStatusRequestModel(string status)
    {
        this.Status = status;
    }

    /// <summary>
    /// Gets or sets the new status value for the customer.
    /// Valid values: "Lead", "Active", "Retired". See CustomerStatus enumeration for details.
    /// </summary>
    /// <example>Active</example>
    public string Status { get; set; }
}