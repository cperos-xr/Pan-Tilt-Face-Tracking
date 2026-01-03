
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ZXing;
using ZXing.QrCode;

public static class QRCodeUtil
{
    // Internal class for chunk metadata
    private class QRChunk
    {
        public string id;
        public QRCodeType type;
        public int index;
        public int total;
        public string data;

        public override string ToString()
        {
            // Format: id|type|index|total|data
            return $"{id}|{type}|{index}|{total}|{data}";
        }

        public static QRChunk Parse(string chunkStr)
        {
            var parts = chunkStr.Split('|');
            if (parts.Length < 5) return null;
            QRCodeType parsedType = QRCodeType.sdp;
            Enum.TryParse(parts[1], out parsedType);
            return new QRChunk
            {
                id = parts[0],
                type = parsedType,
                index = int.Parse(parts[2]),
                total = int.Parse(parts[3]),
                data = string.Join("|", parts, 4, parts.Length - 4)
            };
        }
    }

    /// <summary>
    /// Generates a QR code Texture2D from the given string using ZXing.Net.
    /// </summary>
    public static Texture2D GenerateQRCode(string text, int size = 512)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = size,
                Width = size,
                Margin = 1,
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.Q
            }
        };
        var color32 = writer.Write(text);
        // ZXing always returns a square image, but get actual size from array length
        int qrWidth = writer.Options.Width;
        int qrHeight = writer.Options.Height;
        if (color32.Length != qrWidth * qrHeight)
        {
            // Try to infer actual size if ZXing returns a different size
            int inferred = (int)Math.Sqrt(color32.Length);
            if (inferred * inferred == color32.Length)
            {
                qrWidth = qrHeight = inferred;
            }
            else
            {
                throw new Exception($"ZXing returned unexpected Color32 array length: {color32.Length}");
            }
        }
        var tex = new Texture2D(qrWidth, qrHeight, TextureFormat.RGBA32, false);
        tex.SetPixels32(color32);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Decodes a QR code from a Texture2D using ZXing.Net.
    /// </summary>
    public static string DecodeQRCode(Texture2D tex)
    {
        if (tex == null) return null;
        var reader = new BarcodeReader();
        try
        {
            var result = reader.Decode(tex.GetPixels32(), tex.width, tex.height);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Splits a string into QR-sized chunks with metadata for multi-QR workflows.
    /// </summary>
    public static List<string> SplitToChunks(string input, int maxDataLen)
    {
        return SplitToChunksWithType(input, maxDataLen, QRCodeType.sdp);
    }

    public static List<string> SplitToChunksWithType(string input, int maxDataLen, QRCodeType type)
    {
        var id = Guid.NewGuid().ToString("N");
        var chunks = new List<string>();
        int total = (input.Length + maxDataLen - 1) / maxDataLen;
        for (int i = 0; i < total; i++)
        {
            int start = i * maxDataLen;
            int len = Math.Min(maxDataLen, input.Length - start);
            var chunk = new QRChunk
            {
                id = id,
                type = type,
                index = i,
                total = total,
                data = input.Substring(start, len)
            };
            chunks.Add(chunk.ToString());
        }
        return chunks;
    }

    /// <summary>
    /// Combines a list of chunk strings into the original string (returns null if incomplete).
    /// </summary>
    public static string CombineChunks(List<string> chunkStrs)
    {
        var dict = new Dictionary<int, QRChunk>();
        string id = null;
        int total = -1;
        foreach (var s in chunkStrs)
        {
            var chunk = QRChunk.Parse(s);
            if (chunk == null) continue;
            if (id == null) id = chunk.id;
            if (chunk.id != id) continue; // skip mismatched batches
            dict[chunk.index] = chunk;
            total = chunk.total;
        }
        if (dict.Count != total) return null; // not all chunks present
        var sb = new StringBuilder();
        for (int i = 0; i < total; i++)
        {
            if (!dict.ContainsKey(i)) return null;
            sb.Append(dict[i].data);
        }
        return sb.ToString();
    }

    /// <summary>
    /// One-call: Split a long string into a list of QR code Texture2Ds, each encoding a chunk.
    /// </summary>
    public static List<Texture2D> EncodeStringToQRChunks(string input, int maxDataLen, int qrSize = 512)
    {
        var chunkStrings = SplitToChunks(input, maxDataLen);
        var qrList = new List<Texture2D>(chunkStrings.Count);
        foreach (var chunk in chunkStrings)
        {
            var tex = GenerateQRCode(chunk, qrSize);
            qrList.Add(tex);
        }
        return qrList;
    }

    /// <summary>
    /// One-call: Given a list of decoded QR chunk strings, combine and return the original string (or null if incomplete).
    /// </summary>
    public static string DecodeQRChunksToString(List<string> decodedChunks)
    {
        return CombineChunks(decodedChunks);
    }
}


