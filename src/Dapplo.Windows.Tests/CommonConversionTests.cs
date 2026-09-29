// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the HResult, TypeConverters, conversions and comparisons of the Dapplo.Windows.Common structs
/// </summary>
public class CommonConversionTests
{
    [Fact]
    public void HResult_FailedAndSucceeded()
    {
        Assert.True(HResult.S_OK.Succeeded());
        Assert.False(HResult.S_OK.Failed());
        Assert.True(HResult.S_FALSE.Succeeded());
        Assert.True(HResult.E_FAIL.Failed());
        Assert.False(HResult.E_FAIL.Succeeded());
        Assert.True(HResult.E_ACCESSDENIED.Failed());
        Assert.Equal(unchecked((int)0x80004005), (int)HResult.E_FAIL);

        // Must not throw
        HResult.S_OK.ThrowOnFailure();
        HResult.S_FALSE.ThrowOnFailure();
        var exception = Assert.ThrowsAny<Exception>(() => HResult.E_INVALIDARG.ThrowOnFailure());
        Assert.Equal((int)HResult.E_INVALIDARG, exception.HResult);
    }

    [Fact]
    public void NativeSize_TypeConverter_RoundTrip()
    {
        var typeConverter = TypeDescriptor.GetConverter(typeof(NativeSize));
        var size = new NativeSize(800, 600);
        var stringRepresentation = typeConverter.ConvertToInvariantString(size);
        Assert.Equal("800,600", stringRepresentation);
        Assert.Equal(size, (NativeSize)typeConverter.ConvertFromInvariantString(stringRepresentation));
    }

    [Fact]
    public void NativeSizeFloat_TypeConverter_RoundTrip()
    {
        var typeConverter = TypeDescriptor.GetConverter(typeof(NativeSizeFloat));
        var size = new NativeSizeFloat(800.5f, 600.25f);
        var stringRepresentation = typeConverter.ConvertToInvariantString(size);
        Assert.Equal("800.5,600.25", stringRepresentation);
        Assert.Equal(size, (NativeSizeFloat)typeConverter.ConvertFromInvariantString(stringRepresentation));
        // The exponent format must be accepted
        Assert.Equal(new NativeSizeFloat(1E-05f, 2f), (NativeSizeFloat)typeConverter.ConvertFromInvariantString("1E-05,2"));
    }

    [Fact]
    public void NativePointFloat_TypeConverter_RoundTrip()
    {
        var typeConverter = TypeDescriptor.GetConverter(typeof(NativePointFloat));
        var point = new NativePointFloat(1.5f, -2.25f);
        var stringRepresentation = typeConverter.ConvertToInvariantString(point);
        Assert.Equal("1.5,-2.25", stringRepresentation);
        Assert.Equal(point, (NativePointFloat)typeConverter.ConvertFromInvariantString(stringRepresentation));
    }

    /// <summary>
    ///     sv-SE uses U+2212 as negative sign on ICU, the converters must still write something they can read back
    /// </summary>
    [Fact]
    public void TypeConverters_NegativeValues_NonInvariantCulture()
    {
        var previousCulture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sv-SE");
            var point = new NativePoint(-1920, -5);
            var pointConverter = TypeDescriptor.GetConverter(typeof(NativePoint));
            var pointString = pointConverter.ConvertToString(point);
            Assert.Equal("-1920,-5", pointString);
            Assert.Equal(point, (NativePoint)pointConverter.ConvertFromString(pointString));

            var rect = new NativeRect(-1920, -10, 1920, 1080);
            var rectConverter = TypeDescriptor.GetConverter(typeof(NativeRect));
            var rectString = rectConverter.ConvertToString(rect);
            Assert.Equal("-1920,-10,1920,1080", rectString);
            Assert.Equal(rect, (NativeRect)rectConverter.ConvertFromString(rectString));

            var size = new NativeSize(-1, 2);
            var sizeConverter = TypeDescriptor.GetConverter(typeof(NativeSize));
            Assert.Equal(size, (NativeSize)sizeConverter.ConvertFromString(sizeConverter.ConvertToString(size)));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void NativeSize_CompareTo_Ascending()
    {
        var sizes = new List<NativeSize> { new NativeSize(30, 30), new NativeSize(10, 10), new NativeSize(20, 20) };
        sizes.Sort();
        Assert.Equal(new NativeSize(10, 10), sizes[0]);
        Assert.Equal(new NativeSize(30, 30), sizes[2]);
        Assert.True(new NativeSize(10, 10).CompareTo(new NativeSize(20, 20)) < 0);
        // Area larger than int.MaxValue must not overflow
        Assert.True(new NativeSize(100000, 100000).CompareTo(new NativeSize(10, 10)) > 0);

        var floatSizes = new List<NativeSizeFloat> { new NativeSizeFloat(3f, 3f), new NativeSizeFloat(1f, 1f), new NativeSizeFloat(2f, 2f) };
        floatSizes.Sort();
        Assert.Equal(new NativeSizeFloat(1f, 1f), floatSizes[0]);
        Assert.Equal(new NativeSizeFloat(3f, 3f), floatSizes[2]);
    }

    [Fact]
    public void ExplicitConversions_FloatToInt()
    {
        // Floor, not truncate toward zero
        Assert.Equal(new NativePoint(-1, 0), (NativePoint)new NativePointFloat(-0.5f, 0.5f));
        Assert.Equal(new System.Drawing.Point(-1, 0), (System.Drawing.Point)new NativePointFloat(-0.5f, 0.5f));
        Assert.Equal(new NativePoint(-1, 1), (NativePoint)new System.Drawing.PointF(-0.5f, 1.5f));
        // Sizes are rounded up
        Assert.Equal(new System.Drawing.Size(2, 3), (System.Drawing.Size)new NativeSizeFloat(1.2f, 3f));
        Assert.Equal(new NativeSize(2, 3), (NativeSize)new System.Windows.Size(1.2, 3));
        Assert.Equal(NativeSize.Empty, (NativeSize)System.Windows.Size.Empty);
        // Right does not drift: 0.6 + 0.6 = 1.2, so the containing rectangle is 0..2
        Assert.Equal(new NativeRect(0, 0, 2, 2), (NativeRect)new NativeRectFloat(0.6f, 0.6f, 0.6f, 0.6f));
    }

    [Fact]
    public void WpfRectConversion_NotNormalized_DoesNotThrow()
    {
        var notNormalized = new NativeRect(100, 100, -50, -20);
        System.Windows.Rect rect = notNormalized;
        Assert.Equal(new System.Windows.Rect(50, 80, 50, 20), rect);

        var notNormalizedFloat = new NativeRectFloat(100f, 100f, -50f, -20f);
        System.Windows.Rect rectFromFloat = notNormalizedFloat;
        Assert.Equal(new System.Windows.Rect(50, 80, 50, 20), rectFromFloat);
    }

    [Fact]
    public void WindowsVersion_IsConsistent()
    {
        var version = WindowsVersion.WinVersion;
        Assert.True(version.Major >= 6);
        // The real version is reported, even without a manifest
        Assert.Equal(version.Major == 10, WindowsVersion.IsWindows10OrLater);
        Assert.False(WindowsVersion.IsWindows10 && WindowsVersion.IsWindows11OrLater);
        Assert.Equal(version.Major == 6 && version.Minor == 0, WindowsVersion.IsWindowsVista);
        Assert.True(WindowsVersion.IsWindowsVistaOrLater);
        Assert.True(WindowsVersion.IsWindowsXpOrLater);
        Assert.False(WindowsVersion.IsWindowsXp);
        if (WindowsVersion.IsWindows11OrLater)
        {
            Assert.True(WindowsVersion.IsWindows10BuildOrLater(19041));
        }
    }
}
