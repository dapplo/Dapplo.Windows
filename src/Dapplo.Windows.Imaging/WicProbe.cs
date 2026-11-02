// Temporary probe, scratch branch only
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Internal;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging;

public static class WicProbe
{
    private static byte[] Encode(Guid container, Action<WicMetadataWriter> metadata, StringBuilder report)
    {
        var pixels = new byte[3 * 2 * 4];
        using var output = new MemoryStream();
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            var result = WicImage.Encode(output, new WicEncodeOptions { ContainerFormat = container, Metadata = metadata }, handle.AddrOfPinnedObject(), 3, 2, 12, WicPixelFormats.Format32bppBgra);
            report.AppendLine("  issues: " + string.Join("; ", result.MetadataIssues));
        }
        finally { handle.Free(); }
        return output.ToArray();
    }

    public static string Run()
    {
        var report = new StringBuilder();
        foreach (var container in new[] { WicContainerFormats.Jpeg, WicContainerFormats.Tiff, WicContainerFormats.Png, WicContainerFormats.Wmp })
        {
            report.AppendLine($"== {container}");
            var bytes = Encode(container, w => { w.Set("System.ApplicationName", "App"); w.Set("System.Title", "Title"); }, report);
            var factory = WicNativeMethods.CreateImagingFactory();
            var stream = new ManagedIStream(new MemoryStream(bytes));
            factory.CreateDecoderFromStream(stream, IntPtr.Zero, WicDecodeOptions.MetadataCacheOnLoad, out var decoder);
            decoder.GetFrame(0, out var frame);
            var blockReader = frame as IWICMetadataBlockReader;
            report.AppendLine($"  frame block reader {blockReader != null}");
            if (blockReader != null)
            {
                blockReader.GetCount(out var count);
                for (uint i = 0; i < count; i++)
                {
                    var hr = blockReader.GetReaderByIndex(i, out var reader);
                    reader.GetMetadataFormat(out var format);
                    var ps = reader as IWICPersistStream;
                    string save = "n/a", saveEx = "n/a", sizeMax = "n/a";
                    if (ps != null)
                    {
                        var ms1 = new MemoryStream();
                        saveEx = $"0x{(int)ps.SaveEx(new ManagedIStream(ms1), 0, 0):X8} {ms1.Length}";
                        var ms2 = new MemoryStream();
                        save = $"0x{(int)ps.Save(new ManagedIStream(ms2), 0):X8} {ms2.Length}";
                        sizeMax = $"0x{(int)ps.GetSizeMax(out var max):X8} {max}";
                        var ms3 = new MemoryStream();
                        var r3 = ps.SaveEx(new ManagedIStream(ms3), 4, 0);
                        saveEx += $" nocache 0x{(int)r3:X8} {ms3.Length}";
                    }
                    report.AppendLine($"  block {i} hr 0x{(int)hr:X8} format {format} persist {ps != null} saveEx {saveEx} save {save} sizeMax {sizeMax}");
                    Wic.Release(reader);
                }
            }
            Wic.Release(frame); Wic.Release(decoder); Wic.Release(factory);

            var info = WicImage.GetFrameInfo(new MemoryStream(bytes));
            report.AppendLine("  raw: " + string.Join("; ", info.Metadata.RawBlocks));
            foreach (var raw in info.Metadata.RawBlocks)
            {
                if (container == WicContainerFormats.Tiff || container == WicContainerFormats.Wmp)
                {
                    foreach (var variant in new object[] { WicMetadataValue.ByteVector(raw.Data), raw.Data })
                    {
                        var copy = Encode(container, w => report.AppendLine($"  set 700 {(variant is byte[] ? "blob" : "vector")} -> {w.Set("/ifd/{ushort=700}", variant)}"), report);
                        var copied = WicImage.GetFrameInfo(new MemoryStream(copy));
                        report.AppendLine($"  copied title: {(copied.Metadata.TryGet("/ifd/xmp/dc:title/x-default", out var t) ? t : "missing")}, items {string.Join(" | ", copied.Metadata.Items.Where(x => x.Key.Contains("700") || x.Key.Contains("xmp")).Select(x => x.Key + "=" + x.Value))}");
                    }
                }
                else
                {
                    var copy = Encode(container, w => report.AppendLine($"  addraw {raw.Path} -> {w.AddRawBlock(raw)}"), report);
                    var copied = WicImage.GetFrameInfo(new MemoryStream(copy));
                    report.AppendLine($"  copied title: {(copied.Metadata.TryGet(raw.Path + "/dc:title/x-default", out var t) ? t : "missing")}");
                }
            }
        }
        return report.ToString();
    }
}
