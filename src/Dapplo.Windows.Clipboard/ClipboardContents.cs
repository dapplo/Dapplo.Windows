//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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
using System.IO;
using System.Linq;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// Content of the Clipboards
    /// </summary>
    public sealed class ClipboardContents
    {
        // Used to identify every clipboard change
        private static uint _globalSequenceNumber;

        // Contents of the clipboard
        private readonly IDictionary<string, Lazy<MemoryStream>> _contents = new Dictionary<string, Lazy<MemoryStream>>();

        /// <summary>
        /// A unique ID, given as sequence.
        /// If this number doesn't match, with the global counter, the clipboard content already changed.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Timestamp of the clipboard update event
        /// </summary>
        public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

        /// <summary>
        /// The handle of the window which owns the clipboard content
        /// </summary>
        public IntPtr OwnerHandle { get; }

        /// <summary>
        /// The formats in this clipboard contents
        /// </summary>
        public IEnumerable<string> Formats { get; }

        /// <summary>
        /// This initializes some properties
        /// </summary>
        public ClipboardContents(IntPtr hWnd)
        {
            Id = ++_globalSequenceNumber;
            // Try to get the real one.
            var windowsSequenceNumber = ClipboardNative.SequenceNumber;
            if (windowsSequenceNumber > 0)
            {
                Id = windowsSequenceNumber;
            }
            OwnerHandle = ClipboardNative.CurrentOwner;

            Formats = ClipboardNative.AvailableFormats(hWnd).ToList();

            // Create Lazy for all available formats.
            foreach (var format in Formats)
            {
                _contents[format] = new Lazy<MemoryStream>(() =>
                {
                    using (ClipboardNative.Lock(hWnd))
                    {
                        return ClipboardNative.GetAsStream(format);
                    }
                });
            }
        }

        /// <summary>
        /// Returns the content for a format
        /// </summary>
        /// <param name="format"></param>
        /// <returns>MemoryStream</returns>
        public MemoryStream this[string format] => _contents[format].Value;
    }
}
