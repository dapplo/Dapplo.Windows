#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

using System;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	///     The mouse button which is used in the input generator, and elsewhere
	/// </summary>
	[Flags]
	public enum MouseButtons
	{
		/// <summary>
		///     No button
		/// </summary>
		None,

		/// <summary>
		///     Left button is clicked down/up/clicked
		/// </summary>
		Left = 1,

		/// <summary>
		///     Left button is clicked down/up/clicked
		/// </summary>
		Right = 2,

		/// <summary>
		///     Midle button is clicked down/up/clicked
		/// </summary>
		Middle = 4,

		/// <summary>
		///     X1 button is clicked down/up/clicked
		/// </summary>
		X1 = 4,

		/// <summary>
		///     X2 button is clicked down/up/clicked
		/// </summary>
		X2 = 8,
	}
}