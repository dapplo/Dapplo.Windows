// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Tests.Benchmarks;

[MinColumn, MaxColumn, MemoryDiagnoser]
public class EnumerateWindowsBenchmark
{
    [Benchmark, STAThread]
    public void EnumerateAll()
    {
        var result = WindowsEnumerator.EnumerateWindows().All(window => window != null);
        if (!result)
        {
            throw new ArgumentNullException();
        }
    }

    [Benchmark, STAThread]
    public async Task EnumerateAll_Async()
    {
        var result = await WindowsEnumerator.EnumerateWindowsAsync().All(window => window != null).ToTask();
        if (!result)
        {
            throw new ArgumentNullException();
        }
    }

    [Benchmark, STAThread]
    public async Task Enumerate_Take10_Async()
    {
        var result = await WindowsEnumerator.EnumerateWindowsAsync().Take(10).Count(window => window != null);
        if (result != 10)
        {
            throw new Exception($"Expected 10, actual {result}");
        }
    }

    [Benchmark, STAThread]
    public void Enumerate_Take10()
    {
        var result = WindowsEnumerator.EnumerateWindows().Take(10).Count(window => window != null);
        if (result != 10)
        {
            throw new Exception($"Expected 10, actual {result}");
        }
    }

    [Benchmark, STAThread]
    public void Enumerate_LimitFunc()
    {
        var result = WindowsEnumerator.EnumerateWindows(null, null, (window, i) => i < 10).Count(window => window != null);
        if (result != 10)
        {
            throw new Exception($"Expected 10, actual {result}");
        }
    }

    [Benchmark, STAThread]
    public async Task EnumerateHandlesAll_Select_Async()
    {
        var result = await WindowsEnumerator.EnumerateWindowHandlesAsync().Select(InteropWindowFactory.CreateFor).All(window => window != null).ToTask();
        if (!result)
        {
            throw new ArgumentNullException();
        }
    }

    [Benchmark, STAThread]
    public async Task EnumerateHandles_HandlesOnly_Async()
    {
        var result = await WindowsEnumerator.EnumerateWindowHandlesAsync().All(window => window != IntPtr.Zero).ToTask();
        if (!result)
        {
            throw new ArgumentNullException();
        }
    }
}