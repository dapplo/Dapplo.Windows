// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Dapplo.Windows.Tests.Benchmarks;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {

        // The runtimes must match the TargetFrameworks of this project (net480;net10.0-windows), BenchmarkDotNet builds the benchmarks for each of them
        var jobNet10 = Job.Default
            .WithMaxIterationCount(20)
            .WithRuntime(CoreRuntime.CreateForNewVersion("net10.0-windows", ".NET 10.0"))
            .WithPlatform(Platform.X64);
        var jobNet48 = Job.Default
            .WithMaxIterationCount(20)
            .WithRuntime(ClrRuntime.Net48)
            .WithPlatform(Platform.X64);
        var config = DefaultConfig.Instance
                .AddJob(jobNet10)
                .AddJob(jobNet48)
            ;

        BenchmarkRunner.Run<ScreenboundsBenchmark>(config);
        BenchmarkRunner.Run<ClipboardBenchmarks>(config);
        BenchmarkRunner.Run<EnumerateWindowsBenchmark>(config);
        BenchmarkRunner.Run<InteropWindowBenchmark>(config);
        Console.ReadLine();
    }
}