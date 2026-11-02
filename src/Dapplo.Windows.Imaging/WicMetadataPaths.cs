// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The metadata query paths per container: the same EXIF tag has a different path in each container,
/// e.g. Software is "/app1/ifd/{ushort=305}" in a JPEG and "/ifd/{ushort=305}" in a TIFF or JPEG XR.
/// Every method returns null when the container has no such path (or it isn't known to work).
/// See doc/articles/imaging.md for which paths the codecs of Windows accept.
/// </summary>
public static class WicMetadataPaths
{
    /// <summary>EXIF tag ImageDescription</summary>
    public const ushort TagImageDescription = 270;

    /// <summary>EXIF tag Orientation</summary>
    public const ushort TagOrientation = 274;

    /// <summary>EXIF tag Software</summary>
    public const ushort TagSoftware = 305;

    /// <summary>EXIF tag DateTime (when the file was changed, "YYYY:MM:DD HH:MM:SS")</summary>
    public const ushort TagDateTime = 306;

    /// <summary>EXIF tag Artist</summary>
    public const ushort TagArtist = 315;

    /// <summary>EXIF sub IFD tag DateTimeOriginal (when the image was captured, "YYYY:MM:DD HH:MM:SS")</summary>
    public const ushort TagDateTimeOriginal = 36867;

    /// <summary>The photo metadata policy for the orientation, which WIC maps to the path of each container it knows</summary>
    public const string PolicyOrientation = "System.Photo.Orientation";

    /// <summary>
    /// The path of the main (0th) IFD with the EXIF tags: "/app1/ifd" for JPEG, "/ifd" for TIFF, JPEG XR and HEIF
    /// </summary>
    public static string Ifd(Guid containerFormat)
    {
        if (containerFormat == WicContainerFormats.Jpeg)
        {
            return "/app1/ifd";
        }
        if (containerFormat == WicContainerFormats.Tiff || containerFormat == WicContainerFormats.Wmp || containerFormat == WicContainerFormats.Heif)
        {
            return "/ifd";
        }
        return null;
    }

    /// <summary>
    /// The path of a tag of the main IFD, e.g. ExifTag(Jpeg, 305) is "/app1/ifd/{ushort=305}"
    /// </summary>
    public static string ExifTag(Guid containerFormat, ushort tag)
    {
        var ifd = Ifd(containerFormat);
        return ifd == null ? null : $"{ifd}/{{ushort={tag}}}";
    }

    /// <summary>
    /// The path of a tag of the EXIF sub IFD, e.g. ExifSubTag(Jpeg, 36867) is "/app1/ifd/exif/{ushort=36867}"
    /// </summary>
    public static string ExifSubTag(Guid containerFormat, ushort tag)
    {
        var ifd = Ifd(containerFormat);
        return ifd == null ? null : $"{ifd}/exif/{{ushort={tag}}}";
    }

    /// <summary>
    /// The path of the EXIF orientation
    /// </summary>
    public static string Orientation(Guid containerFormat) => ExifTag(containerFormat, TagOrientation);

    /// <summary>
    /// The path of the software which wrote the file: the EXIF tag Software, for PNG the tEXt keyword "Software"
    /// </summary>
    public static string Software(Guid containerFormat) => containerFormat == WicContainerFormats.Png ? PngText("Software") : ExifTag(containerFormat, TagSoftware);

    /// <summary>
    /// The path of the description: the EXIF tag ImageDescription, for PNG the tEXt keyword "Description"
    /// </summary>
    public static string Description(Guid containerFormat) => containerFormat == WicContainerFormats.Png ? PngText("Description") : ExifTag(containerFormat, TagImageDescription);

    /// <summary>
    /// The path of the EXIF DateTimeOriginal (capture time), the value is a string "YYYY:MM:DD HH:MM:SS"
    /// </summary>
    public static string DateTimeOriginal(Guid containerFormat) => ExifSubTag(containerFormat, TagDateTimeOriginal);

    /// <summary>
    /// The path of a PNG tEXt chunk with the keyword, e.g. "/tEXt/{str=Software}"; keywords like Title, Author, Description, Copyright, Creation Time, Software, Source and Comment are registered
    /// </summary>
    public static string PngText(string keyword) => $"/tEXt/{{str={keyword}}}";

    /// <summary>
    /// The path of the XMP block: "/xmp" for JPEG, "/ifd/xmp" for TIFF and JPEG XR
    /// </summary>
    public static string Xmp(Guid containerFormat)
    {
        if (containerFormat == WicContainerFormats.Jpeg)
        {
            return "/xmp";
        }
        if (containerFormat == WicContainerFormats.Tiff || containerFormat == WicContainerFormats.Wmp)
        {
            return "/ifd/xmp";
        }
        return null;
    }

    /// <summary>
    /// The path of an XMP property, e.g. XmpProperty(Jpeg, "dc:title") is "/xmp/dc:title"
    /// </summary>
    public static string XmpProperty(Guid containerFormat, string property)
    {
        var xmp = Xmp(containerFormat);
        return xmp == null ? null : $"{xmp}/{property}";
    }

    /// <summary>
    /// The queries tried for the orientation: the path of the container, then the policy
    /// </summary>
    internal static IEnumerable<string> OrientationQueries(Guid containerFormat)
    {
        var path = Orientation(containerFormat);
        if (path != null)
        {
            yield return path;
        }
        yield return PolicyOrientation;
    }
}
