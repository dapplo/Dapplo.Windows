// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.Kernel32.Enums
{
#pragma warning disable 1591
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Flags]
    public enum ThreadAccess
    {
        TERMINATE = 0x0001,
        SUSPEND_RESUME = 0x0002,
        GET_CONTEXT = 0x0008,
        SET_CONTEXT = 0x0010,
        SET_INFORMATION = 0x0020,
        QUERY_INFORMATION = 0x0040,
        SET_THREAD_TOKEN = 0x0080,
        IMPERSONATE = 0x0100,
        DIRECT_IMPERSONATION = 0x0200
#pragma warning restore 1591
    }
}