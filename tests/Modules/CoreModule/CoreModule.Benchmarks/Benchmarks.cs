// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace BridgingIT.DevKit.Examples.GettingStarted.Modules.CoreModule.Benchmarks;

using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
//[CPUUsageDiagnoser]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class Benchmarks
{
    private SHA256 sha256 = SHA256.Create();
    private byte[] data;

    /// <summary>
    /// Setup method to initialize data before running benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        this.data = new byte[10000];
        new Random(42).NextBytes(this.data);
    }

    /// <summary>
    ///  Benchmark method that computes the SHA256 hash of a byte array.
    /// </summary>
    [Benchmark]
    public byte[] Sha256()
    {
        return this.sha256.ComputeHash(this.data);
    }
}
