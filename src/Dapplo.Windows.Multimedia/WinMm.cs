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
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Multimedia
{
    /// <summary>
    ///     Windows Mulit-Media API
    /// </summary>
    public static class WinMm
    {
        /// <summary>
        ///     The PlaySound function plays a sound specified by the given file name, resource, or system event. (A system event may be associated with a sound in the registry or in the WIN.INI file.)
        /// </summary>
        /// <param name="soundBytes">byte array with the wave information</param>
        /// <param name="hmod">Handle to the executable file that contains the resource to be loaded. This parameter must be NULL unless SND_RESOURCE is specified in fdwSound.</param>
        /// <param name="fdwSound">Flags for playing the sound.</param>
        /// <returns>Returns TRUE if successful or FALSE otherwise.</returns>
        [DllImport("winmm.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PlaySound(byte[] soundBytes, UIntPtr hmod, SoundSettings fdwSound);

        [DllImport("winmm.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PlaySound(IntPtr ptrToSound, UIntPtr hmod, SoundSettings fdwSound);
    }
}