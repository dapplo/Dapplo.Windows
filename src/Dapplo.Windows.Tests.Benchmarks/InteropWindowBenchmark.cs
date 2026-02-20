// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Desktop;
using System;
using System.Diagnostics;

namespace Dapplo.Windows.Tests.Benchmarks;

[MinColumn, MaxColumn, MemoryDiagnoser]
public class InteropWindowBenchmark : IDisposable
{
    private Process _processForTest;

    [GlobalSetup]
    public void Setup()
    {
        // Start a process to test against
        _processForTest = Process.Start("charmap.exe");
        if (_processForTest == null)
        {
            throw new NotSupportedException("Couldn't start charmap.exe");
        }
        // Make sure it's started
        // Wait until the process started it's message pump (listening for input)
        if (!_processForTest.WaitForInputIdle(2000))
        {
            throw new NotSupportedException("Process not ready");
        }

    }

    [Benchmark, STAThread]
    public void Fill()
    {
        var interopWindow = InteropWindowFactory.CreateFor(_processForTest.MainWindowHandle);
        if (interopWindow.Handle == IntPtr.Zero)
        {
            throw new NotSupportedException("Somehow the window was not found!"); 
        }
        interopWindow.Fill();
    }

    [GlobalCleanup]
    public void Dispose()
    {
        // Kill the process
        _processForTest.Kill();
    }
}