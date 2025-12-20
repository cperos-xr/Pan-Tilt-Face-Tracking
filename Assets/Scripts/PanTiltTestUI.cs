using UnityEngine;
using UnityEngine.UI;

// Simple test UI wiring for Pan/Tilt controller.

public class PanTiltTestUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PanTiltController controller;
    [SerializeField] private ConnectionTypeHandler connectionHandler;

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
        Bind(upButton,    () => Send(controller?.GetAdjustCommand("y", 0f,  -stepDegrees)));
        Bind(downButton,  () => Send(controller?.GetAdjustCommand("y", 0f, stepDegrees)));
        Bind(leftButton,  () => Send(controller?.GetAdjustCommand("x", -stepDegrees, 0f)));
        Bind(rightButton, () => Send(controller?.GetAdjustCommand("x",  stepDegrees, 0f)));
        Bind(sweepXButton, () => Send(controller?.GetSweepCommand("x", -90f, 90f, sweepDuration, 3, sweepDwell)));
        Bind(sweepYButton, () => Send(controller?.GetSweepCommand("y", -90f, 90f, sweepDuration, 3, sweepDwell)));
        Bind(stopButton, () => Send(controller?.GetStopAllCommand()));
        Bind(clearQueueButton, () => Send(controller?.GetClearQueueCommand()));
        Bind(flipXButton, () => Send(controller?.GetToggleInvertXCommand()));
        Bind(flipYButton, () => Send(controller?.GetToggleInvertYCommand()));
        Bind(centerButton, () => Send(controller?.GetCenterCommand()));
    }

    private void Send(string json)
    {
        if (!string.IsNullOrEmpty(json) && connectionHandler != null)
            connectionHandler.SendPanTiltCommand(json);
    }

    private void Bind(Button btn, System.Action action)
    {
        if (btn == null || action == null) return;
        btn.onClick.AddListener(() => action());
    }
}
