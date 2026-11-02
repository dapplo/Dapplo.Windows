// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The WIC vendor GUIDs of wincodec.h, compare them with <see cref="WicCodecInfo.VendorGuid"/>
/// </summary>
public static class WicVendors
{
    /// <summary>
    /// GUID_VendorMicrosoft: the codecs of Microsoft, those which come with Windows (BMP, PNG, JPEG ...) and those of Microsoft from the Store (HEIF, WebP, Raw, JPEG XL)
    /// </summary>
    public static readonly Guid Microsoft = new Guid("F0E749CA-EDEF-4589-A73A-EE0E626A2A2B");

    /// <summary>
    /// GUID_VendorMicrosoftBuiltIn: used to ask for the built-in Microsoft implementation, e.g. with CreateDecoder; the codecs themselves report <see cref="Microsoft"/>
    /// </summary>
    public static readonly Guid MicrosoftBuiltIn = new Guid("257A30FD-06B6-462B-AEA4-63F70B86E533");
}
