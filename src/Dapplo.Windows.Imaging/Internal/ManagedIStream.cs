// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Imaging.Interop;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// COM IStream over a .NET Stream, the position of the stream when this is created is offset 0 for COM,
/// so a decoder or encoder can work on a part of a larger stream. The stream must be seekable.
/// Exceptions of the stream are kept in <see cref="LastException"/> and become an HRESULT for COM.
/// </summary>
/// <remarks>
/// Read and Write get the native buffer of the caller (<see cref="IWicStream"/>): a MemoryStream is copied from or to directly,
/// other streams read and write the native memory directly (.NET) or through a pooled buffer (.NET Framework).
/// No managed array is allocated for a call, the decoders and encoders call these a lot.
/// </remarks>
[ComVisible(true)]
internal sealed class ManagedIStream : IWicStream, IFailureSource
{
    private const int StreamSeekSet = 0;
    private const int StreamSeekCur = 1;
    private const int StreamSeekEnd = 2;
    private const int StgTyStream = 2;
    private const int StgmRead = 0x0;
    private const int StgmWrite = 0x1;
    private const int StgmReadWrite = 0x2;
    private const int StgEInvalidFunction = unchecked((int)0x80030001);
    // Below the large object heap threshold (85000 bytes)
    private const int CopyBufferSize = 81920;

    private readonly Stream _stream;
    private readonly long _origin;

    public ManagedIStream(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (!stream.CanSeek)
        {
            throw new ArgumentException("The stream must be seekable", nameof(stream));
        }
        _origin = stream.Position;
    }

    public Exception LastException { get; private set; }

    public long WrittenLength { get; private set; }

    public void SeekToEndOfWritten() => _stream.Position = _origin + WrittenLength;

    public unsafe void Read(nint pv, int cb, nint pcbRead)
    {
        int total;
        try
        {
            total = cb <= 0 ? 0 : ReadInto((byte*)pv, cb);
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
        if (pcbRead != 0)
        {
            Marshal.WriteInt32(pcbRead, total);
        }
    }

    /// <summary>
    /// Read up to count bytes into the native buffer, less only at the end of the stream
    /// </summary>
    private unsafe int ReadInto(byte* target, int count)
    {
        if (_stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var segment))
        {
            var position = memoryStream.Position;
            var available = (int)Math.Max(0, Math.Min(count, segment.Count - position));
            if (available > 0)
            {
                new ReadOnlySpan<byte>(segment.Array, segment.Offset + (int)position, available).CopyTo(new Span<byte>(target, available));
                memoryStream.Position = position + available;
            }
            return available;
        }

        var read = 0;
#if NET
        while (read < count)
        {
            var bytesRead = _stream.Read(new Span<byte>(target + read, count - read));
            if (bytesRead <= 0)
            {
                break;
            }
            read += bytesRead;
        }
#else
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(count, CopyBufferSize));
        try
        {
            while (read < count)
            {
                var bytesRead = _stream.Read(buffer, 0, Math.Min(buffer.Length, count - read));
                if (bytesRead <= 0)
                {
                    break;
                }
                new ReadOnlySpan<byte>(buffer, 0, bytesRead).CopyTo(new Span<byte>(target + read, bytesRead));
                read += bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
        return read;
    }

    public unsafe void Write(nint pv, int cb, nint pcbWritten)
    {
        try
        {
            if (cb > 0)
            {
                WriteFrom((byte*)pv, cb);
            }
            WrittenLength = Math.Max(WrittenLength, _stream.Position - _origin);
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
        if (pcbWritten != 0)
        {
            Marshal.WriteInt32(pcbWritten, cb);
        }
    }

    private unsafe void WriteFrom(byte* source, int count)
    {
#if NET
        _stream.Write(new ReadOnlySpan<byte>(source, count));
#else
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(count, CopyBufferSize));
        try
        {
            var written = 0;
            while (written < count)
            {
                var chunk = Math.Min(buffer.Length, count - written);
                new ReadOnlySpan<byte>(source + written, chunk).CopyTo(buffer);
                _stream.Write(buffer, 0, chunk);
                written += chunk;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
    }

    public void Seek(long dlibMove, int dwOrigin, nint plibNewPosition)
    {
        long position;
        try
        {
            long target = dwOrigin switch
            {
                StreamSeekSet => _origin + dlibMove,
                StreamSeekCur => _stream.Position + dlibMove,
                StreamSeekEnd => _stream.Length + dlibMove,
                _ => throw new ArgumentOutOfRangeException(nameof(dwOrigin))
            };
            if (target < _origin)
            {
                throw new IOException("Seek before the start of the stream");
            }
            _stream.Position = target;
            position = target - _origin;
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
        if (plibNewPosition != 0)
        {
            Marshal.WriteInt64(plibNewPosition, position);
        }
    }

    public void SetSize(long libNewSize)
    {
        try
        {
            _stream.SetLength(_origin + libNewSize);
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
    }

    public unsafe void CopyTo(IWicStream pstm, long cb, nint pcbRead, nint pcbWritten)
    {
        long copied = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);
        try
        {
            fixed (byte* bufferPointer = buffer)
            {
                while (copied < cb)
                {
                    var read = ReadInto(bufferPointer, (int)Math.Min(CopyBufferSize, cb - copied));
                    if (read <= 0)
                    {
                        break;
                    }
                    pstm.Write((nint)bufferPointer, read, 0);
                    copied += read;
                }
            }
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        if (pcbRead != 0)
        {
            Marshal.WriteInt64(pcbRead, copied);
        }
        if (pcbWritten != 0)
        {
            Marshal.WriteInt64(pcbWritten, copied);
        }
    }

    public void Commit(int grfCommitFlags)
    {
        try
        {
            _stream.Flush();
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
    }

    public void Revert()
    {
        // Not transacted, nothing to revert
    }

    public void LockRegion(long libOffset, long cb, int dwLockType) => throw new COMException("LockRegion is not supported", StgEInvalidFunction);

    public void UnlockRegion(long libOffset, long cb, int dwLockType) => throw new COMException("UnlockRegion is not supported", StgEInvalidFunction);

    public void Stat(out STATSTG pstatstg, int grfStatFlag)
    {
        long length;
        try
        {
            length = _stream.Length - _origin;
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
        pstatstg = new STATSTG
        {
            type = StgTyStream,
            cbSize = length,
            grfMode = _stream.CanRead && _stream.CanWrite ? StgmReadWrite : _stream.CanWrite ? StgmWrite : StgmRead
        };
    }

    public void Clone(out IWicStream ppstm) => throw new COMException("Clone is not supported", StgEInvalidFunction);
}
