// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd162869(v=vs.85).aspx">PrintWindow function</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum PrintWindowFlags : uint
    {
        /// <summary>
        ///     Copy the complete window
        /// </summary>
        PW_COMPLETE = 0x00000000,

        /// <summary>
        ///     Only the client area of the window is copied.
        ///     By default, the entire window is copied.
        /// </summary>
        PW_CLIENTONLY = 0x00000001,

        /// <summary>
        ///     Works on windows that use DirectX or DirectComposition
        /// </summary>
        PW_RENDERFULLCONTENT = 0x00000002
    }
}