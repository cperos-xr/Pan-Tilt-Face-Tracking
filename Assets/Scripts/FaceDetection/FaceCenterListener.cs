using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnityExample.DnnModel;

public class FaceCenterListener : MonoBehaviour
{
    [SerializeField] private PanTiltController controller;
    // Thresholds in pixels (adjust as needed)
    public float thresholdX = 35f;
    public float thresholdY = 35f;

    [Tooltip("Base degrees to nudge per correction step.")]
    public float stepDegrees = 5f;

    [Tooltip("Clamp step size to avoid over-correction.")]
    public float maxStepDegrees = 10f;

    [Tooltip("Minimum step when correction triggers.")]
    public float minStepDegrees = 1.5f;

    [Tooltip("Scale step by how far past the threshold the face is.")]
    public bool scaleStepByError = true;

    [Tooltip("Allow diagonal corrections (move both axes in one command). If false, prioritizes the larger offset axis.")]
    public bool allowDiagonal = true;

    [Header("Direction UI (optional)")]
    [SerializeField] private Image leftIndicator;
    [SerializeField] private Image rightIndicator;
    [SerializeField] private Image upIndicator;
    [SerializeField] private Image downIndicator;
    [SerializeField] private Color activeColor = new Color(1f, 0.35f, 0.2f, 0.85f);
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.15f);

    void OnEnable()
    {
        YuNetV2FaceDetector.OnFaceOffsetVector2Sent += HandleFaceOffset;
    }

    void OnDisable()
    {
        YuNetV2FaceDetector.OnFaceOffsetVector2Sent -= HandleFaceOffset;
    }

    private void HandleFaceOffset(Vector2 offset)
    {
        Debug.Log($"Face offset from center: X={offset.x:F1}, Y={offset.y:F1}");

        // Explicit directional cases
        bool moveLeft = offset.x < -thresholdX;
        bool moveRight = offset.x > thresholdX;
        bool moveUp = offset.y < -thresholdY;
        bool moveDown = offset.y > thresholdY;

        float dx = 0f;
        float dy = 0f;

        if (allowDiagonal)
        {
            float stepX = ComputeStepMagnitude(offset.x, thresholdX);
            float stepY = ComputeStepMagnitude(offset.y, thresholdY);

            if (moveLeft) dx -= stepX;
            if (moveRight) dx += stepX;
            if (moveUp) dy -= stepY;
            if (moveDown) dy += stepY;
        }
        else
        {
            // Move on the dominant axis only
            float absX = Mathf.Abs(offset.x);
            float absY = Mathf.Abs(offset.y);
            if (absX > absY)
            {
                float stepX = ComputeStepMagnitude(offset.x, thresholdX);
                if (moveLeft) dx = -stepX;
                else if (moveRight) dx = stepX;
            }
            else
            {
                float stepY = ComputeStepMagnitude(offset.y, thresholdY);
                if (moveUp) dy = -stepY;
                else if (moveDown) dy = stepY;
            }
        }

        if (dx != 0f || dy != 0f)
        {
            string dir = $"dx={dx:F1}, dy={dy:F1}";
            Debug.LogWarning($"PanTilt adjust | {dir} | offset X={offset.x:F1}, Y={offset.y:F1}");
            controller?.Adjust("xy", dx, dy);
        }

        // Update UI indicators to show commanded direction
        SetIndicator(leftIndicator, moveLeft);
        SetIndicator(rightIndicator, moveRight);
        SetIndicator(upIndicator, moveUp);
        SetIndicator(downIndicator, moveDown);
    }

    private void SetIndicator(Image img, bool active)
    {
        if (img == null) return;
        img.color = active ? activeColor : idleColor;
    }

    private float ComputeStepMagnitude(float offset, float threshold)
    {
        float over = Mathf.Abs(offset) - threshold;
        if (over <= 0f) return 0f;

        if (!scaleStepByError)
            return Mathf.Clamp(stepDegrees, minStepDegrees, maxStepDegrees);

        float ratio = Mathf.Clamp01(over / Mathf.Max(threshold, 0.0001f));
        float step = Mathf.Lerp(minStepDegrees, stepDegrees, ratio);
        return Mathf.Clamp(step, minStepDegrees, maxStepDegrees);
    }
}
