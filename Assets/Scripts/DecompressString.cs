using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecompressString : MonoBehaviour
{
    [Header("Decompression Strings")]
    [TextArea(5, 10)]
    public string compressedText = "";
    [TextArea(5, 10)]
    public string decompressedText = "";


    [ContextMenu("Decompress Text")]
    public void DecompressText()
    {
        if (!string.IsNullOrEmpty(compressedText))
        {
            decompressedText = StringCompressor.DecompressFromBase64(compressedText);
            Debug.Log("Decompressed Text: " + decompressedText);
        }
        else
        {
            Debug.LogWarning("No compressed text to decompress.");
        }
    }

    public void CompressText()
    {
        if (!string.IsNullOrEmpty(decompressedText))
        {
            compressedText = StringCompressor.CompressToBase64(decompressedText);
            Debug.Log("Compressed Text: " + compressedText);
        }
        else
        {
            Debug.LogWarning("No decompressed text to compress.");
        }
    }
}
