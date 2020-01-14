// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Tests.Benchmarks
{
    /// <summary>
    /// Benchmarks for accessing the clipboard
    /// </summary>
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class ClipboardBenchmarks
    {
        private readonly IntPtr _handle = WinProcHandler.Instance.Handle;
        [Benchmark, STAThread]
        public async Task LockClipboard()
        {
            using (await ClipboardNative.AccessAsync(_handle))
            {
            }
        }
    }
}
