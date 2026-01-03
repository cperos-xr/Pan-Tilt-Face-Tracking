using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class StringCompressor
{
    /// <summary>
    /// Compresses a string to a base64-encoded GZip byte array.
    /// Uses System.IO.Compression - works in Editor but may fail on some mobile platforms.
    /// </summary>
    public static string CompressToBase64(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var ms = new MemoryStream())
            {
                using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                {
                    gzip.Write(bytes, 0, bytes.Length);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[StringCompressor] CompressToBase64 failed: {e.Message}");
            // Fallback: just return base64 of raw UTF8 bytes (no compression)
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }
    }

    /// <summary>
    /// Decompresses a base64-encoded GZip byte array to a string.
    /// Uses System.IO.Compression - works in Editor but may fail on some mobile platforms.
    /// Falls back to pure C# GZip decoder on failure.
    /// </summary>
    public static string DecompressFromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return "";
        var bytes = Convert.FromBase64String(base64);
        
        try
        {
            using (var ms = new MemoryStream(bytes))
            using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress))
            using (var outStream = new MemoryStream())
            {
                gzip.CopyTo(outStream);
                return Encoding.UTF8.GetString(outStream.ToArray());
            }
        }
        catch (DllNotFoundException)
        {
            Debug.Log("[StringCompressor] System.IO.Compression not available, using fallback decoder.");
            return DecompressGZipFallback(bytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StringCompressor] DecompressFromBase64 failed: {e.Message}");
            // Try fallback
            try
            {
                return DecompressGZipFallback(bytes);
            }
            catch
            {
                // Last resort: assume it's uncompressed base64
                return Encoding.UTF8.GetString(bytes);
            }
        }
    }

    /// <summary>
    /// Pure C# GZip decompressor fallback for platforms where System.IO.Compression is unavailable.
    /// </summary>
    private static string DecompressGZipFallback(byte[] gzipData)
    {
        // GZip header: 1f 8b
        if (gzipData.Length < 10 || gzipData[0] != 0x1f || gzipData[1] != 0x8b)
        {
            throw new InvalidDataException("Invalid GZip header");
        }

        // Skip GZip header (minimum 10 bytes)
        int headerSize = 10;
        byte flags = gzipData[3];
        
        // Check for extra fields
        if ((flags & 0x04) != 0) // FEXTRA
        {
            int extraLen = gzipData[headerSize] | (gzipData[headerSize + 1] << 8);
            headerSize += 2 + extraLen;
        }
        if ((flags & 0x08) != 0) // FNAME
        {
            while (gzipData[headerSize] != 0) headerSize++;
            headerSize++;
        }
        if ((flags & 0x10) != 0) // FCOMMENT
        {
            while (gzipData[headerSize] != 0) headerSize++;
            headerSize++;
        }
        if ((flags & 0x02) != 0) // FHCRC
        {
            headerSize += 2;
        }

        // Deflate data is between header and trailer (last 8 bytes)
        int deflateLen = gzipData.Length - headerSize - 8;
        byte[] deflateData = new byte[deflateLen];
        Array.Copy(gzipData, headerSize, deflateData, 0, deflateLen);

        // Decompress deflate data
        byte[] decompressed = InflateDeflate(deflateData);
        return Encoding.UTF8.GetString(decompressed);
    }

    /// <summary>
    /// Simple DEFLATE decompressor (supports most common cases).
    /// </summary>
    private static byte[] InflateDeflate(byte[] data)
    {
        using (var output = new MemoryStream())
        {
            var inflater = new Inflater(data);
            inflater.Inflate(output);
            return output.ToArray();
        }
    }

    /// <summary>
    /// Minimal pure C# Deflate inflater.
    /// </summary>
    private class Inflater
    {
        private byte[] _input;
        private int _bitPos;
        private int _bytePos;

        // Fixed Huffman tables
        private static readonly int[] LengthBase = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
        private static readonly int[] LengthExtra = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
        private static readonly int[] DistBase = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
        private static readonly int[] DistExtra = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
        private static readonly int[] CodeLengthOrder = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

        public Inflater(byte[] input)
        {
            _input = input;
            _bitPos = 0;
            _bytePos = 0;
        }

        public void Inflate(MemoryStream output)
        {
            bool finalBlock;
            do
            {
                finalBlock = ReadBits(1) == 1;
                int blockType = ReadBits(2);

                if (blockType == 0)
                    InflateStored(output);
                else if (blockType == 1)
                    InflateFixed(output);
                else if (blockType == 2)
                    InflateDynamic(output);
                else
                    throw new InvalidDataException("Invalid deflate block type");
            } while (!finalBlock);
        }

        private int ReadBits(int count)
        {
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                if (_bytePos >= _input.Length) return result;
                int bit = (_input[_bytePos] >> _bitPos) & 1;
                result |= bit << i;
                _bitPos++;
                if (_bitPos == 8)
                {
                    _bitPos = 0;
                    _bytePos++;
                }
            }
            return result;
        }

        private void InflateStored(MemoryStream output)
        {
            // Align to byte boundary
            if (_bitPos != 0) { _bitPos = 0; _bytePos++; }
            int len = _input[_bytePos] | (_input[_bytePos + 1] << 8);
            _bytePos += 4; // skip len and nlen
            output.Write(_input, _bytePos, len);
            _bytePos += len;
        }

        private void InflateFixed(MemoryStream output)
        {
            // Build fixed Huffman trees
            int[] litLens = new int[288];
            for (int i = 0; i <= 143; i++) litLens[i] = 8;
            for (int i = 144; i <= 255; i++) litLens[i] = 9;
            for (int i = 256; i <= 279; i++) litLens[i] = 7;
            for (int i = 280; i <= 287; i++) litLens[i] = 8;

            int[] distLens = new int[32];
            for (int i = 0; i < 32; i++) distLens[i] = 5;

            var litTree = BuildHuffmanTree(litLens);
            var distTree = BuildHuffmanTree(distLens);

            DecodeBlocks(output, litTree, distTree);
        }

        private void InflateDynamic(MemoryStream output)
        {
            int hlit = ReadBits(5) + 257;
            int hdist = ReadBits(5) + 1;
            int hclen = ReadBits(4) + 4;

            int[] codeLens = new int[19];
            for (int i = 0; i < hclen; i++)
                codeLens[CodeLengthOrder[i]] = ReadBits(3);

            var codeTree = BuildHuffmanTree(codeLens);

            int[] lengths = new int[hlit + hdist];
            int idx = 0;
            while (idx < lengths.Length)
            {
                int sym = DecodeSymbol(codeTree);
                if (sym < 16)
                    lengths[idx++] = sym;
                else if (sym == 16)
                {
                    int rep = ReadBits(2) + 3;
                    int val = lengths[idx - 1];
                    for (int i = 0; i < rep; i++) lengths[idx++] = val;
                }
                else if (sym == 17)
                {
                    int rep = ReadBits(3) + 3;
                    for (int i = 0; i < rep; i++) lengths[idx++] = 0;
                }
                else
                {
                    int rep = ReadBits(7) + 11;
                    for (int i = 0; i < rep; i++) lengths[idx++] = 0;
                }
            }

            int[] litLens = new int[hlit];
            int[] distLens = new int[hdist];
            Array.Copy(lengths, 0, litLens, 0, hlit);
            Array.Copy(lengths, hlit, distLens, 0, hdist);

            var litTree = BuildHuffmanTree(litLens);
            var distTree = BuildHuffmanTree(distLens);

            DecodeBlocks(output, litTree, distTree);
        }

        private void DecodeBlocks(MemoryStream output, int[][] litTree, int[][] distTree)
        {
            while (true)
            {
                int sym = DecodeSymbol(litTree);
                if (sym < 256)
                    output.WriteByte((byte)sym);
                else if (sym == 256)
                    break;
                else
                {
                    int lenIdx = sym - 257;
                    int length = LengthBase[lenIdx] + ReadBits(LengthExtra[lenIdx]);
                    int distSym = DecodeSymbol(distTree);
                    int distance = DistBase[distSym] + ReadBits(DistExtra[distSym]);

                    byte[] buf = output.GetBuffer();
                    long pos = output.Position;
                    for (int i = 0; i < length; i++)
                        output.WriteByte(buf[pos - distance + i]);
                }
            }
        }

        private int DecodeSymbol(int[][] tree)
        {
            int node = 0;
            while (tree[node][0] >= 0)
            {
                int bit = ReadBits(1);
                node = tree[node][bit];
            }
            return -tree[node][0] - 1;
        }

        private int[][] BuildHuffmanTree(int[] lengths)
        {
            int maxLen = 0;
            foreach (int len in lengths) if (len > maxLen) maxLen = len;

            int[] blCount = new int[maxLen + 1];
            foreach (int len in lengths) if (len > 0) blCount[len]++;

            int[] nextCode = new int[maxLen + 1];
            int code = 0;
            for (int bits = 1; bits <= maxLen; bits++)
            {
                code = (code + blCount[bits - 1]) << 1;
                nextCode[bits] = code;
            }

            int[] codes = new int[lengths.Length];
            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] > 0)
                {
                    codes[i] = nextCode[lengths[i]]++;
                }
            }

            // Build tree
            var tree = new System.Collections.Generic.List<int[]>();
            tree.Add(new int[] { 0, 0 });

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] == 0) continue;
                int node = 0;
                for (int bit = lengths[i] - 1; bit >= 0; bit--)
                {
                    int b = (codes[i] >> bit) & 1;
                    if (tree[node][b] == 0)
                    {
                        tree[node][b] = tree.Count;
                        tree.Add(new int[] { 0, 0 });
                    }
                    node = tree[node][b];
                }
                tree[node][0] = -(i + 1);
            }

            return tree.ToArray();
        }
    }
}
