// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

internal class Program
{
    static void Main(string[] args)
    {
        var artifactsPath = Path.Combine(AppContext.BaseDirectory, "BenchmarkDotNet.Artifacts");
        var config = DefaultConfig.Instance.WithArtifactsPath(artifactsPath);
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly, config);
    }
}
