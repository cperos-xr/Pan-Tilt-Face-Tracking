using UnityEngine;
using UnityEngine.UI;

// Simple test UI wiring for Pan/Tilt controller.
public class PanTiltTestUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PanTiltController controller;

    [Header("Buttons")]
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button sweepXButton;
    [SerializeField] private Button sweepYButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button clearQueueButton;
    [SerializeField] private Button flipXButton;
    [SerializeField] private Button flipYButton;
    [SerializeField] private Button centerButton;

    [Header("Tuning")]
    [SerializeField] private float stepDegrees = 10f;
    [SerializeField] private float sweepDuration = 5f;
    [SerializeField] private float sweepDwell = 0.1f;

    private void Awake()
    {
        Bind(upButton,    () => controller?.Adjust("y", 0f,  -stepDegrees));
        Bind(downButton,  () => controller?.Adjust("y", 0f, stepDegrees));
        Bind(leftButton,  () => controller?.Adjust("x", -stepDegrees, 0f));
        Bind(rightButton, () => controller?.Adjust("x",  stepDegrees, 0f));
        Bind(sweepXButton, () => controller?.Sweep("x", -90f, 90f, sweepDuration, 3, sweepDwell));
        Bind(sweepYButton, () => controller?.Sweep("y", -90f, 90f, sweepDuration, 3, sweepDwell));
        Bind(stopButton, () => controller?.StopAll());
        Bind(clearQueueButton, () => controller?.ClearQueue());
        Bind(flipXButton, () => controller?.ToggleInvertX());
        Bind(flipYButton, () => controller?.ToggleInvertY());
        Bind(centerButton, () => controller?.Center());
    }

    private void Bind(Button btn, System.Action action)
    {
        if (btn == null || action == null) return;
        btn.onClick.AddListener(() => action());
    }
}
