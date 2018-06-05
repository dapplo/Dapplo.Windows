//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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
    /// This interface is returned by the ClipboardLockProvider
    /// As long as you have this, you can access the clipboard
    /// </summary>
    public interface IClipboard : IDisposable
    {
        /// <summary>
        /// Check if the clipboard can be accessed
        /// </summary>
        bool CanAccess { get; }

        /// <summary>
        /// This throws a NotSupportedException when the clipboard can't be accessed
        /// </summary>
        void ThrowWhenNoAccess();
    }
}
