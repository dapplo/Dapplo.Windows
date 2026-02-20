// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Messages;
using System.Reactive;

namespace Dapplo.Windows.Tests.Benchmarks;

/// <summary>
/// Benchmarks for accessing the clipboard
/// </summary>
[MinColumn, MaxColumn, MemoryDiagnoser]
public class ClipboardBenchmarks : IDisposable
{
    private IDisposable observable;

    public ClipboardBenchmarks()
    {
        observable = SharedMessageWindow.Listen().Subscribe();
    }

    public void Dispose()
    {
        observable?.Dispose();
    }

    [Benchmark, STAThread]
    public async Task LockClipboard()
    {
        using (await ClipboardNative.AccessAsync())
        {
        }
    }
}