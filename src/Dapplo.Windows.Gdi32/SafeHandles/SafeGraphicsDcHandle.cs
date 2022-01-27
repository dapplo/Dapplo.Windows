// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;

namespace Dapplo.Windows.Gdi32.SafeHandles;

/// <summary>
///     A DeviceContext SafeHandle implementation for the Graphics object
/// </summary>
public class SafeGraphicsDcHandle : SafeDcHandle
{
    private readonly bool _disposeGraphics;
    private readonly Graphics _graphics;

    /// <summary>
    ///     Default constructor is needed to support marshalling!!
    /// </summary>
    [SecurityCritical]
    public SafeGraphicsDcHandle() : base(true)
    {
    }

    /// <summary>
    ///     Construct a SafeGraphicsDcHandle for the specified Graphics
    /// </summary>
    /// <param name="graphics">Graphics</param>
    /// <param name="preexistingHandle">IntPtr hDc, from graphics.GetHdc()</param>
    /// <param name="disposeGraphics">specifies if the Graphics object needs disposing</param>
    [SecurityCritical]
    private SafeGraphicsDcHandle(Graphics graphics, IntPtr preexistingHandle, bool disposeGraphics) : base(true)
    {
        _graphics = graphics;
        _disposeGraphics = disposeGraphics;
        SetHandle(preexistingHandle);
    }

    /// <summary>
    ///     Create a SafeGraphicsDcHandle from a Graphics object
    /// </summary>
    /// <param name="graphics">Graphics object</param>
    /// <param name="disposeGraphics"></param>
    /// <returns>SafeGraphicsDcHandle</returns>
    public static SafeGraphicsDcHandle FromGraphics(Graphics graphics, bool disposeGraphics = false)
    {
        return new SafeGraphicsDcHandle(graphics, graphics.GetHdc(), disposeGraphics);
    }

    /// <summary>
    ///     Call graphics.ReleaseHdc
    /// </summary>
    /// <returns>always true</returns>
    protected override bool ReleaseHandle()
    {
        _graphics.ReleaseHdc(handle);
        if (_disposeGraphics)
        {
            _graphics.Dispose();
        }
        return true;
    }

    /// <summary>
    ///     The SelectObject function selects an object into the device context (DC) which this SafeGraphicsDcHandle
    ///     represents.
    ///     The new object replaces the previous object of the same type.
    /// </summary>
    /// <param name="newHandle">SafeHandle for the new object</param>
    /// <returns>Replaced object</returns>
    public SafeSelectObjectHandle SelectObject(SafeHandle newHandle)
    {
        return new SafeSelectObjectHandle(this, newHandle);
    }
}