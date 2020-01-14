// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Tests
{
    public class InteropWindowTests
    {
        public InteropWindowTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///    Test some of the InteropWindowQuery logic by finding the taskbar and the clock on it.
        /// </summary>
        /// <returns></returns>
        //[Fact]
        public void TestTaskbarInfo()
        {
            var systray = InteropWindowQuery.GetTopWindows().FirstOrDefault(window => window.GetClassname() == "Shell_TrayWnd");
            Assert.NotNull(systray);
            var clock = systray.GetChildren().FirstOrDefault(window => window.GetClassname() == "TrayClockWClass");
            Assert.NotNull(clock);

            var info = clock.GetInfo();
            Assert.True(info.ClientBounds.Width * info.ClientBounds.Height > 0);
        }

        //[Fact]
        public void Test_GetInfo_WithParentCrop()
        {

            const int testHandle = 0x000806a6;
            var testWindow = InteropWindowFactory.CreateFor(testHandle);
	        var ws = testWindow.GetWindowScroller();
	        var info1 = ws.ScrollingWindow.GetInfo();
	        Assert.True(info1.Bounds.Width < 1920);
			var info2 = testWindow.GetInfo();
            Assert.True(info2.Bounds.Width < 1920);
        }
    }
}