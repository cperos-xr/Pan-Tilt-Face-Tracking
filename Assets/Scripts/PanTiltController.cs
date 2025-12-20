using UnityEngine;

// Lightweight pan/tilt controller that just emits JSON commands for X/Y moves.
// Plug in a SerialConnection and call these from any input or AI logic.
public class PanTiltController : MonoBehaviour
{
    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 0.5f; // seconds for absolute moves

    [Header("Axis Inversion")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    private float ApplyInvertX(float value) => invertX ? -value : value;
    private float ApplyInvertY(float value) => invertY ? -value : value;

    // Absolute move to X/Y (degrees, -90..90). Axis can be "x", "y", or "xy".
    public string GetSetCommand(string axis, float x, float y, float durationSeconds = -1f)
    {
        float dur = durationSeconds > 0 ? durationSeconds : defaultDuration;
        float ix = ApplyInvertX(x);
        float iy = ApplyInvertY(y);

        string payload = axis == "xy"
            ? $"{{\"cmd\":\"set\",\"axis\":\"xy\",\"x\":{ix:F2},\"y\":{iy:F2},\"dur\":{dur:F2}}}"
            : axis == "x"
                ? $"{{\"cmd\":\"set\",\"axis\":\"x\",\"value\":{ix:F2},\"dur\":{dur:F2}}}"
                : $"{{\"cmd\":\"set\",\"axis\":\"y\",\"value\":{iy:F2},\"dur\":{dur:F2}}}";
        return payload;
    }

    // Relative move by delta degrees, using speed if provided.
    public string GetAdjustCommand(string axis, float dx, float dy)
    {
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
        return payload;
    }

    public string GetCenterCommand(float durationSeconds = -1f)
    {
        return GetSetCommand("xy", 0f, 0f, durationSeconds);
    }

    public string GetStopAllCommand()
    {
        return "{\"cmd\":\"stopAll\"}";
    }

    public string GetAbortQueueCommand()
    {
        return "{\"cmd\":\"qAbort\"}";
    }

    public string GetClearQueueCommand()
    {
        return "{\"cmd\":\"qClear\"}";
    }

    // Sweep helper: axis = "x", "y", or "xy". loops=0 means continuous.
    public string GetSweepCommand(string axis, float fromDeg, float toDeg, float durationSeconds, int loops = 2, float dwellSeconds = 0.1f)
    {
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
        return payload;
    }

    public string GetQueueStepModeCommand()
    {
        return "{\"cmd\":\"queue\",\"mode\":\"step\"}";
    }

    // Built-in demos from firmware.
    public string GetDemoCommand(int which)
    {
        if (which < 1 || which > 3) return null;
        return $"{{\"cmd\":\"demo{which}\"}}";
    }

    public string GetExamplesCommand()  { return "{\"cmd\":\"examples\"}"; }
    public string GetHelpCommand()      { return "{\"cmd\":\"help\"}"; }
    public string GetStatusCommand()    { return "{\"cmd\":\"status\"}"; }
    public string GetDemoListCommand()  { return "{\"cmd\":\"demo\"}"; }

    // Toggle inversion flags and notify firmware (expects invert command support on ESP side).
    public string GetToggleInvertXCommand()
    {
        invertX = !invertX;
        return $"{{\"cmd\":\"invert\",\"axis\":\"x\",\"state\":{invertX.ToString().ToLowerInvariant()}}}";
    }

    public string GetToggleInvertYCommand()
    {
        invertY = !invertY;
        return $"{{\"cmd\":\"invert\",\"axis\":\"y\",\"state\":{invertY.ToString().ToLowerInvariant()}}}";
    }
}
