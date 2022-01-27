// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard
/// </summary>
public static class ClipboardMiscExtensions
{
    /// <summary>
    /// Empties the clipboard, this assumes that a lock has already been retrieved.
    /// </summary>
    public static void ClearContents(this IClipboardAccessToken clipboardAccessToken)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        NativeMethods.EmptyClipboard();
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format)
    {
        SetDelayedRenderedContent(clipboardAccessToken, (uint)format);
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard, don't forget to subscribe to ClipboardNative.OnRenderFormat
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="format">string with the clipboard format</param>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, string format)
    {
        SetDelayedRenderedContent(clipboardAccessToken, ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// This places delayed rendered content on the clipboard
    /// </summary>
    /// <param name="clipboardAccessToken">The IClipboardAccessToken</param>
    /// <param name="formatId">uint with the clipboard format</param>
    public static void SetDelayedRenderedContent(this IClipboardAccessToken clipboardAccessToken, uint formatId)
    {
        clipboardAccessToken.ThrowWhenNoAccess();
        NativeMethods.SetClipboardDataWithErrorHandling(formatId, IntPtr.Zero);
    }
}