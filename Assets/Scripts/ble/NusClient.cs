using System;
using System.Text;
using UnityEngine;
using UnityEngine.Android;

public class NusClient : MonoBehaviour
{
    public const string NUS_SERVICE = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E";
    public const string NUS_RX      = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
    public const string NUS_TX      = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

    public bool autoConnectFirst = true;
    public string nameContains = "ESP";
    public int requestMtu = 185;
    public int chunkBytes = 120;

    private bool ready;
    private readonly StringBuilder buf = new StringBuilder();

    void Awake()
    {
        // Tell plugin which GameObject receives UnitySendMessage callbacks
        BleGatt.SetUnityObject(gameObject.name);
    }

    public void Start()
    {
        EnsurePermissionsThenScan();
    }

    public void EnsurePermissionsThenScan()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int sdk = GetSdkInt();
        if (sdk >= 31) {
            Request("android.permission.BLUETOOTH_SCAN");
            Request("android.permission.BLUETOOTH_CONNECT");
        } else {
            Request(Permission.FineLocation);
        }
#endif
        // Scan filtered by NUS service UUID (if ESP32 advertises it)
        BleGatt.StartScan("{\"serviceUuids\":[\"" + NUS_SERVICE + "\"]}");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void Request(string perm)
    {
        if (!Permission.HasUserAuthorizedPermission(perm))
            Permission.RequestUserPermission(perm);
    }

    private int GetSdkInt()
    {
        using var v = new AndroidJavaClass("android.os.Build$VERSION");
        return v.GetStatic<int>("SDK_INT");
    }
#endif

    // ===== Callbacks from Android plugin =====

    public void OnScanStarted(string _) => Debug.Log("Scan started");
    public void OnScanStopped(string _) => Debug.Log("Scan stopped");

    // payload is JSON: {"address":"..","name":"..","rssi":..}
    public void OnScanResult(string payload)
    {
        string addr = Extract(payload, "address");
        string name = Extract(payload, "name") ?? "";

        if (string.IsNullOrEmpty(addr)) return;
        Debug.Log($"Found: {addr} {name}");

        if (autoConnectFirst && (string.IsNullOrEmpty(nameContains) || name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            BleGatt.StopScan();
            BleGatt.Connect(addr);
        }
    }

    public void OnConnected(string payload) => Debug.Log("Connected " + payload);

    // Services discovered: now enable notifications + MTU
    public void OnServices(string payload)
    {
        Debug.Log("Services discovered");
        BleGatt.SetNotify(NUS_SERVICE, NUS_TX, true);
        BleGatt.RequestMtu(requestMtu);
        ready = true;
    }

    public void OnNotify(string payload)
    {
        // {"service":"...","char":"...","value_b64":"..."}
        string b64 = Extract(payload, "value_b64");
        if (string.IsNullOrEmpty(b64)) return;

        byte[] bytes = Convert.FromBase64String(b64);
        string s = Encoding.UTF8.GetString(bytes);

        buf.Append(s);

        while (true)
        {
            var all = buf.ToString();
            int idx = all.IndexOf('\n');
            if (idx < 0) break;

            var line = all.Substring(0, idx).Trim();
            buf.Remove(0, idx + 1);

            if (line.Length > 0)
                Debug.Log("JSONL <= " + line);
        }
    }

    public void OnError(string payload) => Debug.LogError("BLE Error: " + payload);
    public void OnDisconnected(string payload) { ready = false; Debug.Log("Disconnected " + payload); }

    // ===== Send JSONL to ESP32 =====
    public void SendJsonLine(string json)
    {
        if (!ready) { Debug.LogWarning("Not ready yet"); return; }
        if (!json.EndsWith("\n")) json += "\n";

        var bytes = Encoding.UTF8.GetBytes(json);
        for (int i = 0; i < bytes.Length; i += chunkBytes)
        {
            int len = Math.Min(chunkBytes, bytes.Length - i);
            var part = new byte[len];
            Buffer.BlockCopy(bytes, i, part, 0, len);
            BleGatt.WriteBytes(NUS_SERVICE, NUS_RX, part, BleGatt.WRITE_TYPE_NO_RESP);
        }
    }

    // tiny JSON string extractor (good enough for these payloads)
    private static string Extract(string json, string key)
    {
        string needle = $"\"{key}\"";
        int k = json.IndexOf(needle, StringComparison.Ordinal);
        if (k < 0) return null;
        int colon = json.IndexOf(':', k);
        if (colon < 0) return null;

        int q1 = json.IndexOf('"', colon + 1);
        if (q1 < 0) return null;
        int q2 = json.IndexOf('"', q1 + 1);
        if (q2 < 0) return null;

        return json.Substring(q1 + 1, q2 - q1 - 1);
    }
}
