using UnityEngine;
using UnityEngine.Android;
using System;
using System.Text;
using System.Collections;

public class BleManager : MonoBehaviour
{
    AndroidJavaClass ble;

    public const string NUS_SERVICE = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    public const string NUS_RX      = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
    public const string NUS_TX      = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

    private bool connectedReady = false;
    public bool IsConnected => connectedReady;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ble = new AndroidJavaClass("com.cperos.xr.bleunityplugin.BleGattBridge");
        ble.CallStatic("setUnityObject", gameObject.name);
        StartCoroutine(RequestPermissions());
#endif
    }

    IEnumerator RequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int sdk = new AndroidJavaClass("android.os.Build$VERSION")
            .GetStatic<int>("SDK_INT");

        if (sdk >= 31)
        {
            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
                Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");

            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
                Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
        }
        else
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                Permission.RequestUserPermission(Permission.FineLocation);
        }

        yield return new WaitForSeconds(1.0f);
#else
        yield return null;
#endif
    }

    // ===== Public API =====

    public void StartScan()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ble.CallStatic("startScan", "{}");
#endif
    }

    public void StopScan()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ble.CallStatic("stopScan");
#endif
    }

    public void Connect(string address)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        connectedReady = false;
        ble.CallStatic("connect", address);
#endif
    }

    public void Disconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ble.CallStatic("disconnect");
        connectedReady = false;
#endif
    }

    public void SendJsonLine(string json)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!connectedReady) return;

        if (!json.EndsWith("\n")) json += "\n";

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        string b64 = Convert.ToBase64String(bytes);

        // writeType = 2 → NO_RESPONSE (best for NUS)
        ble.CallStatic("writeChar", NUS_SERVICE, NUS_RX, b64, 2);
#endif
    }

    // ===== Callbacks from Android =====

    public void OnScanStarted(string json)
    {
        Debug.Log("Scan started");
    }

    public void OnScanResult(string json)
    {
        Debug.Log("Scan result: " + json);
    }

    public void OnConnected(string json)
    {
        Debug.Log("Connected");
    }

    public void OnServices(string json)
    {
        Debug.Log("Services discovered");

#if UNITY_ANDROID && !UNITY_EDITOR
        ble.CallStatic("setNotify", NUS_SERVICE, NUS_TX, true);
        ble.CallStatic("requestMtu", 185);
#endif
        connectedReady = true;
    }

    public void OnNotify(string json)
    {
        Debug.Log("Notify: " + json);
    }

    public void OnWrite(string json)
    {
        Debug.Log("Write: " + json);
    }

    public void OnDisconnected(string json)
    {
        Debug.LogWarning("Disconnected");
        connectedReady = false;
    }

    public void OnError(string json)
    {
        Debug.LogError("BLE error: " + json);
    }
}
