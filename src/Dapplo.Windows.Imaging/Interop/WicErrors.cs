// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// WINCODEC_ERR_* values from winerror.h which are handled
/// </summary>
internal static class WicErrors
{
    /// <summary>
    /// WINCODEC_ERR_PROPERTYNOTFOUND
    /// </summary>
    public const HResult PropertyNotFound = unchecked((HResult)0x88982F40);

    /// <summary>
    /// WINCODEC_ERR_PALETTEUNAVAILABLE
    /// </summary>
    public const HResult PaletteUnavailable = unchecked((HResult)0x88982F45);

    /// <summary>
    /// WINCODEC_ERR_COMPONENTNOTFOUND: no codec for the container format or the stream
    /// </summary>
    public const HResult ComponentNotFound = unchecked((HResult)0x88982F50);
}
