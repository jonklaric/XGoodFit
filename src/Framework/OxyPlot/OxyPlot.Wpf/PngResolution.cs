// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PngResolution.cs" company="XGoodFit">
//   Copyright (c) 2026 XGoodFit contributors
// </copyright>
// <summary>
//   Writes a PNG file with its resolution, as WPF's PNG encoder did.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Buffers.Binary;
    using System.IO;

    /// <summary>
    /// Writes a PNG file with its resolution (a <c>pHYs</c> chunk), as WPF's PNG encoder did.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition, for <see cref="PngExporter"/>. WPF's encoder writes the bitmap's resolution,
    /// which word processors and image editors size the image by; Avalonia's (Skia's) writes none, so a
    /// 300 dpi export would be placed at 96 dpi, three times too large. The resolution is added after the
    /// header, unless the file already has one.
    /// </remarks>
    internal static class PngResolution
    {
        /// <summary>The length of the PNG signature and the header chunk (IHDR), which comes first.</summary>
        private const int HeaderLength = 8 + 4 + 4 + 13 + 4;

        /// <summary>
        /// Writes a PNG file to a stream, with a resolution.
        /// </summary>
        /// <param name="png">The PNG file.</param>
        /// <param name="resolution">The resolution in dots per inch.</param>
        /// <param name="stream">The output stream.</param>
        public static void Write(ReadOnlySpan<byte> png, double resolution, Stream stream)
        {
            if (png.Length <= HeaderLength || !png.Slice(12, 4).SequenceEqual("IHDR"u8) || HasChunk(png, "pHYs"u8))
            {
                stream.Write(png);
                return;
            }

            // The length, the type, pixels per metre across and down (rounded: 96 dpi is 3780), the unit
            // (1, the metre), and the CRC of the type and data.
            Span<byte> chunk = stackalloc byte[4 + 4 + 9 + 4];
            uint pixelsPerMetre = (uint)Math.Round(resolution / 0.0254);
            BinaryPrimitives.WriteUInt32BigEndian(chunk, 9);
            "pHYs"u8.CopyTo(chunk.Slice(4));
            BinaryPrimitives.WriteUInt32BigEndian(chunk.Slice(8), pixelsPerMetre);
            BinaryPrimitives.WriteUInt32BigEndian(chunk.Slice(12), pixelsPerMetre);
            chunk[16] = 1;
            BinaryPrimitives.WriteUInt32BigEndian(chunk.Slice(17), Crc32(chunk.Slice(4, 13)));

            stream.Write(png.Slice(0, HeaderLength));
            stream.Write(chunk);
            stream.Write(png.Slice(HeaderLength));
        }

        /// <summary>
        /// Whether a PNG file has a chunk of a type before its image data, where a <c>pHYs</c> chunk goes.
        /// </summary>
        private static bool HasChunk(ReadOnlySpan<byte> png, ReadOnlySpan<byte> type)
        {
            for (int i = 8; i + 12 <= png.Length;)
            {
                uint length = BinaryPrimitives.ReadUInt32BigEndian(png.Slice(i));
                var chunkType = png.Slice(i + 4, 4);
                if (chunkType.SequenceEqual(type))
                {
                    return true;
                }

                if (chunkType.SequenceEqual("IDAT"u8) || length > (uint)(png.Length - i - 12))
                {
                    return false;
                }

                i += 12 + (int)length;
            }

            return false;
        }

        /// <summary>
        /// The CRC-32 that ends a PNG chunk (ISO 3309, as zlib's).
        /// </summary>
        private static uint Crc32(ReadOnlySpan<byte> data)
        {
            uint crc = uint.MaxValue;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
                }
            }

            return ~crc;
        }
    }
}
