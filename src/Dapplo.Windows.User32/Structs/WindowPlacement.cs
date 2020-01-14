// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.TypeConverters;

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     Contains information about the placement of a window on the screen.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [TypeConverter(typeof(WindowPlacementTypeConverter))]
    public struct WindowPlacement
    {
        /// <summary>
        ///     The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
        ///     <para>
        ///         GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
        ///     </para>
        /// </summary>
        private int _cbSize;
        private WindowPlacementFlags _flags;
        private ShowWindowCommands _showCmd;
        private NativePoint _minPosition;
        private NativePoint _maxPosition;
        private NativeRect _normalPosition;

        /// <summary>
        ///     Specifies flags that control the position of the minimized window and the method by which the window is restored.
        /// </summary>
        public WindowPlacementFlags Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        ///     The current show state of the window.
        /// </summary>
        public ShowWindowCommands ShowCmd
        {
            get { return _showCmd; }
            set { _showCmd = value; }
        }

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is minimized.
        /// </summary>
        public NativePoint MinPosition
        {
            get { return _minPosition; }
            set { _minPosition = value; }
        }

        /// <summary>
        ///     The coordinates of the window's upper-left corner when the window is maximized.
        /// </summary>
        public NativePoint MaxPosition
        {
            get { return _maxPosition; }
            set { _maxPosition = value; }
        }

        /// <summary>
        ///     The window's coordinates when the window is in the restored position.
        /// </summary>
        public NativeRect NormalPosition
        {
            get { return _normalPosition; }
            set { _normalPosition = value; }
        }

        /// <summary>
        ///     Gets the default (empty) value.
        /// </summary>
        public static WindowPlacement Create()
        {
            return new WindowPlacement
            {
                _cbSize = Marshal.SizeOf(typeof(WindowPlacement))
            };
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{Flags: {Flags}; ShowCmd: {ShowCmd}; MinPosition: {MinPosition}; MaxPosition: {MaxPosition}; NormalPosition: {NormalPosition}}}";
        }
    }
}