#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Runtime.InteropServices;

#endregion

namespace Dapplo.Windows.Credentials
{
	/// <summary>
	///     http://www.pinvoke.net/default.aspx/Structures.CREDUI_INFO
	///     http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/credui_info.asp
	/// </summary>
	public struct CredUiInfo
	{
        /// <summary>
        /// Size of the struct, is set by the constructor
        /// </summary>
		public readonly int cbSize;

        /// <summary>
        /// The hWnd for the parent of the dialog
        /// </summary>
		public IntPtr hwndParent;

        /// <summary>
        /// The message to show
        /// </summary>
		[MarshalAs(UnmanagedType.LPWStr)] public string pszMessageText;

        /// <summary>
        /// The text to show
        /// </summary>
		[MarshalAs(UnmanagedType.LPWStr)] public string pszCaptionText;

        /// <summary>
        /// Only used for CredUIPromptForCredentials, not for CredUIPromptForWindowsCredentials
        /// </summary>
		public IntPtr hbmBanner;

        /// <summary>
        /// Constructor which takes all parameters
        /// </summary>
        /// <param name="text">string</param>
        /// <param name="caption">string</param>
        /// <param name="parent">IntPtr with the hWnd for the parent</param>
        /// <param name="hBitmap">IntPtr with the hBitmap to show on the dialog</param>
        public CredUiInfo(string text = null, string caption = null, IntPtr? parent = null, IntPtr? hBitmap = null)
        {
            hwndParent = parent ?? IntPtr.Zero;
            pszMessageText = text;
            pszCaptionText = caption;
            hbmBanner = hBitmap ?? IntPtr.Zero;
            cbSize = 0;
            cbSize = Marshal.SizeOf(this);
        }
    }
}