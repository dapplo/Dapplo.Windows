// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// WICComponentEnumerateOptions from wincodec.idl
/// </summary>
[Flags]
internal enum WicComponentEnumerateOptions : uint
{
    Default = 0x00000000,
    Refresh = 0x00000001,
    BuiltInOnly = 0x20000000,
    Unsigned = 0x40000000,
    Disabled = 0x80000000
}
