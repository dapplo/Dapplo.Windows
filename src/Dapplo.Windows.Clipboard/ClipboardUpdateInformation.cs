//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

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
