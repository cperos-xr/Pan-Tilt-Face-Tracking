using System;
using UnityEngine;

public static class BleGatt
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static readonly AndroidJavaClass _cls =
        new AndroidJavaClass("com.cperos.xr.bleunityplugin.BleGattBridge");
#endif

    public const int WRITE_TYPE_DEFAULT = 1; // BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT
    public const int WRITE_TYPE_NO_RESP = 2; // BluetoothGattCharacteristic.WRITE_TYPE_NO_RESPONSE

    public static void SetUnityObject(string unityGameObjectName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("setUnityObject", unityGameObjectName);
#endif
    }

    public static void StartScan(string filtersJson)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("startScan", filtersJson);
#endif
    }

    public static void StopScan()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("stopScan");
#endif
    }

    public static void Connect(string address)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("connect", address);
#endif
    }

    public static void Disconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("disconnect");
#endif
    }

    public static void RequestMtu(int mtu)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("requestMtu", mtu);
#endif
    }

    public static void Discover()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("discover");
#endif
    }

    public static void SetNotify(string serviceUuid, string charUuid, bool enable)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _cls.CallStatic("setNotify", serviceUuid, charUuid, enable);
#endif
    }

    public static void WriteBytes(string serviceUuid, string charUuid, byte[] bytes, int writeType = WRITE_TYPE_NO_RESP)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string b64 = Convert.ToBase64String(bytes);
        _cls.CallStatic("writeChar", serviceUuid, charUuid, b64, writeType);
#endif
    }
}
