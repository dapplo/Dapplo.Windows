// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Places a complete <see cref="ClipboardContents"/> on the clipboard with an opened <see cref="IClipboardAccessToken"/>.
/// </summary>
public static class ClipboardContentsExtensions
{
    /// <summary>
    /// Replace the content of the clipboard: clears the clipboard (the window of the token becomes the owner) and places all formats of the contents.
    /// If placing a format fails, the clipboard is cleared again and the exception is rethrown: other applications never see half of the content.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken of an opened clipboard</param>
    /// <param name="contents">ClipboardContents with all formats</param>
    /// <exception cref="ClipboardAccessDeniedException">When the token has no access</exception>
    public static void ReplaceContents(this IClipboardAccessToken clipboardAccessToken, ClipboardContents contents)
    {
        if (clipboardAccessToken == null)
        {
            throw new ArgumentNullException(nameof(clipboardAccessToken));
        }
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }
        clipboardAccessToken.ThrowWhenNoAccess();
        clipboardAccessToken.ClearContents();
        try
        {
            contents.PlaceOn(clipboardAccessToken);
        }
        catch
        {
            // Don't leave partial content on the clipboard
            NativeMethods.EmptyClipboard();
            throw;
        }
    }

    /// <summary>
    /// Add the formats of the contents to the current content of the clipboard, without clearing it.
    /// This is only allowed when the clipboard content was placed by the window of the token (e.g. with <see cref="ReplaceContents"/> or ClearContents before),
    /// adding formats to the content of another application would mix two unrelated contents.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken of an opened clipboard</param>
    /// <param name="contents">ClipboardContents with the formats to add, formats which are already on the clipboard are replaced</param>
    /// <exception cref="ClipboardAccessDeniedException">When the token has no access</exception>
    /// <exception cref="InvalidOperationException">When the clipboard content is not owned by the window of the token</exception>
    public static void AddToCurrentContents(this IClipboardAccessToken clipboardAccessToken, ClipboardContents contents)
    {
        if (clipboardAccessToken == null)
        {
            throw new ArgumentNullException(nameof(clipboardAccessToken));
        }
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }
        clipboardAccessToken.ThrowWhenNoAccess();
        clipboardAccessToken.ThrowWhenNotOwner();
        contents.PlaceOn(clipboardAccessToken);
    }
}
