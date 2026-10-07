// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Automation.Interop;

// The native UI Automation client API (UIAutomationCore.dll, UIAutomationClient.h), declared by hand so neither the managed
// System.Windows.Automation (part of WPF) nor a generated interop assembly is needed.
// The order of the methods is the vtable layout: every method up to the last one used is declared, unused ones as placeholders
// which are never called. All used methods are [PreserveSig] so a failing call returns its HRESULT instead of throwing.

/// <summary>
///     Constants of the UI Automation API which are used here
/// </summary>
internal static class UiaConstants
{
    /// <summary>CLSID of the CUIAutomation coclass</summary>
    public static readonly Guid CUIAutomationClsid = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");

    /// <summary>UIA_ScrollPatternId</summary>
    public const int ScrollPatternId = 10004;

    /// <summary>UIA_ScrollHorizontallyScrollablePropertyId</summary>
    public const int HorizontallyScrollablePropertyId = 30057;

    /// <summary>UIA_ScrollVerticallyScrollablePropertyId</summary>
    public const int VerticallyScrollablePropertyId = 30058;

    /// <summary>UIA_ScrollPatternNoScroll: the value for SetScrollPercent to leave a direction alone, also returned when a direction can't scroll</summary>
    public const double NoScroll = -1;

    /// <summary>TreeScope_Descendants</summary>
    public const int TreeScopeDescendants = 0x04;

    /// <summary>UIA_E_ELEMENTNOTAVAILABLE: the element is gone (e.g. the page navigated or the window closed)</summary>
    public const int ElementNotAvailable = unchecked((int)0x80040201);

    public const int S_OK = 0;

    /// <summary>
    ///     True for the HRESULTs which mean that the element or its application is gone: UIA_E_ELEMENTNOTAVAILABLE,
    ///     RPC_E_DISCONNECTED, RPC_S_SERVER_UNAVAILABLE, and COR_E_INVALIDOPERATION (e.g. a WPF dispatcher which has shut down)
    /// </summary>
    public static bool IsGone(int hResult) =>
        hResult == ElementNotAvailable
        || hResult == unchecked((int)0x80010108)
        || hResult == unchecked((int)0x800706BA)
        || hResult == unchecked((int)0x80131509);
}

/// <summary>
///     enum ScrollAmount
/// </summary>
internal enum ScrollAmount
{
    LargeDecrement = 0,
    SmallDecrement = 1,
    NoAmount = 2,
    LargeIncrement = 3,
    SmallIncrement = 4
}

/// <summary>
///     IUIAutomation
/// </summary>
[ComImport]
[Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomation
{
    void CompareElements();
    void CompareRuntimeIds();
    void GetRootElement();

    [PreserveSig]
    int ElementFromHandle(IntPtr hwnd, out IUIAutomationElement element);

    [PreserveSig]
    int ElementFromPoint(NativePoint point, out IUIAutomationElement element);

    void GetFocusedElement();
    void GetRootElementBuildCache();
    void ElementFromHandleBuildCache();
    void ElementFromPointBuildCache();
    void GetFocusedElementBuildCache();
    void CreateTreeWalker();
    void get_ControlViewWalker();
    void get_ContentViewWalker();

    [PreserveSig]
    int get_RawViewWalker(out IUIAutomationTreeWalker walker);

    void get_RawViewCondition();
    void get_ControlViewCondition();
    void get_ContentViewCondition();
    void CreateCacheRequest();
    void CreateTrueCondition();
    void CreateFalseCondition();

    [PreserveSig]
    int CreatePropertyCondition(int propertyId, [MarshalAs(UnmanagedType.Struct)] object value, out IUIAutomationCondition condition);
}

/// <summary>
///     IUIAutomationCondition, only passed around
/// </summary>
[ComImport]
[Guid("352ffba8-0973-437c-a61f-f64cafd81df9")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationCondition
{
}

/// <summary>
///     IUIAutomationTreeWalker
/// </summary>
[ComImport]
[Guid("4042c624-389c-4afc-a630-9df854a541fc")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationTreeWalker
{
    [PreserveSig]
    int GetParentElement(IUIAutomationElement element, out IUIAutomationElement parent);
}

/// <summary>
///     IUIAutomationElement
/// </summary>
[ComImport]
[Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElement
{
    void SetFocus();
    void GetRuntimeId();

    [PreserveSig]
    int FindFirst(int scope, IUIAutomationCondition condition, out IUIAutomationElement found);

    void FindAll();
    void FindFirstBuildCache();
    void FindAllBuildCache();
    void BuildUpdatedCache();
    void GetCurrentPropertyValue();
    void GetCurrentPropertyValueEx();
    void GetCachedPropertyValue();
    void GetCachedPropertyValueEx();
    void GetCurrentPatternAs();
    void GetCachedPatternAs();

    [PreserveSig]
    int GetCurrentPattern(int patternId, [MarshalAs(UnmanagedType.IUnknown)] out object patternObject);

    void GetCachedPattern();
    void GetCachedParent();
    void GetCachedChildren();

    [PreserveSig]
    int get_CurrentProcessId(out int processId);

    void get_CurrentControlType();
    void get_CurrentLocalizedControlType();
    void get_CurrentName();
    void get_CurrentAcceleratorKey();
    void get_CurrentAccessKey();
    void get_CurrentHasKeyboardFocus();
    void get_CurrentIsKeyboardFocusable();
    void get_CurrentIsEnabled();
    void get_CurrentAutomationId();
    void get_CurrentClassName();
    void get_CurrentHelpText();
    void get_CurrentCulture();
    void get_CurrentIsControlElement();
    void get_CurrentIsContentElement();
    void get_CurrentIsPassword();
    void get_CurrentNativeWindowHandle();
    void get_CurrentItemType();
    void get_CurrentIsOffscreen();
    void get_CurrentOrientation();
    void get_CurrentFrameworkId();
    void get_CurrentIsRequiredForForm();
    void get_CurrentItemStatus();

    [PreserveSig]
    int get_CurrentBoundingRectangle(out NativeRect boundingRectangle);
}

/// <summary>
///     IUIAutomationScrollPattern
/// </summary>
[ComImport]
[Guid("88f4d42a-e881-459d-a77c-73bbbb7e02dc")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationScrollPattern
{
    [PreserveSig]
    int Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);

    [PreserveSig]
    int SetScrollPercent(double horizontalPercent, double verticalPercent);

    [PreserveSig]
    int get_CurrentHorizontalScrollPercent(out double percent);

    [PreserveSig]
    int get_CurrentVerticalScrollPercent(out double percent);

    [PreserveSig]
    int get_CurrentHorizontalViewSize(out double viewSize);

    [PreserveSig]
    int get_CurrentVerticalViewSize(out double viewSize);

    [PreserveSig]
    int get_CurrentHorizontallyScrollable(out int scrollable);

    [PreserveSig]
    int get_CurrentVerticallyScrollable(out int scrollable);
}
