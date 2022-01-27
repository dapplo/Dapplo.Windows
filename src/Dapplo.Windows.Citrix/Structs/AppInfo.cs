// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.AppInfo
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct AppInfo
{
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _initialProgram;
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _workingDirectory;
    [MarshalAs(UnmanagedType.LPWStr)]
    private readonly string _applicationName;

    /// <summary>
    ///     Return the InitialProgram
    /// </summary>
    public string InitialProgram => _initialProgram;

    /// <summary>
    ///     Return the WorkingDirectory
    /// </summary>
    public string WorkingDirectory => _workingDirectory;

    /// <summary>
    ///     Return the application name
    /// </summary>
    public string ApplicationName => _applicationName;
}