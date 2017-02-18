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

#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// Information about a native window
	/// Note: This is a dumb container, and doesn't retrieve anything about the window itself
	/// </summary>
	public class InteropWindow : IEquatable<IInteropWindow>, IInteropWindow
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="handle"></param>
		internal InteropWindow(IntPtr handle)
		{
			Handle = handle;
		}

		/// <inheritdoc />
		public IntPtr Handle { get; }

		/// <inheritdoc />
		public RECT? Bounds { get; set; }

		/// <inheritdoc />
		public RECT? ClientBounds { get; set; }

		/// <inheritdoc />
		public IEnumerable<IInteropWindow> Children { get; set; }

		/// <inheritdoc />
		public bool HasChildren => Children?.Any() == true;

		/// <inheritdoc />
		public string Classname { get; set; }

		/// <inheritdoc />
		public bool HasClassname => !string.IsNullOrEmpty(Classname);

		/// <inheritdoc />
		public bool HasParent => Parent.HasValue && Parent != IntPtr.Zero;

		/// <inheritdoc />
		public IntPtr? Parent { get; set; }

		/// <inheritdoc />
		public string Caption { get; set; }

		/// <inheritdoc />
		public string Text { get; set; }

		/// <inheritdoc />
		public bool? IsVisible { get; set; }

		/// <inheritdoc />
		public bool? IsMinimized { get; set; }

		/// <inheritdoc />
		public bool? IsMaximized { get; set; }

		/// <inheritdoc />
		public int? ProcessId { get; set; }

		/// <inheritdoc />
		public ExtendedWindowStyleFlags? ExtendedStyle { get; set; }

		/// <inheritdoc />
		public WindowStyleFlags? Style { get; set; }

		/// <inheritdoc />
		public WindowPlacement? Placement { get; set; }

		/// <inheritdoc />
		public bool? CanScroll { get; set; }

		/// <inheritdoc />
		public static implicit operator InteropWindow(IntPtr handle)
		{
			return InteropWindowFactory.CreateFor(handle);
		}

		/// <inheritdoc />
		public static implicit operator IntPtr(InteropWindow interopWindow)
		{
			return interopWindow.Handle;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return Handle.GetHashCode();
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((IInteropWindow) obj);
		}

		/// <inheritdoc />
		public bool Equals(IInteropWindow other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Handle.Equals(other.Handle);
		}

		/// <inheritdoc />
		public static bool operator ==(InteropWindow left, InteropWindow right)
		{
			return Equals(left, right);
		}

		/// <inheritdoc />
		public StringBuilder Dump(StringBuilder dump = null, string indentation = "")
		{
			dump = dump ?? new StringBuilder();
			this.Fill();
			dump.AppendLine($"{indentation}{nameof(Handle)}={Handle}");
			dump.AppendLine($"{indentation}{nameof(Classname)}={Classname}");
			dump.AppendLine($"{indentation}{nameof(Caption)}={Caption}");
			dump.AppendLine($"{indentation}{nameof(Text)}={Text}");
			dump.AppendLine($"{indentation}{nameof(Bounds)}={Bounds}");
			dump.AppendLine($"{indentation}{nameof(ClientBounds)}={Bounds}");
			dump.AppendLine($"{indentation}{nameof(ExtendedStyle)}={ExtendedStyle}");
			dump.AppendLine($"{indentation}{nameof(Style)}={Style}");
			dump.AppendLine($"{indentation}{nameof(IsMaximized)}={IsMaximized}");
			dump.AppendLine($"{indentation}{nameof(IsMinimized)}={IsMinimized}");
			dump.AppendLine($"{indentation}{nameof(IsVisible)}={IsVisible}");
			dump.AppendLine($"{indentation}{nameof(Parent)}={Parent}");
			if (!HasParent)
			{
				foreach (var child in this.GetZOrderedChildren())
				{
					child.Dump(dump, indentation + "\t");
				}
			}
			return dump;
		}

		/// <inheritdoc />
		public static bool operator !=(InteropWindow left, InteropWindow right)
		{
			return !Equals(left, right);
		}
	}
}