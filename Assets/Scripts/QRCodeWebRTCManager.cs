using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public enum QRCodeType
{
    sdp,
    ice
}

[Serializable]
public class QRCode
{
    public string id;

    public ConnectionData connectionData;
    //public QRCodeType Type;
    public int index;
    public int total;
    //public string Data;
}

[Serializable]
public class ConnectionData
{
    public QRCodeType type;
    public string data;
}

public class QRCodeWebRTCManager : MonoBehaviour
{
    public GameObject qrCodePanel;
    public RawImage qrPreview;

    public TextMeshProUGUI qrIdText;

    public Button showNextChunkButton;
    public Button showPreviousChunkButton;

    //private List<Texture2D> qrChunksPreview; // For multi-QR chunk preview

    private List<Texture2D> qrSdpChunksPreview; // For multi-QR chunk preview
    private List<Texture2D> qrIceChunksPreview; // For multi-QR chunk preview

    //private int currentChunkIndex = 0;

    private int currentSdpChunkIndex = 0;
    private int currentIceChunkIndex = 0;

    private int maxChunkDataLen = 800; // Tune for QR capacity
    //private int totalChunks = 1;
    private int totalSdpChunks = 1;
    private int totalIceChunks = 1;

    [TextArea(5, 10)]
    public string SdpData = "";
    [TextArea(5, 10)]
    public string IceData = "";

    [TextArea(5, 10)]
    public string compressedSdpData = "";
    [TextArea(5, 10)]
    public string compressedIceData = "";

    private QRCodeType qrCodeType = QRCodeType.sdp;
    //private bool qrCodeType = true;
    
    private void OnEnable()
    {
        SimpleWebRTCPair.SdpCreated += OnSdpCreated;
        SimpleWebRTCPair.IceCreated += OnIceCreated;
        SimpleWebRTCPair.ReadSdp += OnReadSdp;
        //assign button listeners
        if (showNextChunkButton != null)
        {
            showNextChunkButton.onClick.AddListener(ShowNextQRChunk);
        }

        if (showPreviousChunkButton != null)
        {
            showPreviousChunkButton.onClick.AddListener(ShowPreviousQRChunk);
        }
        qrCodePanel.SetActive(false);
    }

    private void OnDisable()
    {
        SimpleWebRTCPair.SdpCreated -= OnSdpCreated;
        SimpleWebRTCPair.IceCreated -= OnIceCreated;
        SimpleWebRTCPair.ReadSdp -= OnReadSdp;

        //remove button listeners
        if (showNextChunkButton != null)
        {
            showNextChunkButton.onClick.RemoveListener(ShowNextQRChunk);
        }
        if (showPreviousChunkButton != null)
        {
            showPreviousChunkButton.onClick.RemoveListener(ShowPreviousQRChunk);
        }
    }

    private void OnReadSdp()
    {
        StartCoroutine(ScanQRChunksCoroutine());
    }

    private void OnIceCreated(string desc)
    {
        Debug.Log("ICE Created: " + desc);
        compressedIceData = StringCompressor.CompressToBase64(desc);
        totalIceChunks = 1;
        if (compressedIceData.Length > 1000)
        {
            totalIceChunks = Mathf.CeilToInt((float)compressedIceData.Length / maxChunkDataLen);
        }
        // Convert ICE to QR code chunks
        qrIceChunksPreview = QRCodeUtil.EncodeStringToQRChunks(compressedIceData, maxChunkDataLen, totalIceChunks);
        currentIceChunkIndex = 0;
        totalIceChunks = qrIceChunksPreview.Count;
    }

    private void OnSdpCreated(string desc)
    {
        Debug.Log("SDP Created: " + desc);
        string compressedSdpData = StringCompressor.CompressToBase64(desc);
        this.compressedSdpData = compressedSdpData;
        totalSdpChunks = 1;
        if (compressedSdpData.Length > 1000)
        {
            totalSdpChunks = Mathf.CeilToInt((float)desc.Length / maxChunkDataLen);
        }
        // Convert SDP to QR code chunks
        qrSdpChunksPreview = QRCodeUtil.EncodeStringToQRChunks(compressedSdpData, maxChunkDataLen, totalSdpChunks);
        currentSdpChunkIndex = 0;
        totalSdpChunks = qrSdpChunksPreview.Count;
    }

    public void ShowICEQR()
    {
        qrCodeType = QRCodeType.ice;
        qrCodePanel.SetActive(true);
        currentIceChunkIndex = 0;
        qrPreview.texture = qrIceChunksPreview[currentIceChunkIndex];
        qrIdText.text = "ICE QR Chunk: "+ (currentIceChunkIndex + 1) + " / " + totalIceChunks;
    }

    public void ShowSDPQR()
    {
        qrCodeType = QRCodeType.sdp;
        qrCodePanel.SetActive(true);
        currentSdpChunkIndex = 0;
        qrPreview.texture = qrSdpChunksPreview[currentSdpChunkIndex];
        qrIdText.text = "SDP QR Chunk: "+ (currentSdpChunkIndex + 1) + " / " + totalSdpChunks;
    }

    public void closeQRCodePanel()
    {
        qrCodePanel.SetActive(false);
        currentSdpChunkIndex = 0;
        currentIceChunkIndex = 0;
        qrPreview.texture = null;
    }

    private void ShowPreviousQRChunk()
    {
        Debug.Log("ShowPreviousQRChunk called");
        if(qrCodeType == QRCodeType.sdp)
        {
            currentSdpChunkIndex--;
            if (currentSdpChunkIndex < 0)
            {
                currentSdpChunkIndex = totalSdpChunks - 1;
            }
            // update tmp text
            qrIdText.text = "SDP QR Chunk: "+ (currentSdpChunkIndex + 1) + " / " + totalSdpChunks;
            qrPreview.texture = qrSdpChunksPreview[currentSdpChunkIndex];

        }
        else
        {
            currentIceChunkIndex--;
            if (currentIceChunkIndex < 0)
            {
                currentIceChunkIndex = totalIceChunks - 1;
            }
            // update tmp text
            qrIdText.text = "ICE QR Chunk: "+ (currentIceChunkIndex + 1) + " / " + totalIceChunks;
            qrPreview.texture = qrIceChunksPreview[currentIceChunkIndex];
        }


    }

    private void ShowNextQRChunk()
    {
        Debug.Log("ShowNextQRChunk called");
        if(qrCodeType == QRCodeType.sdp)
        {
            currentSdpChunkIndex++;
            if (currentSdpChunkIndex >= totalSdpChunks)
            {
                currentSdpChunkIndex = 0;
            }
            qrIdText.text = "SDP QR Chunk: "+  (currentSdpChunkIndex + 1)  + " / " + totalSdpChunks;
            qrPreview.texture = qrSdpChunksPreview[currentSdpChunkIndex];
        }
        else
        {   
            currentIceChunkIndex++;
            if (currentIceChunkIndex >= totalIceChunks)
            {
                currentIceChunkIndex = 0;
            }
            qrIdText.text = "ICE QR Chunk: "+ (currentIceChunkIndex + 1) + " / " + totalIceChunks;
            qrPreview.texture = qrIceChunksPreview[currentIceChunkIndex];
        }
    }

    public void ReadWebRTCFromQRChunks()
    {
        // Start scanning coroutine
        StartCoroutine(ScanQRChunksCoroutine());
    }

    private IEnumerator<YieldInstruction> ScanQRChunksCoroutine()
    {
        // Use the first available camera
        WebCamTexture webcamTexture = new WebCamTexture();
        webcamTexture.Play();
        yield return new WaitForSeconds(0.5f); // Let camera warm up


        HashSet<string> foundChunks = new HashSet<string>();
        Dictionary<int, string> chunkTypeByIndex = new Dictionary<int, string>();
        float scanTimeout = 15f; // seconds
        float startTime = Time.time;
        bool done = false;

        qrIdText.text = "Scanning for QR chunks...";
        qrCodePanel.SetActive(true);
        qrPreview.texture = webcamTexture;

        int lastFoundIndex = -1;
        string lastChunkType = "";
        int total = -1;

        while (!done && (Time.time - startTime) < scanTimeout)
        {
            // Get camera frame and correct orientation
            int w = webcamTexture.width;
            int h = webcamTexture.height;
            if (w < 16 || h < 16) { yield return null; continue; } // Wait for valid frame

            Color32[] pixels = webcamTexture.GetPixels32();
            Texture2D frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            frame.SetPixels32(pixels);
            frame.Apply();

            // Handle rotation
            int rot = webcamTexture.videoRotationAngle;
            bool mirrored = webcamTexture.videoVerticallyMirrored;
            Texture2D corrected = frame;
            if (rot != 0 || mirrored)
            {
                int correctedW = w;
                int correctedH = h;
                if (rot == 90 || rot == 270)
                {
                    correctedW = h;
                    correctedH = w;
                }
                corrected = new Texture2D(correctedW, correctedH, TextureFormat.RGBA32, false);
                Color32[] src = frame.GetPixels32();
                Color32[] dst = new Color32[correctedW * correctedH];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int srcIdx = y * w + x;
                        int tx = x, ty = y;
                        switch (rot)
                        {
                            case 90:
                                tx = y;
                                ty = w - x - 1;
                                break;
                            case 180:
                                tx = w - x - 1;
                                ty = h - y - 1;
                                break;
                            case 270:
                                tx = h - y - 1;
                                ty = x;
                                break;
                        }
                        // Apply mirroring (after rotation)
                        if (rot == 90 || rot == 270)
                        {
                            // tx,ty are in correctedW x correctedH
                            if (mirrored)
                                ty = correctedH - ty - 1;
                            int dstIdx = ty * correctedW + tx;
                            if (dstIdx >= 0 && dstIdx < dst.Length && srcIdx < src.Length)
                                dst[dstIdx] = src[srcIdx];
                        }
                        else
                        {
                            if (mirrored)
                                ty = correctedH - ty - 1;
                            int dstIdx = ty * correctedW + tx;
                            if (dstIdx >= 0 && dstIdx < dst.Length && srcIdx < src.Length)
                                dst[dstIdx] = src[srcIdx];
                        }
                    }
                }
                corrected.SetPixels32(dst);
                corrected.Apply();
                Destroy(frame);
            }

            // Crop to square (center)
            int minDim = Mathf.Min(corrected.width, corrected.height);
            int cropX = (corrected.width - minDim) / 2;
            int cropY = (corrected.height - minDim) / 2;
            Texture2D square = new Texture2D(minDim, minDim, TextureFormat.RGBA32, false);
            square.SetPixels(corrected.GetPixels(cropX, cropY, minDim, minDim));
            square.Apply();
            if (corrected != frame) Destroy(corrected);

            string decoded = QRCodeUtil.DecodeQRCode(square);
            int chunkIndex = -1;
            string chunkType = "Chunk";
            try
            {
                var parts = decoded?.Split('|');
                if (parts != null && parts.Length >= 4)
                {
                    chunkIndex = int.Parse(parts[1]);
                    total = int.Parse(parts[2]);
                    // Heuristic: if data decompresses to SDP, call it SDP, else ICE
                    string data = string.Join("|", parts, 3, parts.Length - 3);
                    string uncompressed = null;
                    try { uncompressed = StringCompressor.DecompressFromBase64(data); } catch { }
                    if (!string.IsNullOrEmpty(uncompressed) && uncompressed.StartsWith("v=0"))
                        chunkType = "SDP";
                    else
                        chunkType = "ICE";
                    chunkTypeByIndex[chunkIndex] = chunkType;
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(decoded) && !foundChunks.Contains(decoded))
            {
                foundChunks.Add(decoded);
                lastFoundIndex = chunkIndex;
                lastChunkType = chunkType;
                qrIdText.text = $"{chunkType} chunk {chunkIndex + 1} found. Scan {chunkType} chunk {foundChunks.Count + 1}...";
            }
            else if (total > 0)
            {
                qrIdText.text = $"Scan {lastChunkType} chunk {foundChunks.Count + 1} of {total}...";
            }

            // If we have all chunks, stop
            if (total > 0 && foundChunks.Count >= total)
            {
                done = true;
                qrIdText.text = $"All {lastChunkType} chunks found!";
            }

            Destroy(square);
            yield return new WaitForSeconds(0.2f);
        }

        webcamTexture.Stop();

        // Try to reassemble
        if (foundChunks.Count > 0)
        {
            var chunkList = new List<string>(foundChunks);
            string combined = QRCodeUtil.DecodeQRChunksToString(chunkList);
            if (!string.IsNullOrEmpty(combined))
            {
                // Try to decompress to SDP or ICE
                string uncompressed = StringCompressor.DecompressFromBase64(combined);
                qrIdText.text = "Decoded!";
                // Heuristic: if it starts with v=0, it's SDP; else ICE
                if (uncompressed.StartsWith("v=0"))
                {
                    SdpData = uncompressed;
                    Debug.Log("Decoded SDP from QR");
                }
                else
                {
                    IceData = uncompressed;
                    Debug.Log("Decoded ICE from QR");
                }
            }
            else
            {
                qrIdText.text = "Failed to assemble all chunks.";
                Debug.LogWarning("Failed to decode/assemble from QR chunks.");
            }
        }
        else
        {
            qrIdText.text = "No QR chunks found.";
        }
    }
}
