// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about a low-level keyboard input event.
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644967(v=vs.85).aspx">KBDLLHOOKSTRUCT structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct KeyboardLowLevelHookStruct
    {
        private uint _vkCode;
        private uint _scanCode;
        private ExtendedKeyFlags _flags;
        private uint _time;
        private UIntPtr _dwExtraInfo;

        /// <summary>
        ///     A virtual-key code. The code must be a value in the range 1 to 254.
        /// </summary>
        public VirtualKeyCode VirtualKeyCode
        {
            get { return (VirtualKeyCode)_vkCode; }
            set { _vkCode = (uint)value; }
        }

        /// <summary>
        ///     A hardware scan code for the key.
        /// </summary>
        public ScanCodes ScanCode
        {
            get { return (ScanCodes)_scanCode; }
            set { _scanCode = (uint)value; }
        }

        /// <summary>
        ///     The extended-key flag, event-injected flags, context code, and transition-state flag.
        ///     This member is specified as follows. An application can use the following values to test the keystroke flags.
        ///     Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected.
        ///     If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event
        ///     was injected from a process running at lower integrity level.
        /// </summary>
        public ExtendedKeyFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        ///     The time stamp for this message, equivalent to what GetMessageTime would return for this message.
        /// </summary>
        public uint TimeStamp
        {
            get { return _time; }
            set { _time = value; }
        }

        /// <summary>
        ///     Additional information associated with the message.
        /// </summary>
        public UIntPtr ExtraInfo
        {
            get { return _dwExtraInfo; }
            set { _dwExtraInfo = value; }
        }
    }
}