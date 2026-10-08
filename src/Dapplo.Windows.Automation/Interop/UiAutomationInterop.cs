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

    /// <summary>CLSID of the CUIAutomation8 coclass (Windows 8+), which implements IUIAutomation2</summary>
    public static readonly Guid CUIAutomation8Clsid = new Guid("e22ad333-b25f-460c-83d0-0581107395c9");

    /// <summary>UIA_BoundingRectanglePropertyId</summary>
    public const int BoundingRectanglePropertyId = 30001;

    /// <summary>UIA_IsOffscreenPropertyId</summary>
    public const int IsOffscreenPropertyId = 30022;

    /// <summary>TreeScope_Element</summary>
    public const int TreeScopeElement = 0x01;

    /// <summary>AutomationElementMode_None: the found elements only carry the cached properties, no reference to the live element</summary>
    public const int AutomationElementModeNone = 0;

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
    [PreserveSig]
    int CreateCacheRequest(out IUIAutomationCacheRequest cacheRequest);
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
    [PreserveSig]
    int FindAllBuildCache(int scope, IUIAutomationCondition condition, IUIAutomationCacheRequest cacheRequest, out IUIAutomationElementArray found);
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

    void get_CurrentLabeledBy();
    void get_CurrentAriaRole();
    void get_CurrentAriaProperties();
    void get_CurrentIsDataValidForForm();
    void get_CurrentControllerFor();
    void get_CurrentDescribedBy();
    void get_CurrentFlowsTo();
    void get_CurrentProviderDescription();
    void get_CachedProcessId();
    void get_CachedControlType();
    void get_CachedLocalizedControlType();
    void get_CachedName();
    void get_CachedAcceleratorKey();
    void get_CachedAccessKey();
    void get_CachedHasKeyboardFocus();
    void get_CachedIsKeyboardFocusable();
    void get_CachedIsEnabled();
    void get_CachedAutomationId();
    void get_CachedClassName();
    void get_CachedHelpText();
    void get_CachedCulture();
    void get_CachedIsControlElement();
    void get_CachedIsContentElement();
    void get_CachedIsPassword();
    void get_CachedNativeWindowHandle();
    void get_CachedItemType();

    [PreserveSig]
    int get_CachedIsOffscreen(out int isOffscreen);
    void get_CachedOrientation();
    void get_CachedFrameworkId();
    void get_CachedIsRequiredForForm();
    void get_CachedItemStatus();

    [PreserveSig]
    int get_CachedBoundingRectangle(out NativeRect boundingRectangle);
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

/// <summary>
///     IUIAutomationCacheRequest: which properties are fetched together with the elements of a search
/// </summary>
[ComImport]
[Guid("b32a92b5-bc25-4078-9c08-d7ee95c48e03")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationCacheRequest
{
    [PreserveSig]
    int AddProperty(int propertyId);

    void AddPattern();
    void Clone();
    void get_TreeScope();
    void put_TreeScope();
    void get_TreeFilter();
    void put_TreeFilter();
    void get_AutomationElementMode();

    [PreserveSig]
    int put_AutomationElementMode(int mode);
}

/// <summary>
///     IUIAutomationElementArray
/// </summary>
[ComImport]
[Guid("14314595-b4bc-4055-95f2-58f2e42c9855")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationElementArray
{
    [PreserveSig]
    int get_Length(out int length);

    [PreserveSig]
    int GetElement(int index, out IUIAutomationElement element);
}

/// <summary>
///     IUIAutomation2 (Windows 8+, CUIAutomation8): adds the timeouts. COM interop can't inherit an interface layout, so the
///     55 methods of IUIAutomation are declared as placeholders first.
/// </summary>
[ComImport]
[Guid("34723aff-0c9d-49d0-9896-7ab52df8cd8a")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomation2
{
    void CompareElements();
    void CompareRuntimeIds();
    void GetRootElement();
    void ElementFromHandle();
    void ElementFromPoint();
    void GetFocusedElement();
    void GetRootElementBuildCache();
    void ElementFromHandleBuildCache();
    void ElementFromPointBuildCache();
    void GetFocusedElementBuildCache();
    void CreateTreeWalker();
    void get_ControlViewWalker();
    void get_ContentViewWalker();
    void get_RawViewWalker();
    void get_RawViewCondition();
    void get_ControlViewCondition();
    void get_ContentViewCondition();
    void CreateCacheRequest();
    void CreateTrueCondition();
    void CreateFalseCondition();
    void CreatePropertyCondition();
    void CreatePropertyConditionEx();
    void CreateAndCondition();
    void CreateAndConditionFromArray();
    void CreateAndConditionFromNativeArray();
    void CreateOrCondition();
    void CreateOrConditionFromArray();
    void CreateOrConditionFromNativeArray();
    void CreateNotCondition();
    void AddAutomationEventHandler();
    void RemoveAutomationEventHandler();
    void AddPropertyChangedEventHandlerNativeArray();
    void AddPropertyChangedEventHandler();
    void RemovePropertyChangedEventHandler();
    void AddStructureChangedEventHandler();
    void RemoveStructureChangedEventHandler();
    void AddFocusChangedEventHandler();
    void RemoveFocusChangedEventHandler();
    void RemoveAllEventHandlers();
    void IntNativeArrayToSafeArray();
    void IntSafeArrayToNativeArray();
    void RectToVariant();
    void VariantToRect();
    void SafeArrayToRectNativeArray();
    void CreateProxyFactoryEntry();
    void get_ProxyFactoryMapping();
    void GetPropertyProgrammaticName();
    void GetPatternProgrammaticName();
    void PollForPotentialSupportedPatterns();
    void PollForPotentialSupportedProperties();
    void CheckNotSupported();
    void get_ReservedNotSupportedValue();
    void get_ReservedMixedAttributeValue();
    void ElementFromIAccessible();
    void ElementFromIAccessibleBuildCache();

    void get_AutoSetFocus();
    void put_AutoSetFocus();
    void get_ConnectionTimeout();

    [PreserveSig]
    int put_ConnectionTimeout(uint timeoutMilliseconds);

    void get_TransactionTimeout();

    [PreserveSig]
    int put_TransactionTimeout(uint timeoutMilliseconds);
}
