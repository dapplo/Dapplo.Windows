// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// A managed object called by WIC (stream, pixel source) which keeps the exception it turned into an HRESULT,
/// so the original exception (e.g. an IOException of the stream or one of a row callback) reaches the caller.
/// </summary>
internal interface IFailureSource
{
    Exception LastException { get; }
}
