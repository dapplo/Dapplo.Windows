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
    /// The file name, it can contain a relative path (e.g. "folder\file.txt") when a folder structure is transferred
    /// </summary>
    public string Name { get; }

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
