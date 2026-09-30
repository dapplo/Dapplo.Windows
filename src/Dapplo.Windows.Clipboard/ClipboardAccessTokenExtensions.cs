// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Extensions for <see cref="IClipboardAccessToken"/>
/// </summary>
public static class ClipboardAccessTokenExtensions
{
    /// <summary>
    /// The application which kept the clipboard open when the token couldn't open it (<see cref="IClipboardAccessToken.IsOpenTimeout"/>),
    /// to show to the user: the file name of its executable (e.g. "notepad.exe"), else the process name, else the title of
    /// <see cref="IClipboardAccessToken.BlockingWindow"/>; null when unknown or when the clipboard was opened.
    /// This is the same value as <see cref="ClipboardAccessDeniedException.BlockingProcessName"/>.
    /// </summary>
    /// <remarks>
    /// For the tokens of <see cref="ClipboardNative.Access"/> and <see cref="ClipboardNative.AccessAsync"/> the name was determined when
    /// opening failed, so this doesn't query the process again. For other implementations of <see cref="IClipboardAccessToken"/> it's
    /// determined now from <see cref="IClipboardAccessToken.BlockingProcessId"/> and <see cref="IClipboardAccessToken.BlockingWindow"/>.
    /// </remarks>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <returns>string or null</returns>
    public static string GetBlockingProcessName(this IClipboardAccessToken clipboardAccessToken)
    {
        if (clipboardAccessToken == null)
        {
            throw new ArgumentNullException(nameof(clipboardAccessToken));
        }
        if (clipboardAccessToken is ClipboardAccessToken token)
        {
            return token.BlockingProcessName;
        }
        if (clipboardAccessToken.BlockingProcessId == 0 && clipboardAccessToken.BlockingWindow == IntPtr.Zero)
        {
            return null;
        }
        new ClipboardBlocker(clipboardAccessToken.BlockingWindow, clipboardAccessToken.BlockingProcessId).Describe(out var processName);
        return processName;
    }
}
