// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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