//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
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

namespace Dapplo.Windows.Enums
{
    /// <summary>
    ///     See <a href="http://msdn.microsoft.com/en-us/library/aa909766.aspx">PlaySound</a>
    /// </summary>
    [Flags]
    public enum SoundFlags : uint
    {
        /// <summary>
        ///     Synchronous playback of a sound event. PlaySound returns after the sound event completes.
        /// </summary>
        SND_SYNC = 0x0000,

        /// <summary>
        ///     The sound is played asynchronously and PlaySound returns immediately after beginning the sound. To terminate an
        ///     asynchronously played waveform sound, call PlaySound with pszSound set to NULL.
        /// </summary>
        SND_ASYNC = 0x0001,

        /// <summary>
        ///     No default sound event is used. If the sound cannot be found, PlaySound returns silently without playing the
        ///     default sound.
        /// </summary>
        SND_NODEFAULT = 0x0002,

        /// <summary>
        ///     A sound event's file is loaded in RAM. The parameter specified by pszSound must point to an image of a sound in
        ///     memory.
        /// </summary>
        SND_MEMORY = 0x0004,

        /// <summary>
        ///     The sound plays repeatedly until PlaySound is called again with the pszSound parameter set to NULL. You must also
        ///     specify the SND_ASYNC flag to indicate an asynchronous sound event.
        /// </summary>
        SND_LOOP = 0x0008,

        /// <summary>
        ///     The specified sound event will yield to another sound event that is already playing. If a sound cannot be played
        ///     because the resource needed to generate that sound is busy playing another sound, the function immediately returns
        ///     FALSE without playing the requested sound.
        ///     If this flag is not specified, PlaySound attempts to stop the currently playing sound so that the device can be
        ///     used to play the new sound.
        /// </summary>
        SND_NOSTOP = 0x0010,

        /// <summary>
        ///     If the driver is busy, return immediately without playing the sound.
        /// </summary>
        SND_NOWAIT = 0x00002000,

        /// <summary>
        ///     The pszSound parameter is a system-event alias in the registry or the WIN.INI file. Do not use with either
        ///     SND_FILENAME or SND_RESOURCE.
        /// </summary>
        SND_ALIAS = 0x00010000,

        /// <summary>
        ///     The pszSound parameter is a predefined id
        /// </summary>
        SND_ALIAS_ID = 0x00110000,

        /// <summary>
        ///     The pszSound parameter is a file name.
        /// </summary>
        SND_FILENAME = 0x00020000
    }
}