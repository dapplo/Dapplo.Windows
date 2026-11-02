// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The PROPVARIANT type to use for a value, where the .NET type alone doesn't decide it
/// </summary>
public enum WicMetadataValueKind
{
    /// <summary>The type follows from the .NET type of the value</summary>
    Default,

    /// <summary>VT_LPSTR, e.g. for the EXIF ASCII tags (Software, Make, Model, DateTime) in an IFD</summary>
    AnsiString,

    /// <summary>VT_VECTOR | VT_UI1 instead of VT_BLOB for a byte[]</summary>
    ByteVector
}

/// <summary>
/// A value with an explicit PROPVARIANT type, for <see cref="WicMetadataWriter.Set"/>
/// </summary>
public sealed class WicMetadataValue
{
    private WicMetadataValue(WicMetadataValueKind kind, object value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>The type to use</summary>
    public WicMetadataValueKind Kind { get; }

    /// <summary>The value</summary>
    public object Value { get; }

    /// <summary>A VT_LPSTR string, the EXIF ASCII tags in an IFD need this</summary>
    public static WicMetadataValue AnsiString(string value) => new WicMetadataValue(WicMetadataValueKind.AnsiString, value);

    /// <summary>A VT_VECTOR | VT_UI1 byte array</summary>
    public static WicMetadataValue ByteVector(byte[] value) => new WicMetadataValue(WicMetadataValueKind.ByteVector, value);

    /// <inheritdoc />
    public override string ToString() => $"{Kind}: {Value}";
}
