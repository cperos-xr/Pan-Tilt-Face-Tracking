using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public enum QRCodeType
{
    SDP,
    ICE
}

[Serializable]
public class QRCode
{
    public string Id;
    public int Index;
    public int Total;
    public string Data;
}

public class QRCodeWebRTCManager : MonoBehaviour
{
    public GameObject qrCodePanel;
    public RawImage qrPreview;

    public TextMeshProUGUI QrIdText;

    public Button showNextChunkButton;
    public Button showPreviousChunkButton;

    //private List<Texture2D> qrChunksPreview; // For multi-QR chunk preview

    private List<Texture2D> qrSDPChunksPreview; // For multi-QR chunk preview
    private List<Texture2D> qrICEChunksPreview; // For multi-QR chunk preview

    //private int currentChunkIndex = 0;

    private int currentSDPChunkIndex = 0;
    private int currentICEChunkIndex = 0;

    private int maxChunkDataLen = 800; // Tune for QR capacity
    //private int totalChunks = 1;
    private int totalSDPChunks = 1;
    private int totalICEChunks = 1;

    [TextArea(5, 10)]
    public string SDPData = "";
    [TextArea(5, 10)]
    public string ICEData = "";

    [TextArea(5, 10)]
    public string compressedSDPData = "";

    private QRCodeType qrCodeType = QRCodeType.SDP;
    //private bool qrCodeType = true;
    
    private void OnEnable()
    {
        SimpleWebRTCPair.SDPCreated += OnSDPCreated;
        SimpleWebRTCPair.ICECreated += OnICECreated;
        SimpleWebRTCPair.ReadSDP += OnReadSDP;
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
        SimpleWebRTCPair.SDPCreated -= OnSDPCreated;
        SimpleWebRTCPair.ICECreated -= OnICECreated;
        SimpleWebRTCPair.ReadSDP -= OnReadSDP;

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

    private void OnReadSDP()
    {
        StartCoroutine(ScanQRChunksCoroutine());
    }

    private void OnICECreated(string desc)
    {
        Debug.Log("ICE Created: " + desc);
        ICEData = desc;
        totalICEChunks = 1;
        if (ICEData.Length > 1000)
        {
            totalICEChunks = Mathf.CeilToInt((float)ICEData.Length / maxChunkDataLen);
        }
        // Convert ICE to QR code chunks
        qrICEChunksPreview = QRCodeUtil.EncodeStringToQRChunks(ICEData, maxChunkDataLen, totalICEChunks);
        currentICEChunkIndex = 0;
        totalICEChunks = qrICEChunksPreview.Count;
    }

    private void OnSDPCreated(string desc)
    {
        Debug.Log("SDP Created: " + desc);
        SDPData = desc;
        string compressedText = StringCompressor.CompressToBase64(SDPData);
        compressedSDPData = compressedText;
        totalSDPChunks = 1;
        if (compressedText.Length > 1000)
        {
            totalSDPChunks = Mathf.CeilToInt((float)desc.Length / maxChunkDataLen);
        }
        
        // Convert SDP to QR code chunks
        qrSDPChunksPreview = QRCodeUtil.EncodeStringToQRChunks(compressedText, maxChunkDataLen, totalSDPChunks);
        
        currentSDPChunkIndex = 0;
        totalSDPChunks = qrSDPChunksPreview.Count;
    }

    public void ShowICEQR()
    {
        qrCodeType = QRCodeType.ICE;
        qrCodePanel.SetActive(true);
        currentICEChunkIndex = 0;
        qrPreview.texture = qrICEChunksPreview[currentICEChunkIndex];
        QrIdText.text = "ICE QR Chunk: "+ (currentICEChunkIndex + 1) + " / " + totalICEChunks;
    }

    public void ShowSDPQR()
    {
        qrCodeType = QRCodeType.SDP;
        qrCodePanel.SetActive(true);
        currentSDPChunkIndex = 0;
        qrPreview.texture = qrSDPChunksPreview[currentSDPChunkIndex];
        QrIdText.text = "SDP QR Chunk: "+ (currentSDPChunkIndex + 1) + " / " + totalSDPChunks;
    }

    public void closeQRCodePanel()
    {
        qrCodePanel.SetActive(false);
        currentSDPChunkIndex = 0;
        currentICEChunkIndex = 0;
        qrPreview.texture = null;
    }

    private void ShowPreviousQRChunk()
    {
        Debug.Log("ShowPreviousQRChunk called");
        if(qrCodeType == QRCodeType.SDP)
        {
            currentSDPChunkIndex--;
            if (currentSDPChunkIndex < 0)
            {
                currentSDPChunkIndex = totalSDPChunks - 1;
            }
            // update tmp text
            QrIdText.text = "SDP QR Chunk: "+ (currentSDPChunkIndex + 1) + " / " + totalSDPChunks;
            qrPreview.texture = qrSDPChunksPreview[currentSDPChunkIndex];

        }
        else
        {
            currentICEChunkIndex--;
            if (currentICEChunkIndex < 0)
            {
                currentICEChunkIndex = totalICEChunks - 1;
            }
            // update tmp text
            QrIdText.text = "ICE QR Chunk: "+ (currentICEChunkIndex + 1) + " / " + totalICEChunks;
            qrPreview.texture = qrICEChunksPreview[currentICEChunkIndex];
        }


    }

    private void ShowNextQRChunk()
    {
        Debug.Log("ShowNextQRChunk called");
        if(qrCodeType == QRCodeType.SDP)
        {
            currentSDPChunkIndex++;
            if (currentSDPChunkIndex >= totalSDPChunks)
            {
                currentSDPChunkIndex = 0;
            }
            QrIdText.text = "SDP QR Chunk: "+  (currentSDPChunkIndex + 1)  + " / " + totalSDPChunks;
            qrPreview.texture = qrSDPChunksPreview[currentSDPChunkIndex];
        }
        else
        {   
            currentICEChunkIndex++;
            if (currentICEChunkIndex >= totalICEChunks)
            {
                currentICEChunkIndex = 0;
            }
            QrIdText.text = "ICE QR Chunk: "+ (currentICEChunkIndex + 1) + " / " + totalICEChunks;
            qrPreview.texture = qrICEChunksPreview[currentICEChunkIndex];
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

        QrIdText.text = "Scanning for QR chunks...";
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
                QrIdText.text = $"{chunkType} chunk {chunkIndex + 1} found. Scan {chunkType} chunk {foundChunks.Count + 1}...";
            }
            else if (total > 0)
            {
                QrIdText.text = $"Scan {lastChunkType} chunk {foundChunks.Count + 1} of {total}...";
            }

            // If we have all chunks, stop
            if (total > 0 && foundChunks.Count >= total)
            {
                done = true;
                QrIdText.text = $"All {lastChunkType} chunks found!";
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
                QrIdText.text = "Decoded!";
                // Heuristic: if it starts with v=0, it's SDP; else ICE
                if (uncompressed.StartsWith("v=0"))
                {
                    SDPData = uncompressed;
                    Debug.Log("Decoded SDP from QR");
                }
                else
                {
                    ICEData = uncompressed;
                    Debug.Log("Decoded ICE from QR");
                }
            }
            else
            {
                QrIdText.text = "Failed to assemble all chunks.";
                Debug.LogWarning("Failed to decode/assemble from QR chunks.");
            }
        }
        else
        {
            QrIdText.text = "No QR chunks found.";
        }
    }
}
