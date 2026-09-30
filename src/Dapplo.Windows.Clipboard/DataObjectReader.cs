// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Dapplo.Windows.Kernel32;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Reads an OLE data object (<see cref="System.Runtime.InteropServices.ComTypes.IDataObject"/>), from drag and drop or from
/// <see cref="ClipboardNative.GetOleDataObject"/>. It supports what the Win32 clipboard API can't: formats with an index (lindex),
/// IStream data and virtual files (FileGroupDescriptorW + FileContents, e.g. Outlook attachments).
/// As <see cref="IClipboardDataSource"/> the same read helpers work as for the clipboard and snapshots.
/// </summary>
/// <remarks>
/// OLE data objects are COM objects: use the reader on the thread which got the data object (usually the STA UI thread), and only
/// while the data object is valid (during the drop, or until the clipboard changes). Copy what you need, e.g. with <see cref="TryGetStream(string, int, out Stream)"/>.
/// TYMED_HGLOBAL and TYMED_ISTREAM are supported; TYMED_ISTORAGE (e.g. an Outlook message attached to a message), GDI and metafile handles are not.
/// <para>
/// The data comes from another application: treat it as untrusted. Failing or misbehaving sources make the Try methods return false,
/// data larger than <see cref="MaxDataSize"/> isn't read, and virtual file names can contain paths: use <see cref="VirtualFile.SafeFileName"/> to create files.
/// </para>
/// </remarks>
public sealed class DataObjectReader : IClipboardDataSource, IDisposable
{
    /// <summary>
    /// The format with the descriptions of the virtual files
    /// </summary>
    public const string FileGroupDescriptorWFormat = "FileGroupDescriptorW";

    /// <summary>
    /// The ANSI variant of <see cref="FileGroupDescriptorWFormat"/>
    /// </summary>
    public const string FileGroupDescriptorFormat = "FileGroupDescriptor";

    /// <summary>
    /// The format with the content of the virtual files, one per index
    /// </summary>
    public const string FileContentsFormat = "FileContents";

    private const int DvAspectContent = 1;
    // A data source which keeps returning formats must not hang the reader
    private const int MaxFormats = 10_000;
    private const int SOk = 0;
    // FILEDESCRIPTORW is 592 bytes, FILEDESCRIPTORA 332
    private const int FileDescriptorWSize = 592;
    private const int FileDescriptorASize = 332;

    private readonly bool _ownsDataObject;
    private IDataObject _dataObject;

    /// <summary>
    /// Create a reader for the data object, e.g. from a drop event. The data object isn't released when the reader is disposed.
    /// </summary>
    /// <param name="dataObject">System.Runtime.InteropServices.ComTypes.IDataObject</param>
    public DataObjectReader(IDataObject dataObject) : this(dataObject, false)
    {
    }

    internal DataObjectReader(IDataObject dataObject, bool ownsDataObject)
    {
        _dataObject = dataObject ?? throw new ArgumentNullException(nameof(dataObject));
        _ownsDataObject = ownsDataObject;
    }

    private IDataObject DataObject => _dataObject ?? throw new ObjectDisposedException(nameof(DataObjectReader));

    /// <summary>
    /// The maximum size of the data of one format (or one virtual file) in bytes, default 512 MiB. Larger data isn't read:
    /// <see cref="TryGetStream(string, int, out Stream)"/> returns false and <see cref="VirtualFile.OpenContent"/> null.
    /// This protects against data sources which claim or stream huge amounts of data.
    /// </summary>
    public long MaxDataSize { get; set; } = 512L * 1024 * 1024;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Formats
    {
        get
        {
            var formats = new List<string>();
            IEnumFORMATETC enumerator;
            try
            {
                enumerator = DataObject.EnumFormatEtc(DATADIR.DATADIR_GET);
            }
            catch (Exception ex) when (ex is COMException or NotImplementedException)
            {
                return formats;
            }
            if (enumerator == null)
            {
                return formats;
            }
            try
            {
                var formatEtc = new FORMATETC[1];
                var fetched = new int[1];
                for (var count = 0; count < MaxFormats && enumerator.Next(1, formatEtc, fetched) == SOk && fetched[0] == 1; count++)
                {
                    // The caller owns the target device structure
                    if (formatEtc[0].ptd != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(formatEtc[0].ptd);
                        formatEtc[0].ptd = IntPtr.Zero;
                    }
                    var name = ClipboardFormatExtensions.MapIdToFormat(unchecked((ushort)formatEtc[0].cfFormat));
                    if (!string.IsNullOrEmpty(name) && !formats.Contains(name))
                    {
                        formats.Add(name);
                    }
                }
            }
            catch (Exception ex) when (ex is COMException or InvalidCastException)
            {
                // A broken enumerator: return what was read so far
            }
            finally
            {
                ReleaseIfComObject(enumerator);
            }
            return formats;
        }
    }

    /// <inheritdoc />
    public bool HasFormat(string format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return false;
        }
        var formatEtc = CreateFormatEtc(format, -1);
        try
        {
            return DataObject.QueryGetData(ref formatEtc) == SOk;
        }
        catch (Exception ex) when (ex is COMException or NotImplementedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryGetStream(string format, out Stream stream) => TryGetStream(format, -1, out stream);

    /// <summary>
    /// Get the data of a format with an index (lindex), e.g. FileContents of the virtual file with that index.
    /// The data is copied into a read-only MemoryStream, which stays valid after the data object is gone.
    /// </summary>
    /// <param name="format">string with the format name</param>
    /// <param name="index">int with the lindex, -1 for "all"</param>
    /// <param name="stream">Stream with a copy of the data</param>
    /// <returns>true when the data could be read as HGLOBAL or IStream</returns>
    public bool TryGetStream(string format, int index, out Stream stream)
    {
        stream = null;
        if (!TryGetBytes(format, index, out var bytes))
        {
            return false;
        }
        stream = new MemoryStream(bytes, false);
        return true;
    }

    /// <summary>
    /// The virtual files of FileGroupDescriptorW (or the ANSI FileGroupDescriptor), empty when there are none
    /// </summary>
    /// <returns>list with the VirtualFile descriptions, their content is read lazily with <see cref="VirtualFile.OpenContent"/></returns>
    public IReadOnlyList<VirtualFile> GetVirtualFiles()
    {
        if (TryGetBytes(FileGroupDescriptorWFormat, -1, out var descriptor))
        {
            return ParseFileGroupDescriptor(descriptor, true, OpenFileContents);
        }
        if (TryGetBytes(FileGroupDescriptorFormat, -1, out descriptor))
        {
            return ParseFileGroupDescriptor(descriptor, false, OpenFileContents);
        }
        return Array.Empty<VirtualFile>();
    }

    private Stream OpenFileContents(int index, long? size)
    {
        if (!TryGetBytes(FileContentsFormat, index, out var bytes))
        {
            return null;
        }
        // HGLOBAL allocations can be larger than the file
        var length = size.HasValue && size.Value >= 0 && size.Value < bytes.Length ? (int)size.Value : bytes.Length;
        return new MemoryStream(bytes, 0, length, false, false);
    }

    private bool TryGetBytes(string format, int index, out byte[] bytes)
    {
        bytes = null;
        if (string.IsNullOrEmpty(format))
        {
            return false;
        }
        var formatEtc = CreateFormatEtc(format, index);
        STGMEDIUM medium;
        try
        {
            DataObject.GetData(ref formatEtc, out medium);
        }
        catch (Exception ex) when (ex is COMException or NotImplementedException or ArgumentException)
        {
            return false;
        }
        try
        {
            switch (medium.tymed)
            {
                case TYMED.TYMED_HGLOBAL:
                    bytes = ReadHGlobal(medium.unionmember, MaxDataSize);
                    return bytes != null;
                case TYMED.TYMED_ISTREAM:
                    bytes = ReadIStream(medium.unionmember, MaxDataSize);
                    return bytes != null;
                default:
                    return false;
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or NotImplementedException)
        {
            // The data source failed while the data was read, e.g. IStream.Read returned an error
            bytes = null;
            return false;
        }
        finally
        {
            ReleaseStgMedium(ref medium);
        }
    }

    private static FORMATETC CreateFormatEtc(string format, int index) => new()
    {
        cfFormat = unchecked((short)ClipboardFormatExtensions.MapFormatToId(format)),
        ptd = IntPtr.Zero,
        dwAspect = (DVASPECT)DvAspectContent,
        lindex = index,
        tymed = TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM
    };

    private static byte[] ReadHGlobal(IntPtr hGlobal, long maxSize)
    {
        if (hGlobal == IntPtr.Zero)
        {
            return null;
        }
        var size = (long)Kernel32Api.GlobalSize(hGlobal).ToUInt64();
        if (size > int.MaxValue || size > maxSize)
        {
            return null;
        }
        var pointer = Kernel32Api.GlobalLock(hGlobal);
        if (pointer == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            var bytes = new byte[size];
            Marshal.Copy(pointer, bytes, 0, (int)size);
            return bytes;
        }
        finally
        {
            Kernel32Api.GlobalUnlock(hGlobal);
        }
    }

    private static byte[] ReadIStream(IntPtr streamPointer, long maxSize)
    {
        if (streamPointer == IntPtr.Zero)
        {
            return null;
        }
        var comStream = Marshal.GetObjectForIUnknown(streamPointer) as IStream;
        if (comStream == null)
        {
            return null;
        }
        try
        {
            using var result = new MemoryStream();
            var buffer = new byte[81920];
            var bytesReadPointer = Marshal.AllocCoTaskMem(sizeof(int));
            try
            {
                while (true)
                {
                    Marshal.WriteInt32(bytesReadPointer, 0);
                    comStream.Read(buffer, buffer.Length, bytesReadPointer);
                    var bytesRead = Marshal.ReadInt32(bytesReadPointer);
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    // Don't trust the stream: it can't have read more than was asked, and must stay below the limit
                    if (bytesRead > buffer.Length || result.Length + bytesRead > maxSize || result.Length + bytesRead > int.MaxValue)
                    {
                        return null;
                    }
                    result.Write(buffer, 0, bytesRead);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(bytesReadPointer);
            }
            return result.ToArray();
        }
        finally
        {
            ReleaseIfComObject(comStream);
        }
    }

    /// <summary>
    /// Parse FILEGROUPDESCRIPTORW / FILEGROUPDESCRIPTORA
    /// </summary>
    internal static IReadOnlyList<VirtualFile> ParseFileGroupDescriptor(byte[] data, bool isUnicode, Func<int, long?, Stream> openContent)
    {
        var files = new List<VirtualFile>();
        if (data == null || data.Length < 4)
        {
            return files;
        }
        var count = BitConverter.ToInt32(data, 0);
        var descriptorSize = isUnicode ? FileDescriptorWSize : FileDescriptorASize;
        for (var i = 0; i < count; i++)
        {
            var offset = 4 + i * descriptorSize;
            if (offset + descriptorSize > data.Length)
            {
                break;
            }
            var flags = BitConverter.ToUInt32(data, offset);
            // dwFlags(4) clsid(16) sizel(8) pointl(8) = 36
            var attributes = BitConverter.ToUInt32(data, offset + 36);
            var creationTime = BitConverter.ToInt64(data, offset + 40);
            var accessTime = BitConverter.ToInt64(data, offset + 48);
            var writeTime = BitConverter.ToInt64(data, offset + 56);
            var sizeHigh = BitConverter.ToUInt32(data, offset + 64);
            var sizeLow = BitConverter.ToUInt32(data, offset + 68);
            string name;
            if (isUnicode)
            {
                name = Encoding.Unicode.GetString(data, offset + 72, 520);
            }
            else
            {
                // ANSI in the system code page (Encoding.Default is UTF-8 on .NET)
                var length = 0;
                while (length < 260 && data[offset + 72 + length] != 0)
                {
                    length++;
                }
                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    name = Marshal.PtrToStringAnsi(handle.AddrOfPinnedObject() + offset + 72, length);
                }
                finally
                {
                    handle.Free();
                }
            }
            var terminator = name.IndexOf('\0');
            if (terminator >= 0)
            {
                name = name.Substring(0, terminator);
            }
            files.Add(new VirtualFile(i, name,
                (flags & 0x40) != 0 ? ((long)sizeHigh << 32) | sizeLow : null,
                (flags & 0x04) != 0 ? (FileAttributes)attributes : null,
                (flags & 0x08) != 0 ? ToDateTime(creationTime) : null,
                (flags & 0x10) != 0 ? ToDateTime(accessTime) : null,
                (flags & 0x20) != 0 ? ToDateTime(writeTime) : null,
                openContent));
        }
        return files;
    }

    private static DateTime? ToDateTime(long fileTime)
    {
        if (fileTime <= 0)
        {
            return null;
        }
        try
        {
            return DateTime.FromFileTimeUtc(fileTime);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static void ReleaseIfComObject(object value)
    {
        if (value != null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    /// <summary>
    /// Releases the data object when it came from <see cref="ClipboardNative.GetOleDataObject"/>
    /// </summary>
    public void Dispose()
    {
        var dataObject = _dataObject;
        _dataObject = null;
        if (_ownsDataObject)
        {
            ReleaseIfComObject(dataObject);
        }
    }

    [DllImport("ole32")]
    private static extern void ReleaseStgMedium(ref STGMEDIUM medium);
}
