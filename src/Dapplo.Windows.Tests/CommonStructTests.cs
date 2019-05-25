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

using System.ComponentModel;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class CommonStructTests
    {
        public CommonStructTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        /// <summary>
        ///     Test NativeRect Properties
        /// </summary>
        [Fact]
        private void Test_NativeRect_Properties()
        {
            const int left = 100;
            const int top = 200;
            const int width = 110;
            const int right = left + width;
            const int height = 120;
            const int bottom = top + height;

            var nativeRect = new NativeRect(left, top, new NativeSize(width, height));
            Assert.Equal(left, nativeRect.X);
            Assert.Equal(top, nativeRect.Y);
            Assert.Equal(left, nativeRect.Left);
            Assert.Equal(top, nativeRect.Top);

            Assert.Equal(width, nativeRect.Width);
            Assert.Equal(height, nativeRect.Height);

            Assert.Equal(right, nativeRect.Right);
            Assert.Equal(bottom, nativeRect.Bottom);

            Assert.Equal(new NativePoint(left, top), nativeRect.TopLeft);
            Assert.Equal(new NativePoint(left, bottom), nativeRect.BottomLeft);
            Assert.Equal(new NativePoint(right, top), nativeRect.TopRight);
            Assert.Equal(new NativePoint(right, bottom), nativeRect.BottomRight);

            Assert.Equal(new NativeSize(width, height), nativeRect.Size);
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
        ///     Test NativeSize operators
        /// </summary>
        [Fact]
        private void Test_NativeSize_Operators()
        {
            var nativeSize1 = new NativeSize(123, 456);
            var nativeSize2 = new NativeSize(123, 456);
            var drawingSize = new System.Drawing.Size(123, 456);
            var windowsSize = new System.Windows.Size(123, 456);
            var drawingSizeNotEqual = new System.Drawing.Size(456, 123);
            var windowsSizeNotEqual = new System.Windows.Size(456, 123);
            Assert.True(nativeSize1 == nativeSize2);
            Assert.True(nativeSize1 == drawingSize);
            Assert.True(drawingSize == nativeSize1);
            Assert.False(nativeSize1 != drawingSize);
            Assert.False(drawingSize != nativeSize1);
            Assert.True(drawingSizeNotEqual != nativeSize1);

            Assert.True(nativeSize1 == windowsSize);
            Assert.True(windowsSize == nativeSize1);
            Assert.False(nativeSize1 != windowsSize);
            Assert.False(windowsSize != nativeSize1);
            Assert.True(windowsSizeNotEqual != nativeSize1);
        }

        /// <summary>
        ///     Test NativeSize operators
        /// </summary>
        [Fact]
        private void Test_NativeSizeFloat_Operators()
        {
            var nativeSize1 = new NativeSizeFloat(123, 456);
            var nativeSize2 = new NativeSizeFloat(123, 456);
            var drawingSize = new System.Drawing.Size(123, 456);
            var windowsSize = new System.Windows.Size(123, 456);
            var drawingSizeNotEqual = new System.Drawing.Size(456, 123);
            var windowsSizeNotEqual = new System.Windows.Size(456, 123);
            Assert.True(nativeSize1 == nativeSize2);
            Assert.True(nativeSize1 == drawingSize);
            Assert.True(drawingSize == nativeSize1);
            Assert.False(nativeSize1 != drawingSize);
            Assert.False(drawingSize != nativeSize1);
            Assert.True(drawingSizeNotEqual != nativeSize1);

            Assert.True(nativeSize1 == windowsSize);
            Assert.True(windowsSize == nativeSize1);
            Assert.False(nativeSize1 != windowsSize);
            Assert.False(windowsSize != nativeSize1);
            Assert.True(windowsSizeNotEqual != nativeSize1);
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

        /// <summary>
        ///     Test NativeRect Transform
        /// </summary>
        [Fact]
        private void Test_NativeRect_Transform()
        {
            const int offsetX = 20;
            const int offsetY = 30;
            var nativeRectBefore = new NativeRect(0,0,new NativeSize(200, 60));
            var nativeRectAfter = new NativeRect(offsetX, offsetY, new NativeSize(60, 200));
            var myMatrix = new System.Windows.Media.Matrix(0, 1, 1, 0, offsetX, offsetY);

            Assert.Equal(nativeRectAfter, nativeRectBefore.Transform(myMatrix));
        }
    }
}