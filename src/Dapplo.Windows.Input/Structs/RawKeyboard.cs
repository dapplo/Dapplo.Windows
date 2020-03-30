// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Messages.Enumerations;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     Contains information about the state of the keyboard.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645575.aspx">RAWKEYBOARD structure</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawKeyboard
    {
        // Scan code from the key depression
        private readonly ushort _makecode;
        // One or more of RI_KEY_MAKE, RI_KEY_BREAK, RI_KEY_E0, RI_KEY_E1
        private readonly RawKeyboardFlags _flags;
        // Always 0
        private readonly ushort _reserved;
        // Virtual Key Code
        private readonly VirtualKeyCode _vkey;
        // Corresponding Windows message for exmaple (WM_KEYDOWN, WM_SYASKEYDOWN etc)
        private readonly WindowsMessages _message;
        // The device-specific addition information for the event (seems to always be zero for keyboards)
        private readonly uint _extraInformation;

        /// <summary>
        /// The virtual key code
        /// </summary>
        public VirtualKeyCode VirtualKey
        {
            get { return _vkey; }
        }

        /// <summary>
        /// Scan code flags
        /// </summary>
        public RawKeyboardFlags Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// The scan code
        /// </summary>
        public ushort ScanCode
        {
            get { return _makecode; }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("Rawkeyboard\n Makecode: {0}\n Makecode(hex) : {0:X}\n Flags: {1}\n Reserved: {2}\n VKeyName: {3}\n Message: {4}\n ExtraInformation {5}\n",
                ScanCode, Flags, _reserved, _vkey, _message, _extraInformation);
        }
    }
}