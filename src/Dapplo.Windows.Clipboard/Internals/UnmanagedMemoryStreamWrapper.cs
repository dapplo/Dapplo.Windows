// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// This wraps an UnmanagedMemoryStream, to also take care or disposing some disposable
/// </summary>
internal class UnmanagedMemoryStreamWrapper : UnmanagedMemoryStream
{
    private IDisposable _disposable;

    public unsafe UnmanagedMemoryStreamWrapper(byte* bytes, long length, long capacity, FileAccess fileAccess) : base(bytes, length, capacity, fileAccess)
    {
    }

    public void SetDisposable(IDisposable disposable)
    {
        _disposable = disposable;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _disposable?.Dispose();
    }
}