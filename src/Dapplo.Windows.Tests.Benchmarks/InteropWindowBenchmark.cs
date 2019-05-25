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
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Enums;

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

        #region Implementation of IDisposable
        [GlobalCleanup]
        public void Dispose()
        {
            // Kill the process
            _notepadProcess.Kill();
        }

        #endregion
    }
}
