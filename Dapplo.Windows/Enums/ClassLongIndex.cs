//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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

namespace Dapplo.Windows.Enums
{
	public enum ClassLongIndex
	{
		GCL_CBCLSEXTRA = -20, // the size, in bytes, of the extra memory associated with the class. Setting this value does not change the number of extra bytes already allocated.
		GCL_CBWNDEXTRA = -18, // the size, in bytes, of the extra window memory associated with each window in the class. Setting this value does not change the number of extra bytes already allocated. For information on how to access this memory, see SetWindowLong.
		GCL_HBRBACKGROUND = -10, // a handle to the background brush associated with the class.
		GCL_HCURSOR = -12, // a handle to the cursor associated with the class.
		GCL_HICON = -14, // a handle to the icon associated with the class.
		GCL_HICONSM = -34, // a handle to the small icon associated with the class.
		GCL_HMODULE = -16, // a handle to the module that registered the class.
		GCL_MENUNAME = -8, // the address of the menu name string. The string identifies the menu resource associated with the class.
		GCL_STYLE = -26, // the window-class style bits.
		GCL_WNDPROC = -24 // the address of the window procedure, or a handle representing the address of the window procedure. You must use the CallWindowProc function to call the window procedure. 
	}
}