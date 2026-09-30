// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Desktop;

/// <summary>
///     Information about a native window
///     Note: This is a dumb container, and doesn't retrieve anything about the window itself
/// </summary>
public class InteropWindow : IEquatable<IInteropWindow>, IInteropWindow
{
    /// <summary>
    /// Create an InteropWindow for the specified windows handle
    /// </summary>
    /// <param name="handle">IntPtr</param>
    public InteropWindow(IntPtr handle)
    {
        Handle = handle;
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
    public bool HasOwner => Owner.HasValue && Owner != IntPtr.Zero;

    /// <inheritdoc />
    public IntPtr? Owner { get; set; }

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
    public int? ThreadId { get; set; }

    /// <inheritdoc />
    public int? ProcessId { get; set; }
        
    /// <inheritdoc />
    public WindowPlacement? Placement { get; set; }

    /// <inheritdoc />
    public bool? CanScroll { get; set; }

    /// <inheritdoc />
    public StringBuilder Dump(InteropWindowRetrieveSettings retrieveSettings = InteropWindowRetrieveSettings.CacheAll, StringBuilder dump = null, string indentation = "")
    {
        this.Fill(retrieveSettings);

        dump ??= new StringBuilder();
        dump.AppendLine($"{indentation}{nameof(Handle)}={Handle}");
        if ((retrieveSettings & InteropWindowRetrieveSettings.Classname) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Classname)}={Classname}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Caption) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Caption)}={Caption}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Text) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Text)}={Text}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Info) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Info)}={Info}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Maximized) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(IsMaximized)}={IsMaximized}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Minimized) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(IsMinimized)}={IsMinimized}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Visible) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(IsVisible)}={IsVisible}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Parent) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Parent)}={Parent}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Owner) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Owner)}={Owner}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.ProcessId) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(ProcessId)}={ProcessId}");
            dump.AppendLine($"{indentation}{nameof(ThreadId)}={ThreadId}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.Placement) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(Placement)}={Placement}");
        }

        if ((retrieveSettings & InteropWindowRetrieveSettings.ScrollInfo) != 0)
        {
            dump.AppendLine($"{indentation}{nameof(CanScroll)}={CanScroll}");
        }

        // Fill already retrieved the (Z-ordered) children, when requested. Each child dumps its own children, so the complete tree is dumped.
        if ((retrieveSettings & (InteropWindowRetrieveSettings.Children | InteropWindowRetrieveSettings.ZOrderedChildren)) != 0 && Children != null)
        {
            foreach (var child in Children)
            {
                child.Dump(retrieveSettings, dump, indentation + "\t");
            }
        }

        return dump;
    }

    /// <inheritdoc />
    public bool Equals(IInteropWindow other)
    {
        if (other is null)
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
    public override bool Equals(object obj)
    {
        // Symmetric with Equals(IInteropWindow): every IInteropWindow with the same handle is equal
        return obj is IInteropWindow other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Handle.GetHashCode();
    }

    /// <summary>
    /// Operator == overload
    /// </summary>
    /// <param name="left">InteropWindow</param>
    /// <param name="right">InteropWindow</param>
    /// <returns>bool</returns>
    public static bool operator ==(InteropWindow left, InteropWindow right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Create (cast) a new InteropWindow for an IntPtr, this is the same as <see cref="InteropWindowFactory.CreateFor(IntPtr)"/>
    /// </summary>
    /// <param name="handle">IntPtr</param>
    public static explicit operator InteropWindow(IntPtr handle)
    {
        return InteropWindowFactory.CreateFor(handle);
    }

    /// <summary>
    /// Cast the InteropWindow to it's handle, null results in IntPtr.Zero
    /// </summary>
    /// <param name="interopWindow">InteropWindow</param>
    public static explicit operator IntPtr(InteropWindow interopWindow)
    {
        return interopWindow?.Handle ?? IntPtr.Zero;
    }

    /// <summary>
    /// operator != overload
    /// </summary>
    /// <param name="left">InteropWindow</param>
    /// <param name="right">InteropWindow</param>
    /// <returns>bool</returns>
    public static bool operator !=(InteropWindow left, InteropWindow right)
    {
        return !Equals(left, right);
    }
}