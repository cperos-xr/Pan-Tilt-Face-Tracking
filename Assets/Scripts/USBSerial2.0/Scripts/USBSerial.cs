// Assets/UsbSerial.cs
using UnityEngine;
using System;

public static class UsbSerial
{
    static AndroidJavaClass pluginClass =
        new AndroidJavaClass("com.cperos.xr.serialplugin.SerialPlugin");
    static AndroidJavaObject currentActivity;

    static UsbSerial()
    {
        var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    }

    public static void Init()
    {
        pluginClass.CallStatic("init", currentActivity);
    }

    public static bool Open()
    {
        return pluginClass.CallStatic<bool>("open");
    }

    public static void Write(byte[] data)
    {
        // convert unsigned bytes to signed sbytes
        sbyte[] sdata = Array.ConvertAll(data, b => unchecked((sbyte)b));
        pluginClass.CallStatic("write", (object)sdata);
    }

    public static byte[] Read(int maxBytes)
    {
        // call Java, get sbyte[]
        sbyte[] sdata = pluginClass.CallStatic<sbyte[]>("read", maxBytes);
        if (sdata == null || sdata.Length == 0) return new byte[0];
        // convert back to unsigned bytes
        byte[] data = new byte[sdata.Length];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = unchecked((byte)sdata[i]);
        }
        return data;
    }

    public static void Close()
    {
        pluginClass.CallStatic("close");
    }
}
