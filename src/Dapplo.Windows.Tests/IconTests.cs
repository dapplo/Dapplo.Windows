// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        [WpfFact]
        public void TestIcon_CreateIconFile()
        {
            var testFilename = @"CreatedIcon.ico";
            if (File.Exists(testFilename))
            {
                File.Delete(testFilename);
            }
            var iconStream = File.Create(testFilename);

            var bitmaps = new Bitmap[6];
            bitmaps[0] = new Bitmap(16, 16);
            bitmaps[1] = new Bitmap(32, 32);
            bitmaps[2] = new Bitmap(48, 48);
            bitmaps[3] = new Bitmap(64, 64);
            bitmaps[4] = new Bitmap(128, 128);
            bitmaps[5] = new Bitmap(256, 256);
            iconStream.WriteIcon(bitmaps);
            iconStream.Flush();
            iconStream.Dispose();
            foreach (var bitmap in bitmaps)
            {
                bitmap.Dispose();
            }

            iconStream = File.OpenRead(testFilename);
            var iconDirEntries = iconStream.ReadIconDirEntries();

            foreach (var iconDirEntry in iconDirEntries)
            {
                using var bitmap = iconStream.ReadIconOrCursorImage(iconDirEntry);
                Debug.WriteLine($"Image {bitmap.Width}x{bitmap.Height} icon dir entry {iconDirEntry.Width}x{iconDirEntry.Height} (location {iconDirEntry.ImageOffset})");
                Assert.Equal(iconDirEntry.Width, bitmap.Width);
            }
            iconStream.Dispose();
        }

        [WpfFact]
        public void TestIcon_LoadPredefinedIcon()
        {
            using var iconStream = File.OpenRead(@"TestFiles\64x64.ico");
            var iconDirEntries = iconStream.ReadIconDirEntries();

            foreach (var iconDirEntry in iconDirEntries)
            {
                using var bitmap = iconStream.ReadIconOrCursorImage(iconDirEntry);
                Debug.WriteLine($"{bitmap.Width}x{bitmap.Height}");
            }
            var bestQualityIcon = iconDirEntries.FirstOrDefault();
            var bestBitmap = iconStream.ReadIconOrCursorImage(bestQualityIcon);
            Assert.Equal(256, bestBitmap.Width);
        }

        [WpfFact]
        public void TestIcon_LoadPredefinedCursor()
        {
            using var cursorStream = File.OpenRead(@"C:\Windows\Cursors\aero_arrow_xl.cur");
            var iconDirEntries = cursorStream.ReadIconDirEntries();
            var bestQualityIcon = iconDirEntries.FirstOrDefault();
            var bitmap = cursorStream.ReadIconOrCursorImage(bestQualityIcon);
            Assert.Equal(96, bitmap.Width);
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