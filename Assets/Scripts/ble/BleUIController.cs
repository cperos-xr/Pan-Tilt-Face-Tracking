using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BleUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Your BLE manager instance in the scene.")]
    public BleManager bleManager;

    [Header("UI")]
    public TMP_InputField addressInput;
    public TMP_Text statusText;

    [Tooltip("Optional: if assigned, we will wire it in code.")]
    public Button connectButton;
    public TMP_Text connectButtonText;

    [Tooltip("Optional: if assigned, we will wire it in code.")]
    public Button scanButton;

    [Tooltip("Optional: if assigned, we will wire it in code.")]
    public Button pingButton;

    [Header("Behavior")]
    [Tooltip("Seconds to wait for IsConnected to become true after Connect() is called.")]
    public float connectTimeoutSeconds = 12f;

    [Tooltip("How often we poll IsConnected to detect state transitions.")]
    public float pollIntervalSeconds = 0.2f;

    private Coroutine _connectWatchdog;
    private bool _lastConnected;
    private string _lastUiState = "";

    void Awake()
    {
        // Wire buttons in code so Inspector OnClick is not required.
        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(OnConnectDisconnectPressed);
            connectButton.onClick.AddListener(OnConnectDisconnectPressed);
        }

        if (scanButton != null)
        {
            scanButton.onClick.RemoveListener(OnScanPressed);
            scanButton.onClick.AddListener(OnScanPressed);
        }

        if (pingButton != null)
        {
            pingButton.onClick.RemoveListener(OnSendPingPressed);
            pingButton.onClick.AddListener(OnSendPingPressed);
        }
    }

    void Start()
    {
        if (statusText == null) Debug.LogError("BleUIController: statusText is NULL");
        if (addressInput == null) Debug.LogWarning("BleUIController: addressInput is NULL (connect will require address another way).");
        if (bleManager == null) Debug.LogError("BleUIController: bleManager is NULL");

        _lastConnected = (bleManager != null) && bleManager.IsConnected;

        SetStatus("Idle");
        RefreshUiFromConnectionState(force: true);

        // Start a lightweight poller to reflect connected/disconnected transitions.
        // (Not every frame; just a small interval.)
        InvokeRepeating(nameof(PollConnectionState), pollIntervalSeconds, pollIntervalSeconds);
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(PollConnectionState));
        StopConnectWatchdog();
    }

    // ----------------------------
    // UI button handlers
    // ----------------------------

    public void OnScanPressed()
    {
        if (!EnsureReady(out var err))
        {
            SetStatus(err);
            return;
        }

        SetStatus("Scanning…");
        SafeCall(() => bleManager.StartScan(), "StartScan");
        // If BleManager has callbacks/events, you should update UI there too.
    }

    public void OnConnectDisconnectPressed()
    {
        Debug.Log("BleUIController: Connect/Disconnect pressed");

        if (!EnsureReady(out var err))
        {
            SetStatus(err);
            return;
        }

        if (!bleManager.IsConnected)
        {
            var addr = (addressInput != null) ? addressInput.text.Trim() : "";

            if (string.IsNullOrWhiteSpace(addr))
            {
                SetStatus("Enter a BLE address (or select a scanned device).");
                return;
            }

            // Optional: basic sanity check (you can relax this if your BleManager accepts other formats).
            // Typical BLE MAC: 12 hex chars with ':' separators => "AA:BB:CC:DD:EE:FF"
            if (!LooksLikeMac(addr))
            {
                SetStatus("Address format looks wrong. Expected like AA:BB:CC:DD:EE:FF");
                return;
            }

            SetStatus("Connecting…");
            StopConnectWatchdog();
            SafeCall(() => bleManager.Connect(addr), $"Connect({addr})");
            _connectWatchdog = StartCoroutine(ConnectWatchdog(connectTimeoutSeconds));
        }
        else
        {
            SetStatus("Disconnecting…");
            StopConnectWatchdog();
            SafeCall(() => bleManager.Disconnect(), "Disconnect()");
        }

        RefreshUiFromConnectionState(force: true);
    }

    public void OnSendPingPressed()
    {
        if (!bleManager.IsConnected)
        {
            statusText.text = "Not connected";
            return;
        }

        statusText.text = "Sending sweep…";

        // Match the other app’s behavior
        //bleManager.SendJsonLine("{\"cmd\":\"queue\",\"mode\":\"step\"}");
        bleManager.SendJsonLine("{\"cmd\":\"sweep\",\"axis\":\"x\",\"from\":-45.00,\"to\":45.00,\"dur\":2.00,\"loops\":2,\"dwell\":0.10}");
    }

    // ----------------------------
    // Status + UI
    // ----------------------------

    private void SetStatus(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        if (_lastUiState == msg) return;

        _lastUiState = msg;

        if (statusText != null)
            statusText.text = msg;

        Debug.Log("BLE UI Status: " + msg);
    }

    private void RefreshUiFromConnectionState(bool force)
    {
        if (bleManager == null) return;

        bool connected = bleManager.IsConnected;

        if (connectButtonText != null)
            connectButtonText.text = connected ? "Disconnect" : "Connect";

        // Only override status on stable state transitions, not constantly.
        if (force)
        {
            if (connected) SetStatus("Connected.");
            else if (_lastUiState == "Connected.") SetStatus("Disconnected.");
        }
    }

    private void PollConnectionState()
    {
        if (bleManager == null) return;

        bool connected = bleManager.IsConnected;

        if (connected != _lastConnected)
        {
            _lastConnected = connected;
            StopConnectWatchdog();

            if (connected)
                SetStatus("Connected.");
            else
                SetStatus("Disconnected.");

            RefreshUiFromConnectionState(force: true);
        }
    }

    // ----------------------------
    // Connect watchdog (prevents "Idle forever")
    // ----------------------------

    private IEnumerator ConnectWatchdog(float timeout)
    {
        float t0 = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - t0 < timeout)
        {
            if (bleManager == null) yield break;
            if (bleManager.IsConnected)
            {
                SetStatus("Connected.");
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }

        // If we got here, connect didn't complete.
        // We don't know exact reason without BleManager error reporting, but we can still be explicit.
        SetStatus("Connect timed out. Check permissions, address, and that the device is advertising.");
    }

    private void StopConnectWatchdog()
    {
        if (_connectWatchdog != null)
        {
            StopCoroutine(_connectWatchdog);
            _connectWatchdog = null;
        }
    }

    // ----------------------------
    // Helpers
    // ----------------------------

    private bool EnsureReady(out string error)
    {
        if (bleManager == null)
        {
            error = "BLE Manager not assigned in scene.";
            return false;
        }

        if (statusText == null)
        {
            error = "Status text not assigned in scene.";
            return false;
        }

        error = null;
        return true;
    }

    private void SafeCall(Action action, string opName)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"BleUIController: {opName} threw: {ex}");
            SetStatus($"{opName} failed: {ex.GetType().Name}");
        }
    }

    private static bool LooksLikeMac(string s)
    {
        // Very small validator: "AA:BB:CC:DD:EE:FF"
        if (s.Length != 17) return false;

        for (int i = 0; i < s.Length; i++)
        {
            if (i % 3 == 2)
            {
                if (s[i] != ':') return false;
            }
            else
            {
                char c = s[i];
                bool hex = (c >= '0' && c <= '9') ||
                           (c >= 'a' && c <= 'f') ||
                           (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
        }
        return true;
    }
}
