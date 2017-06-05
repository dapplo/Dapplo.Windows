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

#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32.Structs;

#endregion

namespace Dapplo.Windows.Desktop
{
    /// <summary>
    ///     Information about a native window
    ///     Note: This is a dumb container, and doesn't retrieve anything about the window itself
    /// </summary>
    public class InteropWindow : IEquatable<IInteropWindow>, IInteropWindow
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="handle"></param>
        internal InteropWindow(IntPtr handle)
        {
            Handle = handle;
        }

        /// <inheritdoc />
        public bool Equals(IInteropWindow other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Handle.Equals(other.Handle);
        }

        /// <inheritdoc />
        public IntPtr Handle { get; }

        /// <inheritdoc />
        public bool HasZOrderedChildren { get; set; }

        /// <inheritdoc />
        public WindowInfo? Info { get; set; }

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
        public IInteropWindow ParentWindow { get; set; }

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
        public WindowPlacement? Placement { get; set; }

        /// <inheritdoc />
        public bool? CanScroll { get; set; }

        /// <inheritdoc />
        public StringBuilder Dump(InteropWindowCacheFlags cacheFlags = InteropWindowCacheFlags.CacheAll, StringBuilder dump = null, string indentation = "")
        {
            this.Fill(cacheFlags);

            dump = dump ?? new StringBuilder();
            dump.AppendLine($"{indentation}{nameof(Handle)}={Handle}");
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Classname))
            {
                dump.AppendLine($"{indentation}{nameof(Classname)}={Classname}");
            }

            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Caption))
            {
                dump.AppendLine($"{indentation}{nameof(Caption)}={Caption}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Text))
            {
                dump.AppendLine($"{indentation}{nameof(Text)}={Text}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Info))
            {
                dump.AppendLine($"{indentation}{nameof(Info)}={Info}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Maximized))
            {
                dump.AppendLine($"{indentation}{nameof(IsMaximized)}={IsMaximized}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Minimized))
            {
                dump.AppendLine($"{indentation}{nameof(IsMinimized)}={IsMinimized}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Visible))
            {
                dump.AppendLine($"{indentation}{nameof(IsVisible)}={IsVisible}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Parent))
            {
                dump.AppendLine($"{indentation}{nameof(Parent)}={Parent}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.ScrollInfo))
            {
                dump.AppendLine($"{indentation}{nameof(CanScroll)}={CanScroll}");
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.Children))
            {
                if (!HasParent)
                {
                    foreach (var child in this.GetChildren())
                    {
                        child.Dump(cacheFlags, dump, indentation + "\t");
                    }
                }
            }
            if (cacheFlags.HasFlag(InteropWindowCacheFlags.ZOrderedChildren))
            {
                if (!HasParent)
                {
                    foreach (var child in this.GetZOrderedChildren())
                    {
                        child.Dump(cacheFlags, dump, indentation + "\t");
                    }
                }
            }
            return dump;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((IInteropWindow) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(InteropWindow left, InteropWindow right)
        {
            return Equals(left, right);
        }

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
        public static bool operator !=(InteropWindow left, InteropWindow right)
        {
            return !Equals(left, right);
        }
    }
}