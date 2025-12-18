using UnityEngine;

// Lightweight pan/tilt controller that just emits JSON commands for X/Y moves.
// Plug in a SerialConnection and call these from any input or AI logic.
public class PanTiltController : MonoBehaviour
{
    [Header("Transport")]
    [SerializeField] private SerialConnection serial;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.5f; // seconds for absolute moves

    [Header("Axis Inversion")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    public bool IsReady => serial != null && serial.IsConnected;

    private float ApplyInvertX(float value) => invertX ? -value : value;
    private float ApplyInvertY(float value) => invertY ? -value : value;

    // Absolute move to X/Y (degrees, -90..90). Axis can be "x", "y", or "xy".
    public void Set(string axis, float x, float y, float durationSeconds = -1f)
    {
        if (!IsReady) return;
        float dur = durationSeconds > 0 ? durationSeconds : defaultDuration;
        float ix = ApplyInvertX(x);
        float iy = ApplyInvertY(y);

        string payload = axis == "xy"
            ? $"{{\"cmd\":\"set\",\"axis\":\"xy\",\"x\":{ix:F2},\"y\":{iy:F2},\"dur\":{dur:F2}}}"
            : axis == "x"
                ? $"{{\"cmd\":\"set\",\"axis\":\"x\",\"value\":{ix:F2},\"dur\":{dur:F2}}}"
                : $"{{\"cmd\":\"set\",\"axis\":\"y\",\"value\":{iy:F2},\"dur\":{dur:F2}}}";
        serial.SendString(payload);
    }

    // Relative move by delta degrees, using speed if provided.
    public void Adjust(string axis, float dx, float dy)
    {
        if (!IsReady) return;

        float adx = ApplyInvertX(dx);
        float ady = ApplyInvertY(dy);

        string payload;
        if (axis == "xy")
        {
            payload = $"{{\"cmd\":\"adjust\",\"axis\":\"xy\",\"x\":{adx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},\"y\":{ady.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}}";
        }
        else if (axis == "x")
        {
            payload = $"{{\"cmd\":\"adjust\",\"axis\":\"x\",\"value\":{adx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}}";
        }
        else // "y"
        {
            payload = $"{{\"cmd\":\"adjust\",\"axis\":\"y\",\"value\":{ady.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}}";
        }

        Debug.Log($"Adjust payload: {payload}");
        serial.SendString(payload);
    }


    public void Center(float durationSeconds = -1f)
    {
        Set("xy", 0f, 0f, durationSeconds);
    }

    public void StopAll()
    {
        if (!IsReady) return;
        serial.SendString("{\"cmd\":\"stopAll\"}");
        serial.SendString("{\"cmd\":\"qAbort\"}");
    }

    public void ClearQueue()
    {
        if (!IsReady) return;
        serial.SendString("{\"cmd\":\"qClear\"}");
    }

    // Sweep helper: axis = "x", "y", or "xy". loops=0 means continuous.
    public void Sweep(string axis, float fromDeg, float toDeg, float durationSeconds, int loops = 2, float dwellSeconds = 0.1f)
    {
        if (!IsReady) return;
        serial.SendString("{\"cmd\":\"queue\",\"mode\":\"step\"}");
        float f = fromDeg;
        float t = toDeg;
        if (axis == "x")
        {
            f = ApplyInvertX(fromDeg);
            t = ApplyInvertX(toDeg);
        }
        else if (axis == "y")
        {
            f = ApplyInvertY(fromDeg);
            t = ApplyInvertY(toDeg);
        }
        else if (axis == "xy")
        {
            f = ApplyInvertX(fromDeg);
            t = ApplyInvertY(toDeg);
        }

        string payload = $"{{\"cmd\":\"sweep\",\"axis\":\"{axis}\",\"from\":{f:F2},\"to\":{t:F2},\"dur\":{durationSeconds:F2},\"loops\":{loops},\"dwell\":{dwellSeconds:F2}}}";
        serial.SendString(payload);
    }

    // Built-in demos from firmware.
    public void Demo(int which)
    {
        if (!IsReady) return;
        if (which < 1 || which > 3) return;
        serial.SendString($"{{\"cmd\":\"demo{which}\"}}");
    }

    public void RequestExamples()  { if (IsReady) serial.SendString("{\"cmd\":\"examples\"}"); }
    public void RequestHelp()      { if (IsReady) serial.SendString("{\"cmd\":\"help\"}"); }
    public void RequestStatus()    { if (IsReady) serial.SendString("{\"cmd\":\"status\"}"); }
    public void RequestDemoList()  { if (IsReady) serial.SendString("{\"cmd\":\"demo\"}"); }

    // Toggle inversion flags and notify firmware (expects invert command support on ESP side).
    public void ToggleInvertX()
    {
        invertX = !invertX;
        if (IsReady)
            serial.SendString($"{{\"cmd\":\"invert\",\"axis\":\"x\",\"state\":{invertX.ToString().ToLowerInvariant()}}}");
    }

    public void ToggleInvertY()
    {
        invertY = !invertY;
        if (IsReady)
            serial.SendString($"{{\"cmd\":\"invert\",\"axis\":\"y\",\"state\":{invertY.ToString().ToLowerInvariant()}}}");
    }
}
