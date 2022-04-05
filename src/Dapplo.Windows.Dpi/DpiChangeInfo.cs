// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Dpi;

/// <summary>
/// Stores information about a DPI change
/// </summary>
public class DpiChangeInfo
{
    /// <summary>
    /// The DPI from before the change
    /// </summary>
    public int PreviousDpi { get; }

    /// <summary>
    /// The new DPI
    /// </summary>
    public int NewDpi { get; }

    /// <summary>
    /// Creates a DpiChangeInfo
    /// </summary>
    /// <param name="previousDpi">uint</param>
    /// <param name="newDpi">uint</param>
    public DpiChangeInfo(int previousDpi, int newDpi)
    {
        PreviousDpi = previousDpi;
        NewDpi = newDpi;
    }
}