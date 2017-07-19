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

using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    /// <summary>
    ///     Tests for the NativeRect struct, and it's extensions
    /// </summary>
    public class RectTests
    {
        public RectTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void Ctor_L_R_NativeSize()
        {
            var rect = new NativeRect(10, 20, new NativeSize(100, 200));
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100 + 10, rect.Right);
            Assert.Equal(200 + 20, rect.Bottom);
            Assert.Equal(100, rect.Width);
            Assert.Equal(200, rect.Height);
            Assert.Equal(new NativePoint(10, 20), rect.Location);
            Assert.Equal(new NativeSize(100, 200), rect.Size);
        }

        [Fact]
        public void Ctor_L_R_T_B()
        {
            var rect = new NativeRect(10, 20, 100, 200);
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100, rect.Right);
            Assert.Equal(200, rect.Bottom);
            Assert.Equal(100 - 10, rect.Width);
            Assert.Equal(200 - 20, rect.Height);
            Assert.Equal(new NativePoint(10, 20), rect.Location);
            Assert.Equal(new NativeSize(100 - 10, 200 - 20), rect.Size);
        }

        [Fact]
        public void Ctor_NativePoint_NativeSize()
        {
            var rect = new NativeRect(new NativePoint(10, 20), new NativeSize(100, 200));
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100 + 10, rect.Right);
            Assert.Equal(200 + 20, rect.Bottom);
            Assert.Equal(100, rect.Width);
            Assert.Equal(200, rect.Height);
            Assert.Equal(new NativePoint(10, 20), rect.Location);
            Assert.Equal(new NativeSize(100, 200), rect.Size);
        }

        [Fact]
        public void IsAdjacent()
        {
            const int width = 100;
            const int height = 100;

            const int left = 200;
            const int top = 200;

            // left
            var rect1 = new NativeRect(new NativePoint(left, top), new NativeSize(width, height));
            var rect2 = new NativeRect(new NativePoint(left - width, top), new NativeSize(width, height));
            Assert.Equal(AdjacentTo.Left, rect1.IsAdjacent(rect2));
            // Right
            rect2 = new NativeRect(new NativePoint(left + width, top), new NativeSize(width, height));
            Assert.Equal(AdjacentTo.Right, rect1.IsAdjacent(rect2));
            // Bottom
            rect2 = new NativeRect(new NativePoint(left, top + height), new NativeSize(width, height));
            Assert.Equal(AdjacentTo.Bottom, rect1.IsAdjacent(rect2));
            // Top
            rect2 = new NativeRect(new NativePoint(left, top - height), new NativeSize(width, height));
            Assert.Equal(AdjacentTo.Top, rect1.IsAdjacent(rect2));
        }

        [Fact]
        public void IsDocked()
        {
            const int width = 100;
            const int height = 100;

            const int left = 200;
            const int top = 200;

            // left
            var rect1 = new NativeRect(new NativePoint(left, top), new NativeSize(width, height));
            var rect2 = new NativeRect(new NativePoint(left - width - 1, top), new NativeSize(width, height));
            Assert.True(rect2.IsDockedToLeftOf(rect1));
            Assert.False(rect2.IsDockedToRightOf(rect1));
            // Right
            rect2 = new NativeRect(new NativePoint(left + width + 1, top), new NativeSize(width, height));
            Assert.True(rect2.IsDockedToRightOf(rect1));
            Assert.False(rect2.IsDockedToLeftOf(rect1));
        }
    }
}