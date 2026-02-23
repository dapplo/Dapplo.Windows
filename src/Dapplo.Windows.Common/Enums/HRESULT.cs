// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Common.Enums;

/// <summary>
///     The HRESULT represents Windows error codes
///     See <a href="https://en.wikipedia.org/wiki/HRESULT">wikipedia</a>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HResult : uint
{
#pragma warning disable 1591
    S_OK = 0,
    S_FALSE = 1,
    E_FAIL = 0x80004005,
    E_INVALIDARG = 0x80070057,
    E_NOTIMPL = 0x80004001,
    E_POINTER = 0x80004003,
    E_PENDING = 0x8000000A,
    E_NOINTERFACE = 0x80004002,
    E_ABORT = 0x80004004,
    E_ACCESSDENIED = 0x80070006,
    E_HANDLE = 0x80070006,
    E_UNEXPECTED = 0x8000FFFF,
    E_FILENOTFOUND = 0x80070002,
    E_PATHNOTFOUND = 0x80070003,
    E_INVALID_DATA = 0x8007000D,
    E_OUTOFMEMORY = 0x8007000E,
    E_INSUFFICIENT_BUFFER = 0x8007007A,
    WSAECONNABORTED = 0x80072745,
    WSAECONNRESET = 0x80072746,
    ERROR_TOO_MANY_CMDS = 0x80070038,
    ERROR_NOT_SUPPORTED = 0x80070032,
    TYPE_E_ELEMENTNOTFOUND = 0x8002802B
#pragma warning restore 1591
}