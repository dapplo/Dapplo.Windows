// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// A file which only exists in the data object ("virtual file"), e.g. an Outlook attachment or an image dragged from some
/// browsers: described by FileGroupDescriptorW, its content is the FileContents format with the index of the file.
/// </summary>
public sealed class VirtualFile
{
    private readonly Func<int, long?, Stream> _openContent;

    internal VirtualFile(int index, string name, long? size, FileAttributes? attributes, DateTime? creationTimeUtc, DateTime? lastAccessTimeUtc, DateTime? lastWriteTimeUtc, Func<int, long?, Stream> openContent)
    {
        Index = index;
        Name = name;
        Size = size;
        Attributes = attributes;
        CreationTimeUtc = creationTimeUtc;
        LastAccessTimeUtc = lastAccessTimeUtc;
        LastWriteTimeUtc = lastWriteTimeUtc;
        _openContent = openContent;
    }

    /// <summary>
    /// The index of the file in the descriptor, it's the lindex of its FileContents
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The file name as the source supplied it. It can contain a relative path (e.g. "folder\file.txt") when a folder structure
    /// is transferred, but also "..\" or an absolute path from a malicious source: never combine it with a target folder directly,
    /// use <see cref="SafeFileName"/>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <see cref="Name"/> reduced to a file name which can be created in any folder: only the last path segment, invalid characters
    /// replaced with '_', no trailing dots or spaces, reserved device names (CON, NUL, COM1, …) prefixed with '_', never empty.
    /// </summary>
    public string SafeFileName => MakeSafeFileName(Name);

    internal static string MakeSafeFileName(string name)
    {
        name ??= "";
        var lastSeparator = name.LastIndexOfAny(new[] { '\\', '/', ':' });
        var fileName = lastSeparator >= 0 ? name.Substring(lastSeparator + 1) : name;
        var invalid = Path.GetInvalidFileNameChars();
        var chars = fileName.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalid, chars[i]) >= 0 || chars[i] < 32)
            {
                chars[i] = '_';
            }
        }
        fileName = new string(chars).TrimEnd('.', ' ').TrimStart(' ');
        if (fileName.Length == 0)
        {
            return "file";
        }
        var baseName = fileName.Split('.')[0].TrimEnd(' ');
        string[] reserved = { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        if (Array.Exists(reserved, r => string.Equals(r, baseName, StringComparison.OrdinalIgnoreCase)))
        {
            fileName = "_" + fileName;
        }
        return fileName.Length > 240 ? fileName.Substring(0, 240) : fileName;
    }

    /// <summary>
    /// The size in bytes, null when the producer didn't supply it
    /// </summary>
    public long? Size { get; }

    /// <summary>
    /// The file attributes, null when the producer didn't supply them (e.g. Directory for a folder entry)
    /// </summary>
    public FileAttributes? Attributes { get; }

    /// <summary>
    /// The creation time, null when not supplied
    /// </summary>
    public DateTime? CreationTimeUtc { get; }

    /// <summary>
    /// The last access time, null when not supplied
    /// </summary>
    public DateTime? LastAccessTimeUtc { get; }

    /// <summary>
    /// The last write time, null when not supplied
    /// </summary>
    public DateTime? LastWriteTimeUtc { get; }

    /// <summary>
    /// True for a folder entry, which has no content
    /// </summary>
    public bool IsDirectory => Attributes.HasValue && (Attributes.Value & FileAttributes.Directory) != 0;

    /// <summary>
    /// Read the content (FileContents with the index of this file) into a read-only stream, null when it's not available.
    /// Only call this while the data object is valid (e.g. during the drop), on the thread which got it.
    /// </summary>
    /// <returns>Stream or null</returns>
    public Stream OpenContent() => _openContent(Index, Size);

    /// <inheritdoc />
    public override string ToString() => Name;
}
