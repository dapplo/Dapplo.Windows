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

using System.Linq;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;

#endregion

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