// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about the state of the mouse.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645578.aspx">RAWMOUSE structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawMouse
    {
        [FieldOffset(0)] private readonly MouseStates _usFlags;
        // reserved
        [FieldOffset(4)] private readonly uint _ulButtons;
        [FieldOffset(4)] private readonly MouseButtonStates _usButtonFlags;
        [FieldOffset(6)] private readonly short _usButtonData;
        [FieldOffset(8)] private readonly uint _ulRawButtons;
        [FieldOffset(12)] private readonly int _lLastX;
        [FieldOffset(16)] private readonly int _lLastY;
        [FieldOffset(20)] private readonly uint _ulExtraInformation;

        /// <summary>
        /// The mouse state.
        /// </summary>
        public MouseStates State
        {
            get { return _usFlags; }
        }

        /// <summary>
        /// The button state
        /// </summary>
        public MouseButtonStates ButtonState
        {
            get { return _usButtonFlags; }
        }

        /// <summary>
        /// If usButtonFlags is RI_MOUSE_WHEEL, this member is a signed value that specifies the wheel delta.
        /// </summary>
        public short WheelData
        {
            get { return _usButtonData; }
        }

        /// <summary>
        /// The motion in the X direction.
        /// This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        public int X
        {
            get { return _lLastX; }
        }

        /// <summary>
        /// The motion in the Y direction.
        /// This is signed relative motion or absolute motion, depending on the value of usFlags.
        /// </summary>
        public int Y
        {
            get { return _lLastY; }
        }
    }
}