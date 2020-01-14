// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.App;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;

namespace Dapplo.Windows.Tests
{
    public class AppWindowTests
    {
        private static readonly LogSource Log = new LogSource();
        public AppWindowTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     All apps we find, should give true when IsApp and have a logo.
        /// </summary>
        /// <returns></returns>
        [WpfFact]
        public void TestAppWindowList()
        {
            var apps = AppQuery.WindowsStoreApps.ToList();
            foreach (var interopWindow in apps)
            {
                Log.Debug().WriteLine("{0} - {1}", interopWindow.GetCaption(), interopWindow.GetClassname());
                Assert.True(interopWindow.IsApp());
                var iconBitmapSource = interopWindow.GetIcon<BitmapSource>();
                Assert.NotNull(iconBitmapSource);
            }
        }

        /// <summary>
        ///     Make sure GetTopLevelWindows doesn't return an App Window.
        /// </summary>
        /// <returns></returns>
        [WpfFact]
        public void TestApp_TopLevel()
        {
            var topLevelWindows = InteropWindowQuery.GetTopLevelWindows().ToList();
            Assert.DoesNotContain(topLevelWindows, window => window.IsApp());
        }
    }
}