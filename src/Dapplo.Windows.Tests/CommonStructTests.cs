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

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class CommonStructTests
    {
        private static readonly LogSource Log = new LogSource();

        public CommonStructTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test NativePoint TypeConverter
        /// </summary>
        [Fact]
        private void Test_NativePoint_TypeConverter()
        {
            var nativePoint = new NativePoint(123, 456);

            var typeConverter = TypeDescriptor.GetConverter(typeof(NativePoint));
            Assert.NotNull(typeConverter);
            var stringRepresentation = typeConverter.ConvertToInvariantString(nativePoint);
            Assert.Equal("123,456", stringRepresentation);
            var nativePointResult = (NativePoint?)typeConverter.ConvertFromInvariantString(stringRepresentation);
            Assert.True(nativePointResult.HasValue);
            if (nativePointResult.HasValue)
            {
                Assert.Equal(nativePoint, nativePointResult.Value);
            }
        }

        /// <summary>
        ///     Test NativeRect TypeConverter
        /// </summary>
        [Fact]
        private void Test_NativeRect_TypeConverter()
        {
            var nativeRect = new NativeRect(123, 456, 457, 876);

            var typeConverter = TypeDescriptor.GetConverter(typeof(NativeRect));
            Assert.NotNull(typeConverter);
            var stringRepresentation = typeConverter.ConvertToInvariantString(nativeRect);
            Assert.Equal("123,456,457,876", stringRepresentation);
            var nativePointResult = (NativeRect?)typeConverter.ConvertFromInvariantString(stringRepresentation);
            Assert.True(nativePointResult.HasValue);
            if (nativePointResult.HasValue)
            {
                Assert.Equal(nativeRect, nativePointResult.Value);
            }
        }

        /// <summary>
        ///     Test NativeRectFloat TypeConverter
        /// </summary>
        [Fact]
        private void Test_NativeRectFloat_TypeConverter()
        {
            var nativeRect = new NativeRectFloat(123.1f, 456.2f, 457.3f, 876.4f);

            var typeConverter = TypeDescriptor.GetConverter(typeof(NativeRectFloat));
            Assert.NotNull(typeConverter);
            var stringRepresentation = typeConverter.ConvertToInvariantString(nativeRect);
            Assert.Equal("123.1,456.2,457.3,876.4", stringRepresentation);
            var nativePointResult = (NativeRectFloat?)typeConverter.ConvertFromInvariantString(stringRepresentation);
            Assert.True(nativePointResult.HasValue);
            if (nativePointResult.HasValue)
            {
                Assert.Equal(nativeRect, nativePointResult.Value);
            }
        }
    }
}