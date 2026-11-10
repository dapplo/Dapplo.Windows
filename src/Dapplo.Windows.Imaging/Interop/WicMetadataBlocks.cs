// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// The top-level metadata blocks of a decoded frame (QueryInterface on IWICBitmapFrameDecode), methods in the order of wincodecsdk.idl.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwicmetadatablockreader">IWICMetadataBlockReader interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("FEAA2A8D-B3F3-43E4-B25C-D1DE990A1AE1")]
internal interface IWICMetadataBlockReader
{
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetCount(out uint pcCount);
    [PreserveSig] HResult GetReaderByIndex(uint nIndex, out IWICMetadataReader ppIMetadataReader);
    [PreserveSig] HResult GetEnumerator(out IEnumUnknown ppIEnumMetadata);
}

/// <summary>
/// The top-level metadata blocks of a frame which is encoded (QueryInterface on IWICBitmapFrameEncode), inherits IWICMetadataBlockReader.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwicmetadatablockwriter">IWICMetadataBlockWriter interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("08FB9676-B444-41E8-8DBE-6A53A542BFF1")]
internal interface IWICMetadataBlockWriter
{
    // IWICMetadataBlockReader
    [PreserveSig] HResult GetContainerFormat(out Guid pguidContainerFormat);
    [PreserveSig] HResult GetCount(out uint pcCount);
    [PreserveSig] HResult GetReaderByIndex(uint nIndex, out IWICMetadataReader ppIMetadataReader);
    [PreserveSig] HResult GetEnumerator(out IEnumUnknown ppIEnumMetadata);

    // IWICMetadataBlockWriter
    [PreserveSig] HResult InitializeFromBlockReader(IWICMetadataBlockReader pIMDBlockReader);
    [PreserveSig] HResult GetWriterByIndex(uint nIndex, out IWICMetadataWriter ppIMetadataWriter);
    [PreserveSig] HResult AddWriter(IWICMetadataWriter pIMetadataWriter);
    [PreserveSig] HResult SetWriterByIndex(uint nIndex, IWICMetadataWriter pIMetadataWriter);
    [PreserveSig] HResult RemoveWriterByIndex(uint nIndex);
}

/// <summary>
/// A metadata reader (one block), methods in the order of wincodecsdk.idl.
/// The PROPVARIANT parameters of GetValueByIndex are [in, out, unique]: 0 when not needed, the caller clears them.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwicmetadatareader">IWICMetadataReader interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("9204FE99-D8FC-4FD5-A001-9536B067A899")]
internal interface IWICMetadataReader
{
    [PreserveSig] HResult GetMetadataFormat(out Guid pguidMetadataFormat);
    [PreserveSig] HResult GetMetadataHandlerInfo(out nint ppIHandler);
    [PreserveSig] HResult GetCount(out uint pcCount);
    [PreserveSig] HResult GetValueByIndex(uint nIndex, nint pvarSchema, nint pvarId, nint pvarValue);
    [PreserveSig] HResult GetValue(nint pvarSchema, nint pvarId, nint pvarValue);
    [PreserveSig] HResult GetEnumerator(out nint ppIEnumMetadata);
}

/// <summary>
/// A metadata writer (one block), only the first method is declared, which is allowed for a prefix of the vtable.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwicmetadatawriter">IWICMetadataWriter interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("F7836E16-3BE0-470B-86BB-160D0AECD7DE")]
internal interface IWICMetadataWriter
{
    [PreserveSig] HResult GetMetadataFormat(out Guid pguidMetadataFormat);
}

/// <summary>
/// Persists a metadata block to or from a stream, IPersist and IPersistStream methods first, in the order of the SDK.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwicpersiststream">IWICPersistStream interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00675040-6908-45F8-86A3-49C7DFD6D9AD")]
internal interface IWICPersistStream
{
    // IPersist
    [PreserveSig] HResult GetClassID(out Guid pClassID);

    // IPersistStream
    [PreserveSig] HResult IsDirty();
    [PreserveSig] HResult Load(IWicStream pStm);
    [PreserveSig] HResult Save(IWicStream pStm, int fClearDirty);
    [PreserveSig] HResult GetSizeMax(out ulong pcbSize);

    // IWICPersistStream
    [PreserveSig] HResult LoadEx(IWicStream pIStream, nint pguidPreferredVendor, uint dwPersistOptions);
    [PreserveSig] HResult SaveEx(IWicStream pIStream, uint dwPersistOptions, int fClearDirty);
}

/// <summary>
/// The component factory, the same object as the imaging factory (QueryInterface), it inherits IWICImagingFactory: all of its methods are repeated first.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/wincodecsdk/nn-wincodecsdk-iwiccomponentfactory">IWICComponentFactory interface</a>
/// </summary>
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("412D0C3A-9650-44FA-AF5B-DD2A06C8E8FB")]
internal interface IWICComponentFactory
{
    // IWICImagingFactory, 25 methods which aren't called through this interface
    [PreserveSig] HResult CreateDecoderFromFilename(nint a, nint b, uint c, int d, out nint e);
    [PreserveSig] HResult CreateDecoderFromStream(nint a, nint b, int c, out nint d);
    [PreserveSig] HResult CreateDecoderFromFileHandle(nint a, nint b, int c, out nint d);
    [PreserveSig] HResult CreateComponentInfo(nint a, out nint b);
    [PreserveSig] HResult CreateDecoder(nint a, nint b, out nint c);
    [PreserveSig] HResult CreateEncoder(nint a, nint b, out nint c);
    [PreserveSig] HResult CreatePalette(out nint a);
    [PreserveSig] HResult CreateFormatConverter(out nint a);
    [PreserveSig] HResult CreateBitmapScaler(out nint a);
    [PreserveSig] HResult CreateBitmapClipper(out nint a);
    [PreserveSig] HResult CreateBitmapFlipRotator(out nint a);
    [PreserveSig] HResult CreateStream(out nint a);
    [PreserveSig] HResult CreateColorContext(out nint a);
    [PreserveSig] HResult CreateColorTransformer(out nint a);
    [PreserveSig] HResult CreateBitmap(uint a, uint b, nint c, int d, out nint e);
    [PreserveSig] HResult CreateBitmapFromSource(nint a, int b, out nint c);
    [PreserveSig] HResult CreateBitmapFromSourceRect(nint a, uint b, uint c, uint d, uint e, out nint f);
    [PreserveSig] HResult CreateBitmapFromMemory(uint a, uint b, nint c, uint d, uint e, nint f, out nint g);
    [PreserveSig] HResult CreateBitmapFromHBITMAP(nint a, nint b, int c, out nint d);
    [PreserveSig] HResult CreateBitmapFromHICON(nint a, out nint b);
    [PreserveSig] HResult CreateComponentEnumerator(uint a, uint b, out nint c);
    [PreserveSig] HResult CreateFastMetadataEncoderFromDecoder(nint a, out nint b);
    [PreserveSig] HResult CreateFastMetadataEncoderFromFrameDecode(nint a, out nint b);
    [PreserveSig] HResult CreateQueryWriter(nint a, nint b, out nint c);
    [PreserveSig] HResult CreateQueryWriterFromReader(nint a, nint b, out nint c);

    // IWICComponentFactory
    [PreserveSig] HResult CreateMetadataReader([In] ref Guid guidMetadataFormat, nint pguidVendor, uint dwOptions, IWicStream pIStream, out IWICMetadataReader ppIReader);
    [PreserveSig] HResult CreateMetadataReaderFromContainer(nint guidContainerFormat, nint pguidVendor, uint dwOptions, nint pIStream, out nint ppIReader);
    [PreserveSig] HResult CreateMetadataWriter(nint guidMetadataFormat, nint pguidVendor, uint dwMetadataOptions, out nint ppIWriter);
    [PreserveSig] HResult CreateMetadataWriterFromReader(IWICMetadataReader pIReader, nint pguidVendor, out IWICMetadataWriter ppIWriter);
    [PreserveSig] HResult CreateQueryReaderFromBlockReader(nint pIBlockReader, out nint ppIQueryReader);
    [PreserveSig] HResult CreateQueryWriterFromBlockWriter(nint pIBlockWriter, out nint ppIQueryWriter);
    [PreserveSig] HResult CreateEncoderPropertyBag(nint ppropOptions, uint cCount, out nint ppIPropertyBag);
}
