using UnityEngine;

using UnityEngine.UI;
using OpenCVForUnity.UnityUtils.Helper;
using TMPro;

public class CameraUIControls : MonoBehaviour
{
    [Header("UI References")]
    public Button toggleCameraButton;
    public TextMeshProUGUI toggleCameraButtonText;

    [Header("Panels and Images")]
    public GameObject regularPanel; // Panel with small RawImage and small buttons
    public Button regularMaximizeButton;

    public GameObject fullscreenPanel; // Panel with large RawImage and restore button

    public Button fullscreenRestoreButton;

    [Header("Camera Helper")]
    public MultiSource2MatHelper multiSource2MatHelper;

    private bool cameraOn = true;
    private bool isFullscreen = false;

    void Start()
    {
        if (toggleCameraButton != null)
            toggleCameraButton.onClick.AddListener(ToggleCamera);
        if (regularMaximizeButton != null)
            regularMaximizeButton.onClick.AddListener(ShowFullscreenPanel);
        if (fullscreenRestoreButton != null)
            fullscreenRestoreButton.onClick.AddListener(HideFullscreenPanel);
        ShowRegularPanel();
        UpdateButtonText();
    }

    void ToggleCamera()
    {
        if (multiSource2MatHelper == null) return;

        if (cameraOn)
        {
            multiSource2MatHelper.Pause();
            cameraOn = false;
        }
        else
        {
            multiSource2MatHelper.Play();
            cameraOn = true;
        }
        UpdateButtonText();
    }

    void UpdateButtonText()
    {
        if (toggleCameraButtonText != null)
        {
            toggleCameraButtonText.text = cameraOn ? "Camera\nOff" : "Camera\nOn";
        }
    }

    // Show fullscreen panel, hide regular
    public void ShowFullscreenPanel()
    {
        if (fullscreenPanel != null) fullscreenPanel.SetActive(true);
        if (regularPanel != null) regularPanel.SetActive(false);
        isFullscreen = true;
    }

    // Show regular panel, hide fullscreen
    public void HideFullscreenPanel()
    {
        if (fullscreenPanel != null) fullscreenPanel.SetActive(false);
        if (regularPanel != null) regularPanel.SetActive(true);
        isFullscreen = false;
    }

    // Helper to ensure only regular panel is shown at start
    private void ShowRegularPanel()
    {
        if (fullscreenPanel != null) fullscreenPanel.SetActive(false);
        if (regularPanel != null) regularPanel.SetActive(true);
        isFullscreen = false;
    }
}
