// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Common.Structs;

/// <summary>
/// Helper for a consistent Equals / GetHashCode of the float based structs
/// </summary>
internal static class FloatHelper
{
    /// <summary>
    /// Hash code which is consistent with float.Equals on all frameworks: 0 and -0 are equal, as are all NaN values
    /// </summary>
    /// <param name="value">float</param>
    /// <returns>int</returns>
    public static int GetHashCode(float value)
    {
        if (value == 0f)
        {
            return 0;
        }
        if (float.IsNaN(value))
        {
            return float.NaN.GetHashCode();
        }
        return value.GetHashCode();
    }
}
