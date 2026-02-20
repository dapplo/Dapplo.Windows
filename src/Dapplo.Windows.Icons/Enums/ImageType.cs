// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Icons.Enums;

/// <summary>
/// Used for the type in LoadImage or CopyImage
/// </summary>
public enum ImageType : uint
{
    /// <summary>
    /// Copies / loads a bitmap.
    /// </summary>
    IMAGE_BITMAP = 0,
    /// <summary>
    /// Copies / loads a cursor
    /// </summary>
    IMAGE_ICON = 1,
    /// <summary>
    /// Copies / loads an icon
    /// </summary>
    IMAGE_CURSOR = 2
}