using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class StringCompressor
{
    /// <summary>
    /// Compresses a string to a base64-encoded GZip byte array.
    /// </summary>
    public static string CompressToBase64(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var bytes = Encoding.UTF8.GetBytes(input);
        using (var ms = new MemoryStream())
        {
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(ms.ToArray());
        }
    }

    /// <summary>
    /// Decompresses a base64-encoded GZip byte array to a string.
    /// </summary>
    public static string DecompressFromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return "";
        var bytes = Convert.FromBase64String(base64);
        using (var ms = new MemoryStream(bytes))
        using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
        using (var outStream = new MemoryStream())
        {
            gzip.CopyTo(outStream);
            return Encoding.UTF8.GetString(outStream.ToArray());
        }
    }
}
