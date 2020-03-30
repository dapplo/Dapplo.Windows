// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.IO;

namespace Dapplo.Windows.Ten.Sharing
{
    /// <summary>
    /// The arguments for the sharing UI
    /// </summary>
    public class SharingArguments
    {
        public string Title { get; set; }

        public string ApplicationName { get; set; }

        public string Description { get; set; }

        public string Filename { get; set; }

        public Stream StreamData { get; set; }
        
        public Bitmap Thumbnail { get; set; }
    }
}
