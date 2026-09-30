# Clipboard samples

Byte-for-byte clipboard data used by `ClipboardFormatParsingTests`. They are **synthetic**: each file follows the layout
the named producer writes (header style, offset digits, line endings, row order, masks), with a small known content so
the tests can check every value. Replace or extend them with real captures when a producer behaves differently.

| File | Layout |
|---|---|
| `html-chrome-style.bin` | CF_HTML 0.9, 10-digit offsets, `SourceURL`, `<html>\r\n<body>\r\n<!--StartFragment-->`, non-ASCII text (Chrome / Edge) |
| `html-firefox-style.bin` | CF_HTML 0.9, 8-digit offsets, `<html><body>\n` (Firefox) |
| `html-word-style.bin` | CF_HTML 1.0, full Office document with namespaces and styles, `file:///` source URL (Word) |
| `html-greenshot.bin` | Greenshot's template with `StartSelection` / `EndSelection` and `<!--StartFragment -->` (with a space) |
| `html-character-offsets.bin` | Offsets counted in characters instead of UTF-8 bytes, trailing NULs (a buggy producer): the comments must be used |
| `html-v1-no-context.bin` | CF_HTML 1.0 with `StartHTML:-1` / `EndHTML:-1` |
| `dib-24bpp-bottomup.bin` | BITMAPINFOHEADER, 24 bpp BI_RGB, bottom-up, row padding (Paint / Office CF_DIB) |
| `dib-32bpp-bitfields.bin` | BITMAPINFOHEADER, 32 bpp BI_BITFIELDS with three masks after the header (Windows synthesized from CF_BITMAP) |
| `dib-32bpp-rgb-topdown-alpha.bin` | BITMAPINFOHEADER, 32 bpp BI_RGB, top-down, alpha in the fourth byte (browser style) |
| `dib-32bpp-rgb-zero-alpha.bin` | BITMAPINFOHEADER, 32 bpp BI_RGB, fourth byte 0: opaque |
| `dibv5-bitfields-alpha.bin` | BITMAPV5HEADER, BI_BITFIELDS with alpha mask, sRGB, bottom-up (Snipping Tool / Chromium CF_DIBV5) |
| `dibv4-bitfields-alpha.bin` | BITMAPV4HEADER variant |
| `dibv5-greenshot.bin` | Greenshot's CF_DIBV5: V5 header, then the masks again with all 12 bytes reversed, then the pixels |
| `dib-8bpp-palette.bin` | 8 bpp with a 6 color palette |

All bitmaps are 3x2 pixels: top row red, green (alpha 128), blue (alpha 0); bottom row white, black, gray (alpha 64).
