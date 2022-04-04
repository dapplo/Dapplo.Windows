// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.IO;
using System.Linq;
#if !NETSTANDARD2_0
using System.Windows.Media.Imaging;
#endif
using System.Xml.Linq;
using Dapplo.Windows.App;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Icons.Enums;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Shell32;
using Dapplo.Windows.Shell32.Enums;
using Dapplo.Windows.Shell32.Structs;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Helper code for icons
/// </summary>
public static class IconHelper
{
    /// <summary>
    /// Helper method to get the app logo from the applications AppxManifest
    /// </summary>
    /// <typeparam name="TBitmap">Type for the Bitmap, i.e. BitmapSource or Bitmap</typeparam>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <param name="scale">int with scale, 100 is default</param>
    /// <returns>instance of TBitmap or null if nothing found</returns>
    public static TBitmap GetAppLogo<TBitmap>(IInteropWindow interopWindow, int scale = 100) where TBitmap : class
    {
        // get folder where actual app resides
        var exePath = GetAppProcessPath(interopWindow);
        if (exePath == null)
        {
            return default;
        }
        var dir = Path.GetDirectoryName(exePath);
        if (!Directory.Exists(dir))
        {
            return default;
        }
        var manifestPath = Path.Combine(dir, "AppxManifest.xml");
        if (!File.Exists(manifestPath))
        {
            return default;
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
        var logoDirectoryName = Path.GetDirectoryName(pathToLogo);
        if (logoDirectoryName == null)
        {
            return default;
        }
        var logoDirectory = Path.Combine(dir, logoDirectoryName);

        var logoExtension = Path.GetExtension(pathToLogo);
        var possibleLogos = Directory.GetFiles(logoDirectory, Path.GetFileNameWithoutExtension(pathToLogo) + "*" + logoExtension);

        // Find the best matching logo, this could be one with a scale in it, or just the first
        string finalLogoPath = possibleLogos.FirstOrDefault(logoFile => logoFile.EndsWith($".scale-{scale}.{logoExtension}")) ?? possibleLogos.FirstOrDefault();

        if (finalLogoPath == null || !File.Exists(finalLogoPath))
        {
            return default;
        }
        using (var fileStream = File.OpenRead(finalLogoPath))
        {
#if !NETSTANDARD2_0
            if (typeof(BitmapSource).IsAssignableFrom(typeof(TBitmap)))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = fileStream;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                return img as TBitmap;
            }
#endif
            if (typeof(Bitmap) != typeof(TBitmap))
            {
                return default;
            }
            using (var bitmap = Image.FromStream(fileStream))
            {
                return bitmap.Clone() as TBitmap;
            }
        }
    }

    /// <summary>
    /// Get the path for the real modern app process belonging to the window
    /// </summary>
    /// <param name="interopWindow">IInteropWindow</param>
    /// <returns>string</returns>
    private static string GetAppProcessPath(IInteropWindow interopWindow)
    {
        User32Api.GetWindowThreadProcessId(interopWindow.Handle, out var pid);
        if (string.Equals(interopWindow.GetClassname(), AppQuery.AppFrameWindowClass))
        {
            pid = interopWindow.GetChildren().FirstOrDefault(window => string.Equals(AppQuery.AppWindowClass, window.GetClassname()))?.GetProcessId() ?? 0;
        }
        if (pid <= 0)
        {
            return null;
        }

        return Kernel32Api.GetProcessPath(pid);
    }

    /// <summary>
    ///     Write the images to the stream as icon.
    ///     It's important that the images are not larger than 256x256.
    /// </summary>
    /// <param name="stream">Stream to write to</param>
    /// <param name="images">IEnumerable with images</param>
    public static void WriteIcon(Stream stream, IEnumerable<Image> images)
    {
        var binaryWriter = new BinaryWriter(stream);

        short imageCount = 0;
        var imageSizes = new List<Size>();
        var encodedImages = new List<MemoryStream>();
        foreach (var image in images)
        {
            var imageStream = new MemoryStream();
            image.Save(imageStream, ImageFormat.Png);
            imageSizes.Add(image.Size);

            imageStream.Seek(0, SeekOrigin.Begin);
            encodedImages.Add(imageStream);
            imageCount++;
        }
        //
        // ICONDIR structure
        //
        binaryWriter.Write((short)0); // reserved
        binaryWriter.Write((short)1); // image type (icon)
        binaryWriter.Write(imageCount); // number of images

        //
        // ICONDIRENTRY structure
        //
        const int iconDirSize = 6;
        const int iconDirEntrySize = 16;

        var offset = iconDirSize + imageCount * iconDirEntrySize;
        for (var i = 0; i < imageCount; i++)
        {
            var imageSize = imageSizes[i];
            // Write the width / height, 0 means 256
            binaryWriter.Write(imageSize.Width == 256 ? (byte)0 : (byte)imageSize.Width);
            binaryWriter.Write(imageSize.Height == 256 ? (byte)0 : (byte)imageSize.Height);
            binaryWriter.Write((byte)0); // no pallete
            binaryWriter.Write((byte)0); // reserved
            binaryWriter.Write((short)0); // no color planes
            binaryWriter.Write((short)32); // 32 bpp
            binaryWriter.Write((int)encodedImages[i].Length); // image data length
            binaryWriter.Write(offset);
            offset += (int)encodedImages[i].Length;
        }

        binaryWriter.Flush();
        //
        // Write image data
        //
        foreach (var encodedImage in encodedImages)
        {
            encodedImage.WriteTo(stream);
            encodedImage.Dispose();
        }
    }

    /// <summary>
    ///     Based on <a href="http://www.codeproject.com/KB/cs/IconExtractor.aspx">Extract icons from EXE or DLL files</a>
    ///     And a hint from <a href="http://www.codeproject.com/KB/cs/IconLib.aspx">IconLib - Icons Unfolded (MultiIcon and Windows Vista supported)</a>
    /// </summary>
    /// <param name="iconStream">Stream with the icon information</param>
    /// <returns>Bitmap with the Vista Icon (256x256)</returns>
    public static Bitmap ExtractVistaIcon(this Stream iconStream)
    {
        const int sizeIconDir = 6;
        const int sizeIconDirEntry = 16;
        Bitmap bmpPngExtracted = null;
        try
        {
            var srcBuf = new byte[iconStream.Length];
            _ = iconStream.Read(srcBuf, 0, (int)iconStream.Length);
            int iCount = BitConverter.ToInt16(srcBuf, 4);
            for (var iIndex = 0; iIndex < iCount; iIndex++)
            {
                int iWidth = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex];
                int iHeight = srcBuf[sizeIconDir + sizeIconDirEntry * iIndex + 1];
                if (iWidth != 0 || iHeight != 0)
                {
                    continue;
                }
                var iImageSize = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 8);
                var iImageOffset = BitConverter.ToInt32(srcBuf, sizeIconDir + sizeIconDirEntry * iIndex + 12);
                using (var destStream = new MemoryStream())
                {
                    destStream.Write(srcBuf, iImageOffset, iImageSize);
                    destStream.Seek(0, SeekOrigin.Begin);
                    bmpPngExtracted = new Bitmap(destStream); // This is PNG! :)
                }
                break;
            }
        }
        catch
        {
            return null;
        }
        return bmpPngExtracted;
    }

    /// <summary>
    ///     See: http://msdn.microsoft.com/en-us/library/windows/desktop/ms648069%28v=vs.85%29.aspx
    /// </summary>
    /// <typeparam name="TIcon"></typeparam>
    /// <param name="filePath">The file (EXE or DLL) to get the icon from</param>
    /// <param name="index">Index of the icon</param>
    /// <param name="useLargeIcon">true if the large icon is wanted</param>
    /// <returns>Icon</returns>
    public static TIcon ExtractAssociatedIcon<TIcon>(string filePath, int index = 0, bool useLargeIcon = true) where TIcon : class
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }
        if (!Uri.TryCreate(filePath, UriKind.Absolute, out var uri))
        {
            filePath = Path.GetFullPath(filePath);
            uri = new Uri(filePath);
        }
        if (!uri.IsFile)
        {
            return null;
        }
        if (!File.Exists(filePath))
        {
            return null;
        }
        NativeIconMethods.ExtractIconEx(filePath, index, out var large, out var small, 1);
        TIcon returnIcon = null;
        try
        {
            if (useLargeIcon && !IntPtr.Zero.Equals(large))
            {
                returnIcon = IconHandleTo<TIcon>(large);
            }
            else if (!IntPtr.Zero.Equals(small))
            {
                returnIcon = IconHandleTo<TIcon>(small);
            }
            else if (!IntPtr.Zero.Equals(large))
            {
                returnIcon = IconHandleTo<TIcon>(large);
            }
        }
        finally
        {
            if (!IntPtr.Zero.Equals(small))
            {
                NativeIconMethods.DestroyIcon(small);
            }
            if (!IntPtr.Zero.Equals(large))
            {
                NativeIconMethods.DestroyIcon(large);
            }
        }
        return returnIcon;
    }

    /// <summary>
    ///     Get the number of icon in the file
    /// </summary>
    /// <param name="location">Location of the EXE or DLL</param>
    /// <returns>int with the number of icons in the file</returns>
    public static int CountAssociatedIcons(string location)
    {
        return NativeIconMethods.ExtractIconEx(location, -1, out _, out _, 0);
    }

    /// <summary>
    /// Create a TIcon from the specified iconHandle
    /// </summary>
    /// <typeparam name="TIcon">Bitmap, Icon or BitmapSource</typeparam>
    /// <param name="iconHandle">IntPtr</param>
    /// <returns>TIcon</returns>
    public static TIcon IconHandleTo<TIcon>(IntPtr iconHandle) where TIcon : class
    {
        if (iconHandle == IntPtr.Zero)
        {
            return default;
        }
        if (typeof(TIcon) == typeof(Icon))
        {
            return Icon.FromHandle(iconHandle) as TIcon;
        }
        if (typeof(TIcon) == typeof(Bitmap))
        {
            using (var icon = Icon.FromHandle(iconHandle))
            {
                return icon.ToBitmap() as TIcon;
            }
        }
#if !NETSTANDARD2_0
        if (typeof(TIcon) != typeof(BitmapSource))
        {
            return default;
        }
        using (var icon = Icon.FromHandle(iconHandle))
        {
            return icon.ToBitmapSource() as TIcon;
        }
#else
            return default;
#endif
    }

    /// <summary>
    ///     Get a SafeIconHandle so one can use using to automatically cleanup the HIcon
    /// </summary>
    /// <param name="bitmap">Bitmap</param>
    /// <returns>SafeIconHandle</returns>
    public static SafeIconHandle GetSafeIconHandle(this Bitmap bitmap)
    {
        return new SafeIconHandle(bitmap);
    }

    /// <summary>
    ///     Returns an icon for a given file extension - indicated by the name parameter.
    ///     See: http://msdn.microsoft.com/en-us/library/windows/desktop/bb762179(v=vs.85).aspx
    /// </summary>
    /// <param name="filename">Filename</param>
    /// <param name="size">Large or small</param>
    /// <param name="linkOverlay">Whether to include the link icon</param>
    /// <returns>System.Drawing.Icon</returns>
    public static TIcon GetFileExtensionIcon<TIcon>(string filename, IconSize size, bool linkOverlay) where TIcon : class
    {
        var shellFileInfo = new ShellFileInfo();
        // UseFileAttributes makes it simulate, just gets the icon for the extension
        var flags = ShellGetFileInfoFlags.Icon | ShellGetFileInfoFlags.UseFileAttributes;

        if (linkOverlay)
        {
            flags |= ShellGetFileInfoFlags.LinkOverlay;
        }

        // Check the size specified for return.
        if (IconSize.Small == size)
        {
            flags |= ShellGetFileInfoFlags.SmallIcon;
        }
        else
        {
            flags |= ShellGetFileInfoFlags.LargeIcon;
        }

        Shell32Api.SHGetFileInfo(Path.GetFileName(filename), ShellFileAttributeFlags.Normal, ref shellFileInfo, (uint)Marshal.SizeOf(shellFileInfo), flags);

        // TODO: Fix bad practice for cleanup, and use generics to allow the user to specify if it's an icon/bitmap/-source
        // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
        try
        {
            return IconHelper.IconHandleTo<TIcon>(shellFileInfo.IconHandle);
        }
        finally
        {
            if (shellFileInfo.IconHandle != IntPtr.Zero)
            {
                // Cleanup
                NativeIconMethods.DestroyIcon(shellFileInfo.IconHandle);
            }
        }
    }

    /// <summary>
    ///     Used to access system folder icons.
    /// </summary>
    /// <param name="size">Specify large or small icons.</param>
    /// <param name="folderIconType">Specify open or closed FolderIconType.</param>
    /// <returns>System.Drawing.Icon</returns>
    public static TIcon GetFolderIcon<TIcon>(IconSize size, FolderIconType folderIconType) where TIcon : class
    {
        // Need to add size check, although errors generated at present!
        var flags = ShellGetFileInfoFlags.Icon | ShellGetFileInfoFlags.UseFileAttributes;

        if (FolderIconType.Open == folderIconType)
        {
            flags |= ShellGetFileInfoFlags.OpenIcon;
        }

        if (IconSize.Small == size)
        {
            flags |= ShellGetFileInfoFlags.SmallIcon;
        }
        else
        {
            flags |= ShellGetFileInfoFlags.LargeIcon;
        }

        // Get the folder icon
        var shellFileInfo = new ShellFileInfo();
        Shell32Api.SHGetFileInfo(null, ShellFileAttributeFlags.Directory, ref shellFileInfo, (uint)Marshal.SizeOf(shellFileInfo), flags);

        // Now clone the icon, so that it can be successfully stored in an ImageList
        try
        {
            return IconHelper.IconHandleTo<TIcon>(shellFileInfo.IconHandle);
        }
        finally
        {
            if (shellFileInfo.IconHandle != IntPtr.Zero)
            {
                // Cleanup
                NativeIconMethods.DestroyIcon(shellFileInfo.IconHandle);
            }
        }
    }
}