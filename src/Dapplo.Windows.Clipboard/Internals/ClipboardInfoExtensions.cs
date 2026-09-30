// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;

namespace Dapplo.Windows.Clipboard.Internals;

internal static class ClipboardInfoExtensions
{
    /// <summary>
    /// Throw an InvalidOperationException when the clipboard content is not owned by the window which opened the clipboard (the window of the token).
    /// This is reliable: while the clipboard is open, no other application can call EmptyClipboard, so the owner can't change.
    /// During delayed rendering the SharedMessageWindow is the owner, so the tokens of render requests pass.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <exception cref="InvalidOperationException">When the content belongs to another window (or to nobody)</exception>
    public static void ThrowWhenNotOwner(this IClipboardAccessToken clipboardAccessToken)
    {
        // Only our own tokens know the window which opened the clipboard
        if (clipboardAccessToken is not ClipboardAccessToken { OwnerHandle: var ownerHandle } || ownerHandle == IntPtr.Zero)
        {
            return;
        }
        var currentOwner = NativeMethods.GetClipboardOwner();
        if (currentOwner == ownerHandle)
        {
            return;
        }
        throw new InvalidOperationException(
            $"The clipboard content belongs to another window (0x{currentOwner.ToInt64():X}), not to the window of the access token (0x{ownerHandle.ToInt64():X}): " +
            "writing now would mix your format into the content of another application. Use ReplaceContents, or call ClearContents first.");
    }

    /// <summary>
    /// Try to create ClipboardNativeInfo to read
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <param name="formatId">uint</param>
    /// <param name="readInfo">ClipboardNativeInfo output parameter</param>
    /// <returns>true if the format can be read, false otherwise</returns>
    public static bool TryReadInfo(this IClipboardAccessToken clipboardAccessToken, uint formatId, out ClipboardNativeInfo readInfo)
    {
        readInfo = null;

        if (!clipboardAccessToken.CanAccess)
        {
            return false;
        }

        var hGlobal = NativeMethods.GetClipboardData(formatId);
        if (hGlobal == IntPtr.Zero)
        {
            return false;
        }

        var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
        if (memoryPtr == IntPtr.Zero)
        {
            return false;
        }

        readInfo = new ClipboardNativeInfo
        {
            GlobalHandle = hGlobal,
            MemoryPtr = memoryPtr,
            FormatId = formatId
        };

        return true;
    }

    /// <summary>
    /// Create ClipboardNativeInfo to read
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <param name="formatId">uint</param>
    /// <returns>ClipboardNativeInfo</returns>
    public static ClipboardNativeInfo ReadInfo(this IClipboardAccessToken clipboardAccessToken, uint formatId)
    {
        clipboardAccessToken.ThrowWhenNoAccess();

        var hGlobal = NativeMethods.GetClipboardData(formatId);
        if (hGlobal == IntPtr.Zero)
        {
            // Capture the error before any other call can overwrite it
            var error = Marshal.GetLastWin32Error();
            if (!NativeMethods.IsClipboardFormatAvailable(formatId))
            {
                throw new Win32Exception(error, $"Clipboard format {formatId} is not available.");
            }
            throw new Win32Exception(error, $"Clipboard format {formatId} is available, but retrieving it failed (e.g. delayed rendering failed): {new Win32Exception(error).Message}");
        }
        var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
        if (memoryPtr == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new ClipboardNativeInfo
        {
            GlobalHandle = hGlobal,
            MemoryPtr = memoryPtr,
            FormatId = formatId
        };
    }

    /// <summary>
    /// Factory for the write information, call Commit on the result after the memory was written, and dispose it.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <param name="formatId">uint with the format id</param>
    /// <param name="size">int with the size of the clipboard area</param>
    /// <returns>ClipboardNativeInfo</returns>
    public static ClipboardNativeInfo WriteInfo(this IClipboardAccessToken clipboardAccessToken, uint formatId, long size)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        clipboardAccessToken.ThrowWhenNotOwner();

        var hGlobal = Kernel32Api.GlobalAlloc(GlobalMemorySettings.ZeroInit | GlobalMemorySettings.Movable, new UIntPtr((ulong)size));
        if (hGlobal == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        var memoryPtr = Kernel32Api.GlobalLock(hGlobal);
        if (memoryPtr == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.GlobalFree(hGlobal);
            throw new Win32Exception(error);
        }

        return new ClipboardNativeInfo
        {
            GlobalHandle = hGlobal,
            MemoryPtr = memoryPtr,
            NeedsWrite = true,
            FormatId = formatId
        };
    }
}
