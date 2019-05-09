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

using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class DpiTests
    {
        public DpiTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test ScaleWithDpi
        /// </summary>
        [Fact]
        public void Test_ScaleWithDpi()
        {
            var size96 = DpiHandler.ScaleWithDpi(16, 96);
            Assert.Equal(16, size96);
            var size120 = DpiHandler.ScaleWithDpi(16, 120);
            Assert.Equal(20, size120);
            var size144 = DpiHandler.ScaleWithDpi(16, 144);
            Assert.Equal(24, size144);
            var size192 = DpiHandler.ScaleWithDpi(16, 192);
            Assert.Equal(32, size192);
        }

        /// <summary>
        ///     Test UnscaleWithDpi
        /// </summary>
        [Fact]
        public void Test_UnscaleWithDpi()
        {
            var size96 = DpiHandler.UnscaleWithDpi(16, 96);
            Assert.Equal(16, size96);
            var size120 = DpiHandler.UnscaleWithDpi(16, 120);
            Assert.Equal(12, size120);
            var size144 = DpiHandler.UnscaleWithDpi(16, 144);
            Assert.Equal(10, size144);
            var size192 = DpiHandler.UnscaleWithDpi(16, 192);
            Assert.Equal(8, size192);
        }

        /// <summary>
        ///     Test scale -> unscale
        /// </summary>
        [Fact]
        public void Test_ScaleWithDpi_UnscaleWithDpi()
        {
            var testSize = new NativeSize(16, 16);
            var size96 = DpiHandler.ScaleWithDpi(testSize, 96);
            var resultSize96 = DpiHandler.UnscaleWithDpi(size96, 96);
            Assert.Equal(testSize, resultSize96);

            var size120 = DpiHandler.ScaleWithDpi(testSize, 120);
            var resultSize120 = DpiHandler.UnscaleWithDpi(size120, 120);
            Assert.Equal(testSize, resultSize120);

            var size144 = DpiHandler.ScaleWithDpi(testSize, 144);
            var resultSize144 = DpiHandler.UnscaleWithDpi(size144, 144);
            Assert.Equal(testSize, resultSize144);
        }
    }
}