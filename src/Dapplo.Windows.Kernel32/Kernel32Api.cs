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
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Kernel32.Structs;

#endregion

namespace Dapplo.Windows.Kernel32
{
    /// <summary>
    ///     Kernel 32 functionality
    /// </summary>
    public static class Kernel32Api
    {
        private static DateTimeOffset? _systemStartup;
        private static readonly char[] DirectorySeparator = { '\\'};

        /// <summary>
        ///     Default value for AttachProcess if not specifying a process ID, this uses the console of the parent of the current process.
        /// </summary>
        private const uint AttachParentProcess = 0xff_ff_ff_ff;

        /// <summary>
        /// A helper method to prevent Dll Hijacking, this drastically reduces the DLL search paths!!!!!
        /// This will not help against a version.dll attack..
        /// </summary>
        /// <param name="allowDllDirectory">An optional single directory where additional  DLL searches are made</param>
        public static void PreventDllHijacking(string allowDllDirectory = "")
        {
            SetDllDirectory(allowDllDirectory);
            if (string.IsNullOrWhiteSpace(allowDllDirectory))
            {
                SetDefaultDllDirectories(DefaultDllDirectories.SearchSystem32Directory);
            }
            else
            {
                SetDefaultDllDirectories(DefaultDllDirectories.SearchSystem32Directory | DefaultDllDirectories.SearchUserDirectories);
            }
        }

        /// <summary>
        ///     Method to get the process path for a process
        /// </summary>
        /// <param name="process">Process</param>
        /// <returns>string</returns>
        public static string GetProcessPath(this Process process)
        {
            return GetProcessPath(process.Id);
        }

        /// <summary>
        ///     Method to get the process path
        /// </summary>
        /// <param name="processId">int with the process ID</param>
        /// <returns>string</returns>
        public static string GetProcessPath(int processId)
        {
            // Try the GetModuleFileName method first since it's the fastest. 
            // May return ACCESS_DENIED (due to VM_READ flag) if the process is not owned by the current user.
            // Will fail if we are compiled as x86 and we're trying to open a 64 bit process...not allowed.
            var hProcess = OpenProcess(ProcessAccessRights.QueryInformation | ProcessAccessRights.VirtualMemoryRead, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var path = PsApi.GetModuleFilename(hProcess, IntPtr.Zero);
                    if (path != null)
                    {
                        return path;
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }

            hProcess = OpenProcess(ProcessAccessRights.QueryInformation, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                return null;
            }

            unsafe
            {
                const int capacity = 512;
                var pathBuffer = stackalloc char[capacity];

                try
                {
                    // Try this method for Vista or higher operating systems
                    int bufferSize = capacity;
                    if (Environment.OSVersion.Version.Major >= 6 && QueryFullProcessImageName(hProcess, 0, pathBuffer, ref bufferSize) && bufferSize > 0)
                    {
                        return new string(pathBuffer, 0 , bufferSize);
                    }

                    // Try the GetProcessImageFileName method
                    var dosPath = PsApi.GetProcessImageFileName(hProcess);
                    
                    if (dosPath != null)
                    {
                        foreach (var drive in Environment.GetLogicalDrives())
                        {
                            var nrChars = QueryDosDevice(drive.TrimEnd(DirectorySeparator), pathBuffer, capacity);
                            if (nrChars == 0)
                            {
                                continue;
                            }
                            var dosDevice = new string(pathBuffer, 0, nrChars);
                            if (dosPath.StartsWith(dosDevice))
                            {
                                return drive + dosPath.Remove(0, nrChars);
                            }
                        }
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }

            }

            return null;
        }

        /// <summary>
        /// Specifies a default set of directories to search when the calling process loads a DLL. This search path is used when LoadLibraryEx is called with no LOAD_LIBRARY_SEARCH flags.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh310515(v=vs.85).aspx">SetDefaultDllDirectories function</a>
        /// </summary>
        /// <param name="directoryFlags">DefaultDllDirectories with the directories to search. This parameter can be any combination of the following values.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// The DLL search path is the set of directories that are searched for a DLL when a full path is not specified in a LoadLibrary or LoadLibraryEx function call, or when a full path to the DLL is specified but the system must search for dependent DLLs. For more information about the standard DLL search path, see Dynamic-Link Library Search Order.
        /// The standard DLL search path contains directories that can be vulnerable to a DLL pre-loading attack. An application can use the SetDefaultDllDirectories function to specify a default DLL search path for the process that eliminates the most vulnerable directories and limits the other directories that are searched. The process DLL search path applies only to the calling process and persists for the life of the process.
        /// If the DirectoryFlags parameter specifies more than one flag, the directories are searched in the following order:
        /// The directory that contains the DLL (LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR). This directory is searched only for dependencies of the DLL being loaded.
        /// The application directory (LOAD_LIBRARY_SEARCH_APPLICATION_DIR).
        /// Paths explicitly added to the application search path with the AddDllDirectory function (LOAD_LIBRARY_SEARCH_USER_DIRS) or the SetDllDirectory function. If more than one path has been added, the order in which the paths are searched is unspecified.
        /// The System directory (LOAD_LIBRARY_SEARCH_SYSTEM32).
        /// If SetDefaultDllDirectories does not specify LOAD_LIBRARY_SEARCH_USER_DIRS, directories specified with the AddDllDirectory function are used only for LoadLibraryEx function calls that specify LOAD_LIBRARY_SEARCH_USER_DIRS.
        /// It is not possible to revert to the standard DLL search path or remove any directory specified with SetDefaultDllDirectories from the search path. However, the process DLL search path can be overridden by calling LoadLibraryEx with one or more LOAD_LIBRARY_SEARCH flags, and directories added with AddDllDirectory can be removed by calling RemoveDllDirectory.
        /// Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008:  To call this function in an application, use the GetProcAddress function to retrieve its address from Kernel32.dll. KB2533623 must be installed on the target platform.
        /// </remarks>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDefaultDllDirectories(DefaultDllDirectories directoryFlags);

        /// <summary>
        /// Adds a directory to the search path used to locate DLLs for the application.
        /// </summary>
        /// <param name="lpPathName">The directory to be added to the search path. If this parameter is an empty string (""), the call removes the current directory from the default DLL search order. If this parameter is NULL, the function restores the default search order.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Allocates a new console for the calling process.
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        /// <summary>
        /// Retrieves the process identifier of the calling process.
        /// </summary>
        /// <returns>int</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern int GetCurrentProcessId();

        /// <summary>
        /// Retrieves the thread identifier of the calling thread.
        /// </summary>
        /// <returns>int</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern int GetCurrentThreadId();

        /// <summary>
        /// Attaches the calling process to the console of the specified process.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms681944(v=vs.85).aspx">AllocConsole function</a>
        /// </summary>
        /// <param name="dwProcessId">The identifier of the process whose console is to be used. Or -1 to use the console of the parent of the current process.</param>
        /// <returns>bool if it worked</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachConsole(uint dwProcessId = AttachParentProcess);

        /// <summary>
        /// Closes an open object handle.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724211(v=vs.85).aspx">CloseHandle function</a>
        /// The CloseHandle function closes handles to the following win32 objects:
        ///  * Access token
        ///  * Communications device
        ///  * Console input
        ///  * Console screen buffer
        ///  * Event
        ///  * File
        ///  * File mapping
        ///  * I/O completion port
        ///  * Job
        ///  * Mailslot
        ///  * Memory resource notification
        ///  * Mutex
        ///  * Named pipe
        ///  * Pipe
        ///  * Process
        ///  * Semaphore
        ///  * Thread
        ///  * Transaction
        ///  * Waitable timer
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>true if it worked, use GetLastError if not</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        ///     Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count.
        ///     When the reference count reaches zero, the module is unloaded from the address space of the calling process and the
        ///     handle is no longer valid.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms683152(v=vs.85).aspx">FreeLibrary function</a>
        /// </summary>
        /// <param name="module">IntPtr</param>
        /// <returns>true if it worked, false if an error occured</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr module);

        /// <summary>
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
        /// To avoid the race conditions described in the Remarks section, use the GetModuleHandleEx function.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms683199(v=vs.85).aspx">GetModuleHandle function</a>
        /// </summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default library extension .dll is appended. The file name string can include a trailing point character (.) to indicate that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process.
        /// If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file).
        /// The GetModuleHandle function does not retrieve handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag. For more information, see LoadLibraryEx.</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified module.</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724358.aspx">GetProductInfo function</a>
        /// </summary>
        /// <param name="osMajorVersion">
        ///     The major version number of the operating system. The minimum value is 6.
        ///     The combination of the dwOSMajorVersion, dwOSMinorVersion, dwSpMajorVersion, and dwSpMinorVersion parameters
        ///     describes the maximum target operating system version for the application. For example, Windows Vista and Windows
        ///     Server 2008 are version 6.0.0.0 and Windows 7 and Windows Server 2008 R2 are version 6.1.0.0.
        /// </param>
        /// <param name="osMinorVersion">The minor version number of the operating system. The minimum value is 0.</param>
        /// <param name="spMajorVersion">The major version number of the operating system service pack. The minimum value is 0.</param>
        /// <param name="spMinorVersion">The minor version number of the operating system service pack. The minimum value is 0.</param>
        /// <param name="edition">WindowsProducts</param>
        /// <returns></returns>
        [DllImport("Kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetProductInfo(int osMajorVersion, int osMinorVersion, int spMajorVersion, int spMinorVersion, out WindowsProducts edition);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724451(v=vs.85).aspx">GetVersionEx function</a>
        /// </summary>
        /// <param name="osVersionInfo">OsVersionInfoEx</param>
        /// <returns>If the function fails, the return value is false. To get extended error information, call GetLastError.</returns>
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVersionEx(ref OsVersionInfoEx osVersionInfo);

        /// <summary>
        ///     Loads the specified module into the address space of the calling process. The specified module may cause other
        ///     modules to be loaded.
        ///     See <a href="https://msdn.microsoft.com/en-us/library/ms684175(VS.85).aspx">LoadLibrary function</a>
        /// </summary>
        /// <param name="lpFileName">string with the library</param>
        /// <returns>IntPtr for the module, IntPtr.Zero if this failed, use last error to see what went wrong</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW")]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        /// <summary>
        /// Opens an existing local process object.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms684320(v=vs.85).aspx">OpenProcess function</a>
        /// </summary>
        /// <param name="dwDesiredAccess">ProcessAccessRights</param>
        /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwProcessId">The identifier of the local process to be opened.
        /// If the specified process is the System Process (0x00000000), the function fails and the last error code is ERROR_INVALID_PARAMETER. If the specified process is the Idle process or one of the CSRSS processes, this function fails and the last error code is ERROR_ACCESS_DENIED because their access restrictions prevent user-level code from opening them.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified process.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessRights dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa365461(v=vs.85).aspx">QueryDosDevice function</a>
        /// </summary>
        /// <param name="lpDeviceName">An MS-DOS device name string specifying the target of the query. The device name cannot have a trailing backslash; for example, use "C:", not "C:\".
        /// This parameter can be NULL. In that case, the QueryDosDevice function will store a list of all existing MS-DOS device names into the buffer pointed to by lpTargetPath.</param>
        /// <param name="lpTargetPath">A pointer to a buffer that will receive the result of the query. The function fills this buffer with one or more null-terminated strings. The final null-terminated string is followed by an additional NULL.
        /// If lpDeviceName is non-NULL, the function retrieves information about the particular MS-DOS device specified by lpDeviceName. The first null-terminated string stored into the buffer is the current mapping for the device. The other null-terminated strings represent undeleted prior mappings for the device.
        /// If lpDeviceName is NULL, the function retrieves a list of all existing MS-DOS device names. Each null-terminated string stored into the buffer is the name of an existing MS-DOS device, for example, \Device\HarddiskVolume1 or \Device\Floppy0.</param>
        /// <param name="uuchMax">The maximum number of TCHARs that can be stored into the buffer pointed to by lpTargetPath.</param>
        /// <returns>If the function succeeds, the return value is the number of TCHARs stored into the buffer pointed to by lpTargetPath.</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern unsafe int QueryDosDevice(string lpDeviceName, [Out] char* lpTargetPath, int uuchMax);

        /// <summary>
        /// Retrieves the full name of the executable image for the specified process.
        /// </summary>
        /// <param name="hProcess">A handle to the process. This handle must be created with the PROCESS_QUERY_INFORMATION or PROCESS_QUERY_LIMITED_INFORMATION access right. For more information, see Process Security and Access Rights.</param>
        /// <param name="dwFlags">
        /// This parameter can be one of the following values:
        ///     0 - The name should use the Win32 path format.
        /// PROCESS_NAME_NATIVE 0x00000001 - The name should use the native system path format.
        /// </param>
        /// <param name="lpExeName">The path to the executable image. If the function succeeds, this string is null-terminated.</param>
        /// <param name="lpdwSize">On input, specifies the size of the lpExeName buffer, in characters. On success, receives the number of characters written to the buffer, not including the null-terminating character.</param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern unsafe bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, char * lpExeName, ref int lpdwSize);

        /// <summary>
        /// Allocates the specified number of bytes from the heap.
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa366574(v=vs.85).aspx">GlobalAlloc function</a>
        /// </summary>
        /// <param name="globalMemorySettings">The memory allocation attributes. If zero is specified, the default is GMEM_FIXED. This parameter can be one or more of the following values, except for the incompatible combinations that are specifically noted.</param>
        /// <param name="bytes">The number of bytes to allocate. If this parameter is zero and the uFlags parameter specifies GMEM_MOVEABLE, the function returns a handle to a memory object that is marked as discarded.</param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the newly allocated memory object.
        /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr GlobalAlloc(GlobalMemorySettings globalMemorySettings, UIntPtr bytes);

        /// <summary>
        /// Locks a global memory object and returns a pointer to the first byte of the object's memory block.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory blockk</param>
        /// <returns>IntPtr to the first byte of the global memory block</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr GlobalLock(IntPtr hMem);

        /// <summary>
        /// Decrements the lock count associated with a memory object that was allocated with GMEM_MOVEABLE. This function has no effect on memory objects allocated with GMEM_FIXED.
        /// If the memory object is still locked after decrementing the lock count, the return value is a nonzero value. If the memory object is unlocked after decrementing the lock count, the function returns zero and GetLastError returns NO_ERROR.
        /// If the function fails, the return value is zero and GetLastError returns a value other than NO_ERROR.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory block</param>
        /// <returns>bool if the unlock worked.</returns>
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock(IntPtr hMem);

        /// <summary>
        /// Retrieves the current size of the specified global memory object, in bytes.
        /// </summary>
        /// <param name="hMem">IntPtr with a hGlobal, handle for a global memory blockk</param>
        /// <returns>int with the size</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern int GlobalSize(IntPtr hMem);

        /// <summary>
        /// Retrieves the number of milliseconds that have elapsed since the system was started.
        /// </summary>
        /// <returns>ulong with the ticks</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern ulong GetTickCount64();

        /// <summary>
        /// This function frees the specified local memory object and invalidates its handle.
        /// </summary>
        /// <param name="hMem">IntPtr</param>
        /// <returns>IntPtr</returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr hMem);

        /// <summary>
        /// Returns a DateTimeOffset which specifies when the system started
        /// </summary>
        public static DateTimeOffset SystemStartup
        {
            get
            {
                if (!_systemStartup.HasValue)
                {
                    _systemStartup = DateTimeOffset.Now.Subtract(TimeSpan.FromMilliseconds(GetTickCount64()));
                }
                return _systemStartup.Value;
            }
        } 
    }
}