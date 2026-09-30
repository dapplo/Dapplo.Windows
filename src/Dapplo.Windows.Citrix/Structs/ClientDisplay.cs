// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Citrix.Structs;

/// <summary>
///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientDisplay
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ClientDisplay
{
    private readonly uint _horizontalResolution;
    private readonly uint _verticalResolution;
    private readonly uint _colorDepth;

    /// <summary>
    ///     Return the client's display size
    /// </summary>
    public NativeSize ClientSize => new NativeSize((int)_horizontalResolution, (int)_verticalResolution);

    /// <summary>
    ///     Returns the color depth of the client display, in bits per pixel.
    ///     The native WF color-depth codes (1, 2, 4, 8, 16, 24) are mapped to 4, 8, 16, 24, 15 and 32 bpp.
    /// </summary>
    public uint ColorDepth
    {
        get => _colorDepth switch
            {
                1 => 4,
                2 => 8,
                4 => 16,
                8 => 24,
                16 => 15,
                24 => 32,
                _ => _colorDepth
            };
    }
}