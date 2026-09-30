// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Kernel32;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read a <see cref="ClipboardSnapshot"/> with an open clipboard
/// </summary>
public static class ClipboardSnapshotExtensions
{
    /// <summary>
    /// Copy the formats of the open clipboard into memory.
    /// </summary>
    /// <remarks>
    /// Only formats which are stored in global memory (HGLOBAL) can be copied: handle formats (CF_BITMAP, CF_METAFILEPICT, CF_PALETTE,
    /// CF_ENHMETAFILE, the display and private formats and CF_GDIOBJ*) are skipped. Windows synthesizes CF_DIB / CF_DIBV5 from CF_BITMAP
    /// and CF_TEXT from CF_UNICODETEXT, those are copied. Reading a format makes the application which copied render it when it uses
    /// delayed rendering, so pass only the formats you need.
    /// </remarks>
    /// <param name="clipboardAccessToken">IClipboardAccessToken of the open clipboard</param>
    /// <param name="formats">The format names to read, null reads all available formats (except handle formats)</param>
    /// <param name="maxBytesPerFormat">Formats with more bytes are skipped, default no limit</param>
    /// <returns>ClipboardSnapshot</returns>
    public static ClipboardSnapshot ReadSnapshot(this IClipboardAccessToken clipboardAccessToken, IEnumerable<string> formats = null, long maxBytesPerFormat = long.MaxValue)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        if (maxBytesPerFormat < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytesPerFormat), maxBytesPerFormat, "The size limit must not be negative.");
        }

        var sequenceNumber = NativeMethods.GetClipboardSequenceNumber();
        var owner = NativeMethods.GetClipboardOwner();
        var availableIds = clipboardAccessToken.AvailableFormatIds().ToList();

        List<uint> formatIds;
        if (formats == null)
        {
            formatIds = availableIds;
        }
        else
        {
            // Keep the order of the clipboard, which is the preference of the copying application
            var requested = new HashSet<uint>(formats.Where(f => !string.IsNullOrEmpty(f)).Select(ClipboardFormatExtensions.MapFormatToId));
            formatIds = availableIds.Where(requested.Contains).ToList();
        }

        var names = new List<string>();
        var data = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var skipped = new List<string>();
        foreach (var formatId in formatIds)
        {
            var name = ClipboardFormatExtensions.MapIdToFormat(formatId) ?? $"#{formatId}";
            if (data.ContainsKey(name))
            {
                continue;
            }
            var bytes = IsHandleFormat(formatId) ? null : TryRead(formatId, maxBytesPerFormat);
            if (bytes == null)
            {
                skipped.Add(name);
                continue;
            }
            names.Add(name);
            data[name] = bytes;
        }
        return new ClipboardSnapshot(sequenceNumber, owner, names, data, skipped);
    }

    /// <summary>
    /// Formats whose clipboard data is a GDI handle or unknown, not global memory
    /// </summary>
    internal static bool IsHandleFormat(uint formatId)
    {
        switch ((StandardClipboardFormats)formatId)
        {
            case StandardClipboardFormats.Bitmap:
            case StandardClipboardFormats.MetafilePicture:
            case StandardClipboardFormats.Palette:
            case StandardClipboardFormats.EnhancedMetafile:
            case StandardClipboardFormats.OwnerDisplay:
            case StandardClipboardFormats.DisplayBitmap:
            case StandardClipboardFormats.DisplayMetafilePicture:
            case StandardClipboardFormats.DisplayEnhancedMetafile:
                return true;
        }
        // CF_PRIVATEFIRST - CF_PRIVATELAST and CF_GDIOBJFIRST - CF_GDIOBJLAST
        return formatId >= 0x0200 && formatId <= 0x03FF;
    }

    private static byte[] TryRead(uint formatId, long maxBytes)
    {
        var hGlobal = NativeMethods.GetClipboardData(formatId);
        if (hGlobal == IntPtr.Zero)
        {
            return null;
        }
        var size = (long)Kernel32Api.GlobalSize(hGlobal).ToUInt64();
        if (size > maxBytes || size > int.MaxValue)
        {
            return null;
        }
        var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
        if (memoryPtr == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            var bytes = new byte[size];
            if (size > 0)
            {
                Marshal.Copy(memoryPtr, bytes, 0, (int)size);
            }
            return bytes;
        }
        finally
        {
            Kernel32Api.GlobalUnlock(hGlobal);
        }
    }
}
