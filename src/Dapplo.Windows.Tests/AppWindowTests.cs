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
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.App;
using Xunit;
using Xunit.Abstractions;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Icons;

#endregion

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