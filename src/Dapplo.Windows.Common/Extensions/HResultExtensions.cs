// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Common.Extensions;

/// <summary>
///     Extensions to handle the HResult
/// </summary>
public static class HResultExtensions
{
    /// <summary>
    ///     Test if the HResult respresents a fail
    /// </summary>
    /// <param name="hResult">HResult</param>
    /// <returns>bool</returns>
    [Pure]
    public static bool Failed(this HResult hResult)
    {
        return hResult < 0;
    }

    /// <summary>
    ///     Test if the HResult represents a success
    /// </summary>
    /// <param name="hResult">HResult</param>
    /// <returns>bool</returns>
    [Pure]
    public static bool Succeeded(this HResult hResult)
    {
        return hResult >= HResult.S_OK;
    }

    /// <summary>
    ///     Throw an exception on Failure
    /// </summary>
    /// <param name="hResult">HResult</param>
    public static void ThrowOnFailure(this HResult hResult)
    {
        if (Failed(hResult))
        {
            throw Marshal.GetExceptionForHR((int) hResult);
        }
    }
}