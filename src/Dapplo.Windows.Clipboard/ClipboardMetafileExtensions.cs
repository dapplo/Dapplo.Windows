// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read enhanced metafiles (vector graphics, e.g. copied from Office or Visio) from the clipboard
/// </summary>
public static class ClipboardMetafileExtensions
{
    /// <summary>
    /// Read CF_ENHMETAFILE as the bytes of an EMF file (GetEnhMetaFileBits), which can be saved as .emf or rendered with an imaging library.
    /// The metafile handle belongs to the clipboard and is not deleted.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken of the open clipboard</param>
    /// <param name="emfBytes">byte array with the EMF data</param>
    /// <returns>true when CF_ENHMETAFILE is available</returns>
    public static bool TryGetEnhancedMetafileBits(this IClipboardAccessToken clipboardAccessToken, out byte[] emfBytes)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        emfBytes = null;
        var hEnhMetaFile = Internals.NativeMethods.GetClipboardData((uint)StandardClipboardFormats.EnhancedMetafile);
        if (hEnhMetaFile == IntPtr.Zero)
        {
            return false;
        }
        var size = GetEnhMetaFileBits(hEnhMetaFile, 0, null);
        if (size == 0)
        {
            return false;
        }
        var bytes = new byte[size];
        if (GetEnhMetaFileBits(hEnhMetaFile, size, bytes) != size)
        {
            return false;
        }
        emfBytes = bytes;
        return true;
    }

    [DllImport("gdi32", SetLastError = true)]
    private static extern uint GetEnhMetaFileBits(IntPtr hEmf, uint nSize, [Out] byte[] lpbBuffer);
}
