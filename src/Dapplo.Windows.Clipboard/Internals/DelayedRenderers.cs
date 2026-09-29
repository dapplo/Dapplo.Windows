// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.Messages.Structs;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Handles delayed rendering (WM_RENDERFORMAT, WM_RENDERALLFORMATS and WM_DESTROYCLIPBOARD) for the SharedMessageWindow.
/// All message handling is synchronous on the SharedMessageWindow thread, as Windows requires.
/// </summary>
internal static class DelayedRenderers
{
    private static readonly ConcurrentDictionary<uint, Action<ClipboardRenderFormatRequest>> Renderers = new();
    // The formats which were placed on the clipboard with delayed rendering, and are not rendered yet
    private static readonly ConcurrentDictionary<uint, bool> PendingFormats = new();
    private static readonly object Lock = new();
    private static IDisposable _messageSubscription;

    /// <summary>
    /// Register a renderer for the format
    /// </summary>
    public static IDisposable Register(uint formatId, Action<ClipboardRenderFormatRequest> renderer)
    {
        if (renderer == null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }
        if (formatId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatId), "0 is not a valid clipboard format.");
        }
        EnsureListening();
        if (!Renderers.TryAdd(formatId, renderer))
        {
            throw new InvalidOperationException($"A delayed renderer for clipboard format {formatId} is already registered.");
        }
        // Only remove the registration if it's still ours
        return Disposable.Create(() => ((ICollection<KeyValuePair<uint, Action<ClipboardRenderFormatRequest>>>)Renderers).Remove(new KeyValuePair<uint, Action<ClipboardRenderFormatRequest>>(formatId, renderer)));
    }

    /// <summary>
    /// Check if a renderer is registered for the format
    /// </summary>
    public static bool IsRegistered(uint formatId) => Renderers.ContainsKey(formatId);

    /// <summary>
    /// Mark the format as placed on the clipboard with delayed rendering
    /// </summary>
    public static void MarkPending(uint formatId) => PendingFormats[formatId] = true;

    /// <summary>
    /// Make sure the shared window messages are processed, this subscription lives as long as the process (like the SharedMessageWindow)
    /// </summary>
    private static void EnsureListening()
    {
        lock (Lock)
        {
            _messageSubscription ??= SharedMessageWindow.Messages.Subscribe(OnMessage);
        }
    }

    /// <summary>
    /// Called synchronously in the window procedure of the SharedMessageWindow, this must never throw.
    /// </summary>
    private static void OnMessage(WindowMessage message)
    {
        try
        {
            switch (message.Msg)
            {
                case WindowsMessages.WM_RENDERFORMAT:
                    var formatId = unchecked((uint)(long)message.WParam);
                    if (Renderers.TryGetValue(formatId, out var renderer))
                    {
                        // The clipboard must NOT be opened for WM_RENDERFORMAT
                        Render(message.Hwnd, formatId, renderer, false);
                        message.Handled = true;
                        message.Result = 0;
                    }
                    break;
                case WindowsMessages.WM_RENDERALLFORMATS:
                    if (RenderAllFormats(message.Hwnd))
                    {
                        message.Handled = true;
                        message.Result = 0;
                    }
                    break;
                case WindowsMessages.WM_DESTROYCLIPBOARD:
                    // We are no longer the owner, nothing will be requested anymore
                    PendingFormats.Clear();
                    break;
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError("Dapplo.Windows.Clipboard: processing {0} failed: {1}", message.Msg, ex);
        }
    }

    /// <summary>
    /// Render all pending formats, the clipboard is opened with the owner window, and closed afterwards
    /// </summary>
    /// <returns>true if something was rendered</returns>
    private static bool RenderAllFormats(nint hwnd)
    {
        var formatIds = PendingFormats.Keys.Where(Renderers.ContainsKey).ToList();
        if (formatIds.Count == 0)
        {
            return false;
        }
        // Keep the waiting on the window thread short
        using var accessToken = ClipboardNative.Access(hwnd, 2, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100));
        if (!accessToken.CanAccess)
        {
            Trace.TraceWarning("Dapplo.Windows.Clipboard: couldn't open the clipboard to render all formats.");
            return false;
        }
        // Another application might have taken over the clipboard, then there is nothing to render
        if (NativeMethods.GetClipboardOwner() != hwnd)
        {
            PendingFormats.Clear();
            return true;
        }
        foreach (var formatId in formatIds)
        {
            if (Renderers.TryGetValue(formatId, out var renderer))
            {
                Render(hwnd, formatId, renderer, true);
            }
        }
        return true;
    }

    /// <summary>
    /// Call the renderer with a token which is only valid during the call
    /// </summary>
    private static void Render(nint hwnd, uint formatId, Action<ClipboardRenderFormatRequest> renderer, bool renderAllFormats)
    {
        // This token doesn't open or close the clipboard, it's only valid on this thread and until the renderer returns
        var renderToken = new ClipboardAccessToken
        {
            OwnerHandle = hwnd
        };
        try
        {
            renderer(new ClipboardRenderFormatRequest(formatId, renderAllFormats, renderToken));
            PendingFormats.TryRemove(formatId, out _);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Dapplo.Windows.Clipboard: the delayed renderer for clipboard format {0} failed: {1}", formatId, ex);
        }
        finally
        {
            renderToken.Dispose();
        }
    }
}
