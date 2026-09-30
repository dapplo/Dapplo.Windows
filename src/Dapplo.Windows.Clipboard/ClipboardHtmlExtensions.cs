// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read and write the CF_HTML ("HTML Format") clipboard format
/// </summary>
public static class ClipboardHtmlExtensions
{
    /// <summary>
    /// Place an HTML fragment as CF_HTML ("HTML Format") on the clipboard, with a correct header (UTF-8 byte offsets).
    /// Call ClearContents first, or use <see cref="AddHtml"/> with <see cref="ClipboardNative.ReplaceContents"/>.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardAccessToken</param>
    /// <param name="htmlFragment">string with the HTML, e.g. an &lt;img&gt; element or formatted text</param>
    /// <param name="sourceUrl">optional absolute Uri of the document the HTML comes from</param>
    public static void SetAsHtml(this IClipboardAccessToken clipboardAccessToken, string htmlFragment, Uri sourceUrl = null)
    {
        clipboardAccessToken.SetAsBytes(ClipboardHtml.Create(htmlFragment, sourceUrl), ClipboardHtml.FormatName);
    }

    /// <summary>
    /// Add an HTML fragment as CF_HTML ("HTML Format") to the contents
    /// </summary>
    /// <param name="contents">ClipboardContents</param>
    /// <param name="htmlFragment">string with the HTML</param>
    /// <param name="sourceUrl">optional absolute Uri of the document the HTML comes from</param>
    /// <returns>ClipboardContents for fluent usage</returns>
    public static ClipboardContents AddHtml(this ClipboardContents contents, string htmlFragment, Uri sourceUrl = null)
    {
        if (contents == null)
        {
            throw new ArgumentNullException(nameof(contents));
        }
        return contents.AddBytes(ClipboardHtml.Create(htmlFragment, sourceUrl), ClipboardHtml.FormatName);
    }

    /// <summary>
    /// Read CF_HTML ("HTML Format"), e.g. what a browser or Word copied
    /// </summary>
    /// <param name="source">IClipboardDataSource, e.g. a snapshot or clipboard.AsDataSource()</param>
    /// <param name="html">ClipboardHtml with the fragment, the full document and the source URL</param>
    /// <returns>true when CF_HTML is available and could be parsed</returns>
    public static bool TryGetAsHtml(this IClipboardDataSource source, out ClipboardHtml html)
    {
        html = null;
        return source.TryGetAsBytes(ClipboardHtml.FormatName, out var bytes) && ClipboardHtml.TryParse(bytes, out html);
    }
}
