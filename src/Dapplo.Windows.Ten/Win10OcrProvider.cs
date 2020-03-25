// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Dapplo.Windows.Ten
{
	/// <summary>
	/// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
	/// </summary>
	public class Win10OcrProvider : IOcrProvider
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrProvider));

		/// <summary>
		/// Constructor, this is only debug information
		/// </summary>
		public Win10OcrProvider()
		{
			var languages = OcrEngine.AvailableRecognizerLanguages;
			foreach (var language in languages)
			{
				Log.DebugFormat("Found language {0} {1}", language.NativeName, language.LanguageTag);
			}
		}

        /// <summary>
        /// Scan the surface bitmap for text, and get the OcrResult
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <returns>OcrResult sync</returns>
        public async Task<OcrInformation> DoOcrAsync(ISurface surface)
        {
            OcrInformation result;
            using (var imageStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(surface, imageStream, new SurfaceOutputSettings());
                imageStream.Position = 0;
                var randomAccessStream = imageStream.AsRandomAccessStream();

                result = await DoOcrAsync(randomAccessStream);
            }
            return result;
        }

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
                ImageOutput.SaveToStream(image, null, imageStream, new SurfaceOutputSettings());
                imageStream.Position = 0;
                var randomAccessStream = imageStream.AsRandomAccessStream();

                result = await DoOcrAsync(randomAccessStream);
			}
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
