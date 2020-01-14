// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     The structure for the TITLEBARINFOEX
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969233(v=vs.85).aspx">TITLEBARINFOEX struct</a>
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Sonar Code Smell", "S1450:Trivial properties should be auto-implementedPrivate fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    public struct TitleBarInfoEx
    {
        /// <summary>
        ///     The size of the structure, in bytes.
        ///     The caller must set this member to sizeof(TITLEBARINFOEX).
        /// </summary>
        private uint _cbSize;

        private NativeRect _rcTitleBar;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private ObjectStates[] _rgState;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private NativeRect[] _rgRect;

        /// <summary>
        /// The coordinates of the title bar. These coordinates include all title-bar elements except the window menu.
        /// </summary>
        public NativeRect Bounds => _rcTitleBar;

        /// <summary>
        /// Returns the ObjectState of the specified element
        /// </summary>
        /// <param name="titleBarInfoIndex">TitleBarInfoIndexes used to specify the element</param>
        /// <returns>ObjectStates</returns>
        public ObjectStates ElementState(TitleBarInfoIndexes titleBarInfoIndex) => _rgState[(int)titleBarInfoIndex];

        /// <summary>
        /// Returns the Bounds of the specified element
        /// </summary>
        /// <param name="titleBarInfoIndex">TitleBarInfoIndexes used to specify the element</param>
        /// <returns>RECT</returns>
        public NativeRect ElementBounds(TitleBarInfoIndexes titleBarInfoIndex) => _rgRect[(int)titleBarInfoIndex];

        /// <summary>
        ///     Factory method for a default TitleBarInfoEx.
        /// </summary>
        public static TitleBarInfoEx Create()
        {
            return new TitleBarInfoEx
            {
                _cbSize = (uint) Marshal.SizeOf(typeof(TitleBarInfoEx)),
                _rgState = new ObjectStates[6],
                _rgRect = new NativeRect[6],
                _rcTitleBar = NativeRect.Empty
            };
        }
    }
}