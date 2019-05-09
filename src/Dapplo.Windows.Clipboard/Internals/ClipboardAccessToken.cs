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

namespace Dapplo.Windows.Clipboard.Internals
{
    /// <summary>
    /// This is the clipboard access token
    /// </summary>
    internal class ClipboardAccessToken : IClipboardAccessToken
    {
        private readonly Action _disposeAction;

        public ClipboardAccessToken(Action disposeAction = null)
        {
            _disposeAction = disposeAction;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            CanAccess = false;
            _disposeAction?.Invoke();
        }

        /// <inheritdoc />
        public bool CanAccess { get; internal set; } = true;

        /// <inheritdoc />
        public bool IsOpenTimeout { get; internal set; }

        /// <inheritdoc />
        public bool IsLockTimeout { get; internal set; }

        /// <inheritdoc />
        public void ThrowWhenNoAccess()
        {
            if (CanAccess)
            {
                return;
            }

            if (IsLockTimeout)
            {
                throw new ClipboardAccessDeniedException("The clipboard was already locked by another thread or task in your application, a timeout occured.");
            }
            if (IsOpenTimeout)
            {
                throw new ClipboardAccessDeniedException("The clipboard couldn't be opened for usage, it's probably locked by another process");
            }
            throw new ClipboardAccessDeniedException("The clipboard is no longer locked, please check your disposing code.");
        }
    }
}
