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

#region using
using System;
#endregion

namespace Dapplo.Windows.Multimedia.Enums
{
    /// <summary>
    ///     See <a href="http://msdn.microsoft.com/en-us/library/aa909766.aspx">PlaySound</a>
    /// </summary>
    [Flags]
    public enum SoundSettings : uint
    {
        /// <summary>
        ///     Default: Synchronous playback of a sound event. PlaySound returns after the sound event completes.
        /// </summary>
        None = 0x0000,

        /// <summary>
        ///     The sound is played asynchronously and PlaySound returns immediately after beginning the sound. To terminate an
        ///     asynchronously played waveform sound, call PlaySound with pszSound set to NULL.
        /// </summary>
        Async = 0x0001,

        /// <summary>
        ///     No default sound event is used. If the sound cannot be found, PlaySound returns silently without playing the
        ///     default sound.
        /// </summary>
        NoDefault = 0x0002,

        /// <summary>
        /// The pszSound parameter points to a sound loaded in memory.
        ///  see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd743679(v=vs.85).aspx">Playing WAVE Resources</a>
        /// </summary>
        Memory = 0x0004,

        /// <summary>
        ///     The sound plays repeatedly until PlaySound is called again with the pszSound parameter set to NULL. You must also
        ///     specify the SND_ASYNC flag to indicate an asynchronous sound event.
        /// </summary>
        Loop = 0x0008,

        /// <summary>
        ///     The specified sound event will yield to another sound event that is already playing. If a sound cannot be played
        ///     because the resource needed to generate that sound is busy playing another sound, the function immediately returns
        ///     FALSE without playing the requested sound.
        ///     If this flag is not specified, PlaySound attempts to stop the currently playing sound so that the device can be
        ///     used to play the new sound.
        /// </summary>
        NoStop = 0x0010,

        /// <summary>
        ///     Not supported.
        /// </summary>
        NoWait = 0x00002000,
        /// <summary>
        ///     Not supported.
        /// </summary>
        Purge = 0x0040,

        /// <summary>
        ///     The pszSound parameter is a system-event alias in the registry or the WIN.INI file. Do not use with either
        ///     SND_FILENAME or SND_RESOURCE.
        /// </summary>
        Alias = 0x00010000,

        /// <summary>
        ///     The pszSound parameter is a predefined id
        /// </summary>
        AliasId = 0x00110000,

        /// <summary>
        ///     The pszSound parameter is a file name.
        /// </summary>
        Filename = 0x00020000,

        /// <summary>
        /// The pszSound parameter is a resource identifier; hmod must identify the instance that contains the resource.
        /// see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd743679(v=vs.85).aspx">Playing WAVE Resources</a>
        /// </summary>
        Resource = 0x00040004,

        /// <summary>
        /// If this flag is set, the function triggers a SoundSentry event when the sound is played.
        /// SoundSentry is an accessibility feature that causes the computer to display a visual cue when a sound is played. If the user did not enable SoundSentry, the visual cue is not displayed.
        /// </summary>
        Sentry = 0x00080000,

        /// <summary>
        /// If this flag is set, the sound is assigned to the audio session for system notification sounds. The system volume-control program (SndVol) displays a volume slider that controls system notification sounds. Setting this flag puts the sound under the control of that volume slider
        /// If this flag is not set, the sound is assigned to the default audio session for the application's process.
        /// For more information, see the documentation for the Core Audio APIs.
        /// </summary>
        System = 0x00200000
    }
}