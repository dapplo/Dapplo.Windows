// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs
{
    /// <summary>
    ///     This struct defines the raw input data coming from the specified keyboard.
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645587.aspx">RID_DEVICE_INFO_KEYBOARD structure</a>
    /// Remarks:
    /// For the keyboard, the Usage Page is 1 and the Usage is 6.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ArrangeAccessorOwnerBody")]
    public struct RawInputDeviceInfoKeyboard
    {
        private readonly int _dwType;
        private readonly int _dwSubType;
        private readonly int _dwKeyboardMode;
        private readonly int _dwNumberOfFunctionKeys;
        private readonly int _dwNumberOfIndicators;
        private readonly int _dwNumberOfKeysTotal;

        /// <summary>
        /// The type of the keyboard
        /// </summary>
        public int Type => _dwType;

        /// <summary>
        /// The type of the keyboard
        /// </summary>
        public int SubType => _dwSubType;

        /// <summary>
        /// The scan code mode.
        /// </summary>
        public int KeyboardMode => _dwKeyboardMode;

        /// <summary>
        /// The number of function keys on the keyboard.
        /// </summary>
        public int NumberOfFunctionKeys => _dwNumberOfFunctionKeys;

        /// <summary>
        /// The number of LED indicators on the keyboard.
        /// </summary>
        public int NumberOfIndicators => _dwNumberOfIndicators;

        /// <summary>
        /// The total number of keys on the keyboard.
        /// </summary>
        public int NumberOfKeysTotal => _dwNumberOfKeysTotal;

    }
}