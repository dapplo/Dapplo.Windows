// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Shell32.Enums;

namespace Dapplo.Windows.Shell32.Structs
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ShFileOp
    {
        public IntPtr hWnd;
        [MarshalAs(UnmanagedType.U4)]
        public FileFuncFlags wFunc;
        public string pFrom;
        public string pTo;
        public FILEOP_FLAGS fFlags;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }
}
