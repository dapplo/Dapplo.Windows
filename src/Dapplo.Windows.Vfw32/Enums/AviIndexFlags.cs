// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Vfw32.Enums
{
    /// <summary>
    /// Avi index flags (AVIIF) returned by ICCompress
    /// </summary>
    [Flags]
    public enum AviIndexFlags : uint
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,

        /// <summary>
        /// Key frame.
        /// </summary>
        KeyFrame = 0x00000001
    }
}
