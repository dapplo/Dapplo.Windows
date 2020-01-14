// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Information about what the clipboard contained at the most recent clipboard update.
    /// </summary>
    public class ClipboardUpdateInformation
    {
        /// <summary>
        /// Sequence-number of the clipboard, starts at 0 when the Windows session starts
        /// </summary>
        public uint Id { get; } = ClipboardNative.SequenceNumber;

        /// <summary>
        /// Timestamp of the clipboard update event, this value will not be correct for the first event
        /// </summary>
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

        /// <summary>
        /// The handle of the window which owns the clipboard content
        /// </summary>
        public IntPtr OwnerHandle { get; } = ClipboardNative.CurrentOwner;

        /// <summary>
        /// The formats in this clipboard contents
        /// </summary>
        public IEnumerable<string> Formats => FormatIds
            .Select(ClipboardFormatExtensions.MapIdToFormat)
            .Where(format => !string.IsNullOrEmpty(format));

        /// <summary>
        /// The formats in this clipboard contents
        /// </summary>
        public IEnumerable<uint> FormatIds { get; }

        /// <summary>
        /// This class can only be instanciated when there is a clipboard lock, that is why the constructor is private.
        /// </summary>
        private ClipboardUpdateInformation(IClipboardAccessToken clipboardAccessToken)
        {
            FormatIds = clipboardAccessToken.AvailableFormatIds().ToList();
        }

        /// <summary>
        /// Factory method
        /// </summary>
        /// <param name="hWnd">IntPtr, optional, with the hWnd for the clipboard lock</param>
        /// <returns>ClipboardUpdateInformation</returns>
        public static ClipboardUpdateInformation Create(IntPtr hWnd = default)
        {
#if !NETSTANDARD2_0
            if (hWnd == IntPtr.Zero)
            {
                hWnd = WinProcHandler.Instance.Handle;
            }
#endif
            using (var clipboard = ClipboardNative.Access(hWnd))
            {
                return new ClipboardUpdateInformation(clipboard);
            }
        }
    }
}
