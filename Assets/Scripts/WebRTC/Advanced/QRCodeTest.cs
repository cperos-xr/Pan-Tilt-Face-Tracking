using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class QRCodeTest : MonoBehaviour
{
    [Header("QR Code Test")]
    [TextArea]
    public string qrText = "Hello, QR!";
    [TextArea]
    public string compressedText = "";
    [Range(64, 2048)]
    public int qrSize = 256;
    public Texture2D qrTexture;
    public RawImage qrPreview;

    public TextMeshProUGUI currentQRDisplayedIdentifier;

    public Button showNextChunkButton;
    public Button showPreviousChunkButton;

    [Header("WebRTC Data (for QR)")]
    [TextArea(5, 10)]
    public string sdpText = ""; // SDP offer/answer
    [TextArea(5, 10)]
    public string iceText = ""; // ICE candidates (one per line or JSON)

    [Header("QR Chunks Preview")] 
    public Texture2D[] qrChunksPreview; // For multi-QR chunk preview
    [Range(100, 1000)]
    public int maxChunkDataLen = 400; // Tune for QR capacity

    private int currentChunkIndex = 0;
#if UNITY_EDITOR

    private void Awake()
    {
        SetupChunkNavigationButtons();
        UpdateQRChunkDisplay();
    }

    private void OnEnable()
    {
        SetupChunkNavigationButtons();
        UpdateQRChunkDisplay();
    }

    private void SetupChunkNavigationButtons()
    {
        if (showNextChunkButton != null)
        {
            showNextChunkButton.onClick.RemoveListener(ShowNextQRChunk);
            showNextChunkButton.onClick.AddListener(ShowNextQRChunk);
        }
        if (showPreviousChunkButton != null)
        {
            showPreviousChunkButton.onClick.RemoveListener(ShowPreviousQRChunk);
            showPreviousChunkButton.onClick.AddListener(ShowPreviousQRChunk);
        }
    }

    private void UpdateQRChunkDisplay()
    {
        if (qrChunksPreview != null && qrChunksPreview.Length > 0)
        {
            if (currentChunkIndex < 0 || currentChunkIndex >= qrChunksPreview.Length)
                currentChunkIndex = 0;
            qrPreview.texture = qrChunksPreview[currentChunkIndex];
            if (currentQRDisplayedIdentifier != null)
                currentQRDisplayedIdentifier.text = $"QR Chunk {currentChunkIndex + 1} / {qrChunksPreview.Length}";
        }
        else
        {
            if (currentQRDisplayedIdentifier != null)
                currentQRDisplayedIdentifier.text = "No QR Chunks";
        }
    }

#if UNITY_EDITOR

    [ContextMenu("Generate QR Code")]
    public void GenerateQRCode()
    {
        qrTexture = QRCodeUtil.GenerateQRCode(qrText, qrSize);
        if (qrTexture != null)
        {
            Debug.Log("QR code generated.");
            EditorUtility.SetDirty(this);
            qrPreview.texture = qrTexture;
        }
        else
        {
            Debug.LogWarning("QR code generation failed. Check library setup.");
        }
    }

    [ContextMenu("Encode SDP to QR Chunks")]
    public void EncodeSDPToQRChunks()
    {
        if (string.IsNullOrEmpty(sdpText))
        {
            Debug.LogWarning("No SDP text to encode.");
            return;
        }
        var qrList = QRCodeUtil.EncodeStringToQRChunks(sdpText, maxChunkDataLen, qrSize);
        qrChunksPreview = qrList.ToArray();
        currentChunkIndex = 0;
        UpdateQRChunkDisplay();
        if (qrChunksPreview.Length > 0)
        {
            Debug.Log($"SDP encoded into {qrChunksPreview.Length} QR chunk(s).");
            EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogWarning("Failed to encode SDP to QR chunks.");
        }
    }

    [ContextMenu("Encode ICE to QR Chunks")]
    public void EncodeICEToQRChunks()
    {
        if (string.IsNullOrEmpty(iceText))
        {
            Debug.LogWarning("No ICE text to encode.");
            return;
        }
        var qrList = QRCodeUtil.EncodeStringToQRChunks(iceText, maxChunkDataLen, qrSize);
        qrChunksPreview = qrList.ToArray();
        currentChunkIndex = 0;
        UpdateQRChunkDisplay();
        if (qrChunksPreview.Length > 0)
        {
            Debug.Log($"ICE encoded into {qrChunksPreview.Length} QR chunk(s).");
            EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogWarning("Failed to encode ICE to QR chunks.");
        }
    }

    [ContextMenu("Decode QR Chunks to SDP")] 
    public void DecodeQRChunksToSDP()
    {
        if (qrChunksPreview == null || qrChunksPreview.Length == 0)
        {
            Debug.LogWarning("No QR chunk textures to decode.");
            return;
        }
        var decodedChunks = new System.Collections.Generic.List<string>();
        foreach (var tex in qrChunksPreview)
        {
            var decoded = QRCodeUtil.DecodeQRCode(tex);
            if (!string.IsNullOrEmpty(decoded))
                decodedChunks.Add(decoded);
        }
        string combined = QRCodeUtil.DecodeQRChunksToString(decodedChunks);
        if (!string.IsNullOrEmpty(combined))
        {
            sdpText = combined;
            Debug.Log("Decoded and combined SDP from QR chunks.");
            EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogWarning("Failed to decode/assemble SDP from QR chunks.");
        }
    }

    [ContextMenu("Decode QR Chunks to ICE")]
    public void DecodeQRChunksToICE()
    {
        if (qrChunksPreview == null || qrChunksPreview.Length == 0)
        {
            Debug.LogWarning("No QR chunk textures to decode.");
            return;
        }
        var decodedChunks = new System.Collections.Generic.List<string>();
        foreach (var tex in qrChunksPreview)
        {
            var decoded = QRCodeUtil.DecodeQRCode(tex);
            if (!string.IsNullOrEmpty(decoded))
                decodedChunks.Add(decoded);
        }
        string combined = QRCodeUtil.DecodeQRChunksToString(decodedChunks);
        if (!string.IsNullOrEmpty(combined))
        {
            iceText = combined;
            Debug.Log("Decoded and combined ICE from QR chunks.");
            EditorUtility.SetDirty(this);
        }
        else
        {
            Debug.LogWarning("Failed to decode/assemble ICE from QR chunks.");
        }
    }

    [ContextMenu("Compress and Generate QR Code")]
    public void CompressAndGenerateQRCode()
    {
        compressedText = StringCompressor.CompressToBase64(qrText);
        qrTexture = QRCodeUtil.GenerateQRCode(compressedText, qrSize);
        if (qrTexture != null)
        {
            Debug.Log("Compressed and generated QR code.");
            EditorUtility.SetDirty(this);
            qrPreview.texture = qrTexture;
        }
        else
        {
            Debug.LogWarning("QR code generation failed. Check library setup.");
        }
    }

    [ContextMenu("Decode QR Code")]
    public void DecodeQRCode()
    {
        if (qrTexture == null)
        {
            Debug.LogWarning("No QR texture to decode.");
            return;
        }
        string decoded = QRCodeUtil.DecodeQRCode(qrTexture);
        Debug.Log("Decoded QR: " + (decoded ?? "<none>"));
    }

    [ContextMenu("Decode and Uncompress QR Code")]
    public void DecodeAndUncompressQRCode()
    {
        if (qrTexture == null)
        {
            Debug.LogWarning("No QR texture to decode.");
            return;
        }
        string decoded = QRCodeUtil.DecodeQRCode(qrTexture);
        if (string.IsNullOrEmpty(decoded))
        {
            Debug.LogWarning("Decoded QR is empty or null.");
            return;
        }
        string uncompressed = StringCompressor.DecompressFromBase64(decoded);
        Debug.Log("Decoded and uncompressed QR: " + (uncompressed ?? "<none>"));
    }
#endif

    public void ShowNextQRChunk()
    {
        if (qrChunksPreview == null || qrChunksPreview.Length == 0)
        {
            Debug.LogWarning("No QR chunks to show.");
            return;
        }
        currentChunkIndex = (currentChunkIndex + 1) % qrChunksPreview.Length;
        UpdateQRChunkDisplay();
        Debug.Log($"Showing QR chunk {currentChunkIndex + 1} of {qrChunksPreview.Length}.");
    }

    public void ShowPreviousQRChunk()
    {
        if (qrChunksPreview == null || qrChunksPreview.Length == 0)
        {
            Debug.LogWarning("No QR chunks to show.");
            return;
        }
        currentChunkIndex = (currentChunkIndex - 1 + qrChunksPreview.Length) % qrChunksPreview.Length;
        UpdateQRChunkDisplay();
        Debug.Log($"Showing QR chunk {currentChunkIndex + 1} of {qrChunksPreview.Length}.");
    }
#endif
}
