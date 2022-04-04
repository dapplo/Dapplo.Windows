// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Advapi32.Enums;

/// <summary>
/// Specifies the option to apply when opening the key. 
/// </summary>
[Flags]
public enum RegistryOpenOptions
{
    /// <summary>
    /// No options
    /// </summary>
    None = 0,
    /// <summary>
    /// The key is a symbolic link. Registry symbolic links should only be used when absolutely necessary.
    /// </summary>
    OpenLink = 8

}