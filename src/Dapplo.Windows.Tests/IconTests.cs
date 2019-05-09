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

using System.Diagnostics;
using System.Linq;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class IconTests
    {
        public IconTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test getting an Icon for a top level window
        /// </summary>
        [WpfFact]
        public void TestIcon_GetIcon()
        {
            // Start a process to test against
            using (var process = Process.Start("notepad.exe"))
            {
                // Make sure it's started
                Assert.NotNull(process);
                // Wait until the process started it's message pump (listening for input)
                process.WaitForInputIdle();
                User32Api.SetWindowText(process.MainWindowHandle, "TestIcon_GetIcon");

                var window = InteropWindowQuery.GetTopLevelWindows().First();
                var icon = window.GetIcon<BitmapSource>();
                Assert.NotNull(icon);

                // Kill the process
                process.Kill();
            }
        }

        /// <summary>
        ///     Test getting an Icon for the desktop, which doesn't have one
        /// </summary>
        [WpfFact]
        public void TestIcon_GetIcon_Null()
        {
            var window = InteropWindowQuery.GetDesktopWindow();
            var icon = window.GetIcon<BitmapSource>();

            Assert.Null(icon);
        }
    }
}