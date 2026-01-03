using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZXing;
using System;
using System.Collections.Generic;

public class QRCodeScanner : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Use front-facing camera if true, back camera if false.")]
    public bool useFrontFacing = false;

    [Header("UI Elements")]
    public RawImage cameraPreview;
    public TMP_Text statusText;
    public TMP_Text resultText;
    public Button scanButton;
    public Button addScanButton;
    public Button decompressButton;
    private List<QRCode> QRCodes = new List<QRCode>();
    private string qrText = "";

    public delegate void OnReadQRCode(QRCode decodedText);
    public static event OnReadQRCode QRCodeRead;

    private WebCamTexture _webCamTexture;
    private bool _isScanning = false;
    private IBarcodeReader _barcodeReader;

    void Awake()
    {
        _barcodeReader = new BarcodeReader { AutoRotate = true };
        _barcodeReader.Options.TryInverted = true;
        if (scanButton != null)
            scanButton.onClick.AddListener(StartScanning);
        if (addScanButton != null)
            addScanButton.onClick.AddListener(AddScan);

        if (decompressButton != null)
            decompressButton.onClick.AddListener(DecompressText);
    }

    public void CopyResultTextToClipboard()
    {
        if (!string.IsNullOrEmpty(qrText))
        {
            string fullText = "";
            foreach (QRCode qr in QRCodes)
            {
                fullText += qr.Data;
            }
            GUIUtility.systemCopyBuffer = fullText;
            Debug.Log("QR Text copied to clipboard.");
        }
    }

    private void DecompressText()
    {
        string decompressedText = "";
        foreach (QRCode qr in QRCodes)
        {
            decompressedText += qr.Data;
        }
        decompressedText = StringCompressor.DecompressFromBase64(decompressedText);
        if(resultText != null)
        {
            qrText = decompressedText;
            resultText.text = decompressedText;
            Debug.Log("Decompressed QR Text: " + decompressedText);
        }
    }

    public QRCode GetCurrentQRCodeFromText(string text)
    {
        // Example format: id|index|total|data
        if (string.IsNullOrEmpty(text))
            return null;

        var parts = text.Split('|');
        if (parts.Length < 4)
            return null;

        QRCode qr = new QRCode();
        qr.Id = parts[0];
        int idx, tot;
        if (int.TryParse(parts[1], out idx))
            qr.Index = idx;
        if (int.TryParse(parts[2], out tot))
            qr.Total = tot;
        qr.Data = parts[3];
        return qr;
    }

    public void StartScanning()
    {
        if (_isScanning) return;
        qrText = "";
        QRCodes.Clear();
        statusText.text = "Starting camera...";
        StartCoroutine(StartCamera());
    }

    public void AddScan()
    {
        if (_isScanning) return;
        statusText.text = "Starting camera again...";
        StartCoroutine(StartCamera());
    }

    private System.Collections.IEnumerator StartCamera()
    {
        if (_webCamTexture != null)
        {
            _webCamTexture.Stop();
            Destroy(_webCamTexture);
        }

        var devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            statusText.text = "No camera found.";
            yield break;
        }

        int camIdx = 0;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing == useFrontFacing)
            {
                camIdx = i;
                break;
            }
        }

        _webCamTexture = new WebCamTexture(devices[camIdx].name);
        cameraPreview.texture = _webCamTexture;
        cameraPreview.gameObject.SetActive(true);
        _webCamTexture.Play();
        statusText.text = "Camera started. Scanning...";
        _isScanning = true;
        yield return null;
        StartCoroutine(ScanRoutine());
    }

    private System.Collections.IEnumerator ScanRoutine()
    {
        while (_isScanning && _webCamTexture != null && _webCamTexture.isPlaying)
        {
            try
            {
                Texture2D snap = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
                snap.SetPixels32(_webCamTexture.GetPixels32());
                snap.Apply();
                Result result = _barcodeReader.Decode(snap.GetPixels32(), snap.width, snap.height);
                Destroy(snap);
                if (result != null)
                {

                    _isScanning = false;
                    _webCamTexture.Stop();
                    
                    if(resultText != null)
                    {
                        qrText += result.Text;
                        resultText.text = qrText;
                        QRCode qRCode = GetCurrentQRCodeFromText(result.Text);
                        if (qRCode != null)
                        {
                            QRCodes.Add(qRCode);
                            Debug.Log($"Scanned QR Code Chunk: ID={qRCode.Id}, Index={qRCode.Index}, Total={qRCode.Total}");
                            statusText.text = $"QR Code {qRCode.Id} found! Chunk {qRCode.Index + 1} of {qRCode.Total} added.";
                            QRCodeRead?.Invoke(qRCode);
                            Debug.Log($"QR Code {qRCode.Id} decoded: {qRCode.Data}");
                        }
                        else
                        {
                            statusText.text = "Scanned QR Code but failed to parse chunk info.";
                            Debug.Log("Scanned QR Code but failed to parse chunk info.");
                        }
                    }
                    yield break;
                }
                else
                {
                    statusText.text = "Scanning...";
                }
            }
            catch (Exception e)
            {
                statusText.text = $"Error: {e.Message}";
            }
            yield return new WaitForSeconds(0.2f);
        }
        statusText.text = "Stopped scanning.";
    }

    public void StopScanning()
    {
        _isScanning = false;
        if (_webCamTexture != null)
        {
            _webCamTexture.Stop();
            cameraPreview.texture = null;
        }
        statusText.text = "Stopped.";
    }

    void OnDestroy()
    {
        StopScanning();
        if (scanButton != null)
            scanButton.onClick.RemoveListener(StartScanning);
    }
}
