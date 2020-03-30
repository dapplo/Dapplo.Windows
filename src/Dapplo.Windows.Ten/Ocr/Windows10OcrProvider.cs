// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Dapplo.Windows.Ten.Ocr
{
	/// <summary>
	/// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
	/// </summary>
	public class Windows10OcrProvider : IOcrProvider
    {
        private readonly OcrEngine _ocrEngine;

        /// <summary>
        /// Create an OCR provider which uses the Windows 10 build in OcrEngine
        /// </summary>
        /// <param name="ocrLanguage">Language to use for the OCR process, null if you want to take the user default</param>
        public Windows10OcrProvider(Language ocrLanguage = null)
		{
            if (ocrLanguage == null)
            {
                _ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
                return;
            }
            _ocrEngine = OcrEngine.TryCreateFromLanguage(ocrLanguage);
		}

        /// <summary>
        /// Retrieves all available OCR languages
        /// </summary>
        public static IEnumerable<Language> AvailableLanguages => OcrEngine.AvailableRecognizerLanguages;

        /// <summary>
		/// Scan the Image for text, and get the OcrResult
		/// </summary>
		/// <param name="image">Image</param>
		/// <returns>OcrResult sync</returns>
		public async Task<OcrInformation> DoOcrAsync(Image image)
        {
            OcrInformation result;
            using (var imageStream = new MemoryStream())
            {
                image.Save(imageStream, ImageFormat.Png);
                imageStream.Position = 0;
                result = await DoOcrAsync(imageStream);
			}
			return result;
        }

        /// <summary>
        /// Scan the Image for text, and get the OcrResult
        /// </summary>
        /// <param name="imageStream">Stream with the image data</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(Stream imageStream)
        {
            var randomAccessStream = imageStream.AsRandomAccessStream();

            var result = await DoOcrAsync(randomAccessStream);
            return result;
        }

        /// <summary>
        /// Scan the surface bitmap for text, and get the OcrResult
        /// </summary>
        /// <param name="randomAccessStream">IRandomAccessStream</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(IRandomAccessStream randomAccessStream)
        {
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine is null)
            {
                return null;
            }
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
			var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

			var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            return CreateOcrInformation(ocrResult);
		}

        /// <summary>
        /// Create the OcrInformation
        /// </summary>
        /// <param name="ocrResult">OcrResult</param>
        /// <returns>OcrInformation</returns>
        private OcrInformation CreateOcrInformation(OcrResult ocrResult)
        {
            var result = new OcrInformation();

            foreach (var ocrLine in ocrResult.Lines)
            {
                var line = new Line(ocrLine.Words.Count)
                {
                    Text = ocrLine.Text
                };

                result.Lines.Add(line);

                for (var index = 0; index < ocrLine.Words.Count; index++)
                {
                    var ocrWord = ocrLine.Words[index];
                    var location = new Rectangle((int)ocrWord.BoundingRect.X, (int)ocrWord.BoundingRect.Y,
                        (int)ocrWord.BoundingRect.Width, (int)ocrWord.BoundingRect.Height);

                    var word = line.Words[index];
                    word.Text = ocrWord.Text;
                    word.Bounds = location;
                }
            }

            return result;
        }
    }
}
