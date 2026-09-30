// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.AppRestartManager;

/// <summary>
/// A WM_QUERYENDSESSION or WM_ENDSESSION message, received by the shared message window.
/// </summary>
/// <remarks>
/// This is a thin view on the underlying <see cref="WindowMessage"/>: everything set here is written directly to that message,
/// so all subscribers see the same state, and the answer reaches Windows.
/// <para>
/// Answers (<see cref="CanEndSession"/>, <see cref="Veto"/>) are only honoured when they are made synchronously inside <c>OnNext</c>, on the thread of the shared message window.
/// As soon as there is an <c>ObserveOn</c>, <c>SubscribeOn</c>, an <c>await</c> or any other thread hop, the reply has already been sent to Windows.
/// </para>
/// <para>
/// When WM_ENDSESSION arrives with <see cref="IsSessionEnding"/> set, the process can be terminated at any time after <c>OnNext</c> returned,
/// so save state synchronously.
/// </para>
/// </remarks>
public sealed class EndSessionMessage
{
    [DllImport("user32", EntryPoint = "ShutdownBlockReasonCreate", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonCreate(nint hWnd, string pwszReason);

    [DllImport("user32", EntryPoint = "ShutdownBlockReasonDestroy", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonDestroy(nint hWnd);

    // Tracks if a block reason was created for the shared message window, so it can be removed again
    private static volatile bool _blockReasonCreated;

    private readonly WindowMessage _windowMessage;

    /// <summary>
    /// Create an EndSessionMessage for a WM_QUERYENDSESSION or WM_ENDSESSION window message
    /// </summary>
    /// <param name="windowMessage">WindowMessage with Msg WM_QUERYENDSESSION or WM_ENDSESSION</param>
    /// <exception cref="ArgumentNullException">windowMessage is null</exception>
    /// <exception cref="ArgumentException">windowMessage is not a WM_QUERYENDSESSION or WM_ENDSESSION</exception>
    public EndSessionMessage(WindowMessage windowMessage)
    {
        _windowMessage = windowMessage ?? throw new ArgumentNullException(nameof(windowMessage));
        if (windowMessage.Msg != WindowsMessages.WM_QUERYENDSESSION && windowMessage.Msg != WindowsMessages.WM_ENDSESSION)
        {
            throw new ArgumentException($"Expected WM_QUERYENDSESSION or WM_ENDSESSION, got {windowMessage.Msg}", nameof(windowMessage));
        }
    }

    /// <summary>
    /// The underlying window message
    /// </summary>
    public WindowMessage WindowMessage => _windowMessage;

    /// <summary>
    /// WM_QUERYENDSESSION or WM_ENDSESSION
    /// </summary>
    public WindowsMessages Msg => _windowMessage.Msg;

    /// <summary>
    /// True for WM_QUERYENDSESSION: the system asks if the session may end, this can be answered with <see cref="CanEndSession"/> or <see cref="Veto"/>.
    /// </summary>
    public bool IsQuery => _windowMessage.Msg == WindowsMessages.WM_QUERYENDSESSION;

    /// <summary>
    /// The reason (lParam) for the session end, e.g. ENDSESSION_CLOSEAPP when the Restart Manager wants the application to close.
    /// </summary>
    public EndSessionReasons EndSessionReason => unchecked((EndSessionReasons)(uint)(ulong)(long)_windowMessage.LParam);

    /// <summary>
    /// Only for WM_ENDSESSION: true when the session is really ending, false when the shutdown was cancelled (someone vetoed the WM_QUERYENDSESSION).
    /// Always false for WM_QUERYENDSESSION.
    /// </summary>
    public bool IsSessionEnding => !IsQuery && _windowMessage.WParam != 0;

    /// <summary>
    /// Only for WM_QUERYENDSESSION: the answer to Windows, true (the default) allows the session to end, false blocks it.
    /// The answer is written to the underlying <see cref="WindowMessage"/> (TRUE=1 / FALSE=0), all subscribers share it and the last one which sets it wins.
    /// Only honoured when set synchronously inside OnNext on the window thread.
    /// For WM_ENDSESSION this is always true and cannot be changed.
    /// </summary>
    /// <exception cref="InvalidOperationException">When setting this for WM_ENDSESSION</exception>
    public bool CanEndSession
    {
        get => !IsQuery || !_windowMessage.Handled || _windowMessage.Result != 0;
        set
        {
            if (!IsQuery)
            {
                throw new InvalidOperationException("Only WM_QUERYENDSESSION can be answered, WM_ENDSESSION cannot be blocked.");
            }
            // WM_QUERYENDSESSION is answered with a BOOL, not an HRESULT: TRUE (1) allows the session to end, FALSE (0) blocks it
            _windowMessage.Result = value ? 1 : 0;
            _windowMessage.Handled = true;
        }
    }

    /// <summary>
    /// Only for WM_QUERYENDSESSION: block the session end (sets <see cref="CanEndSession"/> to false) and optionally tell the user why,
    /// using ShutdownBlockReasonCreate. The reason is removed automatically when the following WM_ENDSESSION arrives.
    /// </summary>
    /// <remarks>Note that Windows can still end the session when the user decides to "shut down anyway", or when ENDSESSION_CRITICAL is set.</remarks>
    /// <param name="reason">Optional text which Windows shows to the user in the shutdown UI</param>
    /// <returns>false when a reason was supplied but ShutdownBlockReasonCreate failed, true otherwise</returns>
    public bool Veto(string reason = null)
    {
        CanEndSession = false;
        if (string.IsNullOrEmpty(reason))
        {
            return true;
        }
        if (!ShutdownBlockReasonCreate(_windowMessage.Hwnd, reason))
        {
            return false;
        }
        _blockReasonCreated = true;
        return true;
    }

    /// <summary>
    /// Remove the block reason which was created by <see cref="Veto"/>, this is called when WM_ENDSESSION arrives.
    /// </summary>
    /// <param name="hwnd">The handle of the window which received the messages</param>
    internal static void RemoveBlockReason(nint hwnd)
    {
        if (!_blockReasonCreated)
        {
            return;
        }
        _blockReasonCreated = false;
        ShutdownBlockReasonDestroy(hwnd);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return IsQuery
            ? $"{Msg} {{ EndSessionReason = {EndSessionReason}, CanEndSession = {CanEndSession} }}"
            : $"{Msg} {{ EndSessionReason = {EndSessionReason}, IsSessionEnding = {IsSessionEnding} }}";
    }
}
