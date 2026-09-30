// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Multimedia.Enums;

namespace Dapplo.Windows.Multimedia;

/// <summary>
///     Windows Multi-Media API
/// </summary>
public static class WinMm
{
    private static readonly object PlayMemoryLock = new object();
    // Unmanaged copy of the wave data passed to PlayWave(byte[]), this needs to stay alive while winmm plays it asynchronously
    private static IntPtr _playingMemory = IntPtr.Zero;

    /// <summary>
    /// Play a sound of the Windows sound scheme asynchronously.
    /// </summary>
    /// <param name="systemSound">Value from the SystemSounds enum</param>
    /// <returns>bool true if the sound started playing</returns>
    public static bool PlaySystemSound(SystemSounds systemSound)
    {
        // The enum names are the system-event alias names from the registry, so SND_ALIAS (not SND_ALIAS_ID) is needed
        return PlaySound(systemSound.ToString(), UIntPtr.Zero, SoundSettings.Alias | SoundSettings.Async);
    }

    /// <summary>
    /// Play a native (Win32) WAVE resource asynchronously.
    /// </summary>
    /// <param name="resourceName">Name of the WAVE resource to play</param>
    /// <param name="moduleHandle">
    /// Handle (HMODULE) of the executable or DLL which contains the resource,
    /// <see cref="IntPtr.Zero"/> (default) uses the executable of the current process.
    /// </param>
    /// <returns>bool true if the sound started playing, false if the resource wasn't found or can't be played</returns>
    public static bool PlayResource(string resourceName, IntPtr moduleHandle = default)
    {
        if (resourceName is null)
        {
            throw new ArgumentNullException(nameof(resourceName));
        }
        if (moduleHandle == IntPtr.Zero)
        {
            moduleHandle = GetModuleHandle(null);
        }
        return PlaySound(resourceName, new UIntPtr((ulong)moduleHandle.ToInt64()), SoundSettings.Resource | SoundSettings.Async | SoundSettings.NoDefault);
    }

    /// <summary>
    /// Play a WAV file asynchronously, winmm reads the file itself.
    /// </summary>
    /// <param name="path">Path of the WAV file, PlaySound supports at most 255 characters</param>
    /// <returns>
    /// bool true if the sound was started, false if the file doesn't exist or playing failed. A file which exists but isn't a valid WAV file
    /// can't be detected up front (PlaySound opens it asynchronously), it simply doesn't play: no default sound is played instead.
    /// </returns>
    public static bool PlayFile(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        // With SND_ASYNC PlaySound returns TRUE before the file is opened, so check that it exists first
        if (path.Length == 0 || !File.Exists(path))
        {
            return false;
        }
        return PlaySound(path, UIntPtr.Zero, SoundSettings.Filename | SoundSettings.Async | SoundSettings.NoDefault);
    }

    /// <summary>
    /// Play WAVE data from unmanaged memory, <see cref="SoundSettings.Memory"/> is always added to the settings.
    /// Note: the caller owns the memory, when <see cref="SoundSettings.Async"/> is used it must stay valid until the sound has finished or was stopped with <see cref="StopPlaying"/>.
    /// Prefer <see cref="PlayWave(byte[])"/>, which takes care of this.
    /// </summary>
    /// <param name="memoryPtr">Pointer to the WAVE data (a complete .wav file in memory)</param>
    /// <param name="settings">SoundSettings, e.g. <see cref="SoundSettings.None"/> to play synchronously</param>
    /// <returns>bool true if the sound was played (synchronous) or started playing (asynchronous)</returns>
    public static bool PlayWave(IntPtr memoryPtr, SoundSettings settings)
    {
        return PlaySound(memoryPtr, UIntPtr.Zero, settings | SoundSettings.Memory);
    }

    /// <summary>
    /// Play WAVE data (a complete .wav file) asynchronously.
    /// The wave data is copied into unmanaged memory which is kept alive until the next call to <see cref="PlayWave(byte[])"/> or <see cref="StopPlaying"/>,
    /// so the passed byte[] can be reused or collected directly after this call.
    /// Any sound which is currently playing is stopped first.
    /// See <a href="https://blogs.msdn.microsoft.com/larryosterman/2009/02/19/playsoundxxx-snd_memory-snd_async-is-almost-always-a-bad-idea/">PlaySound(xxx, SND_MEMORY | SND_ASYNC) is almost always a bad idea.</a>
    /// </summary>
    /// <param name="soundBytes">Wave data to play</param>
    /// <returns>bool true if the sound started playing</returns>
    public static bool PlayWave(byte[] soundBytes)
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
    /// Stop playing, this also frees the memory of a sound started with <see cref="PlayWave(byte[])"/>
    /// </summary>
    public static void StopPlaying()
    {
        lock (PlayMemoryLock)
        {
            StopAndFreeMemory();
        }
    }

    /// <summary>
    /// Stop the currently playing sound, and free the memory used by <see cref="PlayWave(byte[])"/>. Must be called while holding PlayMemoryLock.
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
    [DllImport("winmm", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(string pszSound, UIntPtr hmod, SoundSettings fdwSound);

    /// <summary>
    ///  The PlaySound function plays a sound specified by the given file name, resource, or system event. (A system event may be associated with a sound in the registry or in the WIN.INI file.)
    /// </summary>
    /// <param name="memoryPtr">Pointer to memory where a wav file is stored</param>
    /// <param name="hmod">Handle to the executable file that contains the resource to be loaded. This parameter must be NULL unless SND_RESOURCE is specified in fdwSound.</param>
    /// <param name="fdwSound">Flags for playing the sound.</param>
    /// <returns>Returns TRUE if successful or FALSE otherwise.</returns>
    [DllImport("winmm")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(IntPtr memoryPtr, UIntPtr hmod, SoundSettings fdwSound);

    /// <summary>
    /// Retrieves a module handle for the specified module, NULL returns the handle of the file used to create the calling process (the .exe).
    /// </summary>
    [DllImport("kernel32", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);
}