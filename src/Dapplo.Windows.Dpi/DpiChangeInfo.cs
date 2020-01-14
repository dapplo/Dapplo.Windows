// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Dpi
{
    /// <summary>
    /// Stores information about a DPI change
    /// </summary>
    public class DpiChangeInfo
    {
        /// <summary>
        /// The DPI from before the change
        /// </summary>
        public uint PreviousDpi { get; }

        /// <summary>
        /// The new DPI
        /// </summary>
        public uint NewDpi { get; }

        /// <summary>
        /// Creates a DpiChangeInfo
        /// </summary>
        /// <param name="previousDpi">uint</param>
        /// <param name="newDpi">uint</param>
        public DpiChangeInfo(uint previousDpi, uint newDpi)
        {
            PreviousDpi = previousDpi;
            NewDpi = newDpi;
        }
    }
}
