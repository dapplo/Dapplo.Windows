// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Dapplo.Windows.Vfw32.Structs
{
    /// <summary>
    /// Information about a codec.
    /// </summary>
    public class CodecInfo
    {
        /// <summary>
        /// Creates a new instance of <see cref="CodecInfo"/>.
        /// </summary>
        public CodecInfo(FourCC codec, string name)
        {
            Codec = codec;
            Name = name;
        }

        /// <summary>VfwCodec ID.</summary>
        public FourCC Codec { get; }

        /// <summary>
        /// Descriptive codec name that may be show to a user.
        /// </summary>
        public string Name { get; }
    }
}
