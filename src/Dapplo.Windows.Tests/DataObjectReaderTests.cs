// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Xunit;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Dapplo.Windows.Tests;

/// <summary>
/// DataObjectReader over an in-process OLE data object with virtual files
/// </summary>
public class DataObjectReaderTests
{
    internal static readonly DateTime WriteTime = new(2026, 9, 30, 12, 34, 56, DateTimeKind.Utc);
    internal static readonly byte[] FirstContent = Encoding.UTF8.GetBytes("First file, delivered as HGLOBAL");
    internal static readonly byte[] SecondContent = Enumerable.Range(0, 200_000).Select(i => (byte)(i * 7)).ToArray();

    /// <summary>
    /// A data object like Outlook offers for two attachments: the first content as HGLOBAL (larger allocation than the file),
    /// the second as IStream, plus a folder entry and text
    /// </summary>
    internal static TestDataObject CreateVirtualFiles()
    {
        var dataObject = new TestDataObject();
        var descriptor = CreateDescriptor(
            ("Report ä.txt", FirstContent.Length, FileAttributes.Normal),
            (@"Folder\Image.bin", SecondContent.Length, FileAttributes.Normal),
            ("Folder", 0, FileAttributes.Directory));
        dataObject.Add(DataObjectReader.FileGroupDescriptorWFormat, -1, TYMED.TYMED_HGLOBAL, descriptor);
        dataObject.Add(DataObjectReader.FileContentsFormat, 0, TYMED.TYMED_HGLOBAL, FirstContent, extraAllocation: 64);
        dataObject.Add(DataObjectReader.FileContentsFormat, 1, TYMED.TYMED_ISTREAM, SecondContent);
        dataObject.Add("CF_UNICODETEXT", -1, TYMED.TYMED_HGLOBAL, Encoding.Unicode.GetBytes("Two attachments\0"));
        return dataObject;
    }

    private static byte[] CreateDescriptor(params (string Name, long Size, FileAttributes Attributes)[] files)
    {
        const int descriptorSize = 592;
        var data = new byte[4 + files.Length * descriptorSize];
        BitConverter.GetBytes(files.Length).CopyTo(data, 0);
        for (var i = 0; i < files.Length; i++)
        {
            var offset = 4 + i * descriptorSize;
            // FD_ATTRIBUTES | FD_WRITESTIME | FD_FILESIZE | FD_UNICODE
            BitConverter.GetBytes(0x04u | 0x20u | 0x40u | 0x80000000u).CopyTo(data, offset);
            BitConverter.GetBytes((uint)files[i].Attributes).CopyTo(data, offset + 36);
            BitConverter.GetBytes(WriteTime.ToFileTimeUtc()).CopyTo(data, offset + 56);
            BitConverter.GetBytes((uint)(files[i].Size >> 32)).CopyTo(data, offset + 64);
            BitConverter.GetBytes((uint)files[i].Size).CopyTo(data, offset + 68);
            Encoding.Unicode.GetBytes(files[i].Name).CopyTo(data, offset + 72);
        }
        return data;
    }

    internal static void AssertVirtualFiles(DataObjectReader reader)
    {
        Assert.Contains(DataObjectReader.FileGroupDescriptorWFormat, reader.Formats);
        Assert.Contains(DataObjectReader.FileContentsFormat, reader.Formats);
        Assert.True(reader.HasFormat(DataObjectReader.FileGroupDescriptorWFormat));
        Assert.False(reader.HasFormat("Dapplo.Windows.Tests.NotThere"));
        Assert.Equal("Two attachments", reader.GetAsUnicodeString());

        var files = reader.GetVirtualFiles();
        Assert.Equal(3, files.Count);
        Assert.Equal(new[] { "Report ä.txt", @"Folder\Image.bin", "Folder" }, files.Select(f => f.Name));
        Assert.Equal(new long?[] { FirstContent.Length, SecondContent.Length, 0 }, files.Select(f => f.Size));
        Assert.All(files, f => Assert.Equal(WriteTime, f.LastWriteTimeUtc));
        Assert.All(files, f => Assert.Null(f.CreationTimeUtc));
        Assert.True(files[2].IsDirectory);
        Assert.False(files[0].IsDirectory);

        // HGLOBAL: the allocation is larger, the stream has the file size
        using (var first = files[0].OpenContent())
        {
            Assert.Equal(FirstContent, ReadAll(first));
        }
        // IStream
        using (var second = files[1].OpenContent())
        {
            Assert.Equal(SecondContent, ReadAll(second));
        }
        Assert.Null(files[2].OpenContent());
        Assert.False(reader.TryGetStream(DataObjectReader.FileContentsFormat, 7, out _));
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    [Fact]
    public void Reader_InProcessDataObject_VirtualFiles()
    {
        using var reader = new DataObjectReader(CreateVirtualFiles());
        AssertVirtualFiles(reader);
    }

    /// <summary>
    /// The read-only streams expose their buffer, so a caller can use it without another copy
    /// </summary>
    [Fact]
    public void Reader_Streams_ExposeTheirBuffer()
    {
        using var reader = new DataObjectReader(CreateVirtualFiles());
        Assert.True(reader.TryGetStream(DataObjectReader.FileContentsFormat, 1, out var stream));
        var memoryStream = Assert.IsType<MemoryStream>(stream);
        Assert.False(memoryStream.CanWrite);
        Assert.True(memoryStream.TryGetBuffer(out var buffer));
        Assert.Equal(SecondContent, buffer.ToArray());

        // HGLOBAL content: the stream has the file size, the buffer can be larger
        using var first = Assert.IsType<MemoryStream>(reader.GetVirtualFiles()[0].OpenContent());
        Assert.False(first.CanWrite);
        Assert.True(first.TryGetBuffer(out var firstBuffer));
        Assert.Equal(FirstContent, firstBuffer.ToArray());
    }

    [Fact]
    public void Reader_Disposed_Throws()
    {
        var reader = new DataObjectReader(CreateVirtualFiles());
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.HasFormat("CF_UNICODETEXT"));
    }

    [Fact]
    public void Reader_AnsiFileGroupDescriptor()
    {
        const int descriptorSize = 332;
        var data = new byte[4 + descriptorSize];
        BitConverter.GetBytes(1).CopyTo(data, 0);
        BitConverter.GetBytes(0x40u).CopyTo(data, 4);
        BitConverter.GetBytes(5u).CopyTo(data, 4 + 68);
        Encoding.ASCII.GetBytes("ansi.txt").CopyTo(data, 4 + 72);
        var dataObject = new TestDataObject();
        dataObject.Add(DataObjectReader.FileGroupDescriptorFormat, -1, TYMED.TYMED_HGLOBAL, data);
        dataObject.Add(DataObjectReader.FileContentsFormat, 0, TYMED.TYMED_HGLOBAL, Encoding.ASCII.GetBytes("12345"));

        using var reader = new DataObjectReader(dataObject);
        var file = Assert.Single(reader.GetVirtualFiles());
        Assert.Equal("ansi.txt", file.Name);
        Assert.Equal(5, file.Size);
        Assert.Null(file.Attributes);
        using var content = file.OpenContent();
        Assert.Equal("12345", new StreamReader(content).ReadToEnd());
    }

    // ── A hostile data source ────────────────────────────────────────────────

    [Fact]
    public void HostileStream_ClaimsToReadMoreThanAsked_ReturnsFalse()
    {
        var dataObject = new TestDataObject();
        dataObject.AddStream("Dapplo.Windows.Tests.Hostile", -1, () => new HostileStream(HostileStream.Mode.LieAboutBytesRead));
        using var reader = new DataObjectReader(dataObject);
        Assert.False(reader.TryGetStream("Dapplo.Windows.Tests.Hostile", out _));
    }

    [Fact]
    public void HostileStream_FailsWhileReading_ReturnsFalse()
    {
        var dataObject = new TestDataObject();
        dataObject.AddStream("Dapplo.Windows.Tests.Hostile", -1, () => new HostileStream(HostileStream.Mode.Fail));
        using var reader = new DataObjectReader(dataObject);
        Assert.False(reader.TryGetStream("Dapplo.Windows.Tests.Hostile", out _));
        Assert.Null(reader.GetAsBytes("Dapplo.Windows.Tests.Hostile"));
    }

    [Fact]
    public void HostileStream_Endless_StopsAtMaxDataSize()
    {
        var dataObject = new TestDataObject();
        dataObject.AddStream(DataObjectReader.FileContentsFormat, 0, () => new HostileStream(HostileStream.Mode.Endless));
        dataObject.Add(DataObjectReader.FileContentsFormat, 1, TYMED.TYMED_HGLOBAL, new byte[2 * 1024 * 1024]);
        using var reader = new DataObjectReader(dataObject) { MaxDataSize = 1024 * 1024 };
        Assert.False(reader.TryGetStream(DataObjectReader.FileContentsFormat, 0, out _));
        // HGLOBAL larger than the limit
        Assert.False(reader.TryGetStream(DataObjectReader.FileContentsFormat, 1, out _));
    }

    [Fact]
    public async Task HostileEnumerator_Endless_DoesNotHang()
    {
        var dataObject = new TestDataObject { Enumerator = new EndlessEnumFormatEtc() };
        using var reader = new DataObjectReader(dataObject);
        var task = Task.Run(() => reader.Formats, TestContext.Current.CancellationToken);
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.True(completed == task, "Enumerating the formats must stop");
        Assert.Single(await task);
    }

    [Theory]
    [InlineData(@"..\..\Windows\System32\evil.dll", "evil.dll")]
    [InlineData(@"C:\Users\Public\x.txt", "x.txt")]
    [InlineData("C:x.txt", "x.txt")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("report.pdf. . .", "report.pdf")]
    [InlineData("a|b<c>d?.txt", "a_b_c_d_.txt")]
    [InlineData("CON", "_CON")]
    [InlineData("nul.txt", "_nul.txt")]
    [InlineData("..", "file")]
    [InlineData("", "file")]
    [InlineData("folder\\", "file")]
    [InlineData("tab\tname.txt", "tab_name.txt")]
    public void VirtualFile_SafeFileName(string name, string expected)
    {
        name = name.Replace("\\t", "\t");
        var descriptor = new byte[4 + 592];
        BitConverter.GetBytes(1).CopyTo(descriptor, 0);
        Encoding.Unicode.GetBytes(name).CopyTo(descriptor, 4 + 72);
        var dataObject = new TestDataObject();
        dataObject.Add(DataObjectReader.FileGroupDescriptorWFormat, -1, TYMED.TYMED_HGLOBAL, descriptor);
        using var reader = new DataObjectReader(dataObject);
        var file = Assert.Single(reader.GetVirtualFiles());
        Assert.Equal(name, file.Name);
        Assert.Equal(expected, file.SafeFileName);
    }

    [Fact]
    public void HostileDescriptor_CountLargerThanTheData_ReadsWhatIsThere()
    {
        var descriptor = new byte[4 + 592 + 100];
        BitConverter.GetBytes(int.MaxValue).CopyTo(descriptor, 0);
        Encoding.Unicode.GetBytes("only.txt").CopyTo(descriptor, 4 + 72);
        var dataObject = new TestDataObject();
        dataObject.Add(DataObjectReader.FileGroupDescriptorWFormat, -1, TYMED.TYMED_HGLOBAL, descriptor);
        using var reader = new DataObjectReader(dataObject);
        Assert.Equal("only.txt", Assert.Single(reader.GetVirtualFiles()).Name);
    }

    // ── Text without synthesized formats (3.2) ──────────────────────────────

    private static byte[] Terminated(byte[] text) => text.Concat(new byte[] { 0, (byte)'x', (byte)'y', 0 }).ToArray();

    [Theory]
    [InlineData(0x0419u, new byte[] { 0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2 }, "Привет")] // Russian: code page 1251
    [InlineData(0x0407u, new byte[] { 0x47, 0x72, 0xFC, 0xDF, 0x65 }, "Grüße")]       // German: code page 1252
    [InlineData(0x0408u, new byte[] { 0xC1, 0xE8 }, "Αθ")]                             // Greek: code page 1253
    public void DataObject_OnlyCfText_UsesTheCodePageOfCfLocale(uint lcid, byte[] ansi, string expected)
    {
        var dataObject = new TestDataObject();
        dataObject.Add("CF_TEXT", -1, TYMED.TYMED_HGLOBAL, Terminated(ansi));
        dataObject.Add("CF_LOCALE", -1, TYMED.TYMED_HGLOBAL, BitConverter.GetBytes(lcid));
        using var reader = new DataObjectReader(dataObject);

        Assert.False(reader.HasFormat("CF_UNICODETEXT"));
        Assert.Equal(expected, reader.GetAsUnicodeString());
        Assert.True(reader.TryGetAsUnicodeString(out var text));
        Assert.Equal(expected, text);
        Assert.Equal(expected, reader.GetAsUnicodeString(StandardClipboardFormats.UnicodeText.AsString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0x12345678u)] // not a locale: the ANSI code page is used
    public void DataObject_OnlyCfText_WithoutUsableLocale_UsesTheAnsiCodePage(uint? lcid)
    {
        var dataObject = new TestDataObject();
        dataObject.Add("CF_TEXT", -1, TYMED.TYMED_HGLOBAL, Terminated(Encoding.ASCII.GetBytes("Plain ANSI text")));
        if (lcid.HasValue)
        {
            dataObject.Add("CF_LOCALE", -1, TYMED.TYMED_HGLOBAL, BitConverter.GetBytes(lcid.Value));
        }
        using var reader = new DataObjectReader(dataObject);
        Assert.Equal("Plain ANSI text", reader.GetAsUnicodeString());
    }

    [Fact]
    public void DataObject_OnlyCfOemText_IsDecoded()
    {
        var dataObject = new TestDataObject();
        dataObject.Add("CF_OEMTEXT", -1, TYMED.TYMED_HGLOBAL, Terminated(Encoding.ASCII.GetBytes("OEM text")));
        using var reader = new DataObjectReader(dataObject);
        Assert.Equal("OEM text", reader.GetAsUnicodeString());
    }

    [Fact]
    public void DataObject_TextFallback_Order()
    {
        // CF_UNICODETEXT first, then CF_TEXT, then CF_OEMTEXT
        var dataObject = new TestDataObject();
        dataObject.Add("CF_OEMTEXT", -1, TYMED.TYMED_HGLOBAL, Terminated(Encoding.ASCII.GetBytes("oem")));
        dataObject.Add("CF_TEXT", -1, TYMED.TYMED_HGLOBAL, Terminated(Encoding.ASCII.GetBytes("ansi")));
        using (var reader = new DataObjectReader(dataObject))
        {
            Assert.Equal("ansi", reader.GetAsUnicodeString());
        }
        dataObject.Add("CF_UNICODETEXT", -1, TYMED.TYMED_HGLOBAL, Encoding.Unicode.GetBytes("unicode\0"));
        using (var reader = new DataObjectReader(dataObject))
        {
            Assert.Equal("unicode", reader.GetAsUnicodeString());
        }
    }

    [Fact]
    public void DataObject_TextFallback_EmptyAndMissing()
    {
        var dataObject = new TestDataObject();
        dataObject.Add("CF_TEXT", -1, TYMED.TYMED_HGLOBAL, new byte[] { 0, 65, 0 });
        using (var reader = new DataObjectReader(dataObject))
        {
            Assert.Equal("", reader.GetAsUnicodeString());
            // Only for CF_UNICODETEXT, not for other formats
            Assert.Null(reader.GetAsUnicodeString("Dapplo.Windows.Tests.OtherText"));
        }
        using (var reader = new DataObjectReader(new TestDataObject()))
        {
            Assert.Null(reader.GetAsUnicodeString());
            Assert.False(reader.TryGetAsUnicodeString(out var text));
            Assert.Null(text);
        }
    }

    [Fact]
    public void OtherDataSource_OnlyCfText_FallsBackToo()
    {
        var source = new DictionarySource
        {
            ["CF_TEXT"] = Terminated(new byte[] { 0x47, 0x72, 0xFC, 0xDF, 0x65 }),
            ["CF_LOCALE"] = BitConverter.GetBytes(0x0407u)
        };
        Assert.Equal("Grüße", source.GetAsUnicodeString());
    }

    // ── Virtual files (3.2) ──────────────────────────────────────────────────

    [Fact]
    public void HasVirtualFiles_BothDescriptorFormats()
    {
        using (var reader = new DataObjectReader(CreateVirtualFiles()))
        {
            Assert.True(reader.HasVirtualFiles());
        }
        var ansi = new TestDataObject();
        ansi.Add(DataObjectReader.FileGroupDescriptorFormat, -1, TYMED.TYMED_HGLOBAL, new byte[4]);
        using (var reader = new DataObjectReader(ansi))
        {
            Assert.True(reader.HasVirtualFiles());
        }
        var text = new TestDataObject();
        text.Add("CF_UNICODETEXT", -1, TYMED.TYMED_HGLOBAL, Encoding.Unicode.GetBytes("text\0"));
        using (var reader = new DataObjectReader(text))
        {
            Assert.False(reader.HasVirtualFiles());
        }
        Assert.True(new DictionarySource { [DataObjectReader.FileGroupDescriptorWFormat] = new byte[4] }.HasVirtualFiles());
        Assert.Throws<ArgumentNullException>(() => ((IClipboardDataSource)null).HasVirtualFiles());
    }

    /// <summary>
    /// An IClipboardDataSource of another library, e.g. an adapter for a WPF data object
    /// </summary>
    private sealed class DictionarySource : Dictionary<string, byte[]>, IClipboardDataSource
    {
        public IReadOnlyCollection<string> Formats => Keys.ToList();

        public bool HasFormat(string format) => format != null && ContainsKey(format);

        public bool TryGetStream(string format, out Stream stream)
        {
            stream = format != null && TryGetValue(format, out var bytes) ? new MemoryStream(bytes, false) : null;
            return stream != null;
        }
    }

    [Fact]
    public async Task GetOleDataObject_OnMtaThread_Throws()
    {
        var exception = await Task.Run(() => Record.Exception(() => ClipboardNative.GetOleDataObject()));
        Assert.IsType<InvalidOperationException>(exception);
    }
}

/// <summary>
/// Through the real OLE clipboard: OleSetClipboard with the in-process data object, read back with ClipboardNative.GetOleDataObject
/// </summary>
/// <remarks>Interactive: these tests change the clipboard. They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class OleClipboardTests
{
    [WpfFact]
    public void GetOleDataObject_ReadsVirtualFilesThroughOle()
    {
        // Like a WinForms / WPF UI thread: STA with OLE initialized (S_OK, or S_FALSE when it already was)
        var oleInitialize = OleInitialize(IntPtr.Zero);
        Assert.True(oleInitialize is 0 or 1, $"OleInitialize returned 0x{oleInitialize:X}");
        try
        {
            // Clipboard monitors (e.g. the clipboard history) open the clipboard right after every change, retry like an application would
            var dataObject = DataObjectReaderTests.CreateVirtualFiles();
            var hResult = 0;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                hResult = OleSetClipboard(dataObject);
                if (hResult != unchecked((int)0x800401D0))
                {
                    break;
                }
                Thread.Sleep(50);
            }
            Assert.Equal(0, hResult);
            try
            {
                using var reader = ClipboardNative.GetOleDataObject();
                DataObjectReaderTests.AssertVirtualFiles(reader);
            }
            finally
            {
                OleSetClipboard(null);
            }
        }
        finally
        {
            OleUninitialize();
        }
    }

    /// <summary>
    /// OleSetClipboard, retrying while clipboard monitors (e.g. the clipboard history) have the clipboard open
    /// </summary>
    private static void SetOleClipboard(IDataObject dataObject)
    {
        var hResult = 0;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            hResult = OleSetClipboard(dataObject);
            if (hResult != unchecked((int)0x800401D0))
            {
                break;
            }
            Thread.Sleep(50);
        }
        Assert.Equal(0, hResult);
    }

    private static readonly string[] DescriptorFormats = { DataObjectReader.FileGroupDescriptorWFormat, DataObjectReader.FileGroupDescriptorFormat };

    [WpfFact]
    public async Task Snapshot_TryUseVirtualFiles_ReadsThemThroughOle()
    {
        var restore = await ClipboardRestore.SaveAsync();
        var oleInitialize = OleInitialize(IntPtr.Zero);
        Assert.True(oleInitialize is 0 or 1, $"OleInitialize returned 0x{oleInitialize:X}");
        try
        {
            SetOleClipboard(DataObjectReaderTests.CreateVirtualFiles());
            try
            {
                Assert.True(ClipboardNative.HasVirtualFiles());
                var snapshot = await ClipboardNative.ReadSnapshotAsync(DescriptorFormats);
                Assert.True(snapshot.HasVirtualFiles());

                IReadOnlyList<VirtualFile> captured = null;
                Assert.True(snapshot.TryUseVirtualFiles(files =>
                {
                    captured = files;
                    using var first = files[0].OpenContent();
                    using var copy = new MemoryStream();
                    first.CopyTo(copy);
                    return (Names: files.Select(f => f.Name).ToList(), First: copy.ToArray());
                }, out var result));
                Assert.Equal(new[] { "Report ä.txt", @"Folder\Image.bin", "Folder" }, result.Names);
                Assert.Equal(DataObjectReaderTests.FirstContent, result.First);

                // The data object was released when use returned: the files can't be read any more
                Assert.Throws<ObjectDisposedException>(() => captured[1].OpenContent());

                // maxDataSize: the second file (200,000 bytes) isn't read, the descriptor and the first file are
                Assert.True(snapshot.TryUseVirtualFiles(files => files[1].OpenContent() == null && files[0].OpenContent() != null, out var limited, 100_000));
                Assert.True(limited);

                // Exceptions of use are passed on
                Assert.Throws<FormatException>(() => snapshot.TryUseVirtualFiles<bool>(_ => throw new FormatException(), out _));
                Assert.Throws<ArgumentNullException>(() => snapshot.TryUseVirtualFiles<bool>(null, out _));
                Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.TryUseVirtualFiles(_ => true, out _, 0));

                // Not on an STA thread
                var calledOnMta = false;
                var onMta = await Task.Run(() => snapshot.TryUseVirtualFiles(_ => calledOnMta = true, out _));
                Assert.False(onMta);
                Assert.False(calledOnMta);

                // A snapshot without the descriptor
                var textOnly = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_UNICODETEXT" });
                Assert.False(textOnly.TryUseVirtualFiles(_ => true, out var none));
                Assert.False(none);

                // The clipboard changed since the snapshot
                SetOleClipboard(DataObjectReaderTests.CreateVirtualFiles());
                Assert.NotEqual(snapshot.SequenceNumber, ClipboardNative.SequenceNumber);
                var calledAfterChange = false;
                Assert.False(snapshot.TryUseVirtualFiles(_ => calledAfterChange = true, out _));
                Assert.False(calledAfterChange);
            }
            finally
            {
                OleSetClipboard(null);
            }
        }
        finally
        {
            OleUninitialize();
            await restore.RestoreAsync();
        }
    }

    [DllImport("ole32")]
    private static extern int OleInitialize(IntPtr reserved);

    [DllImport("ole32")]
    private static extern void OleUninitialize();

    [DllImport("ole32")]
    private static extern int OleSetClipboard(IDataObject dataObject);
}

/// <summary>
/// A minimal OLE data object, with data per format and index
/// </summary>
[ComVisible(true)]
public sealed class TestDataObject : IDataObject
{
    private const int DvEFormatEtc = unchecked((int)0x80040064);
    private const int DvELindex = unchecked((int)0x80040068);
    private const int DvETymed = unchecked((int)0x80040069);
    private const int OleEAdviseNotSupported = unchecked((int)0x80040003);
    private const int DataSSameFormatEtc = 0x00040130;

    private readonly List<(short Format, int Index, TYMED Tymed, byte[] Data, int ExtraAllocation)> _entries = new();
    private readonly Dictionary<(short Format, int Index), Func<IStream>> _streams = new();

    /// <summary>
    /// Deliver a format as a custom IStream, e.g. a misbehaving one
    /// </summary>
    public void AddStream(string format, int index, Func<IStream> stream)
    {
        var formatId = unchecked((short)ClipboardFormatExtensions.MapFormatToId(format));
        _entries.Add((formatId, index, TYMED.TYMED_ISTREAM, Array.Empty<byte>(), 0));
        _streams[(formatId, index)] = stream;
    }

    /// <summary>
    /// When set, EnumFormatEtc returns this enumerator
    /// </summary>
    public IEnumFORMATETC Enumerator { get; set; }

    public void Add(string format, int index, TYMED tymed, byte[] data, int extraAllocation = 0) =>
        _entries.Add((unchecked((short)ClipboardFormatExtensions.MapFormatToId(format)), index, tymed, data, extraAllocation));

    public void GetData(ref FORMATETC format, out STGMEDIUM medium)
    {
        var request = format;
        var candidates = _entries.Where(e => e.Format == request.cfFormat).ToList();
        if (candidates.Count == 0)
        {
            throw new COMException("Format not available", DvEFormatEtc);
        }
        var entry = candidates.FirstOrDefault(e => e.Index == request.lindex || (request.lindex == -1 && candidates.Count == 1));
        if (entry.Data == null)
        {
            throw new COMException("Index not available", DvELindex);
        }
        if ((request.tymed & entry.Tymed) == 0)
        {
            throw new COMException("Medium not available", DvETymed);
        }
        medium = new STGMEDIUM { tymed = entry.Tymed, pUnkForRelease = null };
        if (entry.Tymed == TYMED.TYMED_HGLOBAL)
        {
            var hGlobal = GlobalAlloc(0x0042, new UIntPtr((uint)(entry.Data.Length + entry.ExtraAllocation)));
            var pointer = GlobalLock(hGlobal);
            Marshal.Copy(entry.Data, 0, pointer, entry.Data.Length);
            GlobalUnlock(hGlobal);
            medium.unionmember = hGlobal;
        }
        else
        {
            var stream = _streams.TryGetValue((entry.Format, entry.Index), out var factory) ? factory() : new TestStream(entry.Data);
            medium.unionmember = Marshal.GetComInterfaceForObject(stream, typeof(IStream));
        }
    }

    public void GetDataHere(ref FORMATETC format, ref STGMEDIUM medium) => throw new NotImplementedException();

    public int QueryGetData(ref FORMATETC format)
    {
        var request = format;
        return _entries.Any(e => e.Format == request.cfFormat && (request.tymed & e.Tymed) != 0) ? 0 : DvEFormatEtc;
    }

    public int GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut)
    {
        formatOut = formatIn;
        formatOut.ptd = IntPtr.Zero;
        return DataSSameFormatEtc;
    }

    public void SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release) => throw new NotImplementedException();

    public IEnumFORMATETC EnumFormatEtc(DATADIR direction)
    {
        if (direction != DATADIR.DATADIR_GET)
        {
            throw new NotImplementedException();
        }
        if (Enumerator != null)
        {
            return Enumerator;
        }
        var formats = _entries
            .GroupBy(e => e.Format)
            .Select(g => new FORMATETC
            {
                cfFormat = g.Key,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = g.Aggregate(TYMED.TYMED_NULL, (tymed, e) => tymed | e.Tymed)
            })
            .ToArray();
        return new TestEnumFormatEtc(formats);
    }

    public int DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection)
    {
        connection = 0;
        return OleEAdviseNotSupported;
    }

    public void DUnadvise(int connection) => throw new COMException("Not supported", OleEAdviseNotSupported);

    public int EnumDAdvise(out IEnumSTATDATA enumAdvise)
    {
        enumAdvise = null;
        return OleEAdviseNotSupported;
    }

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
}

[ComVisible(true)]
public sealed class TestEnumFormatEtc : IEnumFORMATETC
{
    private readonly FORMATETC[] _formats;
    private int _position;

    public TestEnumFormatEtc(FORMATETC[] formats) => _formats = formats;

    public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
    {
        var fetched = 0;
        while (fetched < celt && _position < _formats.Length)
        {
            rgelt[fetched++] = _formats[_position++];
        }
        if (pceltFetched != null && pceltFetched.Length > 0)
        {
            pceltFetched[0] = fetched;
        }
        return fetched == celt ? 0 : 1;
    }

    public int Skip(int celt)
    {
        _position = Math.Min(_formats.Length, _position + celt);
        return _position < _formats.Length ? 0 : 1;
    }

    public int Reset()
    {
        _position = 0;
        return 0;
    }

    public void Clone(out IEnumFORMATETC newEnum) => newEnum = new TestEnumFormatEtc(_formats) { };
}

/// <summary>
/// An IStream which misbehaves
/// </summary>
[ComVisible(true)]
public sealed class HostileStream : IStream
{
    public enum Mode
    {
        LieAboutBytesRead,
        Fail,
        Endless
    }

    private readonly Mode _mode;

    public HostileStream(Mode mode) => _mode = mode;

    public void Read(byte[] pv, int cb, IntPtr pcbRead)
    {
        switch (_mode)
        {
            case Mode.Fail:
                throw new COMException("Read failed", unchecked((int)0x80030005));
            case Mode.LieAboutBytesRead:
                Marshal.WriteInt32(pcbRead, cb * 1000);
                return;
            default:
                Marshal.WriteInt32(pcbRead, cb);
                return;
        }
    }

    public void Write(byte[] pv, int cb, IntPtr pcbWritten) => throw new NotImplementedException();
    public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) => throw new NotImplementedException();
    public void SetSize(long libNewSize) => throw new NotImplementedException();
    public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) => throw new NotImplementedException();
    public void Commit(int grfCommitFlags) { }
    public void Revert() => throw new NotImplementedException();
    public void LockRegion(long libOffset, long cb, int dwLockType) => throw new NotImplementedException();
    public void UnlockRegion(long libOffset, long cb, int dwLockType) => throw new NotImplementedException();
    public void Stat(out STATSTG pstatstg, int grfStatFlag) => pstatstg = new STATSTG { type = 2 };
    public void Clone(out IStream ppstm) => ppstm = new HostileStream(_mode);
}

/// <summary>
/// An enumerator which never ends, always returning CF_UNICODETEXT
/// </summary>
[ComVisible(true)]
public sealed class EndlessEnumFormatEtc : IEnumFORMATETC
{
    public int Next(int celt, FORMATETC[] rgelt, int[] pceltFetched)
    {
        for (var i = 0; i < celt; i++)
        {
            rgelt[i] = new FORMATETC { cfFormat = (short)StandardClipboardFormats.UnicodeText, dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL };
        }
        if (pceltFetched != null && pceltFetched.Length > 0)
        {
            pceltFetched[0] = celt;
        }
        return 0;
    }

    public int Skip(int celt) => 0;
    public int Reset() => 0;
    public void Clone(out IEnumFORMATETC newEnum) => newEnum = new EndlessEnumFormatEtc();
}

/// <summary>
/// An IStream over a byte array
/// </summary>
[ComVisible(true)]
public sealed class TestStream : IStream
{
    private readonly byte[] _data;
    private long _position;

    public TestStream(byte[] data) => _data = data;

    public void Read(byte[] pv, int cb, IntPtr pcbRead)
    {
        var count = (int)Math.Max(0, Math.Min(cb, _data.Length - _position));
        Array.Copy(_data, _position, pv, 0, count);
        _position += count;
        if (pcbRead != IntPtr.Zero)
        {
            Marshal.WriteInt32(pcbRead, count);
        }
    }

    public void Write(byte[] pv, int cb, IntPtr pcbWritten) => throw new NotImplementedException();

    public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
    {
        _position = dwOrigin switch
        {
            0 => dlibMove,
            1 => _position + dlibMove,
            _ => _data.Length + dlibMove
        };
        if (plibNewPosition != IntPtr.Zero)
        {
            Marshal.WriteInt64(plibNewPosition, _position);
        }
    }

    public void SetSize(long libNewSize) => throw new NotImplementedException();

    public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) => throw new NotImplementedException();

    public void Commit(int grfCommitFlags)
    {
    }

    public void Revert() => throw new NotImplementedException();

    public void LockRegion(long libOffset, long cb, int dwLockType) => throw new NotImplementedException();

    public void UnlockRegion(long libOffset, long cb, int dwLockType) => throw new NotImplementedException();

    public void Stat(out STATSTG pstatstg, int grfStatFlag) => pstatstg = new STATSTG { cbSize = _data.Length, type = 2 };

    public void Clone(out IStream ppstm) => ppstm = new TestStream(_data) { _position = _position };
}
