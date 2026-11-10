// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// IStream (and ISequentialStream), with the buffers of Read and Write as pointers.
/// System.Runtime.InteropServices.ComTypes.IStream marshals them as byte[], which allocates a new managed array for every call
/// a decoder or encoder makes, and for a large chunk on the large object heap.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/objidl/nn-objidl-istream">IStream interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("0000000c-0000-0000-C000-000000000046")]
internal interface IWicStream
{
    // ISequentialStream
    void Read(nint pv, int cb, nint pcbRead);
    void Write(nint pv, int cb, nint pcbWritten);

    // IStream
    void Seek(long dlibMove, int dwOrigin, nint plibNewPosition);
    void SetSize(long libNewSize);
    void CopyTo(IWicStream pstm, long cb, nint pcbRead, nint pcbWritten);
    void Commit(int grfCommitFlags);
    void Revert();
    void LockRegion(long libOffset, long cb, int dwLockType);
    void UnlockRegion(long libOffset, long cb, int dwLockType);
    void Stat(out STATSTG pstatstg, int grfStatFlag);
    void Clone(out IWicStream ppstm);
}
