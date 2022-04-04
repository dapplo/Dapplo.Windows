﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Com;

/// <summary>
///     A simple com wrapper which helps with "using"
/// </summary>
/// <typeparam name="T">Type to wrap</typeparam>
public interface IDisposableCom<out T> : IDisposable
{
    /// <summary>
    ///     The actual com object
    /// </summary>
    T ComObject { get; }
}