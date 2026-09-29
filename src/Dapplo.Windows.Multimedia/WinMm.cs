// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Multimedia.Enums;

namespace Dapplo.Windows.Multimedia;

/// <summary>
///     Windows Multi-Media API
/// </summary>
public static class WinMm
{
    private static readonly object PlayMemoryLock = new object();
    // Unmanaged copy of the wave data passed to Play(byte[]), this needs to stay alive while winmm plays it asynchronously
    private static IntPtr _playingMemory = IntPtr.Zero;

    /// <summary>
    /// Play a system sound
    /// </summary>
    /// <param name="systemSound">Value from the SystemSounds enum</param>
    public static void PlaySystemSound(SystemSounds systemSound)
    {
        // The enum names are the system-event alias names from the registry, so SND_ALIAS (not SND_ALIAS_ID) is needed
        PlaySound(systemSound.ToString(), UIntPtr.Zero, SoundSettings.Alias | SoundSettings.Async);
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
    /// Play a wav from memory.
    /// Note: the caller owns the memory, when <see cref="SoundSettings.Async"/> is used it must stay valid until the sound has finished or was stopped with <see cref="StopPlaying"/>.
    /// </summary>
    /// <param name="memoryPtr">Pointer to the wav file to play</param>
    /// <param name="settings">SoundSettings</param>
    public static void Play(IntPtr memoryPtr, SoundSettings settings)
    {
        PlaySound(memoryPtr, UIntPtr.Zero, settings);
    }

    /// <summary>
    /// Play wave data asynchronously.
    /// The wave data is copied into unmanaged memory which is kept alive until the next call to <see cref="Play(byte[])"/> or <see cref="StopPlaying"/>,
    /// so the passed byte[] can be reused or collected directly after this call.
    /// Any sound which is currently playing is stopped first.
    /// See <a href="https://blogs.msdn.microsoft.com/larryosterman/2009/02/19/playsoundxxx-snd_memory-snd_async-is-almost-always-a-bad-idea/">PlaySound(xxx, SND_MEMORY | SND_ASYNC) is almost always a bad idea.</a>
    /// </summary>
    /// <param name="soundBytes">Wave data to play</param>
    /// <returns>bool true if the sound started playing</returns>
    public static bool Play(byte[] soundBytes)
    {
        if (soundBytes is null)
        {
            throw new ArgumentNullException(nameof(soundBytes));
        }

        lock (PlayMemoryLock)
        {
            // Stop the current sound (synchronously) so the previous buffer is no longer used, then free it
            StopAndFreeMemory();

            var memory = Marshal.AllocHGlobal(soundBytes.Length);
            Marshal.Copy(soundBytes, 0, memory, soundBytes.Length);
            if (!PlaySound(memory, UIntPtr.Zero, SoundSettings.Memory | SoundSettings.Async))
            {
                Marshal.FreeHGlobal(memory);
                return false;
            }
            _playingMemory = memory;
            return true;
        }
    }

    /// <summary>
    /// Stop playing, this also frees the memory of a sound started with <see cref="Play(byte[])"/>
    /// </summary>
    public static void StopPlaying()
    {
        lock (PlayMemoryLock)
        {
            StopAndFreeMemory();
        }
    }

    /// <summary>
    /// Stop the currently playing sound, and free the memory used by <see cref="Play(byte[])"/>. Must be called while holding PlayMemoryLock.
    /// </summary>
    private static void StopAndFreeMemory()
    {
        // PlaySound with NULL stops the currently playing (async) sound before it returns
        PlaySound((string)null, UIntPtr.Zero, SoundSettings.None);
        if (_playingMemory == IntPtr.Zero)
        {
            return;
        }
        Marshal.FreeHGlobal(_playingMemory);
        _playingMemory = IntPtr.Zero;
    }

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
    /// <param name="hWndCallback">Handle to a callback window if the "notify" flag was specified in the command string.</param>
    /// <returns>
    /// Returns zero if successful or an error otherwise. The low-order word of the returned DWORD value contains the error return value. If the error is device-specific, the high-order word of the return value is the driver identifier; otherwise, the high-order word is zero.
    /// For a list of possible error values, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757153(v=vs.85).aspx">MCIERR Return Values</a>.
    /// To retrieve a text description of return values, pass the return value to the <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd757158(v=vs.85).aspx">mciGetErrorString</a> function.
    /// </returns>
    [DllImport("winmm")]
    private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hWndCallback);
}