// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Shell32.Enums;

/// <summary>
/// A combination of one or more file attribute flags (FILE_ATTRIBUTE_ values as defined in Winnt.h).
/// If uFlags does not include the SHGFI_USEFILEATTRIBUTES flag, this parameter is ignored.
/// </summary>
[Flags]
public enum ShellFileAttributeFlags : uint {
    /// <summary>
    /// A file or directory that is an archive file or directory. Applications typically use this attribute to mark files for backup or removal .
    /// </summary>
    Archive = 0x20,
    /// <summary>
    /// A file or directory that is compressed. For a file, all of the data in the file is compressed. For a directory, compression is the default for newly created files and subdirectories.
    /// </summary>
    Compressed = 0x800,
    /// <summary>
    /// This value is reserved for system use.
    /// </summary>
    Device = 0x40,
    /// <summary>
    /// The handle that identifies a directory.
    /// </summary>
    Directory = 0x00000010,
    /// <summary>
    /// A file or directory that is encrypted. For a file, all data streams in the file are encrypted. For a directory, encryption is the default for newly created files and subdirectories.
    /// </summary>
    Encrypted = 0x4000,
    /// <summary>
    /// The file or directory is hidden. It is not included in an ordinary directory listing.
    /// </summary>
    Hidden = 0x2,
    /// <summary>
    /// The directory or user data stream is configured with integrity (only supported on ReFS volumes). It is not included in an ordinary directory listing. The integrity setting persists with the file if it's renamed. If a file is copied the destination file will have integrity set if either the source file or destination directory have integrity set.
    /// Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP: This flag is not supported until Windows Server 2012.
    /// </summary>
    IntegrityStream = 0x8000,
    /// <summary>
    /// A file that does not have other attributes set. This attribute is valid only when used alone.
    /// </summary>
    Normal = 0x00000080,
    /// <summary>
    /// The file or directory is not to be indexed by the content indexing service.
    /// </summary>
    NotContextIndexed = 0x2000,
    /// <summary>
    /// The user data stream not to be read by the background data integrity scanner (AKA scrubber). When set on a directory it only provides inheritance. This flag is only supported on Storage Spaces and ReFS volumes. It is not included in an ordinary directory listing.
    /// Windows Server 2008 R2, Windows 7, Windows Server 2008, Windows Vista, Windows Server 2003 and Windows XP: This flag is not supported until Windows 8 and Windows Server 2012.
    /// </summary>
    NoScrubData = 0x20000,
    /// <summary>
    /// The data of a file is not available immediately. This attribute indicates that the file data is physically moved to offline storage.
    /// This attribute is used by Remote Storage, which is the hierarchical storage management software. Applications should not arbitrarily change this attribute.
    /// </summary>
    Offline = 0x1000,
    /// <summary>
    /// A file that is read-only. Applications can read the file, but cannot write to it or delete it. This attribute is not honored on directories. For more information, see You cannot view or change the Read-only or the System attributes of folders in Windows Server 2003, in Windows XP, in Windows Vista or in Windows 7.
    /// </summary>
    Readonly = 0x1,
    /// <summary>
    /// When this attribute is set, it means that the file or directory is not fully present locally.
    /// For a file that means that not all of its data is on local storage (e.g.it may be sparse with some data still in remote storage).
    /// For a directory it means that some of the directory contents are being virtualized from another location.
    /// Reading the file / enumerating the directory will be more expensive than normal, e.g. it will cause at least some of the file/directory content to be fetched from a remote store.
    /// Only kernel-mode callers can set this bit.
    /// </summary>
    RecallOnDataAccess = 0x400000,
    /// <summary>
    /// This attribute only appears in directory enumeration classes(FILE_DIRECTORY_INFORMATION, FILE_BOTH_DIR_INFORMATION, etc.).
    /// When this attribute is set, it means that the file or directory has no physical representation on the local system; the item is virtual.
    /// Opening the item will be more expensive than normal, e.g.it will cause at least some of it to be fetched from a remote store.
    /// </summary>
    RecallOnOpen = 0x40000,
    /// <summary>
    /// A file or directory that has an associated reparse point, or a file that is a symbolic link.
    /// </summary>
    ReparsePoint = 0x400,
    /// <summary>
    /// A file that is a sparse file.
    /// </summary>
    SparseFile = 0x200,
    /// <summary>
    /// A file or directory that the operating system uses a part of, or uses exclusively.
    /// </summary>
    System = 0x4,
    /// <summary>
    /// A file that is being used for temporary storage.
    /// File systems avoid writing data back to mass storage if sufficient cache memory is available, because typically, an application deletes a temporary file after the handle is closed.In that scenario, the system can entirely avoid writing the data.
    /// Otherwise, the data is written after the handle is closed.
    /// </summary>
    Temporary = 0x100,
    /// <summary>
    /// This value is reserved for system use.
    /// </summary>
    Virtual = 0x10000
}