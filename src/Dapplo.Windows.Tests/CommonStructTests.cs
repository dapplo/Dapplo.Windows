// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

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

    /// <summary>
    /// Test implicit conversion for NativeRectFloat
    /// </summary>
    [Fact]
    private void Test_NativeRectFloat_ImplicitConversion()
    {
        var nativeRectFloat = new NativeRectFloat(10.5f, 20.5f, 30.5f, 40.5f);

        var nativeRect = new NativeRect(10, 20, 30, 40);
        var nativeRectFloatExpected1 = new NativeRectFloat(10, 20, 30, 40);
        NativeRectFloat nativeRectFloatConverted1 = nativeRect;
        Assert.Equal(nativeRectFloatExpected1, nativeRectFloatConverted1);

        var nativeRectExpected = new NativeRect(10, 20, 30, 40);
        NativeRect nativeRectConverted = nativeRectFloat;
        Assert.Equal(nativeRectExpected, nativeRectConverted);

#if !NETSTANDARD2_0
        var rect = new Rect(10.0, 20.0, 30.0, 40.0);
        var nativeRectFloatExpected2 = new NativeRectFloat(10.0f, 20.0f, 30.0f, 40.0f);
        NativeRectFloat nativeRectFloatConverted2 = rect;
        Assert.Equal(nativeRectFloatExpected2, nativeRectFloatConverted2);

        var rectExpected = new Rect(10.5, 20.5, 30.5, 40.5);
        Rect rectConverted = nativeRectFloat;
        Assert.Equal(rectExpected, rectConverted);

        var int32Rect = new Int32Rect(10, 20, 30, 40);
        var nativeRectFloatExpected3 = new NativeRectFloat(10, 20, 30, 40);
        NativeRectFloat nativeRectFloatConverted3 = int32Rect;
        Assert.Equal(nativeRectFloatExpected3, nativeRectFloatConverted3);

        var int32RectExpected = new Int32Rect(10, 20, 30, 40);
        Int32Rect int32RectConverted = nativeRectFloat;
        Assert.Equal(int32RectExpected, int32RectConverted);
#endif

        var rectangle = new Rectangle(10, 20, 30, 40);
        var nativeRectFloatExpected4 = new NativeRectFloat(10, 20, 30, 40);
        NativeRectFloat nativeRectFloatConverted4 = rectangle;
        Assert.Equal(nativeRectFloatExpected4, nativeRectFloatConverted4);

        var rectangleExpected = new Rectangle(10, 20, 30, 40);
        Rectangle rectangleConverted = nativeRectFloat;
        Assert.Equal(rectangleExpected, rectangleConverted);

        var rectangleF = new RectangleF(10.5f, 20.5f, 30.5f, 40.5f);
        var nativeRectFloatExpected5 = new NativeRectFloat(10.5f, 20.5f, 30.5f, 40.5f);
        NativeRectFloat nativeRectFloatConverted5 = rectangleF;
        Assert.Equal(nativeRectFloatExpected5, nativeRectFloatConverted5);

        var rectangleFExpected = new RectangleF(10.5f, 20.5f, 30.5f, 40.5f);
        RectangleF rectangleFConverted = nativeRectFloat;
        Assert.Equal(rectangleFExpected, rectangleFConverted);
    }

    /// <summary>
    /// Test inflate for NativeRect
    /// </summary>
    [Theory]
    [InlineData(10,10,40,40,10,10)]
    [InlineData(200, 100, -40, -20, 10, 20)]
    [InlineData(100, 200, -20, -40, 20, 10)]
    [InlineData(100, 100, 40, 40, -10, 10)]
    [InlineData(100, 100, 40, 40, 10, -10)]
    private void Test_NativeRect_Inflate(int x, int y, int width, int height, int inflateX, int inflateY)
    {
        var nativeRect = new NativeRect(x, y, width, height);
        var nativeSize = new NativeSize(inflateX, inflateY);
        Rectangle rectangle = nativeRect;
        var nativeRectInflated = nativeRect.Inflate(inflateX, inflateY);
        var nativeRectInflatedWithSize = nativeRect.Inflate(nativeSize);
        Assert.Equal(nativeRectInflated, nativeRectInflatedWithSize);
        rectangle.Inflate(inflateX, inflateY);
        Assert.NotEqual(nativeRect, nativeRectInflated);
        Assert.Equal(rectangle, (Rectangle)nativeRectInflated);
    }

    /// <summary>
    /// Test union for NativeRect
    /// </summary>
    [Theory]
    [InlineData(10, 10, 40, 40, 20, 20, 10,10)]
    [InlineData(150, 150, 100, 100, 100, 100, 100, 100)]
    private void Test_NativeRect_Union(int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2)
    {
        var nativeRect1 = new NativeRect(x1, y1, width1, height1);
        var nativeRect2 = new NativeRect(x2, y2, width2, height2);
        Rectangle unionNativeRect = nativeRect1.Union(nativeRect2);
        var unionRectangle = Rectangle.Union(nativeRect1, nativeRect2);

        Assert.Equal(unionRectangle, unionNativeRect);
    }

    /// <summary>
    /// Test union for NativeRect
    /// </summary>
    [Theory]
    [InlineData(10, 10, 40, 40, 20, 20, 10, 10)]
    [InlineData(150, 150, 100, 100, 100, 100, 100, 100)]
    private void Test_NativeRect_Intersect(int x1, int y1, int width1, int height1, int x2, int y2, int width2, int height2)
    {
        var nativeRect1 = new NativeRect(x1, y1, width1, height1);
        var nativeRect2 = new NativeRect(x2, y2, width2, height2);
        Rectangle intersectNativeRect = nativeRect1.Intersect(nativeRect2);
        var intersectRectangle = Rectangle.Intersect(nativeRect1, nativeRect2);

        Assert.Equal(intersectRectangle, intersectNativeRect);
    }
}