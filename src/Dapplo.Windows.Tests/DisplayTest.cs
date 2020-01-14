// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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