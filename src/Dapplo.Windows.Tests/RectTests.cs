// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using System.Drawing;
using System.Windows;
using Xunit;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace Dapplo.Windows.Tests;

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
        Assert.Equal(110, rect.Right);
        Assert.Equal(220, rect.Bottom);
        Assert.Equal(100, rect.Width);
        Assert.Equal(200, rect.Height);
        Assert.Equal(new NativePoint(10, 20), rect.Location);
        Assert.Equal(new NativeSize(100, 200), rect.Size);
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
    public void NativeRectCasts()
    {
        var nativeRect = new NativeRect(new NativePoint(10, 20), new NativeSize(100, 200));
        Rectangle rectangle = nativeRect;
        Assert.Equal(nativeRect, (NativeRect)rectangle);
        Assert.Equal(rectangle, (Rectangle)nativeRect);

        Int32Rect rect = nativeRect;
        Assert.Equal(nativeRect, (NativeRect)rect);
        Assert.Equal(rect, (Int32Rect)nativeRect);
    }

    [Fact]
    public void NativeRectExtensions()
    {
        var nativeRect = new NativeRect(new NativePoint(10, 20), new NativeSize(100, 200));
        Rectangle rectangle = nativeRect;

        var rectangleWithXyOffset = rectangle;
        rectangleWithXyOffset.Offset(10,10);
        Assert.Equal(rectangleWithXyOffset, (Rectangle)nativeRect.Offset(10,10));

        var rectangleWithChangedSize = rectangle;
        rectangleWithChangedSize.Size = new Size(50, 50);
        Assert.Equal(rectangleWithChangedSize, (Rectangle)nativeRect.Resize(50, 50));

        var pointOffset = new Point(10,10);
        var rectangleWithPointOffset = rectangle;
        rectangleWithPointOffset.Offset(pointOffset);
        Assert.Equal(rectangleWithPointOffset, (Rectangle)nativeRect.Offset(pointOffset));

        var rectangleInflated = rectangle;
        rectangleInflated.Inflate(10, 10);
        Assert.Equal(rectangleInflated, (Rectangle)nativeRect.Inflate(10, 10));

        var rectangleChangedX = rectangle;
        rectangleChangedX.X = 110;
        Assert.Equal(rectangleChangedX, (Rectangle)nativeRect.ChangeX(110));

        var rectangleChangedY = rectangle;
        rectangleChangedY.Y = 110;
        Assert.Equal(rectangleChangedY, (Rectangle)nativeRect.ChangeY(110));

        var rectangleChangedWidth = rectangle;
        rectangleChangedWidth.Width = 110;
        Assert.Equal(rectangleChangedWidth, (Rectangle)nativeRect.ChangeWidth(110));

        var rectangleChangedHeight = rectangle;
        rectangleChangedHeight.Height = 110;
        Assert.Equal(rectangleChangedHeight, (Rectangle)nativeRect.ChangeHeight(110));
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

        // left, Right is exclusive so flush rectangles have rect2.Right == rect1.Left
        var rect1 = new NativeRect(new NativePoint(left, top), new NativeSize(width, height));
        var rect2 = new NativeRect(new NativePoint(left - width, top), new NativeSize(width, height));
        Assert.Equal(rect1.Left, rect2.Right);
        Assert.True(rect2.IsDockedToLeftOf(rect1));
        Assert.False(rect2.IsDockedToRightOf(rect1));
        // Right
        rect2 = new NativeRect(new NativePoint(left + width, top), new NativeSize(width, height));
        Assert.True(rect2.IsDockedToRightOf(rect1));
        Assert.False(rect2.IsDockedToLeftOf(rect1));

        // A one pixel gap is not docked
        rect2 = new NativeRect(new NativePoint(left - width - 1, top), new NativeSize(width, height));
        Assert.False(rect2.IsDockedToLeftOf(rect1));
        rect2 = new NativeRect(new NativePoint(left + width + 1, top), new NativeSize(width, height));
        Assert.False(rect2.IsDockedToRightOf(rect1));

        // A taller rectangle which extends above and below is docked
        rect2 = new NativeRect(new NativePoint(left - width, top - 50), new NativeSize(width, height + 100));
        Assert.True(rect2.IsDockedToLeftOf(rect1));

        // A rectangle which only touches at the corner (Bottom is exclusive) is not docked
        rect2 = new NativeRect(new NativePoint(left - width, top - height), new NativeSize(width, height));
        Assert.False(rect2.IsDockedToLeftOf(rect1));
    }

    [Fact]
    public void IsDocked_Float()
    {
        var rect1 = new NativeRectFloat(200f, 200f, 100f, 100f);
        var rect2 = new NativeRectFloat(100f, 200f, 100f, 100f);
        Assert.True(rect2.IsDockedToLeftOf(rect1));
        Assert.True(rect1.IsDockedToRightOf(rect2));
        rect2 = new NativeRectFloat(99f, 200f, 100f, 100f);
        Assert.False(rect2.IsDockedToLeftOf(rect1));
    }

    [Fact]
    public void HasOverlap()
    {
        var rect1 = new NativeRect(100, 100, 100, 100);
        // Adjacent rectangles touch, but do not overlap
        Assert.False(rect1.HasOverlap(new NativeRect(200, 100, 100, 100)));
        Assert.False(rect1.HasOverlap(new NativeRect(100, 200, 100, 100)));
        // Partial overlap
        Assert.True(rect1.HasOverlap(new NativeRect(150, 150, 100, 100)));
        // Contained and containing
        Assert.True(rect1.HasOverlap(new NativeRect(120, 120, 10, 10)));
        Assert.True(new NativeRect(120, 120, 10, 10).HasOverlap(rect1));
        // Disjoint
        Assert.False(rect1.HasOverlap(new NativeRect(300, 300, 10, 10)));

        var rectFloat = new NativeRectFloat(100f, 100f, 100f, 100f);
        Assert.False(rectFloat.HasOverlap(new NativeRectFloat(200f, 100f, 100f, 100f)));
        Assert.True(rectFloat.HasOverlap(new NativeRectFloat(120f, 120f, 10f, 10f)));
    }

    [Fact]
    public void Normalize()
    {
        const int width = -100;
        const int height = -100;

        const int left = 200;
        const int top = 200;

        // left
        var unnormalized = new NativeRect(new NativePoint(left, top), new NativeSize(width, height));
        Rectangle normalized = new Rectangle(100, 100, 100, 100);
        Assert.Equal(normalized, (Rectangle)unnormalized.Normalize());
    }

    [Fact]
    public void Intersect()
    {
        var rect1 = new NativeRect(100, 100, new NativeSize(100, 100));
        var rect2 = new NativeRect(90, 90, new NativeSize(20, 20));
        var rect3 = rect1.Intersect(rect2);

        Assert.True(rect1.IntersectsWith(rect2));
        var expected = new NativeRect(100, 100, new NativeSize(10, 10));
        Assert.Equal(expected, rect3);
    }

    [Fact]
    public void Intersect_PartialOverlap()
    {
        var rect1 = new NativeRect(0, 0, 10, 10);
        var rect2 = new NativeRect(5, 5, 10, 10);

        var expected = new NativeRect(5, 5, 5, 5);
        Assert.Equal(expected, rect1.Intersect(rect2));
        Assert.Equal(expected, rect2.Intersect(rect1));
    }

    [Fact]
    public void Intersect_NoOverlap()
    {
        var rect1 = new NativeRect(0, 0, 10, 10);
        var rect2 = new NativeRect(20, 20, 10, 10);

        Assert.Equal(NativeRect.Empty, rect1.Intersect(rect2));
    }

    [Fact]
    public void Union()
    {
        var rect1 = new NativeRect(100, 100, new NativeSize(100, 100));
        var rect2 = new NativeRect(90, 90, new NativeSize(20, 20));
        var rect3 = rect1.Union(rect2);

        var expected = new NativeRect(90, 90, new NativeSize(110, 110));
        Assert.Equal(expected, rect3);
    }
}