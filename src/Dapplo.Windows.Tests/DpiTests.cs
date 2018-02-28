//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Dpi;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class DpiTests
    {
        private static readonly LogSource Log = new LogSource();

        public DpiTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test ScaleWithDpi
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void Test_ScaleWithDpi()
        {
            var size_96 = DpiHandler.ScaleWithDpi(16, 96);
            Assert.Equal(16, size_96);
            var size_120 = DpiHandler.ScaleWithDpi(16, 120);
            Assert.Equal(20, size_120);
            var size_144 = DpiHandler.ScaleWithDpi(16, 144);
            Assert.Equal(24, size_144);
            var size_192 = DpiHandler.ScaleWithDpi(16, 192);
            Assert.Equal(32, size_192);
        }
    }
}