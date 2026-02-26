// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.ArchitectureTests;

using NetArchTest.Rules;

public class ArchitectureFixture
{
    public Types Types { get; } = Types.FromPath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

    public string BaseNamespace { get; } = "BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule";

    /// <summary>
    /// List of other modules in the solution that this module should NOT directly reference.
    /// Add new module names here as they are created (without layer suffixes like .Domain, .Application).
    /// Module boundary tests will derive specific layer namespaces from these base module names.
    /// </summary>
    public string[] ForbiddenModules { get; } =
    [
        "BridgingIT.DevKit.Examples.GettingStarted.Modules.OtherModule1",
        "BridgingIT.DevKit.Examples.GettingStarted.Modules.OtherModule2"
    ];
}
