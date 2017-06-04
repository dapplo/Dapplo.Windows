//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Icons
{
    /// <summary>
    /// Helper code for icons
    /// </summary>
    public static class IconHelper
    {
        /// <summary>
        /// Helper method to get the app logo from the mandest
        /// </summary>
        /// <typeparam name="TBitmap"></typeparam>
        /// <param name="interopWindow">IInteropWindow</param>
        /// <returns></returns>
        public static TBitmap GetModernAppLogo<TBitmap>(IInteropWindow interopWindow) where TBitmap : class
        {
            // get folder where actual app resides
            var exePath = GetModernAppProcessPath(interopWindow);
            var dir = Path.GetDirectoryName(exePath);
            if (!Directory.Exists(dir))
            {
                return default(TBitmap);
            }
            var manifestPath = Path.Combine(dir, "AppxManifest.xml");
            if (!File.Exists(manifestPath))
            {
                return default(TBitmap);
            }
            // this is manifest file
            string pathToLogo;
            using (var fs = File.OpenRead(manifestPath))
            {
                var manifest = XDocument.Load(fs);
                const string ns = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
                // rude parsing - take more care here
                var propertiesNamespace = XName.Get("Properties", ns);
                var logoNamespace = XName.Get("Logo", ns);
                pathToLogo = manifest.Root?.Element(propertiesNamespace)?.Element(logoNamespace)?.Value;
            }
            // now here it is tricky again - there are several files that match logo, for example
            // black, white, contrast white. Here we choose first, but you might do differently
            string finalLogo = Directory.GetFiles(Path.Combine(dir, Path.GetDirectoryName(pathToLogo)), Path.GetFileNameWithoutExtension(pathToLogo) + "*" + Path.GetExtension(pathToLogo)).FirstOrDefault();
            // serach for all files that match file name in Logo element but with any suffix (like "Logo.black.png, Logo.white.png etc)

            if (!File.Exists(finalLogo))
            {
                return default(TBitmap);
            }
            using (var fs = File.OpenRead(finalLogo))
            {
                if (typeof(BitmapImage) == typeof(TBitmap))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = fs;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    return img as TBitmap;

                }
                if (typeof(Bitmap) == typeof(TBitmap)) {
                    return Image.FromStream(fs) as TBitmap;
                }
            }
            return default(TBitmap);
        }

        /// <summary>
        /// Get the path for the real modern app process belonging to the window
        /// </summary>
        /// <param name="interopWindow"></param>
        /// <returns></returns>
        private static string GetModernAppProcessPath(IInteropWindow interopWindow)
        {
            int pid;
            User32Api.GetWindowThreadProcessId(interopWindow.Handle, out pid);
            // now this is a bit tricky. Modern apps are hosted inside ApplicationFrameHost process, so we need to find
            // child window which does NOT belong to this process. This should be the process we need
            foreach (var childHwnd in interopWindow.GetChildren())
            {
                int childPid = childHwnd.GetProcessId();
                if (childPid == pid)
                {
                    continue;
                }
                // here we are
                var childProc = Process.GetProcessById(childPid);
                return childProc.MainModule.FileName;
            }

            throw new Exception("Cannot find a path to Modern App executable file");
        }
    }
}