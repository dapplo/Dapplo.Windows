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
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Multimedia.Enums;

#endregion

namespace Dapplo.Windows.Multimedia
{
    /// <summary>
    ///     Windows Mulit-Media API
    /// </summary>
    public static class WinMm
    {
        /// <summary>
        /// Play a system sound
        /// </summary>
        /// <param name="systemSound">Value from the SystemSounds enum</param>
        public static void PlaySystemSound(SystemSounds systemSound)
        {
            PlaySound(systemSound.ToString(), UIntPtr.Zero, SoundSettings.AliasId | SoundSettings.Async);
        }

        /// <summary>
        /// Play a resource
        /// </summary>
        /// <param name="resource">Resource to play</param>
        public static void Play(string resource)
        {
            PlaySound(resource, UIntPtr.Zero, SoundSettings.Resource | SoundSettings.Async);
        }

        /// <summary>
        /// Play a wav from memorySetLastError
        /// </summary>
        /// <param name="memoryPtr">Pointer to the wav file to play</param>
        /// <param name="settings">SoundSettings</param>
        public static void Play(IntPtr memoryPtr, SoundSettings settings)
        {
            PlaySound(memoryPtr, UIntPtr.Zero, settings);
        }

        /// <summary>
        /// Play wave data
        /// Note: The byte[] should be pinned into memory, and cannot be removed while playing!!
        /// See <a href="https://blogs.msdn.microsoft.com/larryosterman/2009/02/19/playsoundxxx-snd_memory-snd_async-is-almost-always-a-bad-idea/">PlaySound(xxx, SND_MEMORY | SND_ASYNC) is almost always a bad idea.</a>
        /// </summary>
        /// <param name="soundBytes">Wave data to play to play</param>
        public static void Play(byte[] soundBytes)
        {
            PlaySound(soundBytes, UIntPtr.Zero, SoundSettings.Memory | SoundSettings.Async);
        }

        /// <summary>
        /// Stop playing
        /// </summary>
        public static void StopPlaying()
        {
            PlaySound((string)null, UIntPtr.Zero, SoundSettings.None);
        }

        /// <summary>
        ///     The PlaySound function plays a sound specified by the given file name, resource, or system event. (A system event may be associated with a sound in the registry or in the WIN.INI file.)
        /// </summary>
        /// <param name="soundBytes">byte array with the wave information</param>
        /// <param name="hmod">Handle to the executable file that contains the resource to be loaded. This parameter must be NULL unless SND_RESOURCE is specified in fdwSound.</param>
        /// <param name="fdwSound">Flags for playing the sound.</param>
        /// <returns>Returns TRUE if successful or FALSE otherwise.</returns>
        [DllImport("winmm", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PlaySound(byte[] soundBytes, UIntPtr hmod, SoundSettings fdwSound);

        /// <summary>
        ///  The PlaySound function plays a sound specified by the given file name, resource, or system event. (A system event may be associated with a sound in the registry or in the WIN.INI file.)
        /// </summary>
        /// <param name="pszSound">A string that specifies the sound to play. The maximum length, including the null terminator, is 256 characters. If this parameter is NULL, any currently playing waveform sound is stopped.</param>
        /// <param name="hmod">Handle to the executable file that contains the resource to be loaded. This parameter must be NULL unless SND_RESOURCE is specified in fdwSound.</param>
        /// <param name="fdwSound">Flags for playing the sound.</param>
        /// <returns>Returns TRUE if successful or FALSE otherwise.</returns>
        [DllImport("winmm", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PlaySound(string pszSound, UIntPtr hmod, SoundSettings fdwSound);

        /// <summary>
        ///  The PlaySound function plays a sound specified by the given file name, resource, or system event. (A system event may be associated with a sound in the registry or in the WIN.INI file.)
        /// </summary>
        /// <param name="memoryPtr">Pointer to memory where a wav file is stored</param>
        /// <param name="hmod">Handle to the executable file that contains the resource to be loaded. This parameter must be NULL unless SND_RESOURCE is specified in fdwSound.</param>
        /// <param name="fdwSound">Flags for playing the sound.</param>
        /// <returns>Returns TRUE if successful or FALSE otherwise.</returns>
        [DllImport("winmm", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PlaySound(IntPtr memoryPtr, UIntPtr hmod, SoundSettings fdwSound);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command">Pointer to a null-terminated string that specifies an MCI command string. For a list, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd743572(v=vs.85).aspx">Multimedia Command Strings</a></param>
        /// <param name="buffer">Pointer to a buffer that receives return information. If no return information is needed, this parameter can be NULL.</param>
        /// <param name="bufferSize">Size, in characters, of the return buffer specified by the lpszReturnString parameter.</param>
        /// <param name="hwndCallback">Handle to a callback window if the "notify" flag was specified in the command string.</param>
        /// <returns>
        /// Returns zero if successful or an error otherwise. The low-order word of the returned DWORD value contains the error return value. If the error is device-specific, the high-order word of the return value is the driver identifier; otherwise, the high-order word is zero.
        /// For a list of possible error values, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757153(v=vs.85).aspx">MCIERR Return Values</a>.
        /// To retrieve a text description of return values, pass the return value to the <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757158(v=vs.85).aspx">mciGetErrorString</a> function.
        /// </returns>
        [DllImport("winmm")]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);
    }
}