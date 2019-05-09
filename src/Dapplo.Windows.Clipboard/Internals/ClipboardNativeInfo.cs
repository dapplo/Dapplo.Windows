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
using Dapplo.Windows.Kernel32;

namespace Dapplo.Windows.Clipboard.Internals
{
    /// <summary>
    /// This class contains native information to handle the clipboard contents
    /// </summary>
    internal class ClipboardNativeInfo : IDisposable
    {
        internal IntPtr GlobalHandle { get; set; }
        internal bool NeedsWrite { get; set; }

        /// <summary>
        /// The format id which is processed
        /// </summary>
        internal uint FormatId { get; set; }
        internal IntPtr MemoryPtr { get; set; }

        /// <summary>
        /// Returns the size of the clipboard area
        /// </summary>
        internal int Size => Kernel32Api.GlobalSize(GlobalHandle);

        /// <summary>
        /// Cleanup this native info by unlocking the global handle
        /// </summary>
        public void Dispose()
        {
            Kernel32Api.GlobalUnlock(GlobalHandle);
            if (NeedsWrite)
            {
                // Place the content on the clipboard
                NativeMethods.SetClipboardData(FormatId, GlobalHandle);
            }
        }
    }
}
