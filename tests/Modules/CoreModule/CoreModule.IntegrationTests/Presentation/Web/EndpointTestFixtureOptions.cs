// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.IntegrationTests.Presentation.Web;

/// <summary>
/// Options for configuring authentication acquisition for endpoint integration tests.
/// </summary>
public class EndpointTestFixtureOptions
{
    public string TokenEndpoint { get; set; } = "/api/_system/identity/connect/token";

    public string ClientId { get; set; } = "test-client";

    public string Username { get; set; } = "clever.dragon@example.com";

    public string Password { get; set; } = "fantasy";

    public string Scope { get; set; } = "openid profile email roles";
}