// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
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
    }
}
