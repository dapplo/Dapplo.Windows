// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Tests.Benchmarks
{
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class InteropWindowBenchmark : IDisposable
    {
        private Process _notepadProcess;

        [GlobalSetup]
        public void Setup()
        {
            // Start a process to test against
            _notepadProcess = Process.Start("notepad.exe");
            if (_notepadProcess == null)
            {
                throw new NotSupportedException("Couldn't start notepad.exe");
            }
            // Make sure it's started
            // Wait until the process started it's message pump (listening for input)
            _notepadProcess.WaitForInputIdle();
        }

        [Benchmark, STAThread]
        public void Fill()
        {
            var interopWindow = InteropWindowFactory.CreateFor(_notepadProcess.MainWindowHandle);
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
            _notepadProcess.Kill();
        }
    }
}
