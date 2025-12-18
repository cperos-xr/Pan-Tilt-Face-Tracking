using UnityEngine;

// Keeps the device screen awake while this component is enabled.
public class KeepScreenOn : MonoBehaviour
{
    private int _previousTimeout;

    private void OnEnable()
    {
        _previousTimeout = Screen.sleepTimeout;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnDisable()
    {
        Screen.sleepTimeout = _previousTimeout;
    }
}
