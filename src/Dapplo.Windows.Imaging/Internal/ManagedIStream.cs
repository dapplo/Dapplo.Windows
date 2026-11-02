// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// COM IStream over a .NET Stream, the position of the stream when this is created is offset 0 for COM,
/// so a decoder or encoder can work on a part of a larger stream. The stream must be seekable.
/// Exceptions of the stream are kept in <see cref="LastException"/> and become an HRESULT for COM.
/// </summary>
[ComVisible(true)]
internal sealed class ManagedIStream : IStream, IFailureSource
{
    private const int StreamSeekSet = 0;
    private const int StreamSeekCur = 1;
    private const int StreamSeekEnd = 2;
    private const int StgTyStream = 2;
    private const int StgmRead = 0x0;
    private const int StgmWrite = 0x1;
    private const int StgmReadWrite = 0x2;
    private const int StgEInvalidFunction = unchecked((int)0x80030001);

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

    /// <summary>
    /// The highest position (relative to the origin) which was written, the end of what an encoder wrote
    /// </summary>
    public long WrittenLength { get; private set; }

    /// <summary>
    /// Position the stream after what was written
    /// </summary>
    public void SeekToEndOfWritten() => _stream.Position = _origin + WrittenLength;

    private T Guard<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            LastException = ex;
            throw;
        }
    }

    public void Read(byte[] pv, int cb, IntPtr pcbRead)
    {
        var total = Guard(() =>
        {
            int read = 0;
            while (read < cb)
            {
                var count = _stream.Read(pv, read, cb - read);
                if (count <= 0)
                {
                    break;
                }
                read += count;
            }
            return read;
        });
        if (pcbRead != IntPtr.Zero)
        {
            Marshal.WriteInt32(pcbRead, total);
        }
    }

    public void Write(byte[] pv, int cb, IntPtr pcbWritten)
    {
        Guard(() =>
        {
            _stream.Write(pv, 0, cb);
            WrittenLength = Math.Max(WrittenLength, _stream.Position - _origin);
            return cb;
        });
        if (pcbWritten != IntPtr.Zero)
        {
            Marshal.WriteInt32(pcbWritten, cb);
        }
    }

    public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
    {
        var position = Guard(() =>
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
            return target - _origin;
        });
        if (plibNewPosition != IntPtr.Zero)
        {
            Marshal.WriteInt64(plibNewPosition, position);
        }
    }

    public void SetSize(long libNewSize) => Guard(() =>
    {
        _stream.SetLength(_origin + libNewSize);
        return 0;
    });

    public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
    {
        var buffer = new byte[81920];
        long copied = Guard(() =>
        {
            long total = 0;
            while (total < cb)
            {
                var read = _stream.Read(buffer, 0, (int)Math.Min(buffer.Length, cb - total));
                if (read <= 0)
                {
                    break;
                }
                pstm.Write(buffer, read, IntPtr.Zero);
                total += read;
            }
            return total;
        });
        if (pcbRead != IntPtr.Zero)
        {
            Marshal.WriteInt64(pcbRead, copied);
        }
        if (pcbWritten != IntPtr.Zero)
        {
            Marshal.WriteInt64(pcbWritten, copied);
        }
    }

    public void Commit(int grfCommitFlags) => Guard(() =>
    {
        _stream.Flush();
        return 0;
    });

    public void Revert()
    {
        // Not transacted, nothing to revert
    }

    public void LockRegion(long libOffset, long cb, int dwLockType) => throw new COMException("LockRegion is not supported", StgEInvalidFunction);

    public void UnlockRegion(long libOffset, long cb, int dwLockType) => throw new COMException("UnlockRegion is not supported", StgEInvalidFunction);

    public void Stat(out STATSTG pstatstg, int grfStatFlag)
    {
        var length = Guard(() => _stream.Length - _origin);
        pstatstg = new STATSTG
        {
            type = StgTyStream,
            cbSize = length,
            grfMode = _stream.CanRead && _stream.CanWrite ? StgmReadWrite : _stream.CanWrite ? StgmWrite : StgmRead
        };
    }

    public void Clone(out IStream ppstm) => throw new COMException("Clone is not supported", StgEInvalidFunction);
}
