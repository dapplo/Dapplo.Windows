//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// This interface is returned by the ClipboardNative.Access(), which calls the ClipboardLockProvider.
    /// The access token is only valid within the same thread or window.
    /// When you got a IClipboardAccessToken, you can access the clipboard, until it's disposed.
    /// Don't forget to dispose this!!!
    /// </summary>
    public interface IClipboardAccessToken : IDisposable
    {
        /// <summary>
        /// Check if the clipboard can be accessed
        /// </summary>
        bool CanAccess { get; }

        /// <summary>
        /// The clipboard access was denied due to a timeout
        /// </summary>
        bool IsLockTimeout { get; }

        /// <summary>
        /// The clipboard couldn't be opened
        /// </summary>
        bool IsOpenTimeout { get; }

        /// <summary>
        /// This throws a ClipboardAccessDeniedException when the clipboard can't be accessed
        /// </summary>
        void ThrowWhenNoAccess();
    }
}
