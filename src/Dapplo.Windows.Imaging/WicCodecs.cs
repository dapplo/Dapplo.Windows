// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// Discovery of the codecs of the Windows Imaging Component (WIC).
/// Unlike the registry (HKCR\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}\Instance), which doesn't list the built-in codecs on newer Windows versions
/// and never lists packaged (Store) codecs like HEIF/AVIF and WebP, this asks WIC itself, which reports all of them.
/// </summary>
public static class WicCodecs
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    /// All decoders WIC reports, including packaged (Store) codecs
    /// </summary>
    /// <param name="refresh">true to let WIC refresh its list of components (WICComponentEnumerateRefresh), e.g. after a codec was installed</param>
    /// <returns>IReadOnlyList of WicCodecInfo, in the order WIC reports them</returns>
    /// <exception cref="COMException">when WIC isn't available or can't enumerate the codecs</exception>
    public static IReadOnlyList<WicCodecInfo> GetDecoders(bool refresh = false) => GetCodecs(WicCodecKind.Decoder, refresh);

    /// <summary>
    /// All encoders WIC reports, including packaged (Store) codecs
    /// </summary>
    /// <param name="refresh">true to let WIC refresh its list of components (WICComponentEnumerateRefresh), e.g. after a codec was installed</param>
    /// <returns>IReadOnlyList of WicCodecInfo, in the order WIC reports them</returns>
    /// <exception cref="COMException">when WIC isn't available or can't enumerate the codecs</exception>
    public static IReadOnlyList<WicCodecInfo> GetEncoders(bool refresh = false) => GetCodecs(WicCodecKind.Encoder, refresh);

    private static IReadOnlyList<WicCodecInfo> GetCodecs(WicCodecKind kind, bool refresh)
    {
        var componentType = kind == WicCodecKind.Decoder ? WicComponentType.Decoder : WicComponentType.Encoder;
        var options = refresh ? WicComponentEnumerateOptions.Refresh : WicComponentEnumerateOptions.Default;
        var codecs = new List<WicCodecInfo>();

        IWICImagingFactory factory = null;
        IEnumUnknown enumerator = null;
        try
        {
            factory = WicNativeMethods.CreateImagingFactory();
            var hResult = factory.CreateComponentEnumerator(componentType, options, out enumerator);
            if (!hResult.Succeeded() || enumerator == null)
            {
                throw new COMException($"Couldn't enumerate the WIC {kind.ToString().ToLowerInvariant()}s", (int)hResult);
            }

            while (true)
            {
                object item = null;
                try
                {
                    hResult = enumerator.Next(1, out item, out var fetched);
                    if (!hResult.Succeeded())
                    {
                        // Can't continue the enumeration, keep what was found so far
                        Log.Warn().WriteLine("Enumerating the WIC {0}s stopped after {1} entries: {2}", kind, codecs.Count, hResult);
                        break;
                    }
                    if (hResult != HResult.S_OK || fetched == 0 || item == null)
                    {
                        break;
                    }

                    // The cast does the QueryInterface, it gives the same runtime callable wrapper, which is released once in finally
                    if (item is not IWICBitmapCodecInfo codecInfo)
                    {
                        Log.Warn().WriteLine("A WIC {0} doesn't implement IWICBitmapCodecInfo, skipping it", kind);
                        continue;
                    }
                    codecs.Add(ReadCodecInfo(kind, codecInfo));
                }
                catch (COMException ex)
                {
                    // One broken codec must not break the list
                    Log.Warn().WriteLine(ex, "Skipping a WIC {0} which couldn't be read: 0x{1:X8}", kind, ex.ErrorCode);
                }
                finally
                {
                    Release(item);
                }
            }
        }
        finally
        {
            Release(enumerator);
            Release(factory);
        }
        return new ReadOnlyCollection<WicCodecInfo>(codecs);
    }

    private static WicCodecInfo ReadCodecInfo(WicCodecKind kind, IWICBitmapCodecInfo codecInfo)
    {
        Check(codecInfo.GetCLSID(out var clsid), "GetCLSID");
        Check(codecInfo.GetContainerFormat(out var containerFormat), "GetContainerFormat");
        Check(codecInfo.GetVendorGUID(out var vendorGuid), "GetVendorGUID");
        Check(codecInfo.GetSigningStatus(out var signingStatus), "GetSigningStatus");
        var friendlyName = ReadString(codecInfo.GetFriendlyName, nameof(codecInfo.GetFriendlyName));
        var author = ReadString(codecInfo.GetAuthor, nameof(codecInfo.GetAuthor));
        var version = ReadString(codecInfo.GetVersion, nameof(codecInfo.GetVersion));
        var fileExtensions = SplitList(ReadString(codecInfo.GetFileExtensions, nameof(codecInfo.GetFileExtensions)), true);
        var mimeTypes = SplitList(ReadString(codecInfo.GetMimeTypes, nameof(codecInfo.GetMimeTypes)), false);
        return new WicCodecInfo(kind, clsid, containerFormat, vendorGuid, friendlyName, author, version, fileExtensions, mimeTypes, (WicSigningStatus)signingStatus);
    }

    /// <summary>
    /// The signature of the WIC string methods: buffer size in characters, buffer, actual size in characters (including the terminating 0)
    /// </summary>
    private delegate HResult WicStringGetter(uint cch, nint buffer, out uint cchActual);

    /// <summary>
    /// Read a string the WIC way: call once with cch = 0 and a null buffer to get the length, then allocate and call again
    /// </summary>
    private static unsafe string ReadString(WicStringGetter getter, string methodName)
    {
        Check(getter(0, IntPtr.Zero, out var length), methodName);
        if (length == 0)
        {
            return string.Empty;
        }
        var buffer = new char[length];
        uint actual;
        fixed (char* bufferPointer = buffer)
        {
            Check(getter(length, (nint)bufferPointer, out actual), methodName);
        }
        var count = (int)Math.Min(actual, length);
        var end = Array.IndexOf(buffer, '\0', 0, count);
        return new string(buffer, 0, end < 0 ? count : end);
    }

    /// <summary>
    /// Split the comma separated list WIC returns, trim and lower case the entries, drop empty entries and duplicates
    /// </summary>
    /// <param name="value">string with e.g. ".heic,.heif" or "image/heic, image/heif"</param>
    /// <param name="isExtension">true to make sure every entry starts with a dot</param>
    /// <returns>IReadOnlyList of string</returns>
    private static IReadOnlyList<string> SplitList(string value, bool isExtension)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var part in value.Split(','))
        {
            var entry = part.Trim().ToLowerInvariant();
            if (entry.Length == 0)
            {
                continue;
            }
            if (isExtension && entry[0] != '.')
            {
                entry = "." + entry;
            }
            if (entry.Length > (isExtension ? 1 : 0) && seen.Add(entry))
            {
                result.Add(entry);
            }
        }
        return new ReadOnlyCollection<string>(result);
    }

    private static void Check(HResult hResult, string methodName)
    {
        if (!hResult.Succeeded())
        {
            throw new COMException($"IWICBitmapCodecInfo.{methodName} failed", (int)hResult);
        }
    }

    private static void Release(object comObject)
    {
        if (comObject != null && Marshal.IsComObject(comObject))
        {
            Marshal.ReleaseComObject(comObject);
        }
    }
}
