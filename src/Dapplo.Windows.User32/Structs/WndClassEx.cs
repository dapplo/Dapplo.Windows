// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.User32.Enums;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs;

/// <summary>
/// Represents the window class structure used to define the properties and behavior of a window class for registration with the Windows operating system.
/// </summary>
/// <remarks>The WndClassEx structure contains information such as the window procedure, class styles, instance
/// handles, and resource handles required to register a window class using the Windows API. Before registering a window
/// class, ensure that the cbSize field is set to the size of this structure. This structure is typically used with the
/// RegisterClassEx function when creating custom window classes in native Windows applications.</remarks>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct WndClassEx
{
    private int _cbSize;
    private WindowsClassStyles _windowsClassStyle;
    private nint _lpfnWndProc;
    private int _cbClsExtra;
    private int _cbWndExtra;
    private nint _hInstance;
    private nint _hIcon;
    private nint _hCursor;
    private nint _hbrBackground;
    private string _lpszMenuName;
    private string _lpszClassName;
    private nint _hIconSm;

    /// <summary>
    /// Gets or sets the Windows class styles that define the appearance and behavior of the associated window.
    /// </summary>
    /// <remarks>Modifying this property allows customization of window features such as redraw behavior,
    /// double-click support, and other class-level attributes. Changes to the class style may affect how the window
    /// interacts with the operating system and other applications.</remarks>
    public WindowsClassStyles WindowsClassStyle { get => _windowsClassStyle; set => _windowsClassStyle = value; }

    /// <summary>
    /// Gets or sets the handle to the current application instance.
    /// </summary>
    public nint HInstance { get => _hInstance; set => _hInstance = value; }

    /// <summary>
    /// Gets or sets the pointer to the window procedure which processes the messages sent to the windows of the class,
    /// e.g. created with Marshal.GetFunctionPointerForDelegate from a Dapplo.Windows.Messages.Native.WndProc delegate.
    /// </summary>
    /// <remarks>The structure only holds the pointer: the delegate it was created from must be kept referenced
    /// as long as the window class is registered, otherwise the garbage collector collects it while Windows still calls it.</remarks>
    public nint LpfnWndProc { get => _lpfnWndProc; set => _lpfnWndProc = value; }

    /// <summary>
    /// Gets or sets the name of the window class associated with this instance.
    /// </summary>
    public string ClassName { get => _lpszClassName; set => _lpszClassName = value; }

    /// <summary>
    /// Factory for the structure
    /// </summary>
    public static WndClassEx Create()
    {
        var result = new WndClassEx
        {
            _cbSize = Marshal.SizeOf(typeof(WndClassEx))
        };
        return result;
    }
}
