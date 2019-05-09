//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Tests.Benchmarks
{
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
}
