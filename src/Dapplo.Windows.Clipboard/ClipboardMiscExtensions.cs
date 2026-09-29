// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard
/// </summary>
public static class ClipboardMiscExtensions
{
    /// <summary>
    /// Empties the clipboard, this assumes that a lock has already been retrieved.
    /// This makes the window which was used to open the clipboard (default the SharedMessageWindow) the owner of the clipboard.
    /// </summary>
    /// <exception cref="Win32Exception">When EmptyClipboard failed</exception>
    public static void ClearContents(this IClipboardAccessToken clipboardAccessToken)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        if (!NativeMethods.EmptyClipboard())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard, see <see cref="SetDelayedRenderedContent(IClipboardAccessToken, uint)"/>
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format)
    {
        SetDelayedRenderedContent(clipboardAccessToken, (uint)format);
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard, see <see cref="SetDelayedRenderedContent(IClipboardAccessToken, uint)"/>
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="format">string with the clipboard format</param>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, string format)
    {
        SetDelayedRenderedContent(clipboardAccessToken, ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard: the format is announced, and the data is only rendered when an application requests it.
    /// Requirements: register a renderer for the format with <see cref="ClipboardNative.RegisterDelayedRenderer(uint, Action{ClipboardRenderFormatRequest})"/> first,
    /// and call <see cref="ClearContents"/> with the same access token before, so the window of the token (default the SharedMessageWindow) is the clipboard owner.
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="formatId">uint with the clipboard format</param>
    /// <exception cref="InvalidOperationException">When there is no renderer registered for the format, or when the clipboard is not owned by the window of the token</exception>
    /// <exception cref="Win32Exception">When SetClipboardData failed</exception>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, uint formatId)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        var ownerHandle = (clipboardAccessToken as ClipboardAccessToken)?.OwnerHandle ?? IntPtr.Zero;
        var isSharedMessageWindow = ownerHandle != IntPtr.Zero && ownerHandle == SharedMessageWindow.Handle;
        if (isSharedMessageWindow && !DelayedRenderers.IsRegistered(formatId))
        {
            throw new InvalidOperationException($"No delayed renderer is registered for clipboard format {formatId}, use ClipboardNative.RegisterDelayedRenderer first.");
        }
        if (ownerHandle == IntPtr.Zero || NativeMethods.GetClipboardOwner() != ownerHandle)
        {
            throw new InvalidOperationException("The clipboard is not owned by the window of the access token, call ClearContents first. Otherwise the render requests would go to another window.");
        }
        // For delayed rendering SetClipboardData returns NULL on success and on failure, so check the result via IsClipboardFormatAvailable
        NativeMethods.SetClipboardData(formatId, IntPtr.Zero);
        var error = Marshal.GetLastWin32Error();
        if (!NativeMethods.IsClipboardFormatAvailable(formatId))
        {
            throw new Win32Exception(error, $"Placing the delayed rendered clipboard format {formatId} failed: {new Win32Exception(error).Message}");
        }
        if (isSharedMessageWindow)
        {
            DelayedRenderers.MarkPending(formatId);
        }
    }
}
