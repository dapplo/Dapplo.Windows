// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;

namespace Dapplo.Windows.Imaging.Interop;

/// <summary>
/// Marshals PROPVARIANT values of the WIC metadata to and from .NET.
/// A PROPVARIANT is a VARTYPE (ushort) and three reserved ushorts, followed by the value at offset 8.
/// Blobs and vectors (counted arrays) are a 32 bit count at offset 8 and a pointer after it (offset 8 + pointer size, because of the alignment).
/// Everything allocated for a PROPVARIANT uses CoTaskMemAlloc, so PropVariantClear frees it.
/// </summary>
internal static class PropVariant
{
    // VARENUM values
    private const ushort VtEmpty = 0;
    private const ushort VtI2 = 2;
    private const ushort VtI4 = 3;
    private const ushort VtR4 = 4;
    private const ushort VtR8 = 5;
    private const ushort VtDate = 7;
    private const ushort VtBstr = 8;
    private const ushort VtBool = 11;
    private const ushort VtUnknown = 13;
    private const ushort VtI1 = 16;
    private const ushort VtUi1 = 17;
    private const ushort VtUi2 = 18;
    private const ushort VtUi4 = 19;
    private const ushort VtI8 = 20;
    private const ushort VtUi8 = 21;
    private const ushort VtInt = 22;
    private const ushort VtUint = 23;
    private const ushort VtLpstr = 30;
    private const ushort VtLpwstr = 31;
    private const ushort VtFiletime = 64;
    private const ushort VtBlob = 65;
    private const ushort VtClsid = 72;
    private const ushort VtVector = 0x1000;

    /// <summary>
    /// Bytes to allocate for a PROPVARIANT, 24 on 64 bit (16 on 32 bit)
    /// </summary>
    private const int Size = 24;

    private const int ValueOffset = 8;
    private static int PointerOffset => ValueOffset + IntPtr.Size;

    /// <summary>
    /// Allocate an empty (VT_EMPTY) PROPVARIANT, free it with <see cref="Free"/>
    /// </summary>
    public static nint Allocate()
    {
        var propVariant = Marshal.AllocCoTaskMem(Size);
        for (int i = 0; i < Size; i += 4)
        {
            Marshal.WriteInt32(propVariant, i, 0);
        }
        return propVariant;
    }

    /// <summary>
    /// Clear (PropVariantClear) and free the PROPVARIANT
    /// </summary>
    public static void Free(nint propVariant)
    {
        if (propVariant == IntPtr.Zero)
        {
            return;
        }
        PropVariantClear(propVariant);
        Marshal.FreeCoTaskMem(propVariant);
    }

    /// <summary>
    /// The VARTYPE of the PROPVARIANT
    /// </summary>
    public static ushort GetVarType(nint propVariant) => unchecked((ushort)Marshal.ReadInt16(propVariant));

    /// <summary>
    /// Convert the value of a PROPVARIANT to .NET.
    /// VT_UNKNOWN gives the object (runtime callable wrapper), the caller must release it.
    /// </summary>
    /// <param name="propVariant">pointer to the PROPVARIANT</param>
    /// <param name="value">the value, null for VT_EMPTY</param>
    /// <returns>false when the type is not supported</returns>
    public static bool TryRead(nint propVariant, out object value)
    {
        var varType = GetVarType(propVariant);
        var data = propVariant + ValueOffset;
        value = null;
        switch (varType)
        {
            case VtEmpty:
                return true;
            case VtI1:
                value = unchecked((sbyte)Marshal.ReadByte(data));
                return true;
            case VtUi1:
                value = Marshal.ReadByte(data);
                return true;
            case VtI2:
                value = Marshal.ReadInt16(data);
                return true;
            case VtUi2:
                value = unchecked((ushort)Marshal.ReadInt16(data));
                return true;
            case VtI4:
            case VtInt:
                value = Marshal.ReadInt32(data);
                return true;
            case VtUi4:
            case VtUint:
                value = unchecked((uint)Marshal.ReadInt32(data));
                return true;
            case VtI8:
                value = Marshal.ReadInt64(data);
                return true;
            case VtUi8:
                value = unchecked((ulong)Marshal.ReadInt64(data));
                return true;
            case VtR4:
                value = BitConverter.ToSingle(BitConverter.GetBytes(Marshal.ReadInt32(data)), 0);
                return true;
            case VtR8:
                value = BitConverter.Int64BitsToDouble(Marshal.ReadInt64(data));
                return true;
            case VtDate:
                value = DateTime.FromOADate(BitConverter.Int64BitsToDouble(Marshal.ReadInt64(data)));
                return true;
            case VtBool:
                value = Marshal.ReadInt16(data) != 0;
                return true;
            case VtLpstr:
                value = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(data));
                return true;
            case VtLpwstr:
                value = Marshal.PtrToStringUni(Marshal.ReadIntPtr(data));
                return true;
            case VtBstr:
                value = Marshal.PtrToStringBSTR(Marshal.ReadIntPtr(data));
                return true;
            case VtFiletime:
                value = DateTime.FromFileTimeUtc(Marshal.ReadInt64(data));
                return true;
            case VtClsid:
                value = Marshal.PtrToStructure<Guid>(Marshal.ReadIntPtr(data));
                return true;
            case VtUnknown:
                var unknown = Marshal.ReadIntPtr(data);
                value = unknown == IntPtr.Zero ? null : Marshal.GetObjectForIUnknown(unknown);
                return true;
            case VtBlob:
            case VtVector | VtUi1:
                value = ReadArray<byte>(propVariant, 1);
                return true;
            case VtVector | VtI1:
                value = ReadArray<sbyte>(propVariant, 1);
                return true;
            case VtVector | VtUi2:
                value = ReadArray<ushort>(propVariant, 2);
                return true;
            case VtVector | VtI2:
                value = ReadArray<short>(propVariant, 2);
                return true;
            case VtVector | VtUi4:
                value = ReadArray<uint>(propVariant, 4);
                return true;
            case VtVector | VtI4:
                value = ReadArray<int>(propVariant, 4);
                return true;
            case VtVector | VtUi8:
                value = ReadArray<ulong>(propVariant, 8);
                return true;
            case VtVector | VtI8:
                value = ReadArray<long>(propVariant, 8);
                return true;
            case VtVector | VtR4:
                value = ReadArray<float>(propVariant, 4);
                return true;
            case VtVector | VtR8:
                value = ReadArray<double>(propVariant, 8);
                return true;
            case VtVector | VtLpstr:
            case VtVector | VtLpwstr:
                var count = Marshal.ReadInt32(propVariant, ValueOffset);
                var elements = Marshal.ReadIntPtr(propVariant, PointerOffset);
                var strings = new string[count];
                for (int i = 0; i < count; i++)
                {
                    var element = Marshal.ReadIntPtr(elements, i * IntPtr.Size);
                    strings[i] = varType == (VtVector | VtLpstr) ? Marshal.PtrToStringAnsi(element) : Marshal.PtrToStringUni(element);
                }
                value = strings;
                return true;
            default:
                return false;
        }
    }

    private static unsafe T[] ReadArray<T>(nint propVariant, int elementSize) where T : unmanaged
    {
        var count = Marshal.ReadInt32(propVariant, ValueOffset);
        var elements = Marshal.ReadIntPtr(propVariant, PointerOffset);
        var result = new T[count];
        if (count > 0 && elements != IntPtr.Zero)
        {
            fixed (T* destination = result)
            {
                Buffer.MemoryCopy((void*)elements, destination, (long)count * elementSize, (long)count * elementSize);
            }
        }
        return result;
    }

    /// <summary>
    /// Store a .NET value in an empty PROPVARIANT (from <see cref="Allocate"/>)
    /// </summary>
    /// <param name="propVariant">pointer to the PROPVARIANT</param>
    /// <param name="value">the value, see <see cref="WicMetadataWriter.Set"/> for the supported types</param>
    /// <exception cref="ArgumentException">when the type of the value is not supported</exception>
    public static void Write(nint propVariant, object value)
    {
        var data = propVariant + ValueOffset;
        switch (value)
        {
            case null:
                SetVarType(propVariant, VtEmpty);
                break;
            case WicMetadataValue typed:
                Write(propVariant, typed);
                break;
            case string text:
                SetVarType(propVariant, VtLpwstr);
                Marshal.WriteIntPtr(data, Marshal.StringToCoTaskMemUni(text));
                break;
            case bool flag:
                SetVarType(propVariant, VtBool);
                Marshal.WriteInt16(data, flag ? (short)-1 : (short)0);
                break;
            case sbyte number:
                SetVarType(propVariant, VtI1);
                Marshal.WriteByte(data, unchecked((byte)number));
                break;
            case byte number:
                SetVarType(propVariant, VtUi1);
                Marshal.WriteByte(data, number);
                break;
            case short number:
                SetVarType(propVariant, VtI2);
                Marshal.WriteInt16(data, number);
                break;
            case ushort number:
                SetVarType(propVariant, VtUi2);
                Marshal.WriteInt16(data, unchecked((short)number));
                break;
            case int number:
                SetVarType(propVariant, VtI4);
                Marshal.WriteInt32(data, number);
                break;
            case uint number:
                SetVarType(propVariant, VtUi4);
                Marshal.WriteInt32(data, unchecked((int)number));
                break;
            case long number:
                SetVarType(propVariant, VtI8);
                Marshal.WriteInt64(data, number);
                break;
            case ulong number:
                SetVarType(propVariant, VtUi8);
                Marshal.WriteInt64(data, unchecked((long)number));
                break;
            case float number:
                SetVarType(propVariant, VtR4);
                Marshal.WriteInt32(data, BitConverter.ToInt32(BitConverter.GetBytes(number), 0));
                break;
            case double number:
                SetVarType(propVariant, VtR8);
                Marshal.WriteInt64(data, BitConverter.DoubleToInt64Bits(number));
                break;
            case WicRational rational:
                SetVarType(propVariant, VtUi8);
                Marshal.WriteInt64(data, unchecked((long)rational.ToPacked()));
                break;
            case WicSignedRational rational:
                SetVarType(propVariant, VtI8);
                Marshal.WriteInt64(data, rational.ToPacked());
                break;
            case DateTime dateTime:
                SetVarType(propVariant, VtFiletime);
                Marshal.WriteInt64(data, dateTime.ToFileTimeUtc());
                break;
            case DateTimeOffset dateTimeOffset:
                SetVarType(propVariant, VtFiletime);
                Marshal.WriteInt64(data, dateTimeOffset.ToFileTime());
                break;
            case Guid guid:
                SetVarType(propVariant, VtClsid);
                var guidMemory = Marshal.AllocCoTaskMem(16);
                Marshal.StructureToPtr(guid, guidMemory, false);
                Marshal.WriteIntPtr(data, guidMemory);
                break;
            case byte[] bytes:
                WriteArray(propVariant, VtBlob, bytes, 1);
                break;
            case ushort[] numbers:
                WriteArray(propVariant, VtVector | VtUi2, numbers, 2);
                break;
            case uint[] numbers:
                WriteArray(propVariant, VtVector | VtUi4, numbers, 4);
                break;
            case int[] numbers:
                WriteArray(propVariant, VtVector | VtI4, numbers, 4);
                break;
            case string[] texts:
                SetVarType(propVariant, VtVector | VtLpwstr);
                var array = Marshal.AllocCoTaskMem(Math.Max(1, texts.Length) * IntPtr.Size);
                for (int i = 0; i < texts.Length; i++)
                {
                    Marshal.WriteIntPtr(array, i * IntPtr.Size, Marshal.StringToCoTaskMemUni(texts[i] ?? string.Empty));
                }
                Marshal.WriteInt32(propVariant, ValueOffset, texts.Length);
                Marshal.WriteIntPtr(propVariant, PointerOffset, array);
                break;
            default:
                throw new ArgumentException($"Metadata values of type {value.GetType()} are not supported", nameof(value));
        }
    }

    private static void Write(nint propVariant, WicMetadataValue value)
    {
        var data = propVariant + ValueOffset;
        switch (value.Kind)
        {
            case WicMetadataValueKind.AnsiString:
                SetVarType(propVariant, VtLpstr);
                Marshal.WriteIntPtr(data, Marshal.StringToCoTaskMemAnsi((string)value.Value ?? string.Empty));
                break;
            case WicMetadataValueKind.ByteVector:
                WriteArray(propVariant, VtVector | VtUi1, (byte[])value.Value ?? Array.Empty<byte>(), 1);
                break;
            default:
                Write(propVariant, value.Value);
                break;
        }
    }

    private static unsafe void WriteArray<T>(nint propVariant, ushort varType, T[] values, int elementSize) where T : unmanaged
    {
        SetVarType(propVariant, varType);
        var byteCount = (long)values.Length * elementSize;
        var memory = Marshal.AllocCoTaskMem((int)Math.Max(1, byteCount));
        fixed (T* source = values)
        {
            Buffer.MemoryCopy(source, (void*)memory, byteCount, byteCount);
        }
        Marshal.WriteInt32(propVariant, ValueOffset, values.Length);
        Marshal.WriteIntPtr(propVariant, PointerOffset, memory);
    }

    /// <summary>
    /// Store a COM object as VT_UNKNOWN, the PROPVARIANT holds a reference which PropVariantClear releases
    /// </summary>
    public static void WriteUnknown(nint propVariant, object comObject, Type interfaceType)
    {
        SetVarType(propVariant, VtUnknown);
        Marshal.WriteIntPtr(propVariant + ValueOffset, Marshal.GetComInterfaceForObject(comObject, interfaceType));
    }

    private static void SetVarType(nint propVariant, ushort varType) => Marshal.WriteInt16(propVariant, unchecked((short)varType));

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern HResult PropVariantClear(nint pvar);
}
