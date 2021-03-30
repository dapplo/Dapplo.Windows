// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Dapplo.Windows.Tests.Benchmarks
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {

            var jobCore50 = Job.Default
                .WithMaxIterationCount(20)
                .WithRuntime(CoreRuntime.Core50)
                .WithPlatform(Platform.X64);
            var jobCore31 = Job.Default
                .WithMaxIterationCount(20)
                .WithRuntime(CoreRuntime.Core31)
                .WithPlatform(Platform.X64);
            var jobNet472 = Job.Default
                .WithMaxIterationCount(20)
                .WithRuntime(ClrRuntime.Net472)
                .WithPlatform(Platform.X64);
            var config = DefaultConfig.Instance
                    .AddJob(jobCore50)
                    .AddJob(jobCore31)
                    .AddJob(jobNet472)
                ;

            BenchmarkRunner.Run<ScreenboundsBenchmark>(config);
            BenchmarkRunner.Run<ClipboardBenchmarks>(config);
            BenchmarkRunner.Run<EnumerateWindowsBenchmark>(config);
            BenchmarkRunner.Run<InteropWindowBenchmark>(config);
            Console.ReadLine();
        }
    }
}
