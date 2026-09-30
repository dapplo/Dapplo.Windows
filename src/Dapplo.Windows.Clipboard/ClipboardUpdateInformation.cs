// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Information about what the clipboard contained at the most recent clipboard update.
/// This information is collected without opening the clipboard (GetClipboardSequenceNumber, GetClipboardOwner and GetUpdatedClipboardFormats),
/// to read the content use <see cref="ClipboardNative.Access"/> or <see cref="ClipboardNative.AccessAsync"/>.
/// </summary>
public sealed class ClipboardUpdateInformation
{
    private readonly uint[] _formatIds;

    private ClipboardUpdateInformation(uint id, IntPtr ownerHandle, uint[] formatIds)
    {
        Id = id;
        OwnerHandle = ownerHandle;
        _formatIds = formatIds;
    }

    /// <summary>
    /// Sequence-number of the clipboard, starts at 0 when the Windows session starts.
    /// This is 0 when the process has no WINSTA_ACCESSCLIPBOARD access.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Timestamp of the clipboard update event, this value will not be correct for the first event
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

    /// <summary>
    /// The handle of the window which owns the clipboard content
    /// </summary>
    public IntPtr OwnerHandle { get; }

    /// <summary>
    /// The formats in this clipboard contents as strings
    /// </summary>
    public IEnumerable<string> Formats => _formatIds
        .Select(ClipboardFormatExtensions.MapIdToFormat)
        .Where(format => !string.IsNullOrEmpty(format));

    /// <summary>
    /// The formats in this clipboard contents as IDs
    /// </summary>
    public IEnumerable<uint> FormatIds => _formatIds;

    /// <summary>
    /// Factory method, this retrieves the current clipboard information without opening the clipboard.
    /// </summary>
    /// <returns>ClipboardUpdateInformation</returns>
    /// <exception cref="System.ComponentModel.Win32Exception">When the available formats could not be retrieved</exception>
    public static ClipboardUpdateInformation Create()
    {
        return new ClipboardUpdateInformation(ClipboardNative.SequenceNumber, ClipboardNative.CurrentOwner, NativeMethods.GetUpdatedClipboardFormats());
    }
}
