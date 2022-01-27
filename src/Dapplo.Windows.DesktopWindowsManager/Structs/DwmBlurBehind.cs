// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.DesktopWindowsManager.Enums;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.DesktopWindowsManager;

/// <summary>
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa969500(v=vs.85).aspx">DWM_BLURBEHIND structure</a>
///     Specifies Desktop Window Manager (DWM) blur-behind properties.
///     Used by the DwmEnableBlurBehindWindow function.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct DwmBlurBehind
{
    /// <summary>
    ///     A bitwise combination of DWM Blur Behind constant values that indicates which of the members of this structure have
    ///     been set.
    /// </summary>
    private DwmBlurBehindFlags _dwFlags;

    private bool _fEnable;
    private IntPtr _hRgnBlur;
    private bool _fTransitionOnMaximized;

    /// <summary>
    ///     TRUE to register the window handle to DWM blur behind;
    ///     FALSE to unregister the window handle from DWM blur behind.
    /// </summary>
    public bool Enable
    {
        get => _fEnable;
        set
        {
            _fEnable = value;
            _dwFlags |= DwmBlurBehindFlags.Enable;
        }
    }

    /// <summary>
    ///     The region within the client area where the blur behind will be applied.
    ///     A NULL value will apply the blur behind the entire client area.
    /// </summary>
    public IntPtr BlurRegion
    {
        get => _hRgnBlur;
        set
        {
            _hRgnBlur = value;
            _dwFlags |= DwmBlurBehindFlags.BlurRegion;
        }
    }


    /// <summary>
    ///     TRUE if the window's colorization should transition to match the maximized windows; otherwise, FALSE.
    /// </summary>
    public bool TransitionOnMaximized
    {
        get => _fTransitionOnMaximized;
        set
        {
            _fTransitionOnMaximized = value;
            _dwFlags |= DwmBlurBehindFlags.TransitionMaximized;
        }
    }
}