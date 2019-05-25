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

#region using

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class DispayTests
    {
        private static readonly LogSource Log = new LogSource();

        public DispayTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestAllDisplays()
        {
            foreach (var display in DisplayInfo.AllDisplayInfos)
            {
                Log.Debug().WriteLine("Index {0} - Primary {3} - Device {1} - Bounds: {2}", display.Index, display.DeviceName, display.Bounds, display.IsPrimary);
            }
        }

        [Fact]
        public void TestGetBounds()
        {
            var displayInfo = DisplayInfo.AllDisplayInfos.First();
            var displayBounds = DisplayInfo.GetBounds(displayInfo.Bounds.Location);
            Assert.Equal(displayInfo.Bounds, displayBounds);
        }

        [Fact]
        public void TestScreenbounds()
        {
            var screenboundsAllScreens = GetScreenBoundsAllScreens();
            var screenboundsDisplayInfo = DisplayInfo.ScreenBounds;
            Assert.Equal(screenboundsAllScreens, screenboundsDisplayInfo);
        }

        /// <summary>
        /// This is a manual test, it should be started and something needs to change the display settings, it should be detected correctly
        /// </summary>
        //[WpfFact]
        public async Task TestScreenboundsSubscription()
        {
            var screenboundsDisplayInfoBefore = DisplayInfo.ScreenBounds;
            TestAllDisplays();

            await Task.Delay(10000);
            var screenboundsDisplayInfoAfter = DisplayInfo.ScreenBounds;
            TestAllDisplays();

            Assert.NotEqual(screenboundsDisplayInfoBefore, screenboundsDisplayInfoAfter);
        }


        /// <summary>
        ///     Get the bounds of all screens combined.
        /// </summary>
        /// <returns>A NativeRect of the bounds of the entire display area.</returns>
        private NativeRect GetScreenBoundsAllScreens()
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
    }
}