//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

#endregion

using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Common.Enums
{
    /// <summary>
    ///     The HRESULT represents Windows error codes
    ///     See <a href="https://en.wikipedia.org/wiki/HRESULT">wikipedia</a>
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HResult
    {
#pragma warning disable 1591
        S_OK = 0,
        S_FALSE = 1,
        E_FAIL = unchecked((int) 0x80004005),
        E_INVALIDARG = unchecked((int) 0x80070057),
        E_NOTIMPL = unchecked((int) 0x80004001),
        E_POINTER = unchecked((int) 0x80004003),
        E_PENDING = unchecked((int) 0x8000000A),
        E_NOINTERFACE = unchecked((int) 0x80004002),
        E_ABORT = unchecked((int) 0x80004004),
        E_ACCESSDENIED = unchecked((int) 0x80070006),
        E_HANDLE = unchecked((int) 0x80070006),
        E_UNEXPECTED = unchecked((int) 0x8000FFFF),
        E_FILENOTFOUND = unchecked((int) 0x80070002),
        E_PATHNOTFOUND = unchecked((int) 0x80070003),
        E_INVALID_DATA = unchecked((int) 0x8007000D),
        E_OUTOFMEMORY = unchecked((int) 0x8007000E),
        E_INSUFFICIENT_BUFFER = unchecked((int) 0x8007007A),
        WSAECONNABORTED = unchecked((int) 0x80072745),
        WSAECONNRESET = unchecked((int) 0x80072746),
        ERROR_TOO_MANY_CMDS = unchecked((int) 0x80070038),
        ERROR_NOT_SUPPORTED = unchecked((int) 0x80070032),
        TYPE_E_ELEMENTNOTFOUND = unchecked((int) 0x8002802B)
#pragma warning restore 1591
    }
}