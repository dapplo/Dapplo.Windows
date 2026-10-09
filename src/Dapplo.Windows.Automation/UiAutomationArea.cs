// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Automation;

/// <summary>
///     An area of a window as UI Automation reports it: an element of the control view with its bounds, control type, name and the areas
///     inside it. Immutable, a snapshot from <see cref="UiAutomationAreas.FindAreasAsync(IntPtr, int, int, TimeSpan?, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class UiAutomationArea
{
    private static readonly IReadOnlyList<UiAutomationArea> NoChildren = Array.Empty<UiAutomationArea>();

    /// <summary>
    ///     Create an area, e.g. to build a tree by hand
    /// </summary>
    /// <param name="bounds">NativeRect in screen pixels</param>
    /// <param name="controlType">int with the UI Automation control type id (UIA_ButtonControlTypeId = 50000 etc.)</param>
    /// <param name="name">string with the UI Automation name, null is stored as empty</param>
    /// <param name="children">the areas inside this one in the order of the tree (later ones are drawn on top), null for none</param>
    public UiAutomationArea(NativeRect bounds, int controlType, string name, IReadOnlyList<UiAutomationArea> children = null)
    {
        Bounds = bounds;
        ControlType = controlType;
        Name = name ?? string.Empty;
        Children = children ?? NoChildren;
    }

    /// <summary>
    ///     The bounds in screen pixels (physical pixels for a per-monitor DPI aware process), clipped to the parent area
    /// </summary>
    public NativeRect Bounds { get; }

    /// <summary>
    ///     The UI Automation control type id, e.g. 50000 for a button, 50030 for a document, see
    ///     https://learn.microsoft.com/windows/win32/winauto/uiauto-controltype-ids
    /// </summary>
    public int ControlType { get; }

    /// <summary>
    ///     The UI Automation name, empty when the element has none
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The areas inside this one, never null
    /// </summary>
    public IReadOnlyList<UiAutomationArea> Children { get; }

    /// <summary>
    ///     The areas which contain the point, from the deepest one to this one (the root): snap to the first and let the user step up the chain.
    ///     When siblings overlap, the one which comes last in <see cref="Children"/> (drawn on top) is taken. Pure geometry on the snapshot,
    ///     no UI Automation calls. A point on the right or bottom edge of an area is outside of it.
    /// </summary>
    /// <param name="point">NativePoint in screen pixels</param>
    /// <returns>IReadOnlyList with the areas, deepest first; empty when this area doesn't contain the point</returns>
    public IReadOnlyList<UiAutomationArea> GetAreasAt(NativePoint point)
    {
        var chain = new List<UiAutomationArea>();
        if (!Contains(Bounds, point))
        {
            return chain;
        }
        var current = this;
        while (current != null)
        {
            chain.Add(current);
            UiAutomationArea next = null;
            var children = current.Children;
            for (var index = children.Count - 1; index >= 0; index--)
            {
                if (Contains(children[index].Bounds, point))
                {
                    next = children[index];
                    break;
                }
            }
            current = next;
        }
        chain.Reverse();
        return chain;
    }

    private static bool Contains(NativeRect bounds, NativePoint point) =>
        point.X >= bounds.Left && point.X < bounds.Right && point.Y >= bounds.Top && point.Y < bounds.Bottom;

    /// <inheritdoc />
    public override string ToString() => $"UiAutomationArea {{ControlType: {ControlType}; Name: {Name}; Bounds: {Bounds}; Children: {Children.Count}}}";
}
