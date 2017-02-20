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

using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Extensions;
using Dapplo.Windows.Structs;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
    /// <summary>
    /// Tests for the RECT struct, and it'S extensions
    /// </summary>
    public class RectTests
    {
        private static readonly LogSource Log = new LogSource();

        public RectTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        [Fact]
        public void Ctor_L_R_T_B()
        {
            var rect = new RECT(10, 20, 100, 200);
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100, rect.Right);
            Assert.Equal(200, rect.Bottom);
            Assert.Equal(100 - 10, rect.Width);
            Assert.Equal(200 - 20 , rect.Height);
            Assert.Equal(new POINT(10,20), rect.Location);
            Assert.Equal(new SIZE(100-10, 200-20), rect.Size);
        }

        [Fact]
        public void Ctor_L_R_SIZE()
        {
            var rect = new RECT(10, 20, new SIZE(100, 200));
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100 + 10, rect.Right);
            Assert.Equal(200 + 20, rect.Bottom);
            Assert.Equal(100, rect.Width);
            Assert.Equal(200, rect.Height);
            Assert.Equal(new POINT(10, 20), rect.Location);
            Assert.Equal(new SIZE(100, 200), rect.Size);
        }

        [Fact]
        public void Ctor_POINT_SIZE()
        {
            var rect = new RECT(new POINT(10, 20), new SIZE(100, 200));
            Assert.Equal(10, rect.Left);
            Assert.Equal(20, rect.Top);
            Assert.Equal(100 + 10, rect.Right);
            Assert.Equal(200 + 20, rect.Bottom);
            Assert.Equal(100, rect.Width);
            Assert.Equal(200, rect.Height);
            Assert.Equal(new POINT(10, 20), rect.Location);
            Assert.Equal(new SIZE(100, 200), rect.Size);
        }

        [Fact]
        public void IsAdjacent()
        {
            int width = 100;
            int height = 100;

            int left = 200;
            int top = 200;

            // left
            var rect1 = new RECT(new POINT(left, top), new SIZE(width, height));
            var rect2 = new RECT(new POINT(left - width, top), new SIZE(width, height));
            Assert.True(rect1.IsAdjacent(rect2));
            // Right
            rect2 = new RECT(new POINT(left+ width, top), new SIZE(width, height));
            Assert.True(rect1.IsAdjacent(rect2));
            // Bottom
            rect2 = new RECT(new POINT(left, top+height), new SIZE(width, height));
            Assert.True(rect1.IsAdjacent(rect2));
            // Top
            rect2 = new RECT(new POINT(left, top - height), new SIZE(width, height));
            Assert.True(rect1.IsAdjacent(rect2));
        }

        [Fact]
        public void Not_IsAdjacent()
        {
            int width = 100;
            int height = 100;

            int left = 200;
            int top = 200;

            // left
            var rect1 = new RECT(new POINT(left, top), new SIZE(width, height));
            var rect2 = new RECT(new POINT(left - width-1, top), new SIZE(width, height));
            Assert.False(rect1.IsAdjacent(rect2));
            // Right
            rect2 = new RECT(new POINT(left + width+1, top), new SIZE(width, height));
            Assert.False(rect1.IsAdjacent(rect2));
            // Bottom
            rect2 = new RECT(new POINT(left, top + height+1), new SIZE(width, height));
            Assert.False(rect1.IsAdjacent(rect2));
            // Top
            rect2 = new RECT(new POINT(left, top - height -1), new SIZE(width, height));
            Assert.False(rect1.IsAdjacent(rect2));
        }
    }
}
