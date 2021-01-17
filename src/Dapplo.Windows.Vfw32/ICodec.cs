// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Vfw32
{
    /// <summary>
    /// Interface for codecs
    /// </summary>
    public interface ICodec : IDisposable
    {
        /// <summary>
        /// Prepare the codec to do it's work
        /// </summary>
        void Initialize();


    }
}
