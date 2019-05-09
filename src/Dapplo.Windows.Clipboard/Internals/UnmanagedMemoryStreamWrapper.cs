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
using System.IO;

namespace Dapplo.Windows.Clipboard.Internals
{
    /// <summary>
    /// This wraps an UnmanagedMemoryStream, to also take care or disposing some disposable
    /// </summary>
    internal class UnmanagedMemoryStreamWrapper : UnmanagedMemoryStream
    {
        private IDisposable _disposable;

        public unsafe UnmanagedMemoryStreamWrapper(byte* bytes, long length, long capacity, FileAccess fileAccess) : base(bytes, length, capacity, fileAccess)
        {
        }

        public void SetDisposable(IDisposable disposable)
        {
            _disposable = disposable;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _disposable?.Dispose();
        }
    }
}
