// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.IO;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Read access to clipboard-like data: the open clipboard (<see cref="ClipboardDataSourceExtensions.AsDataSource"/>),
/// a <see cref="ClipboardSnapshot"/>, or other data such as drag and drop data.
/// The extension methods in <see cref="ClipboardDataSourceExtensions"/> read text, bytes and file names from every source,
/// so one piece of code works for all of them.
/// </summary>
public interface IClipboardDataSource
{
    /// <summary>
    /// The names of the formats in this source, standard formats are named like "CF_UNICODETEXT"
    /// </summary>
    IReadOnlyCollection<string> Formats { get; }

    /// <summary>
    /// Check if the format is available
    /// </summary>
    /// <param name="format">string with the format name, e.g. "PNG" or StandardClipboardFormats.UnicodeText.AsString()</param>
    /// <returns>bool</returns>
    bool HasFormat(string format);

    /// <summary>
    /// Get the data of a format as a read-only stream
    /// </summary>
    /// <param name="format">string with the format name</param>
    /// <param name="stream">Stream with the data, the caller disposes it</param>
    /// <returns>true when the format is available and could be read</returns>
    bool TryGetStream(string format, out Stream stream);
}
