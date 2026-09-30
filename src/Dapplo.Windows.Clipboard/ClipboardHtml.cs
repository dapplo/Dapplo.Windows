// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// The CF_HTML clipboard format ("HTML Format"): an HTML document with a header which marks the copied fragment,
/// see <a href="https://learn.microsoft.com/windows/win32/dataxchg/html-clipboard-format">HTML Clipboard Format</a>.
/// The offsets in the header are UTF-8 byte offsets.
/// </summary>
public sealed class ClipboardHtml
{
    /// <summary>
    /// The registered clipboard format name of CF_HTML
    /// </summary>
    public const string FormatName = "HTML Format";

    private const string StartFragmentMarker = "<!--StartFragment-->";
    private const string EndFragmentMarker = "<!--EndFragment-->";
    private static readonly Regex StartMarkerRegex = new(@"<!--\s*StartFragment\s*-->", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex EndMarkerRegex = new(@"<!--\s*EndFragment\s*-->", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Create the information
    /// </summary>
    /// <param name="fragment">string with the copied HTML</param>
    /// <param name="fullHtml">string with the complete HTML document, including the fragment</param>
    /// <param name="sourceUrl">optional Uri of the document the fragment was copied from</param>
    /// <param name="version">string with the version of the header, e.g. "0.9"</param>
    public ClipboardHtml(string fragment, string fullHtml, Uri sourceUrl = null, string version = "0.9")
    {
        Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        FullHtml = fullHtml ?? fragment;
        SourceUrl = sourceUrl;
        Version = version;
    }

    /// <summary>
    /// The copied HTML, what the user selected
    /// </summary>
    public string Fragment { get; }

    /// <summary>
    /// The HTML document which contains the fragment, the context of the selection (e.g. the surrounding elements).
    /// When the producer placed no context (StartHTML = -1), this is the fragment.
    /// </summary>
    public string FullHtml { get; }

    /// <summary>
    /// The document the HTML was copied from, if the producer supplied it (SourceURL)
    /// </summary>
    public Uri SourceUrl { get; }

    /// <summary>
    /// The version of the header, "0.9" or "1.0"
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Create the CF_HTML bytes for an HTML fragment: the header with correct UTF-8 byte offsets, followed by a minimal document
    /// with the fragment between the StartFragment and EndFragment markers.
    /// </summary>
    /// <param name="htmlFragment">string with the HTML to place, e.g. "&lt;b&gt;Hello&lt;/b&gt;" or an &lt;img&gt; element</param>
    /// <param name="sourceUrl">optional Uri of the source document, relative links in the fragment are resolved against it by the application which pastes</param>
    /// <returns>byte array with the UTF-8 encoded CF_HTML</returns>
    public static byte[] Create(string htmlFragment, Uri sourceUrl = null)
    {
        if (htmlFragment == null)
        {
            throw new ArgumentNullException(nameof(htmlFragment));
        }
        if (sourceUrl != null && !sourceUrl.IsAbsoluteUri)
        {
            throw new ArgumentException("The source URL must be absolute.", nameof(sourceUrl));
        }
        const string headerFormat = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        var sourceUrlLine = sourceUrl == null ? "" : $"SourceURL:{sourceUrl.AbsoluteUri}\r\n";
        const string documentStart = "<html>\r\n<body>\r\n" + StartFragmentMarker;
        const string documentEnd = EndFragmentMarker + "\r\n</body>\r\n</html>";

        // The header has a fixed length, as the numbers always have 10 digits
        var headerLength = Encoding.UTF8.GetByteCount(string.Format(CultureInfo.InvariantCulture, headerFormat, 0, 0, 0, 0)) + Encoding.UTF8.GetByteCount(sourceUrlLine);
        var startHtml = headerLength;
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(documentStart);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(htmlFragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(documentEnd);

        var header = string.Format(CultureInfo.InvariantCulture, headerFormat, startHtml, endHtml, startFragment, endFragment) + sourceUrlLine;
        return Encoding.UTF8.GetBytes(header + documentStart + htmlFragment + documentEnd);
    }

    /// <summary>
    /// Parse CF_HTML. The offsets of the header are used when they are consistent; when they are missing or wrong (some producers
    /// count characters instead of bytes), the StartFragment / EndFragment comments are used.
    /// </summary>
    /// <param name="data">byte array with the CF_HTML data, trailing NUL bytes are ignored</param>
    /// <param name="html">ClipboardHtml</param>
    /// <returns>true if the data is CF_HTML</returns>
    public static bool TryParse(byte[] data, out ClipboardHtml html)
    {
        html = null;
        if (data == null || data.Length == 0)
        {
            return false;
        }
        // The clipboard memory can be larger than the data, which then ends with a NUL
        var length = Array.IndexOf(data, (byte)0);
        if (length < 0)
        {
            length = data.Length;
        }

        var header = ReadHeader(data, length, out var headerEnd);
        if (!header.ContainsKey("Version") && !header.ContainsKey("StartFragment") && !header.ContainsKey("StartHTML"))
        {
            return false;
        }

        var startHtml = GetOffset(header, "StartHTML");
        var endHtml = GetOffset(header, "EndHTML");
        var startFragment = GetOffset(header, "StartFragment");
        var endFragment = GetOffset(header, "EndFragment");

        var document = Encoding.UTF8.GetString(data, headerEnd, length - headerEnd);
        var startMatch = StartMarkerRegex.Match(document);
        var endMatch = startMatch.Success ? EndMarkerRegex.Match(document, startMatch.Index + startMatch.Length) : Match.Empty;
        var hasMarkers = startMatch.Success && endMatch.Success;

        string fragment = null;
        // Use the offsets when they are in range and point right behind the StartFragment and right before the EndFragment comment
        // (or there are no comments to check against)
        if (IsValidRange(startFragment, endFragment, headerEnd, length) && (!hasMarkers || (EndsWithComment(data, startFragment) && StartsWithComment(data, endFragment, length))))
        {
            fragment = Encoding.UTF8.GetString(data, startFragment, endFragment - startFragment);
        }

        string fullHtml;
        if (startHtml >= 0 && IsValidRange(startHtml, endHtml, headerEnd, length))
        {
            fullHtml = Encoding.UTF8.GetString(data, startHtml, endHtml - startHtml);
        }
        else if (startHtml < 0 && fragment != null)
        {
            // No context (-1): the fragment is all there is
            fullHtml = fragment;
        }
        else
        {
            // Wrong offsets: everything after the header
            fullHtml = document;
        }

        if (fragment == null)
        {
            if (hasMarkers)
            {
                var start = startMatch.Index + startMatch.Length;
                fragment = document.Substring(start, endMatch.Index - start);
            }
            else if (startHtml < 0 && startFragment < 0)
            {
                // No offsets and no comments: the document is the fragment
                fragment = document;
            }
            else
            {
                return false;
            }
        }

        Uri sourceUrl = null;
        if (header.TryGetValue("SourceURL", out var url) && !Uri.TryCreate(url.Trim(), UriKind.Absolute, out sourceUrl))
        {
            sourceUrl = null;
        }
        header.TryGetValue("Version", out var version);
        html = new ClipboardHtml(fragment, fullHtml, sourceUrl, version?.Trim());
        return true;
    }

    /// <summary>
    /// Read the "Key:Value" lines, until the first line which isn't one
    /// </summary>
    private static Dictionary<string, string> ReadHeader(byte[] data, int length, out int headerEnd)
    {
        var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var position = 0;
        while (position < length)
        {
            var lineEnd = Array.IndexOf(data, (byte)'\n', position, length - position);
            var nextPosition = lineEnd < 0 ? length : lineEnd + 1;
            var line = Encoding.UTF8.GetString(data, position, (lineEnd < 0 ? length : lineEnd) - position).TrimEnd('\r');
            var colon = line.IndexOf(':');
            // A header key has no spaces or markup, the document starts with '<'
            if (line.StartsWith("<", StringComparison.Ordinal) || colon <= 0 || line.Substring(0, colon).IndexOfAny(new[] { ' ', '<', '\t' }) >= 0)
            {
                break;
            }
            header[line.Substring(0, colon)] = line.Substring(colon + 1);
            position = nextPosition;
        }
        headerEnd = position;
        return header;
    }

    private static int GetOffset(Dictionary<string, string> header, string key)
    {
        if (header.TryGetValue(key, out var value) && int.TryParse(value.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var offset))
        {
            return offset;
        }
        return -1;
    }

    /// <summary>
    /// The end offset of the usual producers points right at the EndFragment comment
    /// </summary>
    private static bool StartsWithComment(byte[] data, int offset, int length)
    {
        var position = offset;
        while (position < length && (data[position] == (byte)' ' || data[position] == (byte)'\r' || data[position] == (byte)'\n'))
        {
            position++;
        }
        return position + 4 <= length && data[position] == (byte)'<' && data[position + 1] == (byte)'!' && data[position + 2] == (byte)'-' && data[position + 3] == (byte)'-';
    }

    private static bool IsValidRange(int start, int end, int headerEnd, int length) => start >= headerEnd && end >= start && end <= length;

    /// <summary>
    /// The fragment offset of the usual producers points right behind the StartFragment comment. If it doesn't, the offsets are probably wrong.
    /// </summary>
    private static bool EndsWithComment(byte[] data, int offset)
    {
        var position = offset;
        // Some producers put whitespace between the comment and the fragment offset
        while (position > 0 && (data[position - 1] == (byte)' ' || data[position - 1] == (byte)'\r' || data[position - 1] == (byte)'\n'))
        {
            position--;
        }
        return position >= 3 && data[position - 3] == (byte)'-' && data[position - 2] == (byte)'-' && data[position - 1] == (byte)'>';
    }
}
