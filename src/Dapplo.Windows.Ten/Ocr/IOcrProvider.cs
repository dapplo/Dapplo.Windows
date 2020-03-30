// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Dapplo.Windows.Ten.Ocr
{
    /// <summary>
    /// This interface describes something that can do OCR of a bitmap
    /// </summary>
    public interface IOcrProvider
    {
        /// <summary>
        /// Start the actual OCR
        /// </summary>
        /// <param name="image">Image</param>
        /// <returns>OcrInformation</returns>
        Task<OcrInformation> DoOcrAsync(Image image);

        /// <summary>
        /// Start the actual OCR
        /// </summary>
        /// <param name="imageStream">Stream</param>
        /// <returns>OcrInformation</returns>
        Task<OcrInformation> DoOcrAsync(Stream imageStream);
    }
}
