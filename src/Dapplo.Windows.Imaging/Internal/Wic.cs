// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// Shared helpers for the WIC calls
/// </summary>
internal static class Wic
{
    private static readonly ConcurrentDictionary<Guid, int> BitsPerPixelCache = new ConcurrentDictionary<Guid, int>();

    /// <summary>
    /// Throw for a failed HRESULT: the exception of a managed stream or pixel source which caused it,
    /// a <see cref="WicCodecNotFoundException"/> for WINCODEC_ERR_COMPONENTNOTFOUND, otherwise a COMException.
    /// </summary>
    public static void Check(HResult hResult, string operation, IFailureSource first = null, IFailureSource second = null)
    {
        if (hResult.Succeeded())
        {
            return;
        }
        var cause = first?.LastException ?? second?.LastException;
        if (cause != null)
        {
            ExceptionDispatchInfo.Capture(cause).Throw();
        }
        var message = $"{operation} failed with 0x{(int)hResult:X8}";
        if (hResult == WicErrors.ComponentNotFound)
        {
            throw new WicCodecNotFoundException(message, (int)hResult);
        }
        throw new COMException(message, (int)hResult);
    }

    /// <summary>
    /// Release a runtime callable wrapper, null and managed objects are ignored
    /// </summary>
    public static void Release(object comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }

    /// <summary>
    /// The bits per pixel of a WIC pixel format, from IWICPixelFormatInfo
    /// </summary>
    public static int GetBitsPerPixel(IWICImagingFactory factory, Guid pixelFormat)
    {
        if (BitsPerPixelCache.TryGetValue(pixelFormat, out var cached))
        {
            return cached;
        }
        var format = pixelFormat;
        object componentInfo = null;
        try
        {
            var hResult = factory.CreateComponentInfo(ref format, out componentInfo);
            if (!hResult.Succeeded() || componentInfo is not IWICPixelFormatInfo pixelFormatInfo)
            {
                throw new ArgumentException($"{pixelFormat} is not a WIC pixel format (0x{(int)hResult:X8})", nameof(pixelFormat));
            }
            Check(pixelFormatInfo.GetBitsPerPixel(out var bitsPerPixel), "IWICPixelFormatInfo.GetBitsPerPixel");
            BitsPerPixelCache[pixelFormat] = (int)bitsPerPixel;
            return (int)bitsPerPixel;
        }
        finally
        {
            Release(componentInfo);
        }
    }

    /// <summary>
    /// The bytes of one row of pixels
    /// </summary>
    public static int RowBytes(int width, int bitsPerPixel) => checked((int)(((long)width * bitsPerPixel + 7) / 8));
}
