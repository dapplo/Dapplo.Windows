//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class TestGetDisplays
    {
        private static readonly LogSource Log = new LogSource();

        public TestGetDisplays(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void TestAllDisplays()
        {
            foreach (var display in User32Api.AllDisplays())
            {
                Log.Debug().WriteLine("Index {0} - Primary {3} - Device {1} - Bounds: {2}", display.Index, display.DeviceName, display.Bounds, display.IsPrimary);
            }
        }

        [Fact]
        public void TestGetBounds()
        {
            var displayInfo = User32Api.AllDisplays().First();
            var displayBounds = DisplayInfo.GetBounds(displayInfo.Bounds.Location);
            Assert.Equal(displayInfo.Bounds, displayBounds);
        }
    }
}