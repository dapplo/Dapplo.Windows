// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// This interface is returned by the ClipboardNative.Access(), which calls the ClipboardLockProvider.
    /// The access token is only valid within the same thread or window.
    /// When you got a IClipboardAccessToken, you can access the clipboard, until it's disposed.
    /// Don't forget to dispose this!!!
    /// </summary>
    public interface IClipboardAccessToken : IDisposable
    {
        /// <summary>
        /// Check if the clipboard can be accessed
        /// </summary>
        bool CanAccess { get; }

        /// <summary>
        /// The clipboard access was denied due to a timeout
        /// </summary>
        bool IsLockTimeout { get; }

        /// <summary>
        /// The clipboard couldn't be opened
        /// </summary>
        bool IsOpenTimeout { get; }

        /// <summary>
        /// This throws a ClipboardAccessDeniedException when the clipboard can't be accessed
        /// </summary>
        void ThrowWhenNoAccess();
    }
}
