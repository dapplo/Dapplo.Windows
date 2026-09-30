// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// The open clipboard as IClipboardDataSource, only valid while the token is open, on the thread which opened it
/// </summary>
internal sealed class ClipboardTokenDataSource : IClipboardDataSource
{
    private readonly IClipboardAccessToken _clipboardAccessToken;

    public ClipboardTokenDataSource(IClipboardAccessToken clipboardAccessToken)
    {
        _clipboardAccessToken = clipboardAccessToken;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Formats => _clipboardAccessToken.AvailableFormats().ToList();

    /// <inheritdoc />
    public bool HasFormat(string format)
    {
        _clipboardAccessToken.ThrowWhenNoAccess();
        return !string.IsNullOrEmpty(format) && NativeMethods.IsClipboardFormatAvailable(ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <inheritdoc />
    public bool TryGetStream(string format, out Stream stream)
    {
        _clipboardAccessToken.ThrowWhenNoAccess();
        stream = null;
        return !string.IsNullOrEmpty(format) && _clipboardAccessToken.TryGetAsStream(format, out stream);
    }
}
