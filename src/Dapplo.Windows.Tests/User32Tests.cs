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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Extensions;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class User32Tests
    {
        private static readonly LogSource Log = new LogSource();

        public User32Tests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestGetClassname()
        {
            var desktopHandle = InteropWindowQuery.GetDesktopWindow();

            var classname = User32Api.GetClassname(desktopHandle.Handle);
            Assert.Equal("#32769", classname);
        }

        /// <summary>
        ///     Test GetWindow
        /// </summary>
        /// <returns></returns>
        //[Fact]
        private void TestDetectChanges()
        {
            bool foundWindow;
            var initialWindows = InteropWindowQuery.GetTopWindows().Where(window => window.IsVisible()).ToList();

            while (true)
            {
                Thread.Sleep(1000);
                var newWindow = InteropWindowQuery.GetTopWindows().FirstOrDefault(window => window.IsVisible() && !initialWindows.Contains(window));
                if (newWindow != null)
                {
                    foundWindow = true;
                    Log.Debug().WriteLine("{0}", newWindow.Dump());
                    break;
                }
            }

            Assert.True(foundWindow);
        }

        /// <summary>
        ///     Test GetWindow
        /// </summary>
        /// <returns></returns>
        [Fact]
        private void TestGetTopLevelWindows()
        {
            var foundWindow = false;
            foreach (var window in InteropWindowQuery.GetTopWindows().Where(window => window.IsVisible()))
            {
                foundWindow = true;

                Log.Debug().WriteLine("{0}", window.Dump());
                break;
            }
            Assert.True(foundWindow);
        }


        /// <summary>
        ///     Test WindowPlacement_TypeConverter
        /// </summary>
        /// <returns></returns>
        [Fact]
        private void TestWindowPlacement_TypeConverter()
        {
            var windowPlacement = WindowPlacement.Create();
            windowPlacement.MinPosition = new NativePoint(10, 10);
            windowPlacement.MaxPosition = new NativePoint(100, 100);
            windowPlacement.NormalPosition = new NativeRect(100, 100, 200, 200);
            windowPlacement.ShowCmd = ShowWindowCommands.Normal;

            var typeConverter = TypeDescriptor.GetConverter(typeof(WindowPlacement));
            Assert.NotNull(typeConverter);
            var stringRepresentation = typeConverter.ConvertToInvariantString(windowPlacement);
            Assert.Equal("Normal|10,10|100,100|100,100,200,200", stringRepresentation);
            var windowPlacementResult = (WindowPlacement?)typeConverter.ConvertFromInvariantString(stringRepresentation);
            Assert.True(windowPlacementResult.HasValue);
            if (windowPlacementResult.HasValue)
            {
                Assert.Equal(windowPlacement, windowPlacementResult.Value);
            }
        }

        /// <summary>
        ///     Test GetTextFromWindow
        /// </summary>
        [WpfFact]
        private void Test_GetTextFromWindow()
        {
            const string title = "1234567890";
            var window = new Window
            {
                Title = title
            };

            window.Show();
            var handle = window.GetHandle();
            var text = User32Api.GetTextFromWindow(handle);
            window.Close();
            Assert.Equal(title, text);
        }
    }
}