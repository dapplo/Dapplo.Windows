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
using System.Windows.Forms;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Tests.Benchmarks
{
    [MinColumn, MaxColumn, MemoryDiagnoser]
    public class ScreenboundsBenchmark : IDisposable
    {
        [GlobalSetup]
        public void Setup()
        {
            var screenbounds = GetAllScreenBounds();
        }

        [Benchmark]
        public void ScreenBoundsNative()
        {
            var screenbounds = GetScreenBounds();
        }

        [Benchmark]
        public void ScreenBoundsPInvoke()
        {
            var screenbounds = GetAllScreenBounds();
        }

        [Benchmark]
        public void ScreenBoundsPInvokeCached()
        {
            var screenbounds = DisplayInfo.ScreenBounds;
        }

        /// <summary>
        ///     Get the bounds of the complete screen
        /// </summary>
        /// <returns></returns>
        public NativeRect GetAllScreenBounds()
        {
            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (var display in DisplayInfo.AllDisplayInfos)
            {
                var currentBounds = display.Bounds;
                left = Math.Min(left, currentBounds.X);
                top = Math.Min(top, currentBounds.Y);
                var screenAbsRight = currentBounds.X + currentBounds.Width;
                var screenAbsBottom = currentBounds.Y + currentBounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }
            return new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
        }

        /// <summary>
        ///     Get the bounds of all screens combined.
        /// </summary>
        /// <returns>A NativeRect of the bounds of the entire display area.</returns>
        public NativeRect GetScreenBounds()
        {
            int left = 0, top = 0, bottom = 0, right = 0;
            foreach (var screen in Screen.AllScreens)
            {
                left = Math.Min(left, screen.Bounds.X);
                top = Math.Min(top, screen.Bounds.Y);
                var screenAbsRight = screen.Bounds.X + screen.Bounds.Width;
                var screenAbsBottom = screen.Bounds.Y + screen.Bounds.Height;
                right = Math.Max(right, screenAbsRight);
                bottom = Math.Max(bottom, screenAbsBottom);
            }
            return new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
        }

        #region Implementation of IDisposable
        [GlobalCleanup]
        public void Dispose()
        {
        }

        #endregion
    }
}
