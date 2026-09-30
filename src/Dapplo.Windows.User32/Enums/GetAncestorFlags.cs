// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     The ancestor to be retrieved with <see cref="User32Api.GetAncestor"/>
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum GetAncestorFlags : uint
{
    /// <summary>
    ///     Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
    ///     For a top-level window this is the desktop window.
    /// </summary>
    GA_PARENT = 1,

    /// <summary>
    ///     Retrieves the root window by walking the chain of parent windows.
    /// </summary>
    GA_ROOT = 2,

    /// <summary>
    ///     Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
    /// </summary>
    GA_ROOTOWNER = 3
}
