//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    ///     The type of hook procedure to be installed via the SetWindowsHookEx function. This parameter can be one of the
    ///     following values:
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HookTypes
    {
        /// <summary>
        ///     Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box, message
        ///     box, menu, or scroll bar. For more information, see the MessageProc hook procedure.
        /// </summary>
        WH_MSGFILTER = -1,

        /// <summary>
        ///     Installs a hook procedure that records input messages posted to the system message queue. This hook is useful for
        ///     recording macros. For more information, see the JournalRecordProc hook procedure.
        /// </summary>
        WH_JOURNALRECORD = 0,

        /// <summary>
        ///     Installs a hook procedure that posts messages previously recorded by a WH_JOURNALRECORD hook procedure. For more
        ///     information, see the JournalPlaybackProc hook procedure.
        /// </summary>
        WH_JOURNALPLAYBACK = 1,

        /// <summary>
        ///     Installs a hook procedure that monitors keystroke messages. For more information, see the KeyboardProc hook
        ///     procedure.
        /// </summary>
        WH_KEYBOARD = 2,

        /// <summary>
        ///     Installs a hook procedure that monitors messages posted to a message queue. For more information, see the
        ///     GetMsgProc hook procedure.
        /// </summary>
        WH_GETMESSAGE = 3,

        /// <summary>
        ///     Installs a hook procedure that monitors messages before the system sends them to the destination window procedure.
        ///     For more information, see the CallWndProc hook procedure.
        /// </summary>
        WH_CALLWNDPROC = 4,

        /// <summary>
        ///     Installs a hook procedure that receives notifications useful to a CBT application. For more information, see the
        ///     CBTProc hook procedure.
        /// </summary>
        WH_CBT = 5,

        /// <summary>
        ///     Installs a hook procedure that monitors messages generated as a result of an input event in a dialog box, message
        ///     box, menu, or scroll bar. The hook procedure monitors these messages for all applications in the same desktop as
        ///     the calling thread. For more information, see the SysMsgProc hook procedure.
        /// </summary>
        WH_SYSMSGFILTER = 6,

        /// <summary>
        ///     Installs a hook procedure that monitors mouse messages. For more information, see the MouseProc hook procedure.
        /// </summary>
        WH_MOUSE = 7,

        /// <summary>
        ///     Installs a hook procedure that monitors hardware messages. For more information, see the HardwareProc hook
        ///     procedure.
        /// </summary>
        WH_HARDWARE = 8,

        /// <summary>
        ///     Installs a hook procedure useful for debugging other hook procedures. For more information, see the DebugProc hook
        ///     procedure.
        /// </summary>
        WH_DEBUG = 9,

        /// <summary>
        ///     Installs a hook procedure that receives notifications useful to shell applications. For more information, see the
        ///     ShellProc hook procedure.
        /// </summary>
        WH_SHELL = 10,

        /// <summary>
        ///     Installs a hook procedure that will be called when the application's foreground thread is about to become idle.
        ///     This hook is useful for performing low priority tasks during idle time. For more information, see the
        ///     ForegroundIdleProc hook procedure.
        /// </summary>
        WH_FOREGROUNDIDLE = 11,

        /// <summary>
        ///     Installs a hook procedure that monitors messages after they have been processed by the destination window
        ///     procedure. For more information, see the CallWndRetProc hook procedure.
        /// </summary>
        WH_CALLWNDPROCRET = 12,

        /// <summary>
        ///     Installs a hook procedure that monitors low-level keyboard input events. For more information, see the
        ///     LowLevelKeyboardProc hook procedure.
        /// </summary>
        WH_KEYBOARD_LL = 13,

        /// <summary>
        ///     Installs a hook procedure that monitors low-level mouse input events. For more information, see the
        ///     LowLevelMouseProc hook procedure
        /// </summary>
        WH_MOUSE_LL = 14
    }
}