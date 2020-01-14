// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
