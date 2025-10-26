// MIT-License
// Copyright BridgingIT GmbH - All Rights Reserved
// Use of this source code is governed by an MIT-style license that can be
// found in the LICENSE file at https://github.com/bridgingit/bitdevkit/license

namespace CoreModule.Benchmarks;

using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Security.Cryptography;

// For more information on the VS BenchmarkDotNet Diagnosers see https://learn.microsoft.com/visualstudio/profiling/profiling-with-benchmark-dotnet
[CPUUsageDiagnoser]
public class Benchmarks
{
    private SHA256 sha256 = SHA256.Create();
    private byte[] data;

    [GlobalSetup]
    public void Setup()
    {
        data = new byte[10000];
        new Random(42).NextBytes(data);
    }

    [Benchmark]
    public byte[] Sha256()
    {
        return sha256.ComputeHash(data);
    }
}
