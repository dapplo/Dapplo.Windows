// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Dpi;

/// <summary>
///     Calculate with DPI
/// </summary>
public sealed class DpiCalculator
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    ///     This is the default DPI for the screen
    /// </summary>
    public const int DefaultScreenDpi = 96;

    /// <summary>
    /// Calculate a DPI scale factor
    /// </summary>
    /// <param name="oldDpi">int</param>
    /// <param name="newDpi">int</param>
    /// <returns>double</returns>
    public static float DpiScaleFactor(int oldDpi, int newDpi)
    {
        return (float)newDpi / oldDpi;
    }

    /// <summary>
    /// Calculate a DPI scale factor based on the default
    /// </summary>
    /// <param name="dpi">int</param>
    /// <returns>double</returns>
    public static float DpiScaleFactor(int dpi)
    {
        return (float) dpi / DefaultScreenDpi;
    }

    /// <summary>
    ///     Scale the supplied number according to the supplied dpi
    /// </summary>
    /// <param name="someNumber">double with e.g. the width 16 for 16x16 images</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>double with the scaled number</returns>
    public static float ScaleWithDpi(float someNumber, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = (float)DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }

        return dpiScaleFactor * someNumber;
    }

    /// <summary>
    ///     Scale the supplied number according to the supplied dpi
    /// </summary>
    /// <param name="someNumber">double with e.g. the width 16 for 16x16 images</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>double with the scaled number</returns>
    public static double ScaleWithDpi(double someNumber, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }
        return dpiScaleFactor * someNumber;
    }

    /// <summary>
    ///     Scale the supplied number according to the supplied dpi
    /// </summary>
    /// <param name="number">int with e.g. 16 for 16x16 images</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>Scaled width</returns>
    public static int ScaleWithDpi(int number, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }
        return (int)(dpiScaleFactor * number);
    }

    /// <summary>
    ///     Scale the supplied NativeSize according to the supplied dpi
    /// </summary>
    /// <param name="size">NativeSize to resize</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize scaled</returns>
    public static NativeSize ScaleWithDpi(NativeSize size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }
        return new NativeSize((int)(dpiScaleFactor * size.Width), (int)(dpiScaleFactor * size.Height));
    }

    /// <summary>
    ///     Scale the supplied NativePoint according to the supplied dpi
    /// </summary>
    /// <param name="size">NativePoint to resize</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePoint scaled</returns>
    public static NativePoint ScaleWithDpi(NativePoint size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }
        return new NativePoint((int)(dpiScaleFactor * size.X), (int)(dpiScaleFactor * size.Y));
    }

    /// <summary>
    ///     Scale the supplied NativeSizeFloat according to the supplied dpi
    /// </summary>
    /// <param name="size">NativeSizeFloat to resize</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize scaled</returns>
    public static NativeSizeFloat ScaleWithDpi(NativeSizeFloat size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiScaleFactor = DpiScaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiScaleFactor = scaleModifier(dpiScaleFactor);
        }
        return new NativeSizeFloat(dpiScaleFactor * size.Width, dpiScaleFactor * size.Height);
    }

    /// <summary>
    /// Calculate a DPI unscale factor
    /// </summary>
    /// <param name="oldDpi">int</param>
    /// <param name="newDpi">int</param>
    /// <returns>float</returns>
    public static float DpiUnscaleFactor(int oldDpi, int newDpi)
    {
        return (float)oldDpi / newDpi;
    }

    /// <summary>
    /// Calculate a DPI unscale factor
    /// </summary>
    /// <param name="dpi">int</param>
    /// <returns>float</returns>
    public static float DpiUnscaleFactor(int dpi) => DpiUnscaleFactor(DefaultScreenDpi, dpi);

    /// <summary>
    ///     Unscale the supplied number according to the supplied dpi
    /// </summary>
    /// <param name="someNumber">double with e.g. the scaled width</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>float with the unscaled number</returns>
    public static double UnscaleWithDpi(double someNumber, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
        }
        return dpiUnscaleFactor * someNumber;
    }

    /// <summary>
    ///    Unscale the supplied number according to the supplied dpi
    /// </summary>
    /// <param name="number">int with a scaled width</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>Unscaled width</returns>
    public static int UnscaleWithDpi(int number, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
        }
        return (int)(dpiUnscaleFactor * number);
    }

    /// <summary>
    ///     Unscale the supplied NativeSize according to the supplied dpi
    /// </summary>
    /// <param name="size">NativeSize to unscale</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize unscaled</returns>
    public static NativeSize UnscaleWithDpi(NativeSize size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
        }
        return new NativeSize((int)(dpiUnscaleFactor * size.Width), (int)(dpiUnscaleFactor * size.Height));
    }

    /// <summary>
    ///     Unscale the supplied NativePoint according to the supplied dpi
    /// </summary>
    /// <param name="size">NativePoint to unscale</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativePoint unscaled</returns>
    public static NativePoint UnscaleWithDpi(NativePoint size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
        }
        return new NativePoint((int)(dpiUnscaleFactor * size.X), (int)(dpiUnscaleFactor * size.Y));
    }

    /// <summary>
    ///     unscale the supplied NativeSizeFloat according to the supplied dpi
    /// </summary>
    /// <param name="size">NativeSizeFloat to resize</param>
    /// <param name="dpi">current dpi, normal is 96.</param>
    /// <param name="scaleModifier">A function which can modify the scale factor</param>
    /// <returns>NativeSize unscaled</returns>
    public static NativeSizeFloat UnscaleWithDpi(NativeSizeFloat size, int dpi, Func<float, float> scaleModifier = null)
    {
        var dpiUnscaleFactor = DpiUnscaleFactor(dpi);
        if (scaleModifier != null)
        {
            dpiUnscaleFactor = scaleModifier(dpiUnscaleFactor);
        }
        return new NativeSizeFloat(dpiUnscaleFactor * size.Width, dpiUnscaleFactor * size.Height);
    }
}