// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;

namespace Dapplo.Windows.Ten.Ocr
{
    /// <summary>
    /// Contains the information about a word
    /// </summary>
    public class Word
    {
        /// <summary>
        /// The actual text for the word
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The bounds of the word
        /// </summary>
        public Rectangle Bounds { get; set; }
    }
}