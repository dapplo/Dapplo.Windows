// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// Immutable description of one WIC codec as reported by WIC, see <see cref="WicCodecs"/>
/// </summary>
public sealed class WicCodecInfo
{
    internal WicCodecInfo(WicCodecKind kind, Guid clsid, Guid containerFormat, Guid vendorGuid, string friendlyName, string author, string version,
        IReadOnlyList<string> fileExtensions, IReadOnlyList<string> mimeTypes, WicSigningStatus signingStatus)
    {
        Kind = kind;
        Clsid = clsid;
        ContainerFormat = containerFormat;
        VendorGuid = vendorGuid;
        FriendlyName = friendlyName ?? string.Empty;
        Author = author ?? string.Empty;
        Version = version ?? string.Empty;
        FileExtensions = fileExtensions ?? Array.Empty<string>();
        MimeTypes = mimeTypes ?? Array.Empty<string>();
        SigningStatus = signingStatus;
    }

    /// <summary>
    /// Decoder or encoder
    /// </summary>
    public WicCodecKind Kind { get; }

    /// <summary>
    /// The CLSID of the codec
    /// </summary>
    public Guid Clsid { get; }

    /// <summary>
    /// The container format GUID, e.g. GUID_ContainerFormatPng (1B7CFAF4-713F-473C-BBCD-6137425FAEAF)
    /// </summary>
    public Guid ContainerFormat { get; }

    /// <summary>
    /// The vendor GUID, see <see cref="WicVendors"/>
    /// </summary>
    public Guid VendorGuid { get; }

    /// <summary>
    /// The friendly name, e.g. "PNG Decoder" or "Microsoft HEIF Decoder"; empty when the codec doesn't report one
    /// </summary>
    public string FriendlyName { get; }

    /// <summary>
    /// The author; empty when the codec doesn't report one
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// The version of the codec; empty when the codec doesn't report one
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The file extensions, lower case with the dot, e.g. ".heic", without duplicates
    /// </summary>
    public IReadOnlyList<string> FileExtensions { get; }

    /// <summary>
    /// The MIME types, lower case, e.g. "image/heic", without duplicates
    /// </summary>
    public IReadOnlyList<string> MimeTypes { get; }

    /// <summary>
    /// The signing status flags
    /// </summary>
    public WicSigningStatus SigningStatus { get; }

    /// <summary>
    /// True when the codec has the Microsoft vendor GUID (<see cref="WicVendors.Microsoft"/>).
    /// Note: this is true for the codecs which come with Windows and also for the codecs of Microsoft from the Store (HEIF, WebP, Raw, JPEG XL).
    /// </summary>
    public bool IsFromMicrosoft => VendorGuid == WicVendors.Microsoft;

    /// <inheritdoc />
    public override string ToString() => $"{FriendlyName} ({Kind}, {string.Join(" ", FileExtensions)})";
}
